using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using LiveZoomIt.Settings;

namespace LiveZoomIt;

/// <summary>Leitura e aplicação de %APPDATA%\LiveZoomIt\settings.json.</summary>
public partial class MainWindow
{
    private LoadedSettings _config = null!;
    private IReadOnlyDictionary<Key, Color> _colors = new Dictionary<Key, Color>();

    private string CaptureFolder => _config.Settings.CaptureFolder.Length > 0
        ? _config.Settings.CaptureFolder
        : ScreenCapture.DefaultFolder;

    /// <summary>Lê o arquivo e aplica tudo. No recarregamento, reinicia a lupa e o holofote com os novos valores.</summary>
    private void LoadSettings(bool initial)
    {
        _config = SettingsLoader.LoadOrCreate(SettingsLoader.DefaultPath);
        var s = _config.Settings;

        _colors = _config.Colors;
        _penColor = _colors[_config.StartColor];
        _penStyle = PenStyle.Normal;
        _penWidthIndex = NearestIndex(PenWidths, s.PenWidth);
        _fontSizeIndex = NearestIndex(FontSizes, s.FontSize);
        ColorKeysHint.Text = $"{string.Join(" ", _colors.Keys)} cor  ·  Shift+cor marca-texto  ·  L laser  ·  T texto  ·  N números  ·  Ctrl+roda tamanho  ·  W / K quadro";
        UpdateExitHint();

        if (!initial)
        {
            bool magnifierWasRunning = _magnifier?.IsRunning == true;
            _magnifier?.Close();
            _magnifier = null;
            if (magnifierWasRunning) ToggleMagnifier();

            bool spotlightWasRunning = _spotlight?.IsRunning == true;
            _spotlight?.Dispose();
            _spotlight = null;
            if (spotlightWasRunning) ToggleSpotlight();

            RegisterHotKeys(initial: false);
            ApplyPenAttributes();
            UpdateTrayMenu();
        }

        if (_config.Warnings.Count > 0)
        {
            string more = _config.Warnings.Count > 1 ? $" (+{_config.Warnings.Count - 1} aviso(s))" : "";
            ShowToast($"Configurações: {_config.Warnings[0]}{more}", 8);
        }
        else if (!initial)
        {
            ShowToast("Configurações recarregadas");
        }
    }

    private void UpdateExitHint() =>
        ExitHint.Text = "Ctrl+Z / Ctrl+Y desfazer / refazer  ·  Ctrl+C copiar · Ctrl+S salvar  ·  "
            + (_config.Settings.ClearOnExit ? "Esc sair e apagar" : "Esc sair (o desenho fica)");

    /// <summary>Liga/desliga "apagar ao sair" pelo menu do ícone e grava no settings.json.</summary>
    private void ToggleClearOnExit()
    {
        bool enabled = !_config.Settings.ClearOnExit;
        _config.Settings.ClearOnExit = enabled;
        UpdateExitHint();
        try
        {
            SettingsLoader.SaveValue(SettingsLoader.DefaultPath, nameof(AppSettings.ClearOnExit), enabled);
            ShowToast(enabled ? "O desenho será apagado ao sair do modo desenho" : "O desenho ficará na tela ao sair do modo desenho");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            ShowToast($"Alterado só nesta sessão; não foi possível gravar o settings.json: {ex.Message}", 6);
        }
    }

    private static int NearestIndex(double[] values, double target)
    {
        int best = 0;
        for (int i = 1; i < values.Length; i++)
        {
            if (Math.Abs(values[i] - target) < Math.Abs(values[best] - target))
                best = i;
        }
        return best;
    }

    private void OpenSettingsFile()
    {
        string path = SettingsLoader.DefaultPath;
        if (!File.Exists(path))
            SettingsLoader.LoadOrCreate(path); // cria com os padrões

        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Win32Exception)
        {
            // Nenhum programa associado a .json.
            Process.Start("notepad.exe", $"\"{path}\"");
        }
    }

    private void OpenCaptureFolder()
    {
        try
        {
            Directory.CreateDirectory(CaptureFolder);
            Process.Start("explorer.exe", $"\"{CaptureFolder}\"");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or Win32Exception)
        {
            ShowToast($"Não foi possível abrir {CaptureFolder}: {ex.Message}");
        }
    }
}
