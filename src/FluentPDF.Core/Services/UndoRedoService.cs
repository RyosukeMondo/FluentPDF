namespace FluentPDF.Core.Services;

public sealed class UndoRedoService : IUndoRedoService
{
    private readonly Stack<IUndoableAction> _undoStack = new();
    private readonly Stack<IUndoableAction> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public event EventHandler? StateChanged;

    public void Push(IUndoableAction action)
    {
        _undoStack.Push(action);
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public IUndoableAction? Undo()
    {
        if (_undoStack.Count == 0) return null;
        var action = _undoStack.Pop();
        _redoStack.Push(action);
        StateChanged?.Invoke(this, EventArgs.Empty);
        return action;
    }

    public IUndoableAction? Redo()
    {
        if (_redoStack.Count == 0) return null;
        var action = _redoStack.Pop();
        _undoStack.Push(action);
        StateChanged?.Invoke(this, EventArgs.Empty);
        return action;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
