using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using LiveZoomIt.Drawing;

namespace LiveZoomIt;

/// <summary>
/// Ferramentas do modo desenho: caneta e marca-texto, formas por modificador
/// (Ctrl retângulo, Shift linha, Alt elipse, Ctrl+Shift seta), texto, desfazer/refazer
/// e captura da tela anotada.
/// </summary>
public partial class MainWindow
{
    private static readonly double[] PenWidths = [1, 2, 3, 4, 6, 8, 10, 13, 16, 20, 25, 32, 40];
    private static readonly double[] FontSizes = [12, 14, 16, 18, 20, 24, 28, 32, 40, 48, 56, 64, 80, 96, 120];
    private static readonly FontFamily TextFont = new("Segoe UI");

    private readonly UndoStack _undo = new();
    private readonly DispatcherTimer _toastTimer = new() { Interval = TimeSpan.FromSeconds(2.5) };

    private Color _penColor = Colors.Red;
    private bool _highlighter;
    private int _penWidthIndex = 3;
    private int _fontSizeIndex = 6;

    private bool _textTool;
    private TextBox? _activeText;

    private ShapeKind? _shapeKind;
    private Point _shapeStart;
    private Stroke? _shapeStroke;

    // O marca-texto precisa ser bem mais largo que a caneta para cobrir uma linha de texto.
    private double CurrentPenWidth =>
        _highlighter ? Math.Max(PenWidths[_penWidthIndex] * 3, 12) : PenWidths[_penWidthIndex];

