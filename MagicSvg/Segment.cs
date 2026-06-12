namespace MagicSvg;

/// <summary>
/// Segmento rectilíneo entre dos puntos (horizontal o vertical).
/// El algoritmo solo soporta segmentos axis-aligned (SVG de líneas ortogonales).
/// </summary>
public readonly record struct Segment(Point A, Point B)
{
    public bool IsHorizontal => A.Y == B.Y;
    public bool IsVertical => A.X == B.X;

    public long MinX => Math.Min(A.X, B.X);
    public long MaxX => Math.Max(A.X, B.X);
    public long MinY => Math.Min(A.Y, B.Y);
    public long MaxY => Math.Max(A.Y, B.Y);

    public override string ToString() => $"{A} → {B}";
}