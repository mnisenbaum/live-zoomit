using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace LiveZoomIt;

/// <summary>
/// Overlay transparente sobre toda a tela virtual. No modo passivo é click-through
/// (WS_EX_TRANSPARENT); no modo desenho recebe mouse/teclado e o InkCanvas captura os traços.
/// A tela por baixo nunca é congelada: nada é capturado, apenas desenhado por cima.
/// </summary>
public partial class MainWindow : Window
{
    private const int HotkeyToggleDraw = 1;
    private const int HotkeyToggleMagnifier = 2;
    private const int HotkeyQuit = 3;

    // Alfa 1/255: invisível, mas faz a janela em camadas receber cliques sobre áreas "vazias".
    private static readonly Brush HitTestableTransparent = CreateFrozenBrush(Color.FromArgb(1, 0, 0, 0));

    private IntPtr _hwnd;
    private HwndSource? _source;
    private bool _drawing;
    private IntPtr _previousForeground;
    private MagnifierWindow? _magnifier;

    public MainWindow()
    {
        InitializeComponent();

        Ink.DefaultDrawingAttributes = new DrawingAttributes
        {
            Color = Colors.Red,
            Width = 4,
            Height = 4,
            FitToCurve = true,
            StylusTip = StylusTip.Ellipse,
        };

        CoverVirtualScreen();
        PreviewKeyDown += OnPreviewKeyDown;
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
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

    protected override void OnClosed(EventArgs e)
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        if (_hwnd != IntPtr.Zero)
        {
            Native.UnregisterHotKey(_hwnd, HotkeyToggleDraw);
            Native.UnregisterHotKey(_hwnd, HotkeyToggleMagnifier);
            Native.UnregisterHotKey(_hwnd, HotkeyQuit);
        }
        _source?.RemoveHook(WndProc);
        _magnifier?.Close();
        base.OnClosed(e);
    }

    private void RegisterHotKeys()
    {
        var failed = new List<string>();
        if (!Native.RegisterHotKey(_hwnd, HotkeyToggleDraw, Native.MOD_CONTROL | Native.MOD_NOREPEAT, 0x32)) // '2'
            failed.Add("Ctrl+2");
        if (!Native.RegisterHotKey(_hwnd, HotkeyToggleMagnifier, Native.MOD_CONTROL | Native.MOD_NOREPEAT, 0x31)) // '1'
            failed.Add("Ctrl+1");
        if (!Native.RegisterHotKey(_hwnd, HotkeyQuit, Native.MOD_CONTROL | Native.MOD_SHIFT | Native.MOD_NOREPEAT, 0x51)) // 'Q'
            failed.Add("Ctrl+Shift+Q");

        if (failed.Count == 3)
        {
            MessageBox.Show("Nenhum atalho global pôde ser registrado (o LiveZoomIt já está em execução?).",
                "LiveZoomIt", MessageBoxButton.OK, MessageBoxImage.Error);
            Application.Current.Shutdown();
        }
        else if (failed.Count > 0)
        {
            MessageBox.Show($"Atalhos em uso por outro programa: {string.Join(", ", failed)}.",
                "LiveZoomIt", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != Native.WM_HOTKEY)
            return IntPtr.Zero;

        switch (wParam.ToInt32())
        {
            case HotkeyToggleDraw:
                if (_drawing) ExitDrawMode(); else EnterDrawMode();
                break;
            case HotkeyToggleMagnifier:
                ToggleMagnifier();
                break;
            case HotkeyQuit:
                Application.Current.Shutdown();
                break;
        }
        handled = true;
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

    // ---------------------------------------------------------------- teclado (modo desenho)

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_drawing)
            return;

        var modifiers = Keyboard.Modifiers;
        if (modifiers == ModifierKeys.Control && e.Key == Key.Z)
        {
            if (Ink.Strokes.Count > 0)
                Ink.Strokes.RemoveAt(Ink.Strokes.Count - 1);
            e.Handled = true;
            return;
        }
        if (modifiers != ModifierKeys.None)
            return;

        switch (e.Key)
        {
            case Key.R: SetPenColor(Colors.Red); break;
            case Key.G: SetPenColor(Color.FromRgb(0x00, 0xD0, 0x00)); break;
            case Key.B: SetPenColor(Color.FromRgb(0x00, 0x60, 0xFF)); break;
            case Key.Y: SetPenColor(Color.FromRgb(0xFF, 0xE0, 0x00)); break;
            case Key.W: ToggleBoard(Colors.White); break;
            case Key.K: ToggleBoard(Colors.Black); break;
            case Key.E: Ink.Strokes.Clear(); break;
            case Key.Escape: ExitDrawMode(); break;
            default: return;
        }
        e.Handled = true;
    }

