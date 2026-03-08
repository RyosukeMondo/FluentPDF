using FluentPDF.Core.Models;

namespace FluentPDF.Core.Services;

/// <summary>
/// Tracks drawing operations for undo/redo support.
/// </summary>
public interface IUndoRedoService
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    void Push(IUndoableAction action);
    IUndoableAction? Undo();
    IUndoableAction? Redo();
    void Clear();
    event EventHandler? StateChanged;
}

/// <summary>
/// Represents a reversible drawing action.
/// </summary>
public interface IUndoableAction
{
    string DocumentId { get; }
    PageIndex PageIndex { get; }
    UndoActionType ActionType { get; }
}

public enum UndoActionType
{
    AddShape,
    DeleteShape,
    MoveShape
}

/// <summary>Records an add-shape action. Undo = remove the last object on the page.</summary>
public record AddShapeAction(
    string DocumentId,
    PageIndex PageIndex,
    ShapeCreationData CreationData) : IUndoableAction
{
    public UndoActionType ActionType => UndoActionType.AddShape;
}

/// <summary>Records a delete-shape action. Undo = re-create the shape.</summary>
public record DeleteShapeAction(
    string DocumentId,
    PageIndex PageIndex,
    ShapeCreationData CreationData) : IUndoableAction
{
    public UndoActionType ActionType => UndoActionType.DeleteShape;
}

/// <summary>Records a move action. Undo = move by negative delta.</summary>
public record MoveShapeAction(
    string DocumentId,
    PageIndex PageIndex,
    int ObjectIndex,
    float DeltaX,
    float DeltaY) : IUndoableAction
{
    public UndoActionType ActionType => UndoActionType.MoveShape;
}

/// <summary>
/// Stores enough data to recreate a shape.
/// </summary>
public record ShapeCreationData
{
    public required DrawingShapeType ShapeType { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; }
    public double Height { get; init; }
    public double X2 { get; init; }
    public double Y2 { get; init; }
    public double Radius { get; init; }
    public string FillColor { get; init; } = "#00000000";
    public string StrokeColor { get; init; } = "#000000";
    public float StrokeWidth { get; init; } = 2f;
    public double[]? Points { get; init; }
    public string? Text { get; init; }
    public float FontSize { get; init; } = 12f;
    public string FontName { get; init; } = "Helvetica";
}

public enum DrawingShapeType
{
    Rectangle,
    Circle,
    Line,
    Freehand,
    Text
}
