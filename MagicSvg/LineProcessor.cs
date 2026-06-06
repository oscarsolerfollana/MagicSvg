using OpenCvSharp;
using OpenCvSharp.Extensions;
using OcvPoint = OpenCvSharp.Point;
using OcvSize = OpenCvSharp.Size;

namespace MagicSvg;

public static class LineProcessor
{
    public record Settings
    {
        public int    BinaryThreshold        { get; init; } = 200;
        public int    DilationKernelSize     { get; init; } = 3;
        public int    DilationIterations     { get; init; } = 1;
        public int    HoughThreshold         { get; init; } = 40;
        public int    MinLineLength          { get; init; } = 20;
        public int    MaxLineGap             { get; init; } = 15;
        public double AngleToleranceDeg      { get; init; } = 28;
        public int    MergePositionTolerance { get; init; } = 30;
        public int    SegmentGapTolerance    { get; init; } = 35;
        public int    MinOutputSegmentLength { get; init; } = 30;
        public int    OutputLineThickness    { get; init; } = 2;
        public int    SnapTolerance          { get; init; } = 8;
    }

    /// <summary>Imágenes intermedias de cada fase del algoritmo.</summary>
    public record ProcessResult(
        Bitmap Binary,      // umbral + dilatación (líneas negras sobre blanco)
        Bitmap HoughRaw,    // todos los segmentos detectados por Hough
        Bitmap Classified,  // H=azul, V=rojo, diagonales=gris (descartadas)
        Bitmap Merged,      // tras fusión de paralelas y filtro de longitud
        Bitmap Final        // con extensión hasta intersecciones
    );

    public static ProcessResult ProcessImageDetailed(Bitmap input, Settings? settings = null)
    {
        settings ??= new Settings();
        int thickness = settings.OutputLineThickness;
        var black = new Scalar(0, 0, 0);
        var white = new Scalar(255, 255, 255);
        var blue = new Scalar(255, 0, 0);
        var red = new Scalar(0, 0, 255);
        var grayCol = new Scalar(200, 200, 200);

        using var src = BitmapConverter.ToMat(input);
        using var grayMat = new Mat();
        using var binary = new Mat();

        if (src.Channels() == 1) src.CopyTo(grayMat);
        else Cv2.CvtColor(src, grayMat, ColorConversionCodes.BGR2GRAY);

        Cv2.Threshold(grayMat, binary, settings.BinaryThreshold, 255, ThresholdTypes.BinaryInv);
        int ks = Math.Max(1, settings.DilationKernelSize) | 1; // garantizar impar ≥ 1
        using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new OcvSize(ks, ks));
        if (settings.DilationIterations > 0)
            Cv2.Dilate(binary, binary, kernel, iterations: settings.DilationIterations);

        // ── Fase 1: Binario ────────────────────────────────────────────────
        using var binaryDisplay = new Mat();
        using var binaryBgr = new Mat();
        Cv2.BitwiseNot(binary, binaryDisplay);
        Cv2.CvtColor(binaryDisplay, binaryBgr, ColorConversionCodes.GRAY2BGR);
        Bitmap bmpBinary = BitmapConverter.ToBitmap(binaryBgr);

        // ── Detección Hough ────────────────────────────────────────────────
        LineSegmentPoint[] segments = Cv2.HoughLinesP(
            binary,
            rho: 1,
            theta: Math.PI / 180.0,
            threshold: settings.HoughThreshold,
            minLineLength: settings.MinLineLength,
            maxLineGap: settings.MaxLineGap);

        // ── Fase 2: Hough crudo ────────────────────────────────────────────
        using var houghMat = new Mat(src.Rows, src.Cols, MatType.CV_8UC3, white);
        foreach (var seg in segments)
            Cv2.Line(houghMat, seg.P1, seg.P2, black, thickness);
        Bitmap bmpHoughRaw = BitmapConverter.ToBitmap(houghMat);

        // ── Clasificación ──────────────────────────────────────────────────
        double aTol = settings.AngleToleranceDeg;
        var hSegs = new List<(int x1, int y, int x2)>();
        var vSegs = new List<(int x, int y1, int y2)>();

