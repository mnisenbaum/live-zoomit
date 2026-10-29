using LiveZoomIt.Settings;
using Gdi = System.Drawing;
using Forms = System.Windows.Forms;

namespace LiveZoomIt;

/// <summary>Ícone na área de notificação, com menu para os recursos e as configurações.</summary>
public partial class MainWindow
{
    private Forms.NotifyIcon? _tray;
    private readonly List<(Forms.ToolStripMenuItem Item, HotkeyAction Action)> _trayHotkeyItems = [];
    private Forms.ToolStripMenuItem? _trayDraw;
    private Forms.ToolStripMenuItem? _trayMagnifier;
    private Forms.ToolStripMenuItem? _traySpotlight;
    private Forms.ToolStripMenuItem? _trayClicks;
    private Forms.ToolStripMenuItem? _trayKeystrokes;
    private Forms.ToolStripMenuItem? _trayClearOnExit;

    private void CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        _trayDraw = AddTrayItem(menu, "Modo desenho", HotkeyAction.ToggleDraw, ToggleDrawMode);
        _trayMagnifier = AddTrayItem(menu, "Lupa", HotkeyAction.ToggleMagnifier, ToggleMagnifier);
        _traySpotlight = AddTrayItem(menu, "Holofote", HotkeyAction.ToggleSpotlight, ToggleSpotlight);
        _trayClicks = AddTrayItem(menu, "Destaque de cliques", HotkeyAction.ToggleClickHighlights, ToggleClickHighlights);
        _trayKeystrokes = AddTrayItem(menu, "Mostrar teclas", HotkeyAction.ToggleKeystrokes, ToggleKeystrokes);
        menu.Items.Add(new Forms.ToolStripSeparator());
        _trayClearOnExit = AddTrayItem(menu, "Apagar desenho ao sair", null, ToggleClearOnExit);
        AddTrayItem(menu, "Abrir pasta de capturas", null, OpenCaptureFolder);
        AddTrayItem(menu, "Editar configurações…", null, OpenSettingsFile);
        AddTrayItem(menu, "Recarregar configurações", null, () => LoadSettings(initial: false));
        menu.Items.Add(new Forms.ToolStripSeparator());
        AddTrayItem(menu, "Sair", HotkeyAction.Quit, Quit);
        menu.Opening += (_, _) => UpdateTrayMenu();

        _tray = new Forms.NotifyIcon
        {
            Icon = CreateTrayIconImage(),
            Text = "LiveZoomIt",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _tray.MouseDoubleClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left)
                ToggleDrawMode();
        };
        UpdateTrayMenu();
    }

    private Forms.ToolStripMenuItem AddTrayItem(Forms.ContextMenuStrip menu, string text, HotkeyAction? action, Action onClick)
    {
        var item = new Forms.ToolStripMenuItem(text);
        item.Click += (_, _) => onClick();
        menu.Items.Add(item);
        if (action is { } a)
            _trayHotkeyItems.Add((item, a));
        return item;
    }

    /// <summary>Marca os recursos ligados e mostra os atalhos atuais (podem ter mudado nas configurações).</summary>
    private void UpdateTrayMenu()
    {
        if (_tray is null)
            return;

        _trayDraw!.Checked = _drawing;
        _trayMagnifier!.Checked = _magnifier?.IsRunning == true;
        _traySpotlight!.Checked = _spotlight?.IsRunning == true;
        _trayClicks!.Checked = _clickHighlights;
        _trayKeystrokes!.Checked = KeystrokesVisible;
        _trayClearOnExit!.Checked = _config.Settings.ClearOnExit;

        foreach (var (item, action) in _trayHotkeyItems)
            item.ShortcutKeyDisplayString = _config.Hotkeys.TryGetValue(action, out var hotkey) ? hotkey.Display : "";
    }

    private void DisposeTrayIcon()
    {
        if (_tray is null)
            return;
        _tray.Visible = false;
        _tray.ContextMenuStrip?.Dispose();
        _tray.Icon?.Dispose();
        _tray.Dispose();
        _tray = null;
    }

    /// <summary>Lupa azul com um traço vermelho, desenhada em tempo de execução (não há arquivo .ico).</summary>
    private static Gdi.Icon CreateTrayIconImage()
    {
        using var bitmap = new Gdi.Bitmap(32, 32);
        using (var g = Gdi.Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = Gdi.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Gdi.Color.Transparent);

            var blue = Gdi.Color.FromArgb(0x33, 0x99, 0xFF);
            using var lens = new Gdi.Pen(blue, 3.5f);
            g.DrawEllipse(lens, 3, 3, 19, 19);
            using var handle = new Gdi.Pen(blue, 5f)
            {
                StartCap = Gdi.Drawing2D.LineCap.Round,
                EndCap = Gdi.Drawing2D.LineCap.Round,
            };
            g.DrawLine(handle, 20, 20, 28, 28);
            using var ink = new Gdi.Pen(Gdi.Color.FromArgb(0xFF, 0x30, 0x30), 2.5f)
            {
                StartCap = Gdi.Drawing2D.LineCap.Round,
                EndCap = Gdi.Drawing2D.LineCap.Round,
            };
            g.DrawBezier(ink, 7, 15, 10, 8, 14, 20, 18, 11);
        }

        IntPtr handleIcon = bitmap.GetHicon();
        try
        {
            return (Gdi.Icon)Gdi.Icon.FromHandle(handleIcon).Clone();
        }
        finally
        {
            Native.DestroyIcon(handleIcon);
        }
    }
}
