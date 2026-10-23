using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace LiveZoomIt;

/// <summary>
/// Efeitos que funcionam também fora do modo desenho: holofote (Ctrl+3) e
/// destaque de cliques (Ctrl+4), que mostra um anel onde o mouse é clicado.
/// </summary>
public partial class MainWindow
{
    private const double ClickRingDiameter = 56;
    private static readonly Brush LeftClickBrush = CreateFrozenBrush(Color.FromRgb(0xFF, 0xD0, 0x00));
    private static readonly Brush RightClickBrush = CreateFrozenBrush(Color.FromRgb(0x33, 0x99, 0xFF));

    private SpotlightWindow? _spotlight;
    private IntPtr _mouseHook;
    private Native.LowLevelMouseProc? _mouseHookProc; // mantém o delegate vivo enquanto o hook existir

    private void ToggleSpotlight()
    {
        _spotlight ??= new SpotlightWindow();
        if (_spotlight.IsRunning) _spotlight.Stop(); else _spotlight.Start();
    }

    private void ToggleClickHighlights()
    {
        if (_mouseHook != IntPtr.Zero)
        {
            RemoveMouseHook();
            ShowToast("Destaque de cliques desligado");
            return;
        }

        _mouseHookProc ??= OnLowLevelMouse;
        _mouseHook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, _mouseHookProc, Native.GetModuleHandle("user32.dll"), 0);
        ShowToast(_mouseHook != IntPtr.Zero
            ? "Destaque de cliques ligado"
            : "Não foi possível ativar o destaque de cliques");
    }

    private void RemoveMouseHook()
    {
        if (_mouseHook == IntPtr.Zero)
            return;
        Native.UnhookWindowsHookEx(_mouseHook);
        _mouseHook = IntPtr.Zero;
    }

    /// <summary>
    /// Hook global de mouse. Roda na thread da interface a cada evento de mouse do sistema,
    /// então só lê o clique e agenda o anel; nunca bloqueia.
    /// </summary>
    private IntPtr OnLowLevelMouse(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // No modo desenho os cliques são traços: o anel só atrapalharia.
        if (nCode >= 0 && !_drawing)
        {
            int message = wParam.ToInt32();
            if (message is Native.WM_LBUTTONDOWN or Native.WM_RBUTTONDOWN)
            {
                var info = Marshal.PtrToStructure<Native.MSLLHOOKSTRUCT>(lParam);
                bool right = message == Native.WM_RBUTTONDOWN;
                Dispatcher.InvokeAsync(() => ShowClickRing(info.pt, right));
            }
        }
        return Native.CallNextHookEx(_mouseHook, nCode, wParam, lParam);
    }

    private void ShowClickRing(Native.POINT screenPoint, bool rightButton)
    {
        Point center = PointFromScreen(new Point(screenPoint.X, screenPoint.Y));
        var scale = new ScaleTransform(0.2, 0.2);
        var ring = new Ellipse
        {
            Width = ClickRingDiameter,
            Height = ClickRingDiameter,
            Stroke = rightButton ? RightClickBrush : LeftClickBrush,
            StrokeThickness = 4,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = scale,
        };
        Canvas.SetLeft(ring, center.X - ClickRingDiameter / 2);
        Canvas.SetTop(ring, center.Y - ClickRingDiameter / 2);
        EffectsLayer.Children.Add(ring);

        var duration = TimeSpan.FromMilliseconds(450);
        var grow = new DoubleAnimation(0.2, 1, duration) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
        var fade = new DoubleAnimation(1, 0, duration);
        fade.Completed += (_, _) => EffectsLayer.Children.Remove(ring);

        scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
        ring.BeginAnimation(OpacityProperty, fade);
    }
}
