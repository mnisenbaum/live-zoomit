using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LiveZoomIt.Settings;

namespace LiveZoomIt;

/// <summary>
/// Lupa flutuante em tempo real que segue o cursor. Copia a região sob o cursor com BitBlt
/// a cada frame e a exibe ampliada. A própria lupa é excluída da captura
/// (WDA_EXCLUDEFROMCAPTURE) para não se realimentar; sem esse recurso, fica deslocada do cursor.
/// O nível de ampliação muda com Ctrl+Alt+roda (capturada pelo hook de mouse em MainWindow.Effects.cs).
/// </summary>
internal sealed class MagnifierWindow : Window
{
    private readonly double _lensWidth;
    private readonly double _lensHeight;
    private double _zoom;

    private readonly Border _lens;
    private readonly Border _zoomBadge;
    private readonly TextBlock _zoomText;
    private readonly DispatcherTimer _zoomBadgeTimer = new() { Interval = TimeSpan.FromSeconds(1.2) };
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

    public MagnifierWindow(MagnifierSettings settings)
    {
        _lensWidth = settings.Width;
        _lensHeight = settings.Height;
        _zoom = MagnifierZoom.Nearest(settings.Zoom);

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
        Width = _lensWidth;
        Height = _lensHeight;
        Title = "LiveZoomIt Lupa";

        _lens = new Border
        {
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(3),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x99, 0xFF)),
            Background = Brushes.Black,
        };
        _zoomText = new TextBlock { Foreground = Brushes.White, FontFamily = new FontFamily("Segoe UI"), FontSize = 14, FontWeight = FontWeights.SemiBold };
        _zoomBadge = new Border
        {
            Child = _zoomText,
            Background = new SolidColorBrush(Color.FromArgb(0xC0, 0x20, 0x20, 0x20)),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 2, 8, 2),
            Margin = new Thickness(10),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Visibility = Visibility.Collapsed,
        };
        var root = new Grid();
        root.Children.Add(_lens);
        root.Children.Add(_zoomBadge);
        Content = root;

        _zoomBadgeTimer.Tick += (_, _) =>
        {
            _zoomBadgeTimer.Stop();
            _zoomBadge.Visibility = Visibility.Collapsed;
        };
    }

    /// <summary>Muda o nível de ampliação e mostra o novo valor por um instante.</summary>
    public void StepZoom(int direction)
    {
        _zoom = MagnifierZoom.Step(_zoom, direction);
        _zoomText.Text = $"{_zoom:0.#}×";
        _zoomBadge.Visibility = Visibility.Visible;
        _zoomBadgeTimer.Stop();
        _zoomBadgeTimer.Start();
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
        int lensW = (int)Math.Round(_lensWidth * dpi.DpiScaleX);
        int lensH = (int)Math.Round(_lensHeight * dpi.DpiScaleY);
        int srcW = Math.Max(1, (int)(lensW / _zoom));
        int srcH = Math.Max(1, (int)(lensH / _zoom));
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
