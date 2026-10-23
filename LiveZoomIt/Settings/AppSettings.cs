namespace LiveZoomIt.Settings;

/// <summary>
/// Conteúdo de %APPDATA%\LiveZoomIt\settings.json. Os valores iniciais são os padrões,
/// usados quando o campo está ausente ou inválido.
/// </summary>
public sealed class AppSettings
{
    public HotkeySettings Hotkeys { get; set; } = new();

    /// <summary>Tecla (uma letra) → cor (#RRGGBB ou nome, ex.: "Purple").</summary>
    public Dictionary<string, string> Colors { get; set; } = DefaultColors();

    public string StartColor { get; set; } = "R";
    public double PenWidth { get; set; } = 4;
    public double FontSize { get; set; } = 28;
    public MagnifierSettings Magnifier { get; set; } = new();
    public SpotlightSettings Spotlight { get; set; } = new();
    public LaserSettings Laser { get; set; } = new();

    /// <summary>Pasta das capturas (Ctrl+S). Vazio = Imagens\LiveZoomIt.</summary>
    public string CaptureFolder { get; set; } = "";

    public static Dictionary<string, string> DefaultColors() => new()
    {
        ["R"] = "#FF0000",
        ["G"] = "#00D000",
        ["B"] = "#0060FF",
        ["Y"] = "#FFE000",
        ["O"] = "#FF8C00",
        ["P"] = "#FF3EA5",
    };
}

/// <summary>Atalhos globais. Texto vazio desativa o atalho (exceto Quit).</summary>
public sealed class HotkeySettings
{
    public string ToggleDraw { get; set; } = "Ctrl+2";
    public string ToggleMagnifier { get; set; } = "Ctrl+1";
    public string ToggleSpotlight { get; set; } = "Ctrl+3";
    public string ToggleClickHighlights { get; set; } = "Ctrl+4";
    public string ToggleKeystrokes { get; set; } = "Ctrl+5";
    public string Quit { get; set; } = "Ctrl+Shift+Q";
}

public sealed class MagnifierSettings
{
    public double Width { get; set; } = 420;
    public double Height { get; set; } = 260;
    public double Zoom { get; set; } = 2;
}

public sealed class SpotlightSettings
{
    public double Radius { get; set; } = 160;

    /// <summary>Quanto escurecer fora do círculo, de 0,1 (leve) a 0,95 (quase preto).</summary>
    public double Dim { get; set; } = 0.67;
}

public sealed class LaserSettings
{
    public double HoldSeconds { get; set; } = 1.0;
    public double FadeSeconds { get; set; } = 0.5;
}
