using System.Collections;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using LiveZoomIt.Drawing;

namespace LiveZoomIt.Tests;

public class LaserFadeTests
{
    [Fact]
    public void StaysFullyVisibleDuringHold()
    {
        Assert.Equal(1, LaserFade.Opacity(TimeSpan.Zero));
        Assert.Equal(1, LaserFade.Opacity(LaserFade.Hold));
    }

    [Fact]
    public void FadesLinearlyAfterHold() =>
        Assert.Equal(0.5, LaserFade.Opacity(LaserFade.Hold + LaserFade.Duration / 2), 6);

    [Fact]
    public void IsGoneAfterHoldPlusDuration()
    {
        Assert.Equal(0, LaserFade.Opacity(LaserFade.Hold + LaserFade.Duration));
        Assert.Equal(0, LaserFade.Opacity(TimeSpan.FromMinutes(1)));
    }
}

public class StepMarkersTests
{
    [Fact]
    public void FirstMarkerIsOne() => Assert.Equal(1, StepMarkers.NextNumber([]));

    [Fact]
    public void NextIsOneMoreThanHighest() => Assert.Equal(4, StepMarkers.NextNumber([1, 2, 3]));

    [Fact]
    public void GapsLeftByUndoAreNotReused() => Assert.Equal(4, StepMarkers.NextNumber([3, 1]));

    [Theory]
    [InlineData(0xFF, 0xE0, 0x00, true)]  // amarelo
    [InlineData(0xFF, 0x8C, 0x00, true)]  // laranja
    [InlineData(0xFF, 0x00, 0x00, false)] // vermelho
    [InlineData(0x00, 0xD0, 0x00, false)] // verde
    [InlineData(0x00, 0x60, 0xFF, false)] // azul
    [InlineData(0xFF, 0x3E, 0xA5, false)] // rosa
    public void TextContrastFollowsFillBrightness(byte r, byte g, byte b, bool dark) =>
        Assert.Equal(dark, StepMarkers.UseDarkText(Color.FromRgb(r, g, b)));
}

public class EraseActionTests
{
    private static Stroke NewStroke(double x) =>
        new(new StylusPointCollection(new[] { new Point(x, 0), new Point(x + 10, 10) }));

    [Fact]
    public void NothingTouched_IsEmpty()
    {
        var erase = new EraseAction(new StrokeCollection(), new ArrayList());
        Assert.True(erase.IsEmpty);
    }

    [Fact]
    public void ErasedItemsComeBackTogetherOnUndo()
    {
        Stroke a = NewStroke(0), b = NewStroke(50), c = NewStroke(100);
        var strokes = new StrokeCollection { a, b, c };
        IList children = new ArrayList { "marcador 1", "texto" };
        var undo = new UndoStack();

        var erase = new EraseAction(strokes, children);
        erase.EraseStroke(a);
        erase.EraseStroke(c);
        erase.EraseChild("texto");
        undo.Push(erase);

        Assert.False(erase.IsEmpty);
        Assert.Equal([b], strokes);
        Assert.Equal(new object[] { "marcador 1" }, children.Cast<object>());

        undo.Undo();
        Assert.Equal(3, strokes.Count);
        Assert.Contains(a, strokes);
        Assert.Contains(c, strokes);
        Assert.Equal(2, children.Count);

        undo.Redo();
        Assert.Equal([b], strokes);
        Assert.Single(children);
    }

    [Fact]
    public void ErasingSameItemTwiceRecordsItOnce()
    {
        Stroke a = NewStroke(0);
        var strokes = new StrokeCollection { a };
        var erase = new EraseAction(strokes, new ArrayList());

        erase.EraseStroke(a);
        erase.EraseStroke(a); // o arraste passa de novo pelo mesmo ponto
        erase.Undo();

        Assert.Single(strokes);
    }
}
