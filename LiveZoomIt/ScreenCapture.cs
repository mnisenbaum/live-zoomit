using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LiveZoomIt;

/// <summary>Captura do monitor sob o cursor (com as anotações) para a área de transferência ou PNG.</summary>
internal static class ScreenCapture
{
    public static BitmapSource CaptureMonitorUnderCursor()
    {
        Native.GetCursorPos(out var cursor);
        IntPtr monitor = Native.MonitorFromPoint(cursor, Native.MONITOR_DEFAULTTONEAREST);
        var info = new Native.MONITORINFO { cbSize = Marshal.SizeOf<Native.MONITORINFO>() };
        if (!Native.GetMonitorInfo(monitor, ref info))
            throw new InvalidOperationException("Não foi possível identificar o monitor.");

        var r = info.rcMonitor;
        int width = r.Right - r.Left;
        int height = r.Bottom - r.Top;

        IntPtr screenDC = Native.GetDC(IntPtr.Zero);
        IntPtr memDC = Native.CreateCompatibleDC(screenDC);
        IntPtr bitmap = Native.CreateCompatibleBitmap(screenDC, width, height);
        IntPtr old = Native.SelectObject(memDC, bitmap);
        try
        {
            // CAPTUREBLT inclui janelas em camadas, como o próprio overlay com os traços.
            Native.BitBlt(memDC, 0, 0, width, height, screenDC, r.Left, r.Top, Native.SRCCOPY | Native.CAPTUREBLT);
            Native.SelectObject(memDC, old);

            var source = Imaging.CreateBitmapSourceFromHBitmap(bitmap, IntPtr.Zero, Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            var opaque = new FormatConvertedBitmap(source, PixelFormats.Bgr32, null, 0);
            opaque.Freeze();
            return opaque;
        }
        finally
        {
            Native.DeleteObject(bitmap);
            Native.DeleteDC(memDC);
            Native.ReleaseDC(IntPtr.Zero, screenDC);
        }
    }

    /// <summary>Salva em Imagens\LiveZoomIt e devolve o caminho do arquivo.</summary>
    public static string SavePng(BitmapSource image)
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "LiveZoomIt");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, $"LiveZoomIt_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = File.Create(path);
        encoder.Save(stream);
        return path;
    }

    public static void CopyToClipboard(BitmapSource image)
    {
        // A área de transferência pode estar momentaneamente presa por outro processo.
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                Clipboard.SetImage(image);
                return;
            }
            catch (COMException) when (attempt < 4)
            {
                Thread.Sleep(50);
            }
        }
    }
}
