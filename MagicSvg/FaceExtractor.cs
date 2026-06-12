using System;
using System.Collections.Generic;
using System.Text;

namespace MagicSvg
{
    public class FaceExtractor
    {
        public List<List<Point>> Extract(Dictionary<Point, HashSet<Point>> graph)
        {
            var visited = new HashSet<(Point, Point)>();
            var faces = new List<List<Point>>();

            // Iterar todas las half-edges
            foreach (var (u, neighbors) in graph)
                foreach (var v in neighbors)
                {
                    if (visited.Contains((u, v))) continue;

                    var face = TraceFace(u, v, graph, visited);
                    if (face is { Count: >= 3 })
                    {
                        double area = SignedArea(face);
                        // En SVG (Y↓), caras interiores tienen área < 0
                        if (area < 0)
                            faces.Add(face);
                    }
                }

            return faces;
        }

        static List<Point> TraceFace(
            Point start, Point second,
            Dictionary<Point, HashSet<Point>> graph,
            HashSet<(Point, Point)> visited)
        {
            var face = new List<Point>();
            var curU = start;
            var curV = second;

            for (int guard = 0; guard < 1000; guard++)
            {
                if (visited.Contains((curU, curV))) break;
                visited.Add((curU, curV));
                face.Add(curU);

                var next = NextHalfEdge(curU, curV, graph);
                if (next == null) break;

                curU = curV;
                curV = next.Value;

                if (curU == start && curV == second) break;
            }

            return face;
        }

        /// <summary>
        /// Dado que venimos de u→v, devuelve el siguiente nodo w tal que
        /// v→w es la arista con el giro más a la derecha (menor ángulo
        /// anti-horario desde la dirección invertida de llegada).
        /// </summary>
        static Point? NextHalfEdge(
            Point u, Point v,
            Dictionary<Point, HashSet<Point>> graph)
        {
            if (!graph.TryGetValue(v, out var neighbors)) return null;

            double arrivalAngle = Math.Atan2(v.Y - u.Y, v.X - u.X);
            double reverseAngle = arrivalAngle + Math.PI;

            Point? best = null;
            double bestAngle = double.MaxValue;

            foreach (var w in neighbors)
            {
                if (w == u) continue;

                double depAngle = Math.Atan2(w.Y - v.Y, w.X - v.X);
                double rel = depAngle - reverseAngle;

                // Normalizar a [0, 2π)
                rel = ((rel % (2 * Math.PI)) + 2 * Math.PI) % (2 * Math.PI);

                if (rel < bestAngle)
                {
                    bestAngle = rel;
                    best = w;
                }
            }

            return best;
        }

        /// <summary>Área con signo por la fórmula de Shoelace.</summary>
        static double SignedArea(List<Point> pts)
        {
            double area = 0;
            int n = pts.Count;
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                area += (double)pts[i].X * pts[j].Y;
                area -= (double)pts[j].X * pts[i].Y;
            }
            return area / 2.0;
        }
    }
}

