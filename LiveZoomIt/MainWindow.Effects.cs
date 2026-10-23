using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace LiveZoomIt;

/// <summary>
/// Efeitos que funcionam também fora do modo desenho: holofote (Ctrl+3), destaque de cliques (Ctrl+4)
/// e o ajuste de zoom da lupa com Ctrl+Alt+roda. Os dois últimos usam um único hook global de mouse,
/// instalado só enquanto algum deles precisa.
/// </summary>
public partial class MainWindow
{
    private const double ClickRingDiameter = 56;
    private static readonly Brush LeftClickBrush = CreateFrozenBrush(Color.FromRgb(0xFF, 0xD0, 0x00));
    private static readonly Brush RightClickBrush = CreateFrozenBrush(Color.FromRgb(0x33, 0x99, 0xFF));

    private SpotlightWindow? _spotlight;
    private bool _clickHighlights;
    private IntPtr _mouseHook;
    private Native.LowLevelHookProc? _mouseHookProc; // mantém o delegate vivo enquanto o hook existir

    private void ToggleSpotlight()
    {
        var spotlight = _config.Settings.Spotlight;
        _spotlight ??= new SpotlightWindow(spotlight.Radius, spotlight.Dim);
        if (_spotlight.IsRunning) _spotlight.Stop(); else _spotlight.Start();
    }

    private void ToggleMagnifier()
    {
        _magnifier ??= new MagnifierWindow(_config.Settings.Magnifier);
        if (_magnifier.IsRunning) _magnifier.Stop(); else _magnifier.Start();
        UpdateMouseHook();
    }

    private void ToggleClickHighlights()
    {
        _clickHighlights = !_clickHighlights;
        if (!UpdateMouseHook())
        {
            _clickHighlights = false;
            ShowToast("Não foi possível ativar o destaque de cliques");
            return;
        }
        ShowToast(_clickHighlights ? "Destaque de cliques ligado" : "Destaque de cliques desligado");
    }

    /// <summary>Instala ou remove o hook conforme a necessidade; false se a instalação falhou.</summary>
    private bool UpdateMouseHook()
    {
        bool needed = _clickHighlights || _magnifier?.IsRunning == true;
        if (!needed)
        {
            RemoveMouseHook();
            return true;
        }
        if (_mouseHook != IntPtr.Zero)
            return true;

        _mouseHookProc ??= OnLowLevelMouse;
        _mouseHook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, _mouseHookProc, Native.GetModuleHandle("user32.dll"), 0);
        return _mouseHook != IntPtr.Zero;
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
    /// então só lê o evento e agenda o trabalho; nunca bloqueia.
    /// </summary>
    private IntPtr OnLowLevelMouse(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int message = wParam.ToInt32();

            if (message == Native.WM_MOUSEWHEEL && _magnifier?.IsRunning == true
                && Native.IsKeyDown(Native.VK_CONTROL) && Native.IsKeyDown(Native.VK_MENU))
            {
                var wheel = Marshal.PtrToStructure<Native.MSLLHOOKSTRUCT>(lParam);
                int direction = (short)(wheel.mouseData >> 16) > 0 ? 1 : -1;
                Dispatcher.InvokeAsync(() => _magnifier?.StepZoom(direction));
                return 1; // consome a roda: a janela embaixo do cursor não deve rolar nem dar zoom
            }

            // No modo desenho os cliques são traços: o anel só atrapalharia.
            if (_clickHighlights && !_drawing && message is Native.WM_LBUTTONDOWN or Native.WM_RBUTTONDOWN)
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