    private void InitializeDrawing()
    {
        Ink.StrokeCollected += (_, e) => _undo.Push(new StrokeAddedAction(Ink.Strokes, e.Stroke));
        PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        PreviewMouseMove += OnPreviewMouseMove;
        PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
        PreviewMouseWheel += OnPreviewMouseWheel;
        PreviewStylusDown += OnPreviewStylusDown;
        LostMouseCapture += (_, _) => FinishShape();
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            Toast.Visibility = Visibility.Collapsed;
        };
        ApplyPenAttributes();
    }

    // ---------------------------------------------------------------- caneta

    private DrawingAttributes CreateAttributes(bool freehand)
    {
        double width = CurrentPenWidth;
        return new DrawingAttributes
        {
            Color = _penColor,
            Width = width,
            Height = width,
            // Formas não podem ser suavizadas, senão os cantos do retângulo e da seta arredondam.
            FitToCurve = freehand,
            IgnorePressure = !freehand,
            StylusTip = _highlighter ? StylusTip.Rectangle : StylusTip.Ellipse,
            IsHighlighter = _highlighter,
        };
    }

    private void ApplyPenAttributes()
    {
        Ink.DefaultDrawingAttributes = CreateAttributes(freehand: true);
        UpdateHint();
    }

    private void SetPen(Color color, bool highlighter)
    {
        _penColor = color;
        _highlighter = highlighter;
        ApplyPenAttributes();
    }

    private static bool TryGetPenColor(Key key, out Color color)
    {
        switch (key)
        {
            case Key.R: color = Colors.Red; return true;
            case Key.G: color = Color.FromRgb(0x00, 0xD0, 0x00); return true;
            case Key.B: color = Color.FromRgb(0x00, 0x60, 0xFF); return true;
            case Key.Y: color = Color.FromRgb(0xFF, 0xE0, 0x00); return true;
            case Key.O: color = Color.FromRgb(0xFF, 0x8C, 0x00); return true;
            case Key.P: color = Color.FromRgb(0xFF, 0x3E, 0xA5); return true;
            default: color = default; return false;
        }
    }

    private void StepSize(int direction)
    {
        if (_textTool)
        {
            _fontSizeIndex = Math.Clamp(_fontSizeIndex + direction, 0, FontSizes.Length - 1);
            if (_activeText is not null)
                _activeText.FontSize = FontSizes[_fontSizeIndex];
            UpdateHint();
        }
        else
        {
            _penWidthIndex = Math.Clamp(_penWidthIndex + direction, 0, PenWidths.Length - 1);
            ApplyPenAttributes();
        }
    }

    private void UpdateHint()
    {
        ColorSwatch.Fill = CreateFrozenBrush(_penColor);
        ToolText.Text = _textTool
            ? $"Texto · {FontSizes[_fontSizeIndex]} pt"
            : $"{(_highlighter ? "Marca-texto" : "Caneta")} · {CurrentPenWidth:0.#} px";
    }

    // ---------------------------------------------------------------- teclado

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_drawing)
            return;

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        ModifierKeys modifiers = Keyboard.Modifiers;

        // Alt é modificador de arraste: não deve ativar as teclas de acesso do WPF.
        if (key is Key.LeftAlt or Key.RightAlt)
        {
            e.Handled = true;
            return;
        }

        // Digitando: as letras vão para o texto; só Esc e o tamanho da fonte são interceptados.
        if (_activeText is not null)
        {
            if (key == Key.Escape)
            {
                CommitActiveText();
                e.Handled = true;
            }
            else if (modifiers == ModifierKeys.Control && key is Key.Up or Key.Down)
            {
                StepSize(key == Key.Up ? 1 : -1);
                e.Handled = true;
            }
            return;
        }

        if (modifiers == ModifierKeys.Control)
        {
            switch (key)
            {
                case Key.Z: _undo.Undo(); break;
                case Key.Y: _undo.Redo(); break;
                case Key.C: CaptureScreen(save: false); break;
                case Key.S: CaptureScreen(save: true); break;
                case Key.Up: StepSize(1); break;
                case Key.Down: StepSize(-1); break;
                default: return;
            }
            e.Handled = true;
            return;
        }

        if (modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && key == Key.Z)
        {
            _undo.Redo();
            e.Handled = true;
            return;
        }

        if (modifiers == ModifierKeys.Shift && TryGetPenColor(key, out var highlighterColor))
        {
            SetPen(highlighterColor, highlighter: true);
            e.Handled = true;
            return;
        }

        if (modifiers != ModifierKeys.None)
            return;

        if (TryGetPenColor(key, out var penColor))
        {
            SetPen(penColor, highlighter: false);
            e.Handled = true;
            return;
        }

        switch (key)
        {
            case Key.T: SetTextTool(!_textTool); break;
            case Key.W: ToggleBoard(Colors.White); break;
            case Key.K: ToggleBoard(Colors.Black); break;
            case Key.E: ClearDrawing(); break;
            case Key.Escape:
                if (_textTool) SetTextTool(false); else ExitDrawMode();
                break;
            default: return;
        }
        e.Handled = true;
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

    private void ClearDrawing()
    {
        if (Ink.Strokes.Count == 0 && Ink.Children.Count == 0)
            return;

        var clear = new ClearAction(Ink.Strokes, Ink.Children);
        clear.Redo();
        _undo.Push(clear);
    }

    // ---------------------------------------------------------------- mouse: formas e roda

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_drawing)
            return;

        if (_textTool)
        {
            OnTextToolClick(e);
            return;
        }

        if (ShapeBuilder.FromModifiers(Keyboard.Modifiers) is not { } kind)
            return; // sem modificador: o InkCanvas desenha o traço livre

        _shapeKind = kind;
        _shapeStart = e.GetPosition(Ink);
        _shapeStroke = null;
        Ink.EditingMode = InkCanvasEditingMode.None;
        CaptureMouse();
        e.Handled = true;
    }

    private void OnPreviewStylusDown(object sender, StylusDownEventArgs e)
    {
        // Caneta física com modificador: impede o InkCanvas de iniciar um traço livre;
        // o evento de mouse promovido em seguida começa a forma.
        if (_drawing && !_textTool && ShapeBuilder.FromModifiers(Keyboard.Modifiers) is not null)
            Ink.EditingMode = InkCanvasEditingMode.None;
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_shapeKind is not { } kind)
            return;

        Point end = e.GetPosition(Ink);
        if (_shapeStroke is null && ShapeBuilder.IsDegenerate(_shapeStart, end))
            return;

        // O próprio traço final serve de prévia: é criado no primeiro movimento e atualizado a cada passo.
        var points = new StylusPointCollection(ShapeBuilder.Build(kind, _shapeStart, end, CurrentPenWidth));
        if (_shapeStroke is null)
        {
            _shapeStroke = new Stroke(points, CreateAttributes(freehand: false));
            Ink.Strokes.Add(_shapeStroke);
        }
        else
        {
            _shapeStroke.StylusPoints = points;
        }
        e.Handled = true;
    }

    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_shapeKind is null)
            return;
        FinishShape();
        e.Handled = true;
    }

    private void FinishShape()
    {
        if (_shapeKind is null)
            return;

        _shapeKind = null; // antes de liberar a captura, que dispara LostMouseCapture
        if (_shapeStroke is not null)
            _undo.Push(new StrokeAddedAction(Ink.Strokes, _shapeStroke));
        _shapeStroke = null;
        ReleaseMouseCapture();

        if (_drawing && !_textTool)
            Ink.EditingMode = InkCanvasEditingMode.Ink;
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!_drawing || Keyboard.Modifiers != ModifierKeys.Control)
            return;
        StepSize(e.Delta > 0 ? 1 : -1);
        e.Handled = true;
    }

    // ---------------------------------------------------------------- texto

    private void SetTextTool(bool enabled)
    {
        if (!enabled)
            CommitActiveText();

        _textTool = enabled;
        Ink.EditingMode = enabled ? InkCanvasEditingMode.None : InkCanvasEditingMode.Ink;
        Ink.UseCustomCursor = enabled;
        Ink.Cursor = enabled ? Cursors.IBeam : null;
        UpdateHint();
    }

    private void OnTextToolClick(MouseButtonEventArgs e)
    {
        // Clique dentro da caixa ativa só posiciona o cursor de texto.
        if (_activeText is { IsMouseOver: true })
            return;

        CommitActiveText();
        BeginText(e.GetPosition(Ink));
        e.Handled = true;
    }

    private void BeginText(Point position)
    {
        var brush = CreateFrozenBrush(_penColor);
        var box = new TextBox
        {
            FontFamily = TextFont,
            FontWeight = FontWeights.SemiBold,
            FontSize = FontSizes[_fontSizeIndex],
            Foreground = brush,
            CaretBrush = brush,
            Background = Brushes.Transparent,
            BorderBrush = CreateFrozenBrush(Color.FromArgb(0x90, _penColor.R, _penColor.G, _penColor.B)),
            BorderThickness = new Thickness(1),
            AcceptsReturn = true,
            MinWidth = 40,
        };
        // A linha de texto fica centralizada no ponto clicado.
        InkCanvas.SetLeft(box, position.X);
        InkCanvas.SetTop(box, position.Y - box.FontSize * 0.7);
        box.Loaded += (_, _) => Keyboard.Focus(box);

        Ink.Children.Add(box);
        _activeText = box;
    }

    /// <summary>Troca a caixa de edição por um texto fixo (desfazível). Texto vazio é descartado.</summary>
    private void CommitActiveText()
    {
        if (_activeText is not { } box)
            return;

        _activeText = null;
        Ink.Children.Remove(box);

        if (!string.IsNullOrWhiteSpace(box.Text))
        {
            var block = new TextBlock
            {
                Text = box.Text,
                FontFamily = box.FontFamily,
                FontWeight = box.FontWeight,
                FontSize = box.FontSize,
                Foreground = box.Foreground,
                // Compensa a borda e a margem interna da TextBox para o texto não "pular".
                Padding = new Thickness(3, 1, 3, 1),
                IsHitTestVisible = false,
            };
            InkCanvas.SetLeft(block, InkCanvas.GetLeft(box));
            InkCanvas.SetTop(block, InkCanvas.GetTop(box));

            var added = new ElementAddedAction(Ink.Children, block);
            added.Redo();
            _undo.Push(added);
        }

        Keyboard.Focus(Ink);
    }

    // ---------------------------------------------------------------- captura

    private async void CaptureScreen(bool save)
    {
        Hint.Visibility = Visibility.Collapsed;
        Toast.Visibility = Visibility.Collapsed;
        bool magnifierWasRunning = _magnifier?.IsRunning == true;
        _magnifier?.Stop();

        // Dá tempo para a janela em camadas ser recomposta sem a barra de dica.
        await Task.Delay(150);
        try
        {
            var image = ScreenCapture.CaptureMonitorUnderCursor();
            if (save)
            {
                ShowToast($"Salvo em {ScreenCapture.SavePng(image)}");
            }
            else
            {
                ScreenCapture.CopyToClipboard(image);
                ShowToast("Copiado para a área de transferência");
            }
        }
        catch (Exception ex)
        {
            ShowToast($"Falha na captura: {ex.Message}");
        }
        finally
        {
            if (_drawing) Hint.Visibility = Visibility.Visible;
            if (magnifierWasRunning) _magnifier!.Start();
        }
    }

    private void ShowToast(string message)
    {
        ToastText.Text = message;
        Toast.Visibility = Visibility.Visible;
        _toastTimer.Stop();
        _toastTimer.Start();
    }
}
