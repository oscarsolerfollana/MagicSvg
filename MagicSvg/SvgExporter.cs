using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MagicSvg;

/// <summary>
/// Builds an SVG where every minimal closed region (planar face) formed by the
/// processed line segments becomes a &lt;polygon&gt; element.
///
/// Algorithm:
///   1. Split every segment at its intersections with all other segments.
///   2. Snap nearby vertices together (snapTol pixels).
///   3. Build a planar graph (adjacency lists sorted by polar angle).
///   4. Trace faces with the half-edge "next-CCW-after-back" rule.
///   5. Keep only bounded faces (positive shoelace area in screen coords).
/// </summary>
public static class SvgExporter
{
    const double Eps = 1e-9;

    // ── Geometry helpers ────────────────────────────────────────────────────

    static double SegLen(LineProcessor.Segment s)
    {
        double dx = s.X2 - s.X1, dy = s.Y2 - s.Y1;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    // Returns (tA, tB, x, y) for the intersection of the infinite lines through A and B.
    // tA=0 → A.P1, tA=1 → A.P2.
    static (double tA, double tB, double x, double y)? LineIntersect(
        double ax1, double ay1, double ax2, double ay2,
        double bx1, double by1, double bx2, double by2)
    {
        double dax = ax2 - ax1, day = ay2 - ay1;
        double dbx = bx2 - bx1, dby = by2 - by1;
        double denom = dax * dby - day * dbx;
        if (Math.Abs(denom) < Eps) return null;
        double t = ((bx1 - ax1) * dby - (by1 - ay1) * dbx) / denom;
        double u = ((bx1 - ax1) * day - (by1 - ay1) * dax) / denom;
        return (t, u, ax1 + t * dax, ay1 + t * day);
    }

    // Angular distance from refAngle to angle going CCW, result in (0, 2π].
    static double CcwDiff(double angle, double refAngle)
    {
        double d = angle - refAngle;
        if (d <= -Math.PI) d += 2 * Math.PI;
        else if (d > Math.PI) d -= 2 * Math.PI;
        if (d <= Eps) d += 2 * Math.PI;
        return d;
    }

    // Shoelace signed area (positive = CW in screen coords = bounded face).
    static double SignedArea(List<int> face, double[] xs, double[] ys)
    {
        double area = 0;
        int n = face.Count;
        for (int i = 0; i < n; i++)
        {
            int a = face[i], b = face[(i + 1) % n];
            area += xs[a] * ys[b] - xs[b] * ys[a];
        }
        return area * 0.5;
    }

    // ── Vertex detection ────────────────────────────────────────────────────

    /// <summary>
    /// Splits every segment at its intersections with all other segments and
    /// returns the resulting (snapped) vertex coordinates plus, for each
    /// segment, the ordered chain of vertex indices it passes through.
    /// </summary>
    public static (double[] xs, double[] ys, List<List<int>> chains) ComputeVertices(
        IEnumerable<LineProcessor.Segment> rawSegments,
        double snapTol = 2.0)
    {
        var segs = rawSegments.ToList();

        // Step 1: collect split points on each segment.
        var splits = new List<(double t, double x, double y)>[segs.Count];
        for (int i = 0; i < segs.Count; i++)
            splits[i] = [(0.0, segs[i].X1, segs[i].Y1), (1.0, segs[i].X2, segs[i].Y2)];

        for (int i = 0; i < segs.Count; i++)
        {
            var si   = segs[i];
            double tI = snapTol / Math.Max(1.0, SegLen(si));

            for (int j = i + 1; j < segs.Count; j++)
            {
                var sj   = segs[j];
                double tJ = snapTol / Math.Max(1.0, SegLen(sj));

                var pt = LineIntersect(
                    si.X1, si.Y1, si.X2, si.Y2,
                    sj.X1, sj.Y1, sj.X2, sj.Y2);
                if (pt is null) continue;
                var (tA, tB, ix, iy) = pt.Value;

                if (tA < -tI || tA > 1 + tI) continue;
                if (tB < -tJ || tB > 1 + tJ) continue;

                splits[i].Add((Math.Clamp(tA, 0.0, 1.0), ix, iy));
                splits[j].Add((Math.Clamp(tB, 0.0, 1.0), ix, iy));
            }
        }

        // Step 2: build vertex list with snapping.
        var vxs   = new List<double>();
        var vys   = new List<double>();
        double sq = snapTol * snapTol;

        int AddVert(double x, double y)
        {
            for (int k = 0; k < vxs.Count; k++)
            {
                double dx = vxs[k] - x, dy = vys[k] - y;
                if (dx * dx + dy * dy <= sq) return k;
            }
            vxs.Add(x); vys.Add(y);
            return vxs.Count - 1;
        }

        var chains = new List<List<int>>(segs.Count);
        for (int i = 0; i < segs.Count; i++)
            chains.Add([.. splits[i].OrderBy(p => p.t).Select(p => AddVert(p.x, p.y))]);

        return ([.. vxs], [.. vys], chains);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public static string BuildSvg(
        IEnumerable<LineProcessor.Segment> rawSegments,
        int width, int height,
        double snapTol = 2.0)
    {
        var segs = rawSegments.ToList();
        if (segs.Count == 0) return EmptySvg(width, height);

        var (xs, ys, chains) = ComputeVertices(segs, snapTol);

        // Build adjacency from sub-segments.
        var edgeSet = new HashSet<(int, int)>();
        var adj     = new Dictionary<int, List<int>>();

        for (int i = 0; i < segs.Count; i++)
        {
            var chain = chains[i];
            for (int k = 0; k < chain.Count - 1; k++)
            {
                int a = chain[k], b = chain[k + 1];
                if (a == b) continue;
                var key = a < b ? (a, b) : (b, a);
                if (!edgeSet.Add(key)) continue;
                if (!adj.ContainsKey(a)) adj[a] = [];
                if (!adj.ContainsKey(b)) adj[b] = [];
                adj[a].Add(b);
                adj[b].Add(a);
            }
        }

        if (edgeSet.Count == 0) return EmptySvg(width, height);

        // Sort adjacency lists by polar angle around each vertex.
        foreach (var (v, nbrs) in adj)
            nbrs.Sort((a, b) =>
                Math.Atan2(ys[a] - ys[v], xs[a] - xs[v])
                    .CompareTo(Math.Atan2(ys[b] - ys[v], xs[b] - xs[v])));

        // Trace planar faces (half-edge traversal).
        // For each directed half-edge (u→v), the next half-edge in the face is
        // the outgoing edge from v that comes first CCW after the direction (v→u).
        var visited = new HashSet<(int, int)>();
        var faces   = new List<List<int>>();

        foreach (var (u0, nbrs0) in adj)
        foreach (int v0 in nbrs0)
        {
            if (visited.Contains((u0, v0))) continue;

            var face  = new List<int>();
            int u = u0, v = v0;
            int guard = edgeSet.Count * edgeSet.Count + 100;

            while (!visited.Contains((u, v)) && guard-- > 0)
            {
                visited.Add((u, v));
                face.Add(u);

                double back = Math.Atan2(ys[u] - ys[v], xs[u] - xs[v]);
                int    nw   = -1;
                double best = double.MaxValue;

                foreach (int w in adj[v])
                {
                    double d = CcwDiff(Math.Atan2(ys[w] - ys[v], xs[w] - xs[v]), back);
                    if (d < best) { best = d; nw = w; }
                }

                if (nw < 0) break;
                (u, v) = (v, nw);
            }

            if (face.Count >= 3 && adj.ContainsKey(u) && adj[u].Contains(face[0])) faces.Add(face);
        }

        // Keep bounded faces only.
        // Positive shoelace area ↔ clockwise winding in screen coords ↔ bounded face.
        faces = faces.Where(f => SignedArea(f, xs, ys) > 1.0).ToList();

        System.Diagnostics.Debug.WriteLine($"Vertices: {xs.Length}");
        System.Diagnostics.Debug.WriteLine($"Edges: {edgeSet.Count}");
        System.Diagnostics.Debug.WriteLine($"Faces traced: {faces.Count}");
        foreach (var f in faces)
            System.Diagnostics.Debug.WriteLine($"  Face area={SignedArea(f, xs, ys):F0} verts={f.Count}");

        return RenderSvg(width, height, xs, ys, faces);
    }

    // ── SVG rendering ────────────────────────────────────────────────────────

    static string EmptySvg(int w, int h) =>
        $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{w}\" height=\"{h}\" " +
        $"viewBox=\"0 0 {w} {h}\"></svg>";

    static string RenderSvg(int w, int h, double[] xs, double[] ys, List<List<int>> faces)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{w}\" height=\"{h}\" " +
            $"viewBox=\"0 0 {w} {h}\">");

        foreach (var face in faces)
        {
            sb.Append("  <polygon points=\"");
            for (int i = 0; i < face.Count; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(FormattableString.Invariant(
                    $"{xs[face[i]]:F1},{ys[face[i]]:F1}"));
            }
            sb.AppendLine("\" fill=\"none\" stroke=\"black\" stroke-width=\"1\"/>");
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    // ── Line-based SVG export ───────────────────────────────────────────────

    /// <summary>
    /// Returns an SVG string with one &lt;line&gt; element per segment.
    /// </summary>
    public static string Export(List<LineProcessor.Segment> segments, int width, int height)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" " +
            $"viewBox=\"0 0 {width} {height}\">");
        sb.AppendLine("  <rect width=\"100%\" height=\"100%\" fill=\"white\"/>");

        foreach (var s in segments)
            sb.AppendLine(
                $"  <line x1=\"{s.X1}\" y1=\"{s.Y1}\" x2=\"{s.X2}\" y2=\"{s.Y2}\" " +
                $"stroke=\"black\" stroke-width=\"1\" stroke-linecap=\"round\"/>");

        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>
    /// Calls <see cref="Export"/> and writes the result to <paramref name="path"/>.
    /// </summary>
    public static void SaveToFile(
        List<LineProcessor.Segment> segments, int width, int height, string path)
        => File.WriteAllText(path, Export(segments, width, height), System.Text.Encoding.UTF8);

    // ── Polygon SVG export (GraphBuilder + FaceExtractor) ───────────────────────────

    /// <summary>
    /// Convierte los segmentos en las caras mínimas (polígonos) del plano usando
    /// <see cref="GraphBuilder"/> para construir el grafo planar y
    /// <see cref="FaceExtractor"/> para extraer las caras.
    /// </summary>
    public static List<List<Point>> ExtractFaces(List<LineProcessor.Segment> segments)
    {
        var segs = segments
            .Select(s => new Segment(new Point(s.X1, s.Y1), new Point(s.X2, s.Y2)))
            .ToList();

        var graph = new GraphBuilder().Build(segs);
        return new FaceExtractor().Extract(graph);
    }

    /// <summary>
    /// Genera el SVG de polígonos a partir de las caras extraídas. Si se proporciona
    /// <paramref name="roomNames"/>, cada polígono cuyo índice tenga un nombre asociado
    /// recibe el atributo <c>data-room</c> con ese nombre.
    /// </summary>
    public static string RenderPolygonsSvg(
        List<List<Point>> faces, int width, int height,
        IReadOnlyDictionary<int, string>? roomNames = null,
        IReadOnlyList<SvgTextItem>? texts = null)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{width}\" height=\"{height}\" " +
            $"viewBox=\"0 0 {width} {height}\">");
        sb.AppendLine("  <rect width=\"100%\" height=\"100%\" fill=\"white\"/>");

