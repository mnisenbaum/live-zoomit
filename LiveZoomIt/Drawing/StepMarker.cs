using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LiveZoomIt.Drawing;

/// <summary>Regras dos marcadores numerados, separadas da parte visual para poderem ser testadas.</summary>
public static class StepMarkers
{
    /// <summary>Próximo número: um a mais que o maior existente (lacunas deixadas por desfazer não são reaproveitadas).</summary>
    public static int NextNumber(IEnumerable<int> existing) => existing.DefaultIfEmpty(0).Max() + 1;

    /// <summary>Texto preto sobre cores claras (amarelo, laranja), branco sobre as escuras.</summary>
    public static bool UseDarkText(Color fill) => (0.299 * fill.R + 0.587 * fill.G + 0.114 * fill.B) / 255 > 0.6;
}

/// <summary>Círculo colorido com um número, colocado com a ferramenta N.</summary>
public sealed class StepMarker : Grid
{
    public int Number { get; }

    public StepMarker(int number, Color color, double diameter)
    {
        Number = number;
        Width = diameter;
        Height = diameter;
        IsHitTestVisible = false;

        var fill = new SolidColorBrush(color);
        fill.Freeze();
        Children.Add(new Ellipse
        {
            Fill = fill,
            Stroke = Brushes.White,
            StrokeThickness = Math.Max(1.5, diameter / 18),
        });
        Children.Add(new TextBlock
        {
            Text = number.ToString(),
            FontFamily = new FontFamily("Segoe UI"),
            FontWeight = FontWeights.Bold,
            FontSize = diameter * 0.5,
            Foreground = StepMarkers.UseDarkText(color) ? Brushes.Black : Brushes.White,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });
    }
}
