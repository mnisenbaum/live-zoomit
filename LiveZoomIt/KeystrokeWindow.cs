using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using LiveZoomIt.Keystrokes;

namespace LiveZoomIt;

/// <summary>
/// Etiquetas com as teclas apertadas, no rodapé do monitor principal (Ctrl+5).
/// Janela própria e do tamanho do conteúdo: atualizar a cada tecla não redesenha o overlay de tela cheia.
/// </summary>
internal sealed class KeystrokeWindow : Window
{
    private static readonly Brush ChipBackground = Freeze(new SolidColorBrush(Color.FromArgb(0xE0, 0x20, 0x20, 0x20)));
    private static readonly FontFamily ChipFont = new("Segoe UI");

    private readonly KeystrokeLog _log = new();
    private readonly StackPanel _panel = new();
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private readonly Stopwatch _clock = Stopwatch.StartNew();

    public KeystrokeWindow()
    {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.WidthAndHeight;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = -32000;
        Top = -32000;
        Title = "LiveZoomIt Teclas";
        Content = _panel;

        SizeChanged += (_, _) => Reposition();
        _timer.Tick += (_, _) =>
        {
            if (_log.Prune(_clock.Elapsed))
                Render();
            if (_log.Entries.Count == 0)
                _timer.Stop();
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        IntPtr hwnd = new WindowInteropHelper(this).Handle;
        int ex = Native.GetWindowLong(hwnd, Native.GWL_EXSTYLE);
        ex |= Native.WS_EX_TRANSPARENT | Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
        Native.SetWindowLong(hwnd, Native.GWL_EXSTYLE, ex);
    }

    public void Push(Keystroke keystroke)
    {
        _log.Add(keystroke, _clock.Elapsed);
        Render();
        _timer.Start();
    }

    private void Render()
    {
        _panel.Children.Clear();
        foreach (var entry in _log.Entries)
        {
            _panel.Children.Add(new Border
            {
                Background = ChipBackground,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 6, 16, 6),
                Margin = new Thickness(0, 4, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Child = new TextBlock
                {
                    Text = entry.Text,
                    FontFamily = ChipFont,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 26,
                    Foreground = Brushes.White,
                },
            });
        }
    }

    private void Reposition()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + (workArea.Width - ActualWidth) / 2;
        Top = workArea.Bottom - ActualHeight - 60;
    }

    private static Brush Freeze(Brush brush)
    {
        brush.Freeze();
        return brush;
    }
}
