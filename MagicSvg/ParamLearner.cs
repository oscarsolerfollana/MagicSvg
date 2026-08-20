using System.Text.Json;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace MagicSvg;

/// <summary>
/// Aprendizaje basado en ejemplos: guarda parámetros validados por el usuario junto
/// con características de la imagen, y sugiere parámetros para imágenes nuevas
/// buscando los ejemplos más parecidos (k-NN).
/// </summary>
public static class ParamLearner
{
    public record Example(double[] Features, LineProcessor.Settings Settings);

    private const int K = 3;

    private static readonly string StorePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MagicSvg", "learned_params.json");

    /// <summary>Número de ejemplos guardados hasta ahora.</summary>
    public static int ExampleCount => Load().Count;

    /// <summary>
    /// Calcula un vector de características simples de la imagen (tamaño, proporción,
    /// brillo medio, contraste y proporción de píxeles oscuros).
    /// </summary>
    public static double[] ExtractFeatures(Bitmap bitmap)
    {
        using var mat = BitmapConverter.ToMat(bitmap);
        using var gray = new Mat();
        Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);

        Cv2.MeanStdDev(gray, out var mean, out var stddev);

        using var dark = new Mat();
        Cv2.Threshold(gray, dark, 128, 255, ThresholdTypes.BinaryInv);
        double darkRatio = Cv2.CountNonZero(dark) / (double)(gray.Width * gray.Height);

        return
        [
            gray.Width  / 1000.0,
            gray.Height / 1000.0,
            gray.Width  / (double)gray.Height,
            mean.Val0    / 255.0,
            stddev.Val0  / 255.0,
            darkRatio,
        ];
    }

    /// <summary>Guarda los parámetros actuales como ejemplo válido para esta imagen.</summary>
    public static void AddExample(Bitmap bitmap, LineProcessor.Settings settings)
    {
        var examples = Load();
        examples.Add(new Example(ExtractFeatures(bitmap), settings));
        Save(examples);
    }

    /// <summary>
    /// Sugiere parámetros para la imagen dada a partir de los ejemplos aprendidos más
    /// parecidos, o <c>null</c> si todavía no hay ningún ejemplo guardado.
    /// </summary>
    public static LineProcessor.Settings? Suggest(Bitmap bitmap)
    {
        var examples = Load();
        if (examples.Count == 0) return null;

        var target = ExtractFeatures(bitmap);

        var neighbors = examples
            .Select(e => new
            {
                e.Settings,
                Distance = Distance(target, e.Features)
            })
            .OrderBy(n => n.Distance)
            .Take(Math.Min(K, examples.Count))
            .ToList();

        var weighted = neighbors
            .Select(n => (Weight: 1.0 / (n.Distance + 1e-6), n.Settings))
            .ToList();

        return WeightedAverage(weighted);
    }

    private static double Distance(double[] a, double[] b)
    {
        double sum = 0;
        for (int i = 0; i < a.Length; i++)
        {
            double d = a[i] - b[i];
            sum += d * d;
        }
        return Math.Sqrt(sum);
    }

    private static LineProcessor.Settings WeightedAverage(List<(double Weight, LineProcessor.Settings Settings)> items)
    {
        double totalWeight = items.Sum(i => i.Weight);
        double Avg(Func<LineProcessor.Settings, double> select) =>
            items.Sum(i => i.Weight * select(i.Settings)) / totalWeight;
        int AvgInt(Func<LineProcessor.Settings, double> select) =>
            (int)Math.Round(Avg(select));

        return new LineProcessor.Settings
        {
            BinaryThreshold        = AvgInt(s => s.BinaryThreshold),
            DilationKernelSize     = AvgInt(s => s.DilationKernelSize),
            DilationIterations     = AvgInt(s => s.DilationIterations),
            HoughThreshold         = AvgInt(s => s.HoughThreshold),
            MinLineLength          = AvgInt(s => s.MinLineLength),
            MaxLineGap             = AvgInt(s => s.MaxLineGap),
            AngleSnapTolerance     = Math.Round(Avg(s => s.AngleSnapTolerance), 1),
            MergePositionTolerance = AvgInt(s => s.MergePositionTolerance),
            SegmentGapTolerance    = AvgInt(s => s.SegmentGapTolerance),
            MinOutputSegmentLength = AvgInt(s => s.MinOutputSegmentLength),
            OutputLineThickness    = AvgInt(s => s.OutputLineThickness),
            MinComponentArea       = AvgInt(s => s.MinComponentArea),
            ContactTolerance       = AvgInt(s => s.ContactTolerance),
            IntersectionTolerance  = AvgInt(s => s.IntersectionTolerance),
            CornerTolerance        = AvgInt(s => s.CornerTolerance),
            MinCornerSegmentLength = AvgInt(s => s.MinCornerSegmentLength),
            ClipTolerance          = AvgInt(s => s.ClipTolerance),
            ExtendMaxDistance      = AvgInt(s => s.ExtendMaxDistance),
        };
    }

    private static List<Example> Load()
    {
        try
        {
            if (!File.Exists(StorePath)) return [];
            var json = File.ReadAllText(StorePath);
            return JsonSerializer.Deserialize<List<Example>>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static void Save(List<Example> examples)
    {
        var dir = Path.GetDirectoryName(StorePath)!;
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(examples, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(StorePath, json);
    }
}