        for (int f = 0; f < faces.Count; f++)
        {
            var face = faces[f];
            sb.Append("  <polygon points=\"");
            for (int i = 0; i < face.Count; i++)
            {
                if (i > 0) sb.Append(' ');
                sb.Append(FormattableString.Invariant($"{face[i].X},{face[i].Y}"));
            }
            sb.Append('"');

            if (roomNames != null && roomNames.TryGetValue(f, out var room) && !string.IsNullOrWhiteSpace(room))
                sb.Append($" data-room=\"{System.Security.SecurityElement.Escape(room)}\"");

            sb.AppendLine(" fill=\"none\" stroke=\"black\" stroke-width=\"1\"/>");
        }

        if (texts != null)
        {
            foreach (var t in texts)
            {
                if (string.IsNullOrWhiteSpace(t.Text)) continue;
                string coords = FormattableString.Invariant(
                    $"x=\"{t.X:F1}\" y=\"{t.Y:F1}\" font-size=\"{t.FontSize:F0}\"");
                sb.AppendLine(
                    $"  <text {coords} font-family=\"sans-serif\" " +
                    $"text-anchor=\"middle\" dominant-baseline=\"middle\">" +
                    $"{System.Security.SecurityElement.Escape(t.Text)}</text>");
            }
        }

        sb.Append("</svg>");
        return sb.ToString();
    }

    /// <summary>
    /// Convierte los segmentos en un SVG de polígonos usando
    /// <see cref="GraphBuilder"/> para construir el grafo planar y
    /// <see cref="FaceExtractor"/> para extraer las caras mínimas.
    /// </summary>
    public static string ExportPolygons(List<LineProcessor.Segment> segments, int width, int height)
        => RenderPolygonsSvg(ExtractFaces(segments), width, height);
}
