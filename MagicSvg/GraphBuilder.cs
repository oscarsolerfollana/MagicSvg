using System;
using System.Collections.Generic;
using System.Text;

namespace MagicSvg
{
    public class GraphBuilder
    {
        /// <summary>
        /// Devuelve el grafo como diccionario Point → conjunto de vecinos directos.
        /// Cada arista aparece en ambas direcciones.
        /// </summary>
        public Dictionary<Point, HashSet<Point>> Build(IReadOnlyList<Segment> segments)
        {
            var graph = new Dictionary<Point, HashSet<Point>>();

            foreach (var sorted in BuildChains(segments))
            {
                // Añadir cada sub-segmento al grafo
                for (int i = 0; i < sorted.Count - 1; i++)
                {
                    var a = sorted[i];
                    var b = sorted[i + 1];
                    if (a == b) continue;

                    if (!graph.ContainsKey(a)) graph[a] = [];
                    if (!graph.ContainsKey(b)) graph[b] = [];
                    graph[a].Add(b);
                    graph[b].Add(a);
                }
            }

            return graph;
        }

        /// <summary>
        /// Para cada segmento, calcula todos los puntos de corte con los demás y los
        /// devuelve ordenados a lo largo del segmento, formando la cadena de
        /// sub-segmentos mínimos en que queda dividido.
        /// </summary>
        public List<List<Point>> BuildChains(IReadOnlyList<Segment> segments)
        {
            var chains = new List<List<Point>>();

            foreach (var seg in segments)
            {
                var cuts = FindCutPoints(seg, segments);

                var sorted = seg.IsHorizontal
                    ? cuts.OrderBy(p => p.X).ToList()
                    : cuts.OrderBy(p => p.Y).ToList();

                chains.Add(sorted);
            }

            return chains;
        }

        /// <summary>
        /// Devuelve todos los puntos donde <paramref name="seg"/> se intersecta con
        /// otros segmentos de la lista, incluyendo sus propios extremos.
        /// </summary>
        private HashSet<Point> FindCutPoints(Segment seg, IReadOnlyList<Segment> all)
        {
            var cuts = new HashSet<Point> { seg.A, seg.B };

            foreach (var other in all)
            {
                if (other == seg) continue;

                var pt = Intersect(seg, other);
                if (pt.HasValue) cuts.Add(pt.Value);
            }

            return cuts;
        }

        /// <summary>
        /// Intersección entre dos segmentos axis-aligned.
        /// Devuelve null si no se cruzan o son paralelos.
        /// </summary>
        private Point? Intersect(Segment a, Segment b)
        {
            if (a.IsHorizontal && b.IsVertical)
            {
                long y = a.A.Y;
                long x = b.A.X;
                if (b.MinY <= y && y <= b.MaxY && a.MinX <= x && x <= a.MaxX)
                    return new Point(x, y);
            }
            else if (a.IsVertical && b.IsHorizontal)
            {
                long x = a.A.X;
                long y = b.A.Y;
                if (a.MinY <= y && y <= a.MaxY && b.MinX <= x && x <= b.MaxX)
                    return new Point(x, y);
            }
            else if (a.IsHorizontal && b.IsHorizontal && a.A.Y == b.A.Y)
            {
                // Colineales horizontales: los extremos solapados se añaden como cortes
                long y = a.A.Y;
                long lo = Math.Max(a.MinX, b.MinX);
                long hi = Math.Min(a.MaxX, b.MaxX);
                // Solo nos interesan los extremos del otro segmento que caigan dentro del primero
                if (b.MinX >= a.MinX && b.MinX <= a.MaxX) return new Point(b.MinX, y);
                if (b.MaxX >= a.MinX && b.MaxX <= a.MaxX) return new Point(b.MaxX, y);
            }
            else if (a.IsVertical && b.IsVertical && a.A.X == b.A.X)
            {
                long x = a.A.X;
                if (b.MinY >= a.MinY && b.MinY <= a.MaxY) return new Point(x, b.MinY);
                if (b.MaxY >= a.MinY && b.MaxY <= a.MaxY) return new Point(x, b.MaxY);
            }

            return null;
        }
    }
}