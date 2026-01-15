namespace FluentPDF.Core.Models;

/// <summary>
/// Represents a single bookmark node in a hierarchical bookmark tree.
/// Bookmarks can have optional page destinations and child bookmarks.
/// </summary>
public sealed class BookmarkNode
{
    /// <summary>
    /// Gets the title text of the bookmark.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the 1-based page number this bookmark links to.
    /// Null if the bookmark has no destination.
    /// </summary>
    public int? PageNumber { get; init; }

    /// <summary>
    /// Gets the X coordinate of the destination on the page.
    /// Null if no specific location is set.
    /// </summary>
    public float? X { get; init; }

    /// <summary>
    /// Gets the Y coordinate of the destination on the page.
    /// Null if no specific location is set.
    /// </summary>
    public float? Y { get; init; }

    /// <summary>
    /// Gets the list of child bookmarks.
    /// Empty list if this bookmark has no children.
    /// </summary>
    public List<BookmarkNode> Children { get; init; } = new();

    /// <summary>
    /// Calculates the total number of nodes in this subtree (including this node).
    /// </summary>
    /// <returns>Total count of this node plus all descendant nodes.</returns>
    public int GetTotalNodeCount()
    {
        int count = 1;
        foreach (var child in Children)
        {
            count += child.GetTotalNodeCount();
        }
        return count;
    }
}