        using var classifiedMat = new Mat(src.Rows, src.Cols, MatType.CV_8UC3, white);

        foreach (var seg in segments)
        {
            double dx = seg.P2.X - seg.P1.X;
            double dy = seg.P2.Y - seg.P1.Y;
            double angle = Math.Abs(Math.Atan2(dy, dx) * 180.0 / Math.PI);

            if (angle < aTol || angle > 180.0 - aTol)
            {
                int y = (seg.P1.Y + seg.P2.Y) / 2;
                int x1 = Math.Min(seg.P1.X, seg.P2.X);
                int x2 = Math.Max(seg.P1.X, seg.P2.X);
                hSegs.Add((x1, y, x2));
                Cv2.Line(classifiedMat, new OcvPoint(x1, y), new OcvPoint(x2, y), blue, thickness);
            }
            else if (angle > 90.0 - aTol && angle < 90.0 + aTol)
            {
                int x = (seg.P1.X + seg.P2.X) / 2;
                int y1 = Math.Min(seg.P1.Y, seg.P2.Y);
                int y2 = Math.Max(seg.P1.Y, seg.P2.Y);
                vSegs.Add((x, y1, y2));
                Cv2.Line(classifiedMat, new OcvPoint(x, y1), new OcvPoint(x, y2), red, thickness);
            }
            else
            {
                Cv2.Line(classifiedMat, seg.P1, seg.P2, grayCol, 1);
            }
        }

        Bitmap bmpClassified = BitmapConverter.ToBitmap(classifiedMat);

        // ── Fusión y filtro ────────────────────────────────────────────────
        var mergedH = MergeHLines(hSegs, settings.MergePositionTolerance, settings.SegmentGapTolerance);
        var mergedV = MergeVLines(vSegs, settings.MergePositionTolerance, settings.SegmentGapTolerance);
        int minLen = settings.MinOutputSegmentLength;
        mergedH = mergedH.Where(s => s.x2 - s.x1 >= minLen).ToList();
        mergedV = mergedV.Where(s => s.y2 - s.y1 >= minLen).ToList();

        // ── Fase 4: Fusionados ─────────────────────────────────────────────
        using var mergedMat = new Mat(src.Rows, src.Cols, MatType.CV_8UC3, white);
        foreach (var (x1, y, x2) in mergedH)
            Cv2.Line(mergedMat, new OcvPoint(x1, y), new OcvPoint(x2, y), black, thickness);
        foreach (var (x, y1, y2) in mergedV)
            Cv2.Line(mergedMat, new OcvPoint(x, y1), new OcvPoint(x, y2), black, thickness);
        Bitmap bmpMerged = BitmapConverter.ToBitmap(mergedMat);

        // ── Fase 5: Extensión ─────────────────────────────────────────────
        (mergedH, mergedV) = ExtendToIntersections(mergedH, mergedV, settings.SnapTolerance);

        using var output = new Mat(src.Rows, src.Cols, MatType.CV_8UC3, white);
        foreach (var (x1, y, x2) in mergedH)
            Cv2.Line(output, new OcvPoint(x1, y), new OcvPoint(x2, y), black, thickness);
        foreach (var (x, y1, y2) in mergedV)
            Cv2.Line(output, new OcvPoint(x, y1), new OcvPoint(x, y2), black, thickness);

        Bitmap bmpFinal = BitmapConverter.ToBitmap(output);

