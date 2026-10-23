namespace LiveZoomIt;

/// <summary>Níveis de ampliação da lupa, trocados com Ctrl+Alt+roda.</summary>
public static class MagnifierZoom
{
    public static readonly double[] Levels = [1.5, 2, 3, 4, 6, 8];

    public static double Nearest(double zoom) => Levels.MinBy(level => Math.Abs(level - zoom));

    public static double Step(double current, int direction)
    {
        int index = Array.IndexOf(Levels, Nearest(current));
        return Levels[Math.Clamp(index + Math.Sign(direction), 0, Levels.Length - 1)];
    }
}
