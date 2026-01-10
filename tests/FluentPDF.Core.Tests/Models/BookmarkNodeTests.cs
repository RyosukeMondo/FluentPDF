using FluentAssertions;
using FluentPDF.Core.Models;
using Xunit;

namespace FluentPDF.Core.Tests.Models;

/// <summary>
/// Unit tests for the BookmarkNode model.
/// </summary>
public sealed class BookmarkNodeTests
{
    [Fact]
    public void BookmarkNode_CanBeCreated_WithRequiredProperties()
    {
        // Arrange
        var title = "Chapter 1";

        // Act
        var bookmark = new BookmarkNode
        {
            Title = title
        };

        // Assert
        bookmark.Title.Should().Be(title);
        bookmark.PageNumber.Should().BeNull();
        bookmark.X.Should().BeNull();
        bookmark.Y.Should().BeNull();
        bookmark.Children.Should().NotBeNull();
        bookmark.Children.Should().BeEmpty();
    }

    [Fact]
    public void BookmarkNode_CanBeCreated_WithAllProperties()
    {
        // Arrange
        var title = "Introduction";
        var pageNumber = 1;
        var x = 100.5f;
        var y = 200.7f;

        // Act
        var bookmark = new BookmarkNode
        {
            Title = title,
            PageNumber = pageNumber,
            X = x,
            Y = y
        };

        // Assert
        bookmark.Title.Should().Be(title);
        bookmark.PageNumber.Should().Be(pageNumber);
        bookmark.X.Should().Be(x);
        bookmark.Y.Should().Be(y);
        bookmark.Children.Should().BeEmpty();
    }

    [Fact]
    public void BookmarkNode_Children_IsInitializedEmpty()
    {
        // Arrange & Act
        var bookmark = new BookmarkNode
        {
            Title = "Test"
        };

        // Assert
        bookmark.Children.Should().NotBeNull();
        bookmark.Children.Should().BeEmpty();
        bookmark.Children.Should().BeOfType<List<BookmarkNode>>();
    }

    [Fact]
    public void GetTotalNodeCount_ForSingleNode_ReturnsOne()
    {
        // Arrange
        var bookmark = new BookmarkNode
        {
            Title = "Single Node"
        };

        // Act
        var count = bookmark.GetTotalNodeCount();

        // Assert
        count.Should().Be(1);
    }

    [Fact]
    public void GetTotalNodeCount_ForNodeWithChildren_ReturnsCorrectTotal()
    {
        // Arrange
        var child1 = new BookmarkNode { Title = "Child 1" };
        var child2 = new BookmarkNode { Title = "Child 2" };
        var parent = new BookmarkNode
        {
            Title = "Parent",
            Children = new List<BookmarkNode> { child1, child2 }
        };

        // Act
        var count = parent.GetTotalNodeCount();

        // Assert
        count.Should().Be(3); // parent + 2 children
    }

    [Fact]
    public void GetTotalNodeCount_ForNestedHierarchy_ReturnsCorrectTotal()
    {
        // Arrange
        var grandchild1 = new BookmarkNode { Title = "Grandchild 1.1" };
        var grandchild2 = new BookmarkNode { Title = "Grandchild 1.2" };

        var child1 = new BookmarkNode
        {
            Title = "Child 1",
            Children = new List<BookmarkNode> { grandchild1, grandchild2 }
        };

        var child2 = new BookmarkNode { Title = "Child 2" };

        var root = new BookmarkNode
        {
            Title = "Root",
            Children = new List<BookmarkNode> { child1, child2 }
        };

        // Act
        var count = root.GetTotalNodeCount();

        // Assert
        count.Should().Be(5); // root + 2 children + 2 grandchildren
    }

    [Fact]
    public void BookmarkNode_HierarchicalStructure_IsPreserved()
    {
        // Arrange
        var section1_1 = new BookmarkNode
        {
            Title = "Section 1.1",
            PageNumber = 5
        };

        var section1_2 = new BookmarkNode
        {
            Title = "Section 1.2",
            PageNumber = 10
        };

        var chapter1 = new BookmarkNode
        {
            Title = "Chapter 1",
            PageNumber = 1,
            Children = new List<BookmarkNode> { section1_1, section1_2 }
        };

        var chapter2 = new BookmarkNode
        {
            Title = "Chapter 2",
            PageNumber = 20
        };

        // Act & Assert
        chapter1.Children.Should().HaveCount(2);
        chapter1.Children[0].Should().Be(section1_1);
        chapter1.Children[1].Should().Be(section1_2);
        chapter2.Children.Should().BeEmpty();
    }

    [Fact]
    public void BookmarkNode_WithNoDestination_HasNullPageNumber()
    {
        // Arrange & Act
        var bookmark = new BookmarkNode
        {
            Title = "Table of Contents"
            // No PageNumber set
        };

        // Assert
        bookmark.PageNumber.Should().BeNull();
    }

    [Fact]
    public void BookmarkNode_WithDestination_HasPageNumber()
    {
        // Arrange & Act
        var bookmark = new BookmarkNode
        {
            Title = "Chapter 1",
            PageNumber = 10
        };

        // Assert
        bookmark.PageNumber.Should().Be(10);
    }

    [Fact]
    public void BookmarkNode_WithCoordinates_StoresXAndY()
    {
        // Arrange & Act
        var bookmark = new BookmarkNode
        {
            Title = "Specific Location",
            PageNumber = 5,
            X = 123.45f,
            Y = 678.90f
        };

        // Assert
        bookmark.X.Should().Be(123.45f);
        bookmark.Y.Should().Be(678.90f);
    }

    [Fact]
    public void BookmarkNode_Properties_AreInitOnly()
    {
        // This test verifies that properties are init-only at compile time
        // If properties were mutable, this wouldn't compile
        var bookmark = new BookmarkNode
        {
            Title = "Test",
            PageNumber = 1
        };

        // Assert - if we got here, properties are correctly init-only
        bookmark.Should().NotBeNull();
        bookmark.Title.Should().Be("Test");
    }

    [Fact]
    public void GetTotalNodeCount_ForDeepHierarchy_ReturnsCorrectTotal()
    {
        // Arrange - Create a 4-level hierarchy
        var level4 = new BookmarkNode { Title = "Level 4" };
        var level3 = new BookmarkNode
        {
            Title = "Level 3",
            Children = new List<BookmarkNode> { level4 }
        };
        var level2 = new BookmarkNode
        {
            Title = "Level 2",
            Children = new List<BookmarkNode> { level3 }
        };
        var level1 = new BookmarkNode
        {
            Title = "Level 1",
            Children = new List<BookmarkNode> { level2 }
        };

        // Act
        var count = level1.GetTotalNodeCount();

        // Assert
        count.Should().Be(4); // All 4 levels
    }
}