        return new ProcessResult(bmpBinary, bmpHoughRaw, bmpClassified, bmpMerged, bmpFinal);
    }

    public static Bitmap ProcessImage(Bitmap input, Settings? settings = null)
        => ProcessImageDetailed(input, settings).Final;

    // ── Fusión de horizontales ────────────────────────────────────────────────

    private static List<(int x1, int y, int x2)> MergeHLines(
        List<(int x1, int y, int x2)> segs, int yTol, int gapTol)
    {
        if (segs.Count == 0) return [];

        var sorted = segs.OrderBy(s => s.y).ToList();
        var groups = new List<List<(int x1, int y, int x2)>>();
        var current = new List<(int x1, int y, int x2)> { sorted[0] };

        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].y - sorted[i - 1].y <= yTol)
                current.Add(sorted[i]);
            else
            {
                groups.Add(current);
                current = [sorted[i]];
            }
        }
        groups.Add(current);

        var result = new List<(int x1, int y, int x2)>();
        foreach (var g in groups)
        {
            int avgY = (int)Math.Round(g.Average(s => s.y));
            foreach (var (start, end) in MergeRanges(g.Select(s => (s.x1, s.x2)).ToList(), gapTol))
                result.Add((start, avgY, end));
        }
        return result;
    }

    // ── Fusión de verticales ──────────────────────────────────────────────────

    private static List<(int x, int y1, int y2)> MergeVLines(
        List<(int x, int y1, int y2)> segs, int xTol, int gapTol)
    {
        if (segs.Count == 0) return [];

        var sorted = segs.OrderBy(s => s.x).ToList();
        var groups = new List<List<(int x, int y1, int y2)>>();
        var current = new List<(int x, int y1, int y2)> { sorted[0] };

        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].x - sorted[i - 1].x <= xTol)
                current.Add(sorted[i]);
            else
            {
                groups.Add(current);
                current = [sorted[i]];
            }
        }
        groups.Add(current);

        var result = new List<(int x, int y1, int y2)>();
        foreach (var g in groups)
        {
            int avgX = (int)Math.Round(g.Average(s => s.x));
            foreach (var (start, end) in MergeRanges(g.Select(s => (s.y1, s.y2)).ToList(), gapTol))
                result.Add((avgX, start, end));
        }
        return result;
    }

    // ── Utilidad: fusionar rangos 1-D solapados o próximos ───────────────────

    private static List<(int start, int end)> MergeRanges(
        List<(int start, int end)> ranges, int gap)
    {
        var result = new List<(int start, int end)>();
        if (ranges.Count == 0) return result;

        var sorted = ranges.OrderBy(r => r.start).ToList();
        int curStart = sorted[0].start;
        int curEnd = sorted[0].end;

        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i].start <= curEnd + gap)
                curEnd = Math.Max(curEnd, sorted[i].end);
            else
            {
                result.Add((curStart, curEnd));
                curStart = sorted[i].start;
                curEnd = sorted[i].end;
            }
        }
        result.Add((curStart, curEnd));
        return result;
    }

    // ── Extensión hasta intersecciones ─────────────────────────────────────────

    private static (List<(int x1, int y, int x2)>, List<(int x, int y1, int y2)>)
    ExtendToIntersections(
        List<(int x1, int y, int x2)> hSegs,
        List<(int x, int y1, int y2)> vSegs,
        int snapTol)
    {
        bool VSpansY((int x, int y1, int y2) v, int y) =>
            v.y1 - snapTol <= y && y <= v.y2 + snapTol;

        bool HSpansX((int x1, int y, int x2) h, int x) =>
            h.x1 - snapTol <= x && x <= h.x2 + snapTol;

        // ── Esquinas entre extremos libres próximos ───────────────────────────
        int cornerTol = snapTol * 10;

        bool HEndFree(int ex, int y) =>
            !vSegs.Any(v => Math.Abs(v.x - ex) <= snapTol && VSpansY(v, y));

        bool VEndFree(int ey, int x) =>
            !hSegs.Any(h => Math.Abs(h.y - ey) <= snapTol && HSpansX(h, x));

        var newH = hSegs.ToList();
        var newV = vSegs.ToList();

        for (int hi = 0; hi < newH.Count; hi++)
        {
            var h = newH[hi];

            foreach (bool rightSide in new[] { false, true })
            {
                int hEx = rightSide ? h.x2 : h.x1;
                if (!HEndFree(hEx, h.y)) continue;

                int bestDist = int.MaxValue;
                int bestVi = -1;
                bool bestBottom = false;

                for (int vi = 0; vi < newV.Count; vi++)
                {
                    var v = newV[vi];

                    foreach (bool bottomSide in new[] { false, true })
                    {
                        int vEy = bottomSide ? v.y2 : v.y1;
                        if (!VEndFree(vEy, v.x)) continue;

                        int dx = Math.Abs(v.x - hEx);
                        int dy = Math.Abs(vEy - h.y);
                        int dist = dx + dy;

                        if (dist < bestDist && dx <= cornerTol && dy <= cornerTol)
                        {
                            bestDist = dist;
                            bestVi = vi;
                            bestBottom = bottomSide;
                        }
                    }
                }

                if (bestVi < 0) continue;

                var bv = newV[bestVi];

                newH[hi] = rightSide
                    ? (x1: h.x1, y: h.y, x2: bv.x)
                    : (x1: bv.x, y: h.y, x2: h.x2);

                newV[bestVi] = bestBottom
                    ? (x: bv.x, y1: bv.y1, y2: h.y)
                    : (x: bv.x, y1: h.y, y2: bv.y2);

                h = newH[hi];
            }
        }

        // ── Paso 1: horizontales → verticales ────────────────────────────────
        int ResolveH(int a, int segLo, int segHi, int y, bool rightSide)
        {
            int? b = rightSide
                ? newV.Where(v => v.x >= segLo - snapTol && v.x <= a + snapTol && VSpansY(v, y))
                      .Select(v => (int?)v.x).Max()
                : newV.Where(v => v.x >= a - snapTol && v.x <= segHi + snapTol && VSpansY(v, y))
                      .Select(v => (int?)v.x).Min();

            int? c = rightSide
                ? newV.Where(v => v.x > a + snapTol && VSpansY(v, y))
                      .Select(v => (int?)v.x).Min()
                : newV.Where(v => v.x < a - snapTol && VSpansY(v, y))
                      .Select(v => (int?)v.x).Max();

            if (c is null) return b ?? a;
            if (b is null) return c.Value;

            int distBA = Math.Abs(a - b.Value);
            int distAC = Math.Abs(c.Value - a);
            return distAC <= distBA ? c.Value : b.Value;
        }

        newH = newH.Select(h =>
        {
            int nx1 = ResolveH(h.x1, h.x1, h.x2, h.y, rightSide: false);
            int nx2 = ResolveH(h.x2, h.x1, h.x2, h.y, rightSide: true);
            return (x1: Math.Min(nx1, nx2), y: h.y, x2: Math.Max(nx1, nx2));
        }).Where(h => h.x1 < h.x2).ToList();

        // ── Paso 2: verticales → horizontales ya resueltas ───────────────────
        int ResolveV(int a, int segLo, int segHi, int x, bool bottomSide,
                     List<(int x1, int y, int x2)> hRef)
        {
            int? b = bottomSide
                ? hRef.Where(h => h.y >= segLo - snapTol && h.y <= a + snapTol && HSpansX(h, x))
                      .Select(h => (int?)h.y).Max()
                : hRef.Where(h => h.y >= a - snapTol && h.y <= segHi + snapTol && HSpansX(h, x))
                      .Select(h => (int?)h.y).Min();

            int? c = bottomSide
                ? hRef.Where(h => h.y > a + snapTol && HSpansX(h, x))
                      .Select(h => (int?)h.y).Min()
                : hRef.Where(h => h.y < a - snapTol && HSpansX(h, x))
                      .Select(h => (int?)h.y).Max();

            if (c is null) return b ?? a;
            if (b is null) return c.Value;

            int distBA = Math.Abs(a - b.Value);
            int distAC = Math.Abs(c.Value - a);
            return distAC <= distBA ? c.Value : b.Value;
        }

        newV = newV.Select(v =>
        {
            int ny1 = ResolveV(v.y1, v.y1, v.y2, v.x, bottomSide: false, newH);
            int ny2 = ResolveV(v.y2, v.y1, v.y2, v.x, bottomSide: true, newH);
            return (x: v.x, y1: Math.Min(ny1, ny2), y2: Math.Max(ny1, ny2));
        }).Where(v => v.y1 < v.y2).ToList();

        return (newH, newV);
    }
}