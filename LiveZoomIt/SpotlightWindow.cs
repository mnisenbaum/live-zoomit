using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Media;

namespace LiveZoomIt;

/// <summary>
/// Holofote: escurece todos os monitores e deixa um círculo claro em volta do cursor.
/// Em vez de redesenhar uma imagem do tamanho da área de trabalho a cada frame, usa uma janela
/// preta com transparência uniforme (LWA_ALPHA) e "recorta" o círculo com uma região da janela,
/// que o Windows atualiza sem custo de renderização.
/// É uma janela Win32 pura: uma Window do WPF remove o WS_EX_LAYERED que esse truque exige.
/// </summary>
internal sealed class SpotlightWindow : IDisposable
{
    private const string ClassName = "LiveZoomItSpotlight";
    private const double RadiusDip = 160;
    private const byte DimAlpha = 170;

    private IntPtr _hwnd;
    private int _originX;
    private int _originY;
    private int _width;
    private int _height;
    private Native.POINT _lastCursor;

    public bool IsRunning { get; private set; }

    public void Start()
    {
        if (IsRunning) return;
        EnsureWindow();
        IsRunning = true;
        CoverVirtualScreen();
        Native.ShowWindow(_hwnd, Native.SW_SHOWNOACTIVATE);
        CompositionTarget.Rendering += OnRendering;
    }

    public void Stop()
    {
        if (!IsRunning) return;
        IsRunning = false;
        CompositionTarget.Rendering -= OnRendering;
        Native.ShowWindow(_hwnd, Native.SW_HIDE);
    }

    public void Dispose()
    {
        Stop();
        if (_hwnd != IntPtr.Zero)
        {
            Native.DestroyWindow(_hwnd);
            _hwnd = IntPtr.Zero;
        }
    }

    public void CoverVirtualScreen()
    {
        if (_hwnd == IntPtr.Zero)
            return;

        _originX = Native.GetSystemMetrics(Native.SM_XVIRTUALSCREEN);
        _originY = Native.GetSystemMetrics(Native.SM_YVIRTUALSCREEN);
        _width = Native.GetSystemMetrics(Native.SM_CXVIRTUALSCREEN);
        _height = Native.GetSystemMetrics(Native.SM_CYVIRTUALSCREEN);
        Native.SetWindowPos(_hwnd, Native.HWND_TOPMOST, _originX, _originY, _width, _height, Native.SWP_NOACTIVATE);

        _lastCursor = new Native.POINT { X = int.MinValue, Y = int.MinValue };
        UpdateHole();
    }

    private void EnsureWindow()
    {
        if (_hwnd != IntPtr.Zero)
            return;

        IntPtr instance = Native.GetModuleHandle(null);
        var windowClass = new Native.WNDCLASSEX
        {
            cbSize = Marshal.SizeOf<Native.WNDCLASSEX>(),
            // A janela não trata nenhuma mensagem: o procedimento padrão já pinta o fundo preto da classe.
            lpfnWndProc = Native.GetProcAddress(Native.GetModuleHandle("user32.dll"), "DefWindowProcW"),
            hInstance = instance,
            hbrBackground = Native.GetStockObject(Native.BLACK_BRUSH),
            lpszClassName = ClassName,
        };
        if (Native.RegisterClassEx(ref windowClass) == 0 && Marshal.GetLastWin32Error() != Native.ERROR_CLASS_ALREADY_EXISTS)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        const int exStyle = Native.WS_EX_LAYERED | Native.WS_EX_TRANSPARENT | Native.WS_EX_TOOLWINDOW
            | Native.WS_EX_NOACTIVATE | Native.WS_EX_TOPMOST;
        _hwnd = Native.CreateWindowEx(exStyle, ClassName, "LiveZoomIt Holofote", Native.WS_POPUP,
            0, 0, 1, 1, IntPtr.Zero, IntPtr.Zero, instance, IntPtr.Zero);
        if (_hwnd == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error());

        Native.SetLayeredWindowAttributes(_hwnd, 0, DimAlpha, Native.LWA_ALPHA);
    }

    private void OnRendering(object? sender, EventArgs e) => UpdateHole();

    private void UpdateHole()
    {
        if (!Native.GetCursorPos(out var cursor) || (cursor.X == _lastCursor.X && cursor.Y == _lastCursor.Y))
            return;
        _lastCursor = cursor;

        int r = (int)Math.Round(RadiusDip * Native.GetDpiForWindow(_hwnd) / 96.0);
        int cx = cursor.X - _originX;
        int cy = cursor.Y - _originY;

        IntPtr region = Native.CreateRectRgn(0, 0, _width, _height);
        IntPtr hole = Native.CreateEllipticRgn(cx - r, cy - r, cx + r + 1, cy + r + 1);
        Native.CombineRgn(region, region, hole, Native.RGN_DIFF);
        Native.DeleteObject(hole);

        // Em caso de sucesso a região passa a pertencer ao sistema e não deve ser apagada.
        if (Native.SetWindowRgn(_hwnd, region, true) == 0)
            Native.DeleteObject(region);
    }
}
