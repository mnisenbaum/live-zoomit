namespace LiveZoomIt.Drawing;

/// <summary>Curva de desaparecimento da caneta laser: fica inteira por um tempo e depois some.</summary>
public static class LaserFade
{
    public static readonly TimeSpan Hold = TimeSpan.FromSeconds(1.0);
    public static readonly TimeSpan Duration = TimeSpan.FromSeconds(0.5);

    /// <summary>Opacidade (0–1) de um traço laser terminado há <paramref name="age"/>, com os tempos padrão.</summary>
    public static double Opacity(TimeSpan age) => Opacity(age, Hold, Duration);

    public static double Opacity(TimeSpan age, TimeSpan hold, TimeSpan duration)
    {
        if (age <= hold)
            return 1;
        double progress = (age - hold) / duration;
        return progress >= 1 ? 0 : 1 - progress;
    }
}