    private void SetPenColor(Color color)
    {
        var attributes = Ink.DefaultDrawingAttributes.Clone();
        attributes.Color = color;
        Ink.DefaultDrawingAttributes = attributes;
        ColorSwatch.Fill = CreateFrozenBrush(color);
    }

    private void ToggleBoard(Color color)
    {
        bool alreadyShown = Board.Visibility == Visibility.Visible
            && Board.Fill is SolidColorBrush current && current.Color == color;

        if (alreadyShown)
        {
            Board.Visibility = Visibility.Collapsed;
        }
        else
        {
            Board.Fill = CreateFrozenBrush(color);
            Board.Visibility = Visibility.Visible;
        }
    }

    // ---------------------------------------------------------------- lupa

    private void ToggleMagnifier()
    {
        _magnifier ??= new MagnifierWindow();
        if (_magnifier.IsRunning) _magnifier.Stop(); else _magnifier.Start();
    }

    // ---------------------------------------------------------------- geometria

    private void OnDisplaySettingsChanged(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(CoverVirtualScreen);

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

/// <summary>
/// Lupa flutuante em tempo real que segue o cursor. Copia a região sob o cursor com BitBlt
/// a cada frame e a exibe ampliada. A própria lupa é excluída da captura
/// (WDA_EXCLUDEFROMCAPTURE) para não se realimentar; sem esse recurso, fica deslocada do cursor.
/// </summary>
internal sealed class MagnifierWindow : Window
{
    private const double LensWidth = 420;
    private const double LensHeight = 260;
    private const double Zoom = 2.0;

    private readonly Border _lens;
    private IntPtr _hwnd;
    private bool _excludedFromCapture;

    private IntPtr _memDC;
    private IntPtr _dib;
    private IntPtr _oldBitmap;
    private IntPtr _bits;
    private int _srcWidth;
    private int _srcHeight;
    private WriteableBitmap? _frame;

    public bool IsRunning { get; private set; }

    public MagnifierWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = -32000;
        Top = -32000;
        Width = LensWidth;
        Height = LensHeight;
        Title = "LiveZoomIt Lupa";

        _lens = new Border
        {
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(3),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x99, 0xFF)),
            Background = Brushes.Black,
        };
        Content = _lens;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwnd = new WindowInteropHelper(this).Handle;

        int ex = Native.GetWindowLong(_hwnd, Native.GWL_EXSTYLE);
        ex |= Native.WS_EX_TRANSPARENT | Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
        Native.SetWindowLong(_hwnd, Native.GWL_EXSTYLE, ex);

