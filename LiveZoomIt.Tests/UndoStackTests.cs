using System.Collections;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using LiveZoomIt.Drawing;

namespace LiveZoomIt.Tests;

public class UndoStackTests
{
    private static Stroke NewStroke(double x) =>
        new(new StylusPointCollection(new[] { new Point(x, 0), new Point(x + 10, 10) }));

    [Fact]
    public void UndoAndRedo_OnEmptyStack_ReturnFalse()
    {
        var undo = new UndoStack();

        Assert.False(undo.Undo());
        Assert.False(undo.Redo());
    }

    [Fact]
    public void UndoRedo_ReplayStrokesInOrder()
    {
        var strokes = new StrokeCollection();
        var undo = new UndoStack();
        Stroke a = NewStroke(0), b = NewStroke(50);
        strokes.Add(a); undo.Push(new StrokeAddedAction(strokes, a));
        strokes.Add(b); undo.Push(new StrokeAddedAction(strokes, b));

        Assert.True(undo.Undo());
        Assert.Equal([a], strokes);
        Assert.True(undo.Undo());
        Assert.Empty(strokes);

        Assert.True(undo.Redo());
        Assert.Equal([a], strokes);
        Assert.True(undo.Redo());
        Assert.Equal([a, b], strokes);
    }

    [Fact]
    public void Push_AfterUndo_DiscardsRedoHistory()
    {
        var strokes = new StrokeCollection();
        var undo = new UndoStack();
        Stroke a = NewStroke(0), b = NewStroke(50);
        strokes.Add(a); undo.Push(new StrokeAddedAction(strokes, a));
        undo.Undo();

        strokes.Add(b); undo.Push(new StrokeAddedAction(strokes, b));

        Assert.False(undo.CanRedo);
        Assert.False(undo.Redo());
        Assert.Equal([b], strokes);
    }

    [Fact]
    public void ClearAction_IsReversible()
    {
        var strokes = new StrokeCollection { NewStroke(0), NewStroke(50) };
        IList children = new ArrayList { "texto 1", "texto 2" };
        var original = strokes.ToArray();
        var undo = new UndoStack();

        var clear = new ClearAction(strokes, children);
        clear.Redo();
        undo.Push(clear);
        Assert.Empty(strokes);
        Assert.Empty(children);

        undo.Undo();
        Assert.Equal(original, strokes);
        Assert.Equal(new object[] { "texto 1", "texto 2" }, children.Cast<object>());

        undo.Redo();
        Assert.Empty(strokes);
        Assert.Empty(children);
    }

    [Fact]
    public void ElementAddedAction_RemovesAndRestoresElement()
    {
        IList children = new ArrayList();
        var added = new ElementAddedAction(children, "texto");

        added.Redo();
        Assert.Single(children);
        added.Undo();
        Assert.Empty(children);
    }
}
