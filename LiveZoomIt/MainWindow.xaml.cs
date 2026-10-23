using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using LiveZoomIt.Settings;
using Microsoft.Win32;

namespace LiveZoomIt;

/// <summary>
/// Overlay transparente sobre toda a tela virtual. No modo passivo é click-through
/// (WS_EX_TRANSPARENT); no modo desenho recebe mouse/teclado e o InkCanvas captura os traços.
/// A tela por baixo nunca é congelada: nada é capturado, apenas desenhado por cima.
/// Demais partes: MainWindow.Drawing.cs (ferramentas), MainWindow.Effects.cs (holofote, cliques, zoom da lupa),
/// MainWindow.Keystrokes.cs (exibição de teclas), MainWindow.Settings.cs e MainWindow.Tray.cs.
/// </summary>
public partial class MainWindow : Window
{
    // Alfa 1/255: invisível, mas faz a janela em camadas receber cliques sobre áreas "vazias".
    private static readonly Brush HitTestableTransparent = CreateFrozenBrush(Color.FromArgb(1, 0, 0, 0));

    private readonly HashSet<HotkeyAction> _registeredHotkeys = [];
    private IntPtr _hwnd;
    private HwndSource? _source;
    private bool _drawing;
    private bool _quitting;
    private IntPtr _previousForeground;
    private MagnifierWindow? _magnifier;

    public MainWindow()
    {
        InitializeComponent();
        LoadSettings(initial: true);
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
        RegisterHotKeys(initial: true);
        CreateTrayIcon();
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
        UnregisterHotKeys();
        _source?.RemoveHook(WndProc);
        RemoveMouseHook();
        RemoveKeyboardHook();
        _magnifier?.Close();
        _spotlight?.Dispose();
        _keystrokes?.Close();
        DisposeTrayIcon();
        base.OnClosed(e);
    }

    private void Quit()
    {
        _quitting = true;
        Application.Current.Shutdown();
    }

    // ---------------------------------------------------------------- atalhos globais

    private static int HotkeyId(HotkeyAction action) => (int)action + 1;

    private void UnregisterHotKeys()
    {
        foreach (var action in _registeredHotkeys)
            Native.UnregisterHotKey(_hwnd, HotkeyId(action));
        _registeredHotkeys.Clear();
    }

    private void RegisterHotKeys(bool initial)
    {
        if (_hwnd == IntPtr.Zero)
            return;

        UnregisterHotKeys();
        var failed = new List<string>();
        foreach (var (action, hotkey) in _config.Hotkeys)
        {
            if (Native.RegisterHotKey(_hwnd, HotkeyId(action), hotkey.Modifiers | Native.MOD_NOREPEAT, hotkey.VirtualKey))
                _registeredHotkeys.Add(action);
            else
                failed.Add(hotkey.Display);
        }

        if (failed.Count == 0)
            return;

        if (initial && _registeredHotkeys.Count == 0)
        {
            MessageBox.Show("Nenhum atalho global pôde ser registrado (o LiveZoomIt já está em execução?).",
                "LiveZoomIt", MessageBoxButton.OK, MessageBoxImage.Error);
            Quit();
            return;
        }

        string message = $"Atalhos em uso por outro programa: {string.Join(", ", failed)}.";
        if (initial)
            MessageBox.Show(message, "LiveZoomIt", MessageBoxButton.OK, MessageBoxImage.Warning);
        else
            ShowToast(message, 6);
    }

    private void RunHotkey(HotkeyAction action)
    {
        switch (action)
        {
            case HotkeyAction.ToggleDraw: ToggleDrawMode(); break;
            case HotkeyAction.ToggleMagnifier: ToggleMagnifier(); break;
            case HotkeyAction.ToggleSpotlight: ToggleSpotlight(); break;
            case HotkeyAction.ToggleClickHighlights: ToggleClickHighlights(); break;
            case HotkeyAction.ToggleKeystrokes: ToggleKeystrokes(); break;
            case HotkeyAction.Quit: Quit(); break;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case Native.WM_HOTKEY:
                RunHotkey((HotkeyAction)(wParam.ToInt32() - 1));
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

    private void ToggleDrawMode()
    {
        if (_drawing) ExitDrawMode(); else EnterDrawMode();
    }

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
