using OpenCvSharp;
using OpenCvSharp.Extensions;
using OcvPoint = OpenCvSharp.Point;
using OcvSize = OpenCvSharp.Size;

namespace MagicSvg;

public static class LineProcessor
{
    public record Segment(int X1, int Y1, int X2, int Y2, int AngleDeg);

    public record Settings
    {
        public int    BinaryThreshold        { get; init; } = 200;
        public int    DilationKernelSize     { get; init; } = 3;
        public int    DilationIterations     { get; init; } = 1;
        public int    HoughThreshold         { get; init; } = 40;
        public int    MinLineLength          { get; init; } = 20;
        public int    MaxLineGap             { get; init; } = 15;
        /// <summary>Tolerancia en grados para clasificar un segmento en uno de los 8 ángulos.</summary>
        public double AngleSnapTolerance     { get; init; } = 10.0;
        public int    MergePositionTolerance { get; init; } = 30;
        public int    SegmentGapTolerance    { get; init; } = 35;
        public int    MinOutputSegmentLength { get; init; } = 30;
        public int    OutputLineThickness    { get; init; } = 2;
        public int    SnapTolerance          { get; init; } = 8;
        /// <summary>Área mínima en píxeles de un componente conectado. Los más pequeños se eliminan como artefactos.</summary>
        public int    MinComponentArea       { get; init; } = 500;
    }

    public record ProcessResult(
        Bitmap Binary,
        Bitmap Artifacts,
        Bitmap HoughRaw,
        Bitmap Classified,
        Bitmap Merged,
        Bitmap Final,
        List<Segment> Segments
    );

    static readonly int[] CandidateAngles = [0, 30, 45, 60, 90, 120, 135, 150];

    static readonly Dictionary<int, Scalar> AngleColors = new()
    {
        [0]   = new Scalar(255,   0,   0),   // Blue
        [30]  = new Scalar(  0, 200,   0),   // Green
        [45]  = new Scalar(  0, 165, 255),   // Orange
        [60]  = new Scalar(255, 200,   0),   // Cyan
        [90]  = new Scalar(  0,   0, 255),   // Red
        [120] = new Scalar(255,   0, 255),   // Magenta
        [135] = new Scalar(  0, 255, 255),   // Yellow
        [150] = new Scalar(180, 105, 255),   // Pink
    };

    // ── Geometry helpers ──────────────────────────────────────────────────────

    static double RawAngle(double dx, double dy)
    {
        double a = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        if (a < 0)     a += 180.0;
        if (a >= 180.0) a -= 180.0;
        return a;
    }

    static int? SnapAngle(double raw, double tol)
    {
        int best = -1;
        double bestDist = double.MaxValue;
        foreach (int c in CandidateAngles)
        {
            double diff = Math.Abs(raw - c);
            double d = Math.Min(diff, 180.0 - diff);
            if (d < bestDist) { bestDist = d; best = c; }
        }
        return bestDist <= tol ? best : null;
    }

    static (double dx, double dy) Dir(int angleDeg)
    {
        double rad = angleDeg * Math.PI / 180.0;
        return (Math.Cos(rad), Math.Sin(rad));
    }

    static Segment Rectify(LineSegmentPoint seg, int angleDeg)
    {
        double mx = (seg.P1.X + seg.P2.X) / 2.0;
        double my = (seg.P1.Y + seg.P2.Y) / 2.0;
        var (dx, dy) = Dir(angleDeg);

        double t1 = (seg.P1.X - mx) * dx + (seg.P1.Y - my) * dy;
        double t2 = (seg.P2.X - mx) * dx + (seg.P2.Y - my) * dy;
        if (t1 > t2) (t1, t2) = (t2, t1);

        return new Segment(
            (int)Math.Round(mx + t1 * dx), (int)Math.Round(my + t1 * dy),
            (int)Math.Round(mx + t2 * dx), (int)Math.Round(my + t2 * dy),
            angleDeg);
    }

