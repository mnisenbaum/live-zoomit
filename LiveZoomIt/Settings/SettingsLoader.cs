using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows.Input;
using System.Windows.Media;

namespace LiveZoomIt.Settings;

/// <summary>Configurações já validadas, com os avisos sobre o que foi corrigido.</summary>
public sealed record LoadedSettings(
    AppSettings Settings,
    IReadOnlyDictionary<HotkeyAction, Hotkey> Hotkeys,
    IReadOnlyDictionary<Key, Color> Colors,
    Key StartColor,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Lê settings.json (criando-o com os padrões na primeira vez) e valida cada campo.
/// Um campo inválido nunca impede o programa de abrir: volta ao padrão e gera um aviso.
/// </summary>
public static class SettingsLoader
{
    public static readonly string DefaultPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LiveZoomIt", "settings.json");

    /// <summary>Teclas que já são ferramentas no modo desenho e não podem virar cor.</summary>
    public static readonly IReadOnlySet<Key> ReservedKeys = new HashSet<Key> { Key.L, Key.T, Key.N, Key.W, Key.K, Key.E };

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        // O arquivo é editado à mão: "Ctrl+2" em vez de "Ctrl+2". Ele nunca vai para HTML, então o escape não é necessário.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
    };

    public static LoadedSettings LoadOrCreate(string path)
    {
        if (!File.Exists(path))
        {
            var warnings = new List<string>();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, Serialize(new AppSettings()));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                warnings.Add($"Não foi possível criar {path}: {ex.Message}");
            }
            return Resolve(new AppSettings(), warnings);
        }

        try
        {
            return FromJson(File.ReadAllText(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Resolve(new AppSettings(), [$"Não foi possível ler {path}: {ex.Message}"]);
        }
    }

    public static string Serialize(AppSettings settings) => JsonSerializer.Serialize(settings, Options);

    public static LoadedSettings FromJson(string json)
    {
        try
        {
            return Resolve(JsonSerializer.Deserialize<AppSettings>(json, Options) ?? new AppSettings(), []);
        }
        catch (JsonException ex)
        {
            return Resolve(new AppSettings(), [$"settings.json inválido ({ex.Message}); usando os padrões."]);
        }
    }

    public static LoadedSettings Resolve(AppSettings s, List<string> warnings)
    {
        var defaults = new AppSettings();
        s.Hotkeys ??= new HotkeySettings();
        s.Magnifier ??= new MagnifierSettings();
        s.Spotlight ??= new SpotlightSettings();
        s.Laser ??= new LaserSettings();
        s.Colors ??= AppSettings.DefaultColors();
        s.StartColor ??= defaults.StartColor;
        s.CaptureFolder ??= "";

        var hotkeys = ResolveHotkeys(s.Hotkeys, defaults.Hotkeys, warnings);
        var colors = ResolveColors(s.Colors, warnings);

        Key startColor;
        if (TryParseColorKey(s.StartColor, out var start) && colors.ContainsKey(start))
        {
            startColor = start;
        }
        else
        {
            startColor = colors.Keys.First();
            warnings.Add($"StartColor \"{s.StartColor}\" não está em Colors; usando {startColor}.");
        }

        s.PenWidth = Clamp(s.PenWidth, 1, 40, defaults.PenWidth, nameof(s.PenWidth), warnings);
        s.FontSize = Clamp(s.FontSize, 12, 120, defaults.FontSize, nameof(s.FontSize), warnings);
        s.Magnifier.Width = Clamp(s.Magnifier.Width, 100, 1600, defaults.Magnifier.Width, "Magnifier.Width", warnings);
        s.Magnifier.Height = Clamp(s.Magnifier.Height, 80, 1200, defaults.Magnifier.Height, "Magnifier.Height", warnings);
        s.Magnifier.Zoom = Clamp(s.Magnifier.Zoom, MagnifierZoom.Levels[0], MagnifierZoom.Levels[^1],
            defaults.Magnifier.Zoom, "Magnifier.Zoom", warnings);
        s.Spotlight.Radius = Clamp(s.Spotlight.Radius, 40, 800, defaults.Spotlight.Radius, "Spotlight.Radius", warnings);
        s.Spotlight.Dim = Clamp(s.Spotlight.Dim, 0.1, 0.95, defaults.Spotlight.Dim, "Spotlight.Dim", warnings);
        s.Laser.HoldSeconds = Clamp(s.Laser.HoldSeconds, 0, 10, defaults.Laser.HoldSeconds, "Laser.HoldSeconds", warnings);
        s.Laser.FadeSeconds = Clamp(s.Laser.FadeSeconds, 0.1, 5, defaults.Laser.FadeSeconds, "Laser.FadeSeconds", warnings);

        s.CaptureFolder = Environment.ExpandEnvironmentVariables(s.CaptureFolder.Trim());
        if (s.CaptureFolder.Length > 0 && !Path.IsPathFullyQualified(s.CaptureFolder))
        {
            warnings.Add($"CaptureFolder precisa ser um caminho completo (ex.: C:\\Capturas); usando Imagens\\LiveZoomIt.");
            s.CaptureFolder = "";
        }

        return new LoadedSettings(s, hotkeys, colors, startColor, warnings);
    }

    private static Dictionary<HotkeyAction, Hotkey> ResolveHotkeys(HotkeySettings configured, HotkeySettings defaults, List<string> warnings)
    {
        var hotkeys = new Dictionary<HotkeyAction, Hotkey>();
        foreach (var action in Enum.GetValues<HotkeyAction>())
        {
            string? text = GetHotkeyText(configured, action);
            if (string.IsNullOrWhiteSpace(text) && action != HotkeyAction.Quit)
                continue; // atalho desativado

            if (!HotkeyParser.TryParse(text, out var hotkey))
            {
                string fallback = GetHotkeyText(defaults, action)!;
                warnings.Add($"Atalho inválido em Hotkeys.{action}: \"{text}\"; usando {fallback}.");
                HotkeyParser.TryParse(fallback, out hotkey);
            }

            if (hotkeys.FirstOrDefault(h => h.Value.Display == hotkey.Display) is { Value.Display: not null } clash)
            {
                warnings.Add($"Hotkeys.{action} repete o atalho {hotkey.Display} de Hotkeys.{clash.Key}; ignorado.");
                continue;
            }
            hotkeys[action] = hotkey;
        }
        return hotkeys;
    }

    private static string? GetHotkeyText(HotkeySettings h, HotkeyAction action) => action switch
    {
        HotkeyAction.ToggleDraw => h.ToggleDraw,
        HotkeyAction.ToggleMagnifier => h.ToggleMagnifier,
        HotkeyAction.ToggleSpotlight => h.ToggleSpotlight,
        HotkeyAction.ToggleClickHighlights => h.ToggleClickHighlights,
        HotkeyAction.ToggleKeystrokes => h.ToggleKeystrokes,
        HotkeyAction.Quit => h.Quit,
        _ => null,
    };

    private static Dictionary<Key, Color> ResolveColors(Dictionary<string, string> configured, List<string> warnings)
    {
        var colors = new Dictionary<Key, Color>();
        foreach (var (name, value) in configured)
        {
            if (!TryParseColorKey(name, out var key))
            {
                warnings.Add($"Tecla de cor inválida: \"{name}\" (use uma letra; L, T, N, W, K e E são reservadas).");
                continue;
            }
            if (!TryParseColor(value, out var color))
            {
                warnings.Add($"Cor inválida para {name}: \"{value}\".");
                continue;
            }
            colors[key] = color;
        }

        if (colors.Count > 0)
            return colors;

        warnings.Add("Nenhuma cor válida em Colors; usando as cores padrão.");
        return ResolveColors(AppSettings.DefaultColors(), warnings);
    }

    private static bool TryParseColorKey(string? name, out Key key)
    {
        key = default;
        if (name is not { Length: 1 } || !char.IsAsciiLetter(name[0]))
            return false;
        key = Key.A + (char.ToUpperInvariant(name[0]) - 'A');
        return !ReservedKeys.Contains(key);
    }

    private static bool TryParseColor(string? value, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        try
        {
            if (ColorConverter.ConvertFromString(value.Trim()) is Color parsed)
            {
                color = parsed;
                return true;
            }
        }
        catch (FormatException)
        {
        }
        return false;
    }

    private static double Clamp(double value, double min, double max, double fallback, string name, List<string> warnings)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            warnings.Add($"{name} inválido; usando {fallback}.");
            return fallback;
        }
        if (value >= min && value <= max)
            return value;

        double clamped = Math.Clamp(value, min, max);
        warnings.Add($"{name} = {value} fora do intervalo {min}–{max}; usando {clamped}.");
        return clamped;
    }
}
