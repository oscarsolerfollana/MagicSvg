namespace MagicSvg
{
    public readonly record struct Point(long X, long Y)
    {
        public override string ToString() => $"({X},{Y})";
    }

}