    static double SegLen(Segment s)
    {
        double dx = s.X2 - s.X1, dy = s.Y2 - s.Y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    // Perpendicular distance from point (px,py) to the infinite line through s.
    static double PerpDist(Segment s, double px, double py)
    {
        double dx = s.X2 - s.X1, dy = s.Y2 - s.Y1;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-9)
            return Math.Sqrt((px - s.X1) * (px - s.X1) + (py - s.Y1) * (py - s.Y1));
        return Math.Abs((px - s.X1) * dy - (py - s.Y1) * dx) / len;
    }

    // Signed distance from s.P1 along s's unit direction to the projection of (px,py).
    static double ProjectOnto(Segment s, double px, double py)
    {
        double dx = s.X2 - s.X1, dy = s.Y2 - s.Y1;
        double len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-9) return 0;
        return ((px - s.X1) * dx + (py - s.Y1) * dy) / len;
    }

    // Intersection of the two infinite lines defined by a and b.
    static (double px, double py)? Intersect(Segment a, Segment b)
    {
        double dx1 = a.X2 - a.X1, dy1 = a.Y2 - a.Y1;
        double dx2 = b.X2 - b.X1, dy2 = b.Y2 - b.Y1;
        double denom = dx1 * dy2 - dy1 * dx2;
        if (Math.Abs(denom) < 1e-9) return null;
        double t = ((b.X1 - a.X1) * dy2 - (b.Y1 - a.Y1) * dx2) / denom;
        return (a.X1 + t * dx1, a.Y1 + t * dy1);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public static ProcessResult ProcessImageDetailed(Bitmap input, Settings? settings = null)
    {
        settings ??= new Settings();
        int thickness = settings.OutputLineThickness;
        var black   = new Scalar(0, 0, 0);
        var white   = new Scalar(255, 255, 255);
        var grayCol = new Scalar(200, 200, 200);

        using var src     = BitmapConverter.ToMat(input);
        using var grayMat = new Mat();
        using var binary  = new Mat();

        if (src.Channels() == 1) src.CopyTo(grayMat);
        else Cv2.CvtColor(src, grayMat, ColorConversionCodes.BGR2GRAY);

        Cv2.Threshold(grayMat, binary, settings.BinaryThreshold, 255, ThresholdTypes.BinaryInv);
        int ks = Math.Max(1, settings.DilationKernelSize) | 1;
        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OcvSize(ks, ks));
        if (settings.DilationIterations > 0)
            Cv2.Dilate(binary, binary, kernel, iterations: settings.DilationIterations);

        // Captura previa a la limpieza, para poder visualizar lo descartado
        using var preCleanup = binary.Clone();

        // Eliminar componentes conectados pequeños (artefactos aislados)
        if (settings.MinComponentArea > 0)
        {
            using var labels    = new Mat();
            using var stats     = new Mat();
            using var centroids = new Mat();
            using var mask      = new Mat();

            int numLabels = Cv2.ConnectedComponentsWithStats(
                binary, labels, stats, centroids,
                PixelConnectivity.Connectivity8, MatType.CV_32S);

            for (int label = 1; label < numLabels; label++) // 0 es el fondo
            {
                int area = stats.At<int>(label, (int)ConnectedComponentsTypes.Area);
                if (area < settings.MinComponentArea)
                {
                    Cv2.Compare(labels, new Scalar(label), mask, CmpTypes.EQ);
                    binary.SetTo(new Scalar(0), mask);
                }
            }
        }

        // Phase 1: binary display (sin eliminar artefactos, para ver la entrada real de la limpieza)
        using var binaryDisplay = new Mat();
        using var binaryBgr     = new Mat();
        Cv2.BitwiseNot(preCleanup, binaryDisplay);
        Cv2.CvtColor(binaryDisplay, binaryBgr, ColorConversionCodes.GRAY2BGR);
        Bitmap bmpBinary = BitmapConverter.ToBitmap(binaryBgr);

        // Phase 1b: artefactos eliminados (negro = conservado, rojo = descartado)
        using var removedMask = new Mat();
        Cv2.Subtract(preCleanup, binary, removedMask);

        using var artifactsMat = new Mat(src.Rows, src.Cols, MatType.CV_8UC3, white);
        artifactsMat.SetTo(black, binary);
        artifactsMat.SetTo(new Scalar(0, 0, 255), removedMask); // rojo (BGR)
        Bitmap bmpArtifacts = BitmapConverter.ToBitmap(artifactsMat);

        // Phase 2: Hough detection
        LineSegmentPoint[] houghSegs = Cv2.HoughLinesP(
            binary, rho: 1, theta: Math.PI / 180.0,
            threshold: settings.HoughThreshold,
            minLineLength: settings.MinLineLength,
            maxLineGap: settings.MaxLineGap);

        using var houghMat = new Mat(src.Rows, src.Cols, MatType.CV_8UC3, white);
        foreach (var seg in houghSegs)
            Cv2.Line(houghMat, seg.P1, seg.P2, black, thickness);
        Bitmap bmpHoughRaw = BitmapConverter.ToBitmap(houghMat);

        // Phase 3: classify + rectify
        using var classifiedMat = new Mat(src.Rows, src.Cols, MatType.CV_8UC3, white);
        var classified = new List<Segment>();

        foreach (var seg in houghSegs)
        {
            double dx  = seg.P2.X - seg.P1.X;
            double dy  = seg.P2.Y - seg.P1.Y;
            double raw = RawAngle(dx, dy);
            int? snapped = SnapAngle(raw, settings.AngleSnapTolerance);

            if (snapped is null)
            {
                Cv2.Line(classifiedMat, seg.P1, seg.P2, grayCol, 1);
            }
            else
            {
                var rect = Rectify(seg, snapped.Value);
                classified.Add(rect);
                Cv2.Line(classifiedMat,
                    new OcvPoint(rect.X1, rect.Y1),
                    new OcvPoint(rect.X2, rect.Y2),
                    AngleColors[snapped.Value], thickness);
            }
        }
        Bitmap bmpClassified = BitmapConverter.ToBitmap(classifiedMat);

        // Phase 4: merge parallel + filter by length
        var merged = MergeSegments(classified,
            settings.MergePositionTolerance, settings.SegmentGapTolerance);
        int minLen = settings.MinOutputSegmentLength;
        merged = [.. merged.Where(s => SegLen(s) >= minLen)];

        using var mergedMat = new Mat(src.Rows, src.Cols, MatType.CV_8UC3, white);
        foreach (var s in merged)
            Cv2.Line(mergedMat, new OcvPoint(s.X1, s.Y1), new OcvPoint(s.X2, s.Y2), black, thickness);
        Bitmap bmpMerged = BitmapConverter.ToBitmap(mergedMat);

        // Phase 5: extend to intersections (draw AFTER this, never before)
        var extended = ExtendToIntersections(merged, settings.SnapTolerance);

        using var output = new Mat(src.Rows, src.Cols, MatType.CV_8UC3, white);
        foreach (var s in extended)
            Cv2.Line(output, new OcvPoint(s.X1, s.Y1), new OcvPoint(s.X2, s.Y2), black, thickness);
        Bitmap bmpFinal = BitmapConverter.ToBitmap(output);

        return new ProcessResult(bmpBinary, bmpArtifacts, bmpHoughRaw, bmpClassified, bmpMerged, bmpFinal, extended);
    }

    public static Bitmap ProcessImage(Bitmap input, Settings? settings = null)
        => ProcessImageDetailed(input, settings).Final;

    // ── Merge parallel segments ───────────────────────────────────────────────

    static List<Segment> MergeSegments(List<Segment> segs, int posTol, int gapTol)
    {
        var result = new List<Segment>();

        foreach (var angleGroup in segs.GroupBy(s => s.AngleDeg))
        {
            int angleDeg = angleGroup.Key;
            var (ddx, ddy) = Dir(angleDeg);
            double nx = -ddy, ny = ddx; // perpendicular direction

            // For each segment: compute perpendicular coordinate and axis projection range
            var items = angleGroup
                .Select(s =>
                {
                    double mx   = (s.X1 + s.X2) / 2.0;
                    double my   = (s.Y1 + s.Y2) / 2.0;
                    double perp = mx * nx + my * ny;
                    double ta   = s.X1 * ddx + s.Y1 * ddy;
                    double tb   = s.X2 * ddx + s.Y2 * ddy;
                    return (seg: s, perp, t1: Math.Min(ta, tb), t2: Math.Max(ta, tb));
                })
                .OrderBy(x => x.perp)
                .ToList();

            // Chain-group by perpendicular coordinate within posTol
            var groups = new List<List<(Segment seg, double perp, double t1, double t2)>>();
            var current = new List<(Segment seg, double perp, double t1, double t2)> { items[0] };
            for (int i = 1; i < items.Count; i++)
            {
                if (items[i].perp - items[i - 1].perp <= posTol)
                    current.Add(items[i]);
                else
                {
                    groups.Add(current);
                    current = [items[i]];
                }
            }
            groups.Add(current);

            foreach (var g in groups)
            {
                // Weighted-average perpendicular position
                double totalLen = g.Sum(x => SegLen(x.seg));
                double avgPerp  = totalLen > 0
                    ? g.Sum(x => x.perp * SegLen(x.seg)) / totalLen
                    : g.Average(x => x.perp);

                // Merge 1-D ranges along the axis
                var ranges = g.Select(x =>
                    ((int)Math.Round(x.t1), (int)Math.Round(x.t2))).ToList();

                foreach (var (start, end) in MergeRanges(ranges, gapTol))
                {
                    result.Add(new Segment(
                        (int)Math.Round(start * ddx + avgPerp * nx),
                        (int)Math.Round(start * ddy + avgPerp * ny),
                        (int)Math.Round(end   * ddx + avgPerp * nx),
                        (int)Math.Round(end   * ddy + avgPerp * ny),
                        angleDeg));
                }
            }
        }

        return result;
    }

    // ── Merge 1-D ranges ─────────────────────────────────────────────────────

    static List<(int start, int end)> MergeRanges(List<(int start, int end)> ranges, int gap)
    {
        var result = new List<(int start, int end)>();
        if (ranges.Count == 0) return result;

        var sorted   = ranges.OrderBy(r => r.start).ToList();
        int curStart = sorted[0].start;
        int curEnd   = sorted[0].end;

        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].start <= curEnd + gap)
                curEnd = Math.Max(curEnd, sorted[i].end);
            else
            {
                result.Add((curStart, curEnd));
                curStart = sorted[i].start;
                curEnd   = sorted[i].end;
            }
        }
        result.Add((curStart, curEnd));
        return result;
    }

    // ── Extend / clip segments to intersections ───────────────────────────────

    static List<Segment> ExtendToIntersections(List<Segment> segs, int snapTol)
    {

        int cornerTol = snapTol * 10;
        var result = segs.ToList();

        for (int i = 0; i < result.Count; i++)
        {
            var s = result[i];
            System.Diagnostics.Debug.WriteLine(
                $"INPUT Seg[{i}] angle={s.AngleDeg} P1=({s.X1},{s.Y1}) P2=({s.X2},{s.Y2}) len={SegLen(s):F0}");
        }

        // True if endpoint (ex,ey) is not covered by any other segment in the list.
        bool IsEndFree(int selfIdx, double ex, double ey)
        {
            for (int k = 0; k < result.Count; k++)
            {
                if (k == selfIdx) continue;
                var other = result[k];
                double perp = PerpDist(other, ex, ey);
                if (perp > snapTol) continue;
                double t = ProjectOnto(other, ex, ey);
                double len = SegLen(other);
                if (t >= -snapTol && t <= len + snapTol) return false;
            }
            return true;
        }

        // ── Corner phase: extend free endpoints to nearby intersecting corners ──
        for (int i = 0; i < result.Count; i++)
        {
            foreach (bool isP2i in new[] { false, true })
            {
                var s  = result[i];
                double exi = isP2i ? s.X2 : s.X1;
                double eyi = isP2i ? s.Y2 : s.Y1;
                if (!IsEndFree(i, exi, eyi)) continue;

                double bestDist = double.MaxValue;
                int    bestJ    = -1;
                bool   bestP2j  = false;
                double bestPx   = 0, bestPy = 0;

                for (int j = 0; j < result.Count; j++)
                {
                    if (j == i) continue;
                    if (result[j].AngleDeg == result[i].AngleDeg) continue; // parallel

                    foreach (bool isP2j in new[] { false, true })
                    {
                        var sj  = result[j];
                        double exj = isP2j ? sj.X2 : sj.X1;
                        double eyj = isP2j ? sj.Y2 : sj.Y1;
                        if (!IsEndFree(j, exj, eyj)) continue;

                        var pt = Intersect(result[i], sj);
                        if (pt is null) continue;
                        var (px, py) = pt.Value;

                        double di = Math.Sqrt((px - exi) * (px - exi) + (py - eyi) * (py - eyi));
                        double dj = Math.Sqrt((px - exj) * (px - exj) + (py - eyj) * (py - eyj));
                        if (di > cornerTol || dj > cornerTol) continue;

                        double dist = di + dj;
                        if (dist < bestDist)
                        {
                            bestDist = dist; bestJ = j; bestP2j = isP2j;
                            bestPx = px; bestPy = py;
                        }
                    }
                }

                if (bestJ < 0) continue;

                int rpx = (int)Math.Round(bestPx), rpy = (int)Math.Round(bestPy);

                var newI = isP2i
                    ? result[i] with { X2 = rpx, Y2 = rpy }
                    : result[i] with { X1 = rpx, Y1 = rpy };

                var newJ = bestP2j
                    ? result[bestJ] with { X2 = rpx, Y2 = rpy }
                    : result[bestJ] with { X1 = rpx, Y1 = rpy };

                // Solo aplicar si ambos segmentos siguen teniendo longitud razonable
                if (SegLen(newI) >= snapTol && SegLen(newJ) >= snapTol)
                {
                    result[i] = newI;
                    result[bestJ] = newJ;
                }
            }
        }

        for (int i = 0; i < result.Count; i++)
        {
            var s = result[i];
            System.Diagnostics.Debug.WriteLine(
                $"AFTER CORNERS Seg[{i}] angle={s.AngleDeg} P1=({s.X1},{s.Y1}) P2=({s.X2},{s.Y2}) len={SegLen(s):F0}");
        }


        // ── Resolve phase: extend/clip each endpoint to nearest intersections ───
        // Snapshot so both endpoints of each segment are resolved against the same state.
        var snapshot = result.ToList();
        var resolved = new List<Segment>(snapshot.Count);

        for (int i = 0; i < snapshot.Count; i++)
        {
            var s  = snapshot[i];
            double dx  = s.X2 - s.X1, dy = s.Y2 - s.Y1;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-9) { resolved.Add(s); continue; }
            double ux = dx / len, uy = dy / len;
            double tLen = len; // t of P2 (P1 is at t = 0)

            // Collect all intersection t-values on this segment's axis (vs. other segments).
            var intersections = new List<double>();
            for (int j = 0; j < snapshot.Count; j++)
            {
                if (j == i) continue;
                var pt = Intersect(s, snapshot[j]);
                if (pt is null) continue;

                // Solo considerar si el punto está cerca del segmento j (con margen generoso)
                double tj = ProjectOnto(snapshot[j], pt.Value.px, pt.Value.py);
                double lenJ = SegLen(snapshot[j]);
                if (tj < -snapTol * 3 || tj > lenJ + snapTol * 3) continue;

                double t = (pt.Value.px - s.X1) * ux + (pt.Value.py - s.Y1) * uy;
                intersections.Add(t);
            }

            int nx1 = s.X1, ny1 = s.Y1;
            int nx2 = s.X2, ny2 = s.Y2;

            // Resolve P1 (a = 0): can extend toward negative t, clip toward positive t.
            {
                double a   = 0;
                double? b  = null; // nearest inside intersection to a
                double? c  = null; // nearest outside intersection to a

                foreach (double t in intersections)
                {
                    if (t >= 0 && t <= tLen)        // dentro del segmento
                    {
                        if (b is null || t < b.Value) b = t; // el más cercano a P1 = el menor
                    }
                    else if (t < 0)                  // más allá de P1
                    {
                        if (c is null || t > c.Value) c = t; // el más cercano = el mayor
                    }
                }

                System.Diagnostics.Debug.WriteLine(
        $"Resolve Seg[{i}] angle={s.AngleDeg} P1 a={a:F1} b={b:F1} c={c:F1} " +
        $"ts=[{string.Join(",", intersections.Select(t => t.ToString("F1")))}]");

                // ...cálculo de b y c...

                // Si b es null, el extremo no toca nada: solo extender si es realmente libre
                if (b is null && c is not null)
                {
                    bool free = !snapshot.Where((_, k) => k != i)
                        .Any(other => PerpDist(other, s.X1, s.Y1) <= snapTol &&
                                      ProjectOnto(other, s.X1, s.Y1) >= -snapTol &&
                                      ProjectOnto(other, s.X1, s.Y1) <= SegLen(other) + snapTol);
                    if (!free) c = null;
                }

                double tNew = ResolveEndpoint(a, b, c);
                nx1 = (int)Math.Round(s.X1 + tNew * ux);
                ny1 = (int)Math.Round(s.Y1 + tNew * uy);
            }

            // Resolve P2 (a = tLen): can extend toward positive t, clip toward negative t.
            {
                double a  = tLen;
                double? b = null;
                double? c = null;

                foreach (double t in intersections)
                {
                    if (t >= 0 && t <= tLen)         // dentro del segmento
                    {
                        if (b is null || t > b.Value) b = t; // el más cercano a P2 = el mayor
                    }
                    else if (t > tLen)               // más allá de P2
                    {
                        if (c is null || t < c.Value) c = t; // el más cercano = el menor
                    }
                }

                System.Diagnostics.Debug.WriteLine(
    $"Resolve Seg[{i}] angle={s.AngleDeg} P2 a={a:F1} b={b:F1} c={c:F1} " +
    $"ts=[{string.Join(",", intersections.Select(t => t.ToString("F1")))}]");

                // Si b es null, el extremo no toca nada: solo extender si es realmente libre
                if (b is null && c is not null)
                {
                    bool free = !snapshot.Where((_, k) => k != i)
                        .Any(other => PerpDist(other, s.X2, s.Y2) <= snapTol &&
                                      ProjectOnto(other, s.X2, s.Y2) >= -snapTol &&
                                      ProjectOnto(other, s.X2, s.Y2) <= SegLen(other) + snapTol);
                    if (!free) c = null;
                }

                double tNew = ResolveEndpoint(a, b, c);
                nx2 = (int)Math.Round(s.X1 + tNew * ux);
                ny2 = (int)Math.Round(s.Y1 + tNew * uy);
            }

            resolved.Add(new Segment(nx1, ny1, nx2, ny2, s.AngleDeg));
        }

        return [.. resolved.Where(s =>
        {
            double dx = s.X2 - s.X1, dy = s.Y2 - s.Y1;
            return Math.Sqrt(dx * dx + dy * dy) >= 1.0;
        })];
    }

    static double ResolveEndpoint(double a, double? b, double? c)
    {
        if (c is null) return b ?? a;
        if (b is null) return c.Value;
        return Math.Abs(c.Value - a) <= Math.Abs(b.Value - a) ? c.Value : b.Value;
    }
}
