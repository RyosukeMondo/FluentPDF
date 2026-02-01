using System.Drawing;

namespace FluentPDF.Core.Models;

/// <summary>
/// Represents a text selection with its bounds and character-level information.
/// Used for creating text markup annotations (highlight, underline, strikethrough).
/// </summary>
public class TextSelection
{
    /// <summary>
    /// Gets or sets the selected text.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the character-level bounding boxes for each character in the selection.
    /// Used for precise annotation placement.
    /// </summary>
    public List<RectangleF> CharacterBounds { get; set; } = new();

    /// <summary>
    /// Gets or sets the overall selection bounds in PDF coordinates.
    /// This is the rectangle that encompasses all character bounds.
    /// </summary>
    public RectangleF SelectionBounds { get; set; }

    /// <summary>
    /// Gets or sets the page number where the selection is located (zero-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets whether the selection has any text.
    /// </summary>
    public bool HasText => !string.IsNullOrWhiteSpace(Text);

    /// <summary>
    /// Converts character bounds to quad points for PDF text markup annotations.
    /// Quad points define four corners of each character bound: x1,y1,x2,y2,x3,y3,x4,y4.
    /// If no character bounds are available, uses the overall selection bounds.
    /// </summary>
    /// <returns>List of floats representing quad points.</returns>
    public List<float> ToQuadPoints()
    {
        // If we have character-level bounds, use them for precise annotation
        if (CharacterBounds.Count > 0)
        {
            var quadPoints = new List<float>(CharacterBounds.Count * 8);

            foreach (var rect in CharacterBounds)
            {
                // PDF quad points: bottom-left, bottom-right, top-left, top-right
                // x1, y1 (bottom-left)
                quadPoints.Add(rect.Left);
                quadPoints.Add(rect.Bottom);

                // x2, y2 (bottom-right)
                quadPoints.Add(rect.Right);
                quadPoints.Add(rect.Bottom);

                // x3, y3 (top-left)
                quadPoints.Add(rect.Left);
                quadPoints.Add(rect.Top);

                // x4, y4 (top-right)
                quadPoints.Add(rect.Right);
                quadPoints.Add(rect.Top);
            }

            return quadPoints;
        }
        else
        {
            // Fallback: use overall selection bounds as a single quad
            var quadPoints = new List<float>(8);

            // Bottom-left
            quadPoints.Add(SelectionBounds.Left);
            quadPoints.Add(SelectionBounds.Bottom);

            // Bottom-right
            quadPoints.Add(SelectionBounds.Right);
            quadPoints.Add(SelectionBounds.Bottom);

            // Top-left
            quadPoints.Add(SelectionBounds.Left);
            quadPoints.Add(SelectionBounds.Top);

            // Top-right
            quadPoints.Add(SelectionBounds.Right);
            quadPoints.Add(SelectionBounds.Top);

            return quadPoints;
        }
    }
}