        _excludedFromCapture = Native.SetWindowDisplayAffinity(_hwnd, Native.WDA_EXCLUDEFROMCAPTURE);
    }

    public void Start()
    {
        if (IsRunning) return;
        IsRunning = true;
        Show();
        UpdateFrame();
        CompositionTarget.Rendering += OnRendering;
    }

    public void Stop()
    {
        if (!IsRunning) return;
        IsRunning = false;
        CompositionTarget.Rendering -= OnRendering;
        Hide();
    }

    protected override void OnClosed(EventArgs e)
    {
        Stop();
        ReleaseBuffers();
        base.OnClosed(e);
    }

    private void OnRendering(object? sender, EventArgs e) => UpdateFrame();

    private void UpdateFrame()
    {
        if (!Native.GetCursorPos(out var cursor))
            return;

        var dpi = VisualTreeHelper.GetDpi(this);
        int lensW = (int)Math.Round(LensWidth * dpi.DpiScaleX);
        int lensH = (int)Math.Round(LensHeight * dpi.DpiScaleY);
        int srcW = Math.Max(1, (int)(lensW / Zoom));
        int srcH = Math.Max(1, (int)(lensH / Zoom));
        EnsureBuffers(srcW, srcH);

        int srcX = cursor.X - srcW / 2;
        int srcY = cursor.Y - srcH / 2;

        // Centralizada no cursor quando invisível à captura; senão, deslocada para fora da região copiada.
        int winX = _excludedFromCapture ? cursor.X - lensW / 2 : cursor.X + srcW / 2 + 16;
        int winY = _excludedFromCapture ? cursor.Y - lensH / 2 : cursor.Y + srcH / 2 + 16;
        Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, winX, winY, 0, 0,
            Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);

        IntPtr screenDC = Native.GetDC(IntPtr.Zero);
        Native.BitBlt(_memDC, 0, 0, srcW, srcH, screenDC, srcX, srcY, Native.SRCCOPY);
        Native.ReleaseDC(IntPtr.Zero, screenDC);
        Native.GdiFlush();

        _frame!.WritePixels(new Int32Rect(0, 0, srcW, srcH), _bits, srcW * srcH * 4, srcW * 4);
    }

    private void EnsureBuffers(int width, int height)
    {
        if (_dib != IntPtr.Zero && width == _srcWidth && height == _srcHeight)
            return;

        ReleaseBuffers();

        var header = new Native.BITMAPINFOHEADER
        {
            biSize = Marshal.SizeOf<Native.BITMAPINFOHEADER>(),
            biWidth = width,
            biHeight = -height, // top-down
            biPlanes = 1,
            biBitCount = 32,
            biCompression = Native.BI_RGB,
        };

        IntPtr screenDC = Native.GetDC(IntPtr.Zero);
        _memDC = Native.CreateCompatibleDC(screenDC);
        _dib = Native.CreateDIBSection(screenDC, ref header, Native.DIB_RGB_COLORS, out _bits, IntPtr.Zero, 0);
        Native.ReleaseDC(IntPtr.Zero, screenDC);
        _oldBitmap = Native.SelectObject(_memDC, _dib);

        _srcWidth = width;
        _srcHeight = height;

        // Bgr32 ignora o canal alfa, que o BitBlt deixa zerado.
        _frame = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
        var brush = new ImageBrush(_frame) { Stretch = Stretch.Fill };
        RenderOptions.SetBitmapScalingMode(brush, BitmapScalingMode.Linear);
        _lens.Background = brush;
    }

    private void ReleaseBuffers()
    {
        if (_memDC == IntPtr.Zero)
            return;

        Native.SelectObject(_memDC, _oldBitmap);
        Native.DeleteObject(_dib);
        Native.DeleteDC(_memDC);
        _memDC = _dib = _oldBitmap = _bits = IntPtr.Zero;
    }
}

internal static class Native
{
    public const int WM_HOTKEY = 0x0312;

    public const int GWL_EXSTYLE = -20;
    public const int WS_EX_TRANSPARENT = 0x00000020;
    public const int WS_EX_TOOLWINDOW = 0x00000080;
    public const int WS_EX_LAYERED = 0x00080000;
    public const int WS_EX_NOACTIVATE = 0x08000000;

    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_NOREPEAT = 0x4000;

    public static readonly IntPtr HWND_TOPMOST = new(-1);
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOMOVE = 0x0002;
    public const uint SWP_NOZORDER = 0x0004;
    public const uint SWP_NOACTIVATE = 0x0010;
    public const uint SWP_FRAMECHANGED = 0x0020;

    public const int SM_XVIRTUALSCREEN = 76;
    public const int SM_YVIRTUALSCREEN = 77;
    public const int SM_CXVIRTUALSCREEN = 78;
    public const int SM_CYVIRTUALSCREEN = 79;

    public const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    public const int SRCCOPY = 0x00CC0020;
    public const int BI_RGB = 0;
    public const uint DIB_RGB_COLORS = 0;

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public int biSize;
        public int biWidth;
        public int biHeight;
        public short biPlanes;
        public short biBitCount;
        public int biCompression;
        public int biSizeImage;
        public int biXPelsPerMeter;
        public int biYPelsPerMeter;
        public int biClrUsed;
        public int biClrImportant;
    }

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    public static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFOHEADER pbmi, uint usage, out IntPtr ppvBits, IntPtr hSection, uint offset);

    [DllImport("gdi32.dll")]
    public static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteObject(IntPtr ho);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int w, int h, IntPtr hdcSrc, int xSrc, int ySrc, int rop);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GdiFlush();
}
