using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace LiveZoomIt;

/// <summary>
/// Overlay transparente sobre toda a tela virtual. No modo passivo é click-through
/// (WS_EX_TRANSPARENT); no modo desenho recebe mouse/teclado e o InkCanvas captura os traços.
/// A tela por baixo nunca é congelada: nada é capturado, apenas desenhado por cima.
/// As ferramentas de desenho ficam em MainWindow.Drawing.cs; holofote e destaque de cliques em MainWindow.Effects.cs.
/// </summary>
public partial class MainWindow : Window
{
    private const int HotkeyToggleDraw = 1;
    private const int HotkeyToggleMagnifier = 2;
    private const int HotkeyQuit = 3;
    private const int HotkeyToggleSpotlight = 4;
    private const int HotkeyToggleClickHighlights = 5;

    private static readonly (int Id, uint Modifiers, uint Key, string Name)[] Hotkeys =
    [
        (HotkeyToggleDraw, Native.MOD_CONTROL, 0x32, "Ctrl+2"),
        (HotkeyToggleMagnifier, Native.MOD_CONTROL, 0x31, "Ctrl+1"),
        (HotkeyToggleSpotlight, Native.MOD_CONTROL, 0x33, "Ctrl+3"),
        (HotkeyToggleClickHighlights, Native.MOD_CONTROL, 0x34, "Ctrl+4"),
        (HotkeyQuit, Native.MOD_CONTROL | Native.MOD_SHIFT, 0x51, "Ctrl+Shift+Q"),
    ];

    // Alfa 1/255: invisível, mas faz a janela em camadas receber cliques sobre áreas "vazias".
    private static readonly Brush HitTestableTransparent = CreateFrozenBrush(Color.FromArgb(1, 0, 0, 0));

    private IntPtr _hwnd;
    private HwndSource? _source;
    private bool _drawing;
    private bool _quitting;
    private IntPtr _previousForeground;
    private MagnifierWindow? _magnifier;

    public MainWindow()
    {
        InitializeComponent();
        InitializeDrawing();

        CoverVirtualScreen();
        PreviewKeyDown += OnPreviewKeyDown;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        Application.Current.SessionEnding += (_, _) => _quitting = true;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _hwnd = new WindowInteropHelper(this).Handle;
        _source = HwndSource.FromHwnd(_hwnd);
        _source.AddHook(WndProc);

        CoverVirtualScreen();
        SetPassiveMode();
        RegisterHotKeys();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Alt+F4 não fecha o overlay (os atalhos globais deixariam de funcionar); só sai do modo desenho.
        if (!_quitting)
        {
            e.Cancel = true;
            if (_drawing) ExitDrawMode();
            return;
        }
        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        if (_hwnd != IntPtr.Zero)
        {
            foreach (var hotkey in Hotkeys)
                Native.UnregisterHotKey(_hwnd, hotkey.Id);
        }
        _source?.RemoveHook(WndProc);
        RemoveMouseHook();
        _magnifier?.Close();
        _spotlight?.Dispose();
        base.OnClosed(e);
    }

    private void Quit()
    {
        _quitting = true;
        Application.Current.Shutdown();
    }

