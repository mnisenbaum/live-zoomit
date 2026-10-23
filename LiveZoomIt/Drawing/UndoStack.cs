using System.Collections;
using System.Windows.Ink;

namespace LiveZoomIt.Drawing;

public interface IUndoableAction
{
    void Undo();
    void Redo();
}

/// <summary>Desfazer/refazer por ação (traço, forma, texto, marcador, borracha ou "limpar tudo").</summary>
public sealed class UndoStack
{
    private readonly Stack<IUndoableAction> _undo = new();
    private readonly Stack<IUndoableAction> _redo = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    /// <summary>Registra uma ação já aplicada. Uma ação nova descarta o que havia para refazer.</summary>
    public void Push(IUndoableAction action)
    {
        _undo.Push(action);
        _redo.Clear();
    }

    public bool Undo()
    {
        if (!_undo.TryPop(out var action))
            return false;
        action.Undo();
        _redo.Push(action);
        return true;
    }

    public bool Redo()
    {
        if (!_redo.TryPop(out var action))
            return false;
        action.Redo();
        _undo.Push(action);
        return true;
    }
}

public sealed class StrokeAddedAction(StrokeCollection strokes, Stroke stroke) : IUndoableAction
{
    public void Undo() => strokes.Remove(stroke);
    public void Redo() => strokes.Add(stroke);
}

/// <summary>Elemento (ex.: texto) adicionado aos filhos do InkCanvas.</summary>
public sealed class ElementAddedAction(IList children, object element) : IUndoableAction
{
    public void Undo() => children.Remove(element);
    public void Redo() => children.Add(element);
}

/// <summary>Uma passada da borracha (botão direito): tudo o que ela tocou some e volta junto no desfazer.</summary>
public sealed class EraseAction(StrokeCollection strokes, IList children) : IUndoableAction
{
    private readonly List<Stroke> _erasedStrokes = [];
    private readonly List<object> _erasedChildren = [];

    public bool IsEmpty => _erasedStrokes.Count == 0 && _erasedChildren.Count == 0;

    public void EraseStroke(Stroke stroke)
    {
        if (strokes.Remove(stroke))
            _erasedStrokes.Add(stroke);
    }

    public void EraseChild(object child)
    {
        if (!children.Contains(child))
            return;
        children.Remove(child);
        _erasedChildren.Add(child);
    }

    public void Undo()
    {
        foreach (var stroke in _erasedStrokes)
            strokes.Add(stroke);
        foreach (var child in _erasedChildren)
            children.Add(child);
    }

    public void Redo()
    {
        foreach (var stroke in _erasedStrokes)
            strokes.Remove(stroke);
        foreach (var child in _erasedChildren)
            children.Remove(child);
    }
}

/// <summary>"Limpar tudo" reversível. Guarda o conteúdo atual; <see cref="Redo"/> executa a limpeza.</summary>
public sealed class ClearAction : IUndoableAction
{
    private readonly StrokeCollection _strokes;
    private readonly IList _children;
    private readonly StrokeCollection _savedStrokes;
    private readonly object[] _savedChildren;

    public ClearAction(StrokeCollection strokes, IList children)
    {
        _strokes = strokes;
        _children = children;
        _savedStrokes = new StrokeCollection(strokes);
        _savedChildren = children.Cast<object>().ToArray();
    }

    public void Redo()
    {
        _strokes.Clear();
        _children.Clear();
    }

    public void Undo()
    {
        _strokes.Add(_savedStrokes);
        foreach (var child in _savedChildren)
            _children.Add(child);
    }
}
