namespace MagicSvg
{
    /// <summary>
    /// Elemento de texto libre colocado sobre el SVG, en coordenadas de imagen.
    /// </summary>
    public sealed class SvgTextItem
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string Text { get; set; } = "";
        public double FontSize { get; set; } = 14;
    }
}