    private void RegisterHotKeys()
    {
        var failed = new List<string>();
        foreach (var hotkey in Hotkeys)
        {
            if (!Native.RegisterHotKey(_hwnd, hotkey.Id, hotkey.Modifiers | Native.MOD_NOREPEAT, hotkey.Key))
                failed.Add(hotkey.Name);
        }

        if (failed.Count == Hotkeys.Length)
        {
            MessageBox.Show("Nenhum atalho global pôde ser registrado (o LiveZoomIt já está em execução?).",
                "LiveZoomIt", MessageBoxButton.OK, MessageBoxImage.Error);
            Quit();
        }
        else if (failed.Count > 0)
        {
            MessageBox.Show($"Atalhos em uso por outro programa: {string.Join(", ", failed)}.",
                "LiveZoomIt", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case Native.WM_HOTKEY:
                switch (wParam.ToInt32())
                {
                    case HotkeyToggleDraw:
                        if (_drawing) ExitDrawMode(); else EnterDrawMode();
                        break;
                    case HotkeyToggleMagnifier:
                        ToggleMagnifier();
                        break;
                    case HotkeyToggleSpotlight:
                        ToggleSpotlight();
                        break;
                    case HotkeyToggleClickHighlights:
                        ToggleClickHighlights();
                        break;
                    case HotkeyQuit:
                        Quit();
                        break;
                }
                handled = true;
                break;

            // Soltar Alt (usado no arraste da elipse) abriria o menu de sistema da janela.
            case Native.WM_SYSCOMMAND when (wParam.ToInt64() & 0xFFF0) == Native.SC_KEYMENU:
                handled = true;
                break;
        }
        return IntPtr.Zero;
    }

    // ---------------------------------------------------------------- modos

    private void EnterDrawMode()
    {
        _drawing = true;
        _previousForeground = Native.GetForegroundWindow();

        int ex = Native.GetWindowLong(_hwnd, Native.GWL_EXSTYLE);
        ex &= ~(Native.WS_EX_TRANSPARENT | Native.WS_EX_NOACTIVATE);
        ex |= Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW;
        ApplyExStyle(ex);

        Ink.Background = HitTestableTransparent;
        Ink.EditingMode = InkCanvasEditingMode.Ink;
        Hint.Visibility = Visibility.Visible;

        // WM_HOTKEY concede ao processo o direito de trazer a janela para o primeiro plano.
        Activate();
        Native.SetForegroundWindow(_hwnd);
        Ink.Focus();
        Keyboard.Focus(Ink);
    }

    private void ExitDrawMode()
    {
        FinishShape();
        FinishErase();
        if (_tool != Tool.Pen) SetTool(Tool.Pen);
        _drawing = false;

        Board.Visibility = Visibility.Collapsed;
        Hint.Visibility = Visibility.Collapsed;
        Ink.EditingMode = InkCanvasEditingMode.None;
        Ink.Background = Brushes.Transparent;
        SetPassiveMode();

        if (_previousForeground != IntPtr.Zero && _previousForeground != _hwnd)
            Native.SetForegroundWindow(_previousForeground);
    }

    private void SetPassiveMode()
    {
        int ex = Native.GetWindowLong(_hwnd, Native.GWL_EXSTYLE);
        ex |= Native.WS_EX_TRANSPARENT | Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
        ApplyExStyle(ex);
    }

    private void ApplyExStyle(int exStyle)
    {
        Native.SetWindowLong(_hwnd, Native.GWL_EXSTYLE, exStyle);
        Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, 0, 0, 0, 0,
            Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE | Native.SWP_FRAMECHANGED);
    }

    // ---------------------------------------------------------------- lupa

    private void ToggleMagnifier()
    {
        _magnifier ??= new MagnifierWindow();
        if (_magnifier.IsRunning) _magnifier.Stop(); else _magnifier.Start();
    }

    // ---------------------------------------------------------------- geometria

    private void OnDisplaySettingsChanged(object? sender, EventArgs e) =>
        Dispatcher.InvokeAsync(() =>
        {
            CoverVirtualScreen();
            if (_spotlight?.IsRunning == true)
                _spotlight.CoverVirtualScreen();
        });

    private void CoverVirtualScreen()
    {
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        PrimaryArea.Margin = new Thickness(-SystemParameters.VirtualScreenLeft, -SystemParameters.VirtualScreenTop, 0, 0);
        PrimaryArea.Width = SystemParameters.PrimaryScreenWidth;
        PrimaryArea.Height = SystemParameters.PrimaryScreenHeight;

        // Garante cobertura exata em pixels de dispositivo (arredondamentos de DPI em multi-monitor).
        if (_hwnd != IntPtr.Zero)
        {
            Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST,
                Native.GetSystemMetrics(Native.SM_XVIRTUALSCREEN),
                Native.GetSystemMetrics(Native.SM_YVIRTUALSCREEN),
                Native.GetSystemMetrics(Native.SM_CXVIRTUALSCREEN),
                Native.GetSystemMetrics(Native.SM_CYVIRTUALSCREEN),
                Native.SWP_NOACTIVATE);
        }
    }

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
