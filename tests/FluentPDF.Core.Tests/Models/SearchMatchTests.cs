using FluentAssertions;
using FluentPDF.Core.Models;
using Xunit;

namespace FluentPDF.Core.Tests.Models;

/// <summary>
/// Unit tests for the SearchMatch and SearchOptions models.
/// </summary>
public sealed class SearchMatchTests
{
    #region SearchMatch Tests

    [Fact]
    public void SearchMatch_CanBeCreated_WithAllProperties()
    {
        // Arrange
        var boundingBox = new PdfRectangle(10, 20, 100, 50);

        // Act
        var match = new SearchMatch(
            PageNumber: 0,
            CharIndex: 42,
            Length: 5,
            Text: "hello",
            BoundingBox: boundingBox
        );

        // Assert
        match.PageNumber.Should().Be(0);
        match.CharIndex.Should().Be(42);
        match.Length.Should().Be(5);
        match.Text.Should().Be("hello");
        match.BoundingBox.Should().Be(boundingBox);
    }

    [Fact]
    public void SearchMatch_EndIndex_CalculatedCorrectly()
    {
        // Arrange
        var match = new SearchMatch(
            PageNumber: 1,
            CharIndex: 100,
            Length: 15,
            Text: "search term abc",
            BoundingBox: new PdfRectangle(10, 20, 100, 50)
        );

        // Act
        var endIndex = match.EndIndex;

        // Assert
        endIndex.Should().Be(115); // 100 + 15
    }

    [Fact]
    public void SearchMatch_IsValid_ReturnsTrue_WhenAllPropertiesValid()
    {
        // Arrange
        var match = new SearchMatch(
            PageNumber: 0,
            CharIndex: 0,
            Length: 5,
            Text: "valid",
            BoundingBox: new PdfRectangle(10, 20, 100, 50)
        );

        // Act
        var isValid = match.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void SearchMatch_IsValid_ReturnsFalse_WhenPageNumberNegative()
    {
        // Arrange
        var match = new SearchMatch(
            PageNumber: -1,
            CharIndex: 0,
            Length: 5,
            Text: "hello",
            BoundingBox: new PdfRectangle(10, 20, 100, 50)
        );

        // Act
        var isValid = match.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void SearchMatch_IsValid_ReturnsFalse_WhenCharIndexNegative()
    {
        // Arrange
        var match = new SearchMatch(
            PageNumber: 0,
            CharIndex: -1,
            Length: 5,
            Text: "hello",
            BoundingBox: new PdfRectangle(10, 20, 100, 50)
        );

        // Act
        var isValid = match.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void SearchMatch_IsValid_ReturnsFalse_WhenLengthZero()
    {
        // Arrange
        var match = new SearchMatch(
            PageNumber: 0,
            CharIndex: 0,
            Length: 0,
            Text: "",
            BoundingBox: new PdfRectangle(10, 20, 100, 50)
        );

        // Act
        var isValid = match.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void SearchMatch_IsValid_ReturnsFalse_WhenLengthNegative()
    {
        // Arrange
        var match = new SearchMatch(
            PageNumber: 0,
            CharIndex: 0,
            Length: -1,
            Text: "",
            BoundingBox: new PdfRectangle(10, 20, 100, 50)
        );

        // Act
        var isValid = match.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void SearchMatch_IsValid_ReturnsFalse_WhenTextEmpty()
    {
        // Arrange
        var match = new SearchMatch(
            PageNumber: 0,
            CharIndex: 0,
            Length: 5,
            Text: "",
            BoundingBox: new PdfRectangle(10, 20, 100, 50)
        );

        // Act
        var isValid = match.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void SearchMatch_IsValid_ReturnsFalse_WhenTextLengthMismatch()
    {
        // Arrange
        var match = new SearchMatch(
            PageNumber: 0,
            CharIndex: 0,
            Length: 10,
            Text: "hello", // Only 5 chars
            BoundingBox: new PdfRectangle(10, 20, 100, 50)
        );

        // Act
        var isValid = match.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void SearchMatch_IsValid_ReturnsFalse_WhenBoundingBoxInvalid()
    {
        // Arrange
        var match = new SearchMatch(
            PageNumber: 0,
            CharIndex: 0,
            Length: 5,
            Text: "hello",
            BoundingBox: new PdfRectangle(100, 20, 10, 50) // Right < Left
        );

        // Act
        var isValid = match.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void SearchMatch_OverlapsWith_ReturnsTrue_WhenRangesOverlap()
    {
        // Arrange
        var match1 = new SearchMatch(
            PageNumber: 0,
            CharIndex: 10,
            Length: 10,
            Text: "0123456789",
            BoundingBox: new PdfRectangle(10, 20, 100, 50)
        );

        var match2 = new SearchMatch(
            PageNumber: 0,
            CharIndex: 15,
            Length: 10,
            Text: "5678901234",
            BoundingBox: new PdfRectangle(50, 20, 140, 50)
        );

        // Act
        var overlaps = match1.OverlapsWith(match2);

        // Assert
        overlaps.Should().BeTrue();
    }

    [Fact]
    public void SearchMatch_OverlapsWith_ReturnsFalse_WhenRangesDoNotOverlap()
    {
        // Arrange
        var match1 = new SearchMatch(
            PageNumber: 0,
            CharIndex: 10,
            Length: 10,
            Text: "0123456789",
            BoundingBox: new PdfRectangle(10, 20, 100, 50)
        );

        var match2 = new SearchMatch(
            PageNumber: 0,
            CharIndex: 25,
            Length: 10,
            Text: "5678901234",
            BoundingBox: new PdfRectangle(150, 20, 240, 50)
        );

        // Act
        var overlaps = match1.OverlapsWith(match2);

        // Assert
        overlaps.Should().BeFalse();
    }

    [Fact]
    public void SearchMatch_OverlapsWith_ReturnsFalse_WhenRangesAdjacent()
    {
        // Arrange
        var match1 = new SearchMatch(
            PageNumber: 0,
            CharIndex: 10,
            Length: 10,
            Text: "0123456789",
            BoundingBox: new PdfRectangle(10, 20, 100, 50)
        );

        var match2 = new SearchMatch(
            PageNumber: 0,
            CharIndex: 20,
            Length: 10,
            Text: "0123456789",
            BoundingBox: new PdfRectangle(100, 20, 190, 50)
        );

        // Act
        var overlaps = match1.OverlapsWith(match2);

        // Assert
        overlaps.Should().BeFalse();
    }

    [Fact]
    public void SearchMatch_OverlapsWith_ReturnsFalse_WhenDifferentPages()
    {
        // Arrange
        var match1 = new SearchMatch(
            PageNumber: 0,
            CharIndex: 10,
            Length: 10,
            Text: "0123456789",
            BoundingBox: new PdfRectangle(10, 20, 100, 50)
        );

        var match2 = new SearchMatch(
            PageNumber: 1,
            CharIndex: 15,
            Length: 10,
            Text: "5678901234",
            BoundingBox: new PdfRectangle(50, 20, 140, 50)
        );

        // Act
        var overlaps = match1.OverlapsWith(match2);

        // Assert
        overlaps.Should().BeFalse();
    }

    [Fact]
    public void SearchMatch_OverlapsWith_ReturnsTrue_WhenOneContainsOther()
    {
        // Arrange
        var match1 = new SearchMatch(
            PageNumber: 0,
            CharIndex: 10,
            Length: 20,
            Text: "01234567890123456789",
            BoundingBox: new PdfRectangle(10, 20, 200, 50)
        );

        var match2 = new SearchMatch(
            PageNumber: 0,
            CharIndex: 15,
            Length: 5,
            Text: "56789",
            BoundingBox: new PdfRectangle(50, 20, 90, 50)
        );

        // Act
        var overlaps1 = match1.OverlapsWith(match2);
        var overlaps2 = match2.OverlapsWith(match1);

        // Assert
        overlaps1.Should().BeTrue();
        overlaps2.Should().BeTrue();
    }

    [Fact]
    public void SearchMatch_IsValueType_StructEquality()
    {
        // Arrange
        var boundingBox = new PdfRectangle(10, 20, 100, 50);
        var match1 = new SearchMatch(0, 42, 5, "hello", boundingBox);
        var match2 = new SearchMatch(0, 42, 5, "hello", boundingBox);
        var match3 = new SearchMatch(0, 42, 5, "world", boundingBox);

        // Act & Assert
        match1.Should().Be(match2);           // Equal values
        match1.Should().NotBe(match3);        // Different text
        match1.Equals(match2).Should().BeTrue();
    }

    #endregion

    #region SearchOptions Tests

    [Fact]
    public void SearchOptions_Default_HasExpectedValues()
    {
        // Arrange & Act
        var options = new SearchOptions();

        // Assert
        options.CaseSensitive.Should().BeFalse();
        options.WholeWord.Should().BeFalse();
    }

    [Fact]
    public void SearchOptions_CanSetCaseSensitive()
    {
        // Arrange & Act
        var options = new SearchOptions { CaseSensitive = true };

        // Assert
        options.CaseSensitive.Should().BeTrue();
        options.WholeWord.Should().BeFalse();
    }

    [Fact]
    public void SearchOptions_CanSetWholeWord()
    {
        // Arrange & Act
        var options = new SearchOptions { WholeWord = true };

        // Assert
        options.CaseSensitive.Should().BeFalse();
        options.WholeWord.Should().BeTrue();
    }

    [Fact]
    public void SearchOptions_CanSetBothFlags()
    {
        // Arrange & Act
        var options = new SearchOptions
        {
            CaseSensitive = true,
            WholeWord = true
        };

        // Assert
        options.CaseSensitive.Should().BeTrue();
        options.WholeWord.Should().BeTrue();
    }

    [Fact]
    public void SearchOptions_Default_Property_HasDefaultValues()
    {
        // Arrange & Act
        var options = SearchOptions.Default;

        // Assert
        options.CaseSensitive.Should().BeFalse();
        options.WholeWord.Should().BeFalse();
    }

    [Fact]
    public void SearchOptions_CaseSensitiveSearch_ReturnsCorrectOptions()
    {
        // Arrange & Act
        var options = SearchOptions.CaseSensitiveSearch();

        // Assert
        options.CaseSensitive.Should().BeTrue();
        options.WholeWord.Should().BeFalse();
    }

    [Fact]
    public void SearchOptions_WholeWordSearch_ReturnsCorrectOptions()
    {
        // Arrange & Act
        var options = SearchOptions.WholeWordSearch();

        // Assert
        options.CaseSensitive.Should().BeFalse();
        options.WholeWord.Should().BeTrue();
    }

    [Fact]
    public void SearchOptions_CaseSensitiveWholeWordSearch_ReturnsCorrectOptions()
    {
        // Arrange & Act
        var options = SearchOptions.CaseSensitiveWholeWordSearch();

        // Assert
        options.CaseSensitive.Should().BeTrue();
        options.WholeWord.Should().BeTrue();
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void SearchMatch_ComplexScenario_MultiPageDocument()
    {
        // Arrange - Simulate finding "PDF" across multiple pages
        var match1 = new SearchMatch(
            PageNumber: 0,
            CharIndex: 150,
            Length: 3,
            Text: "PDF",
            BoundingBox: new PdfRectangle(100, 200, 130, 220)
        );

        var match2 = new SearchMatch(
            PageNumber: 1,
            CharIndex: 75,
            Length: 3,
            Text: "PDF",
            BoundingBox: new PdfRectangle(50, 300, 80, 320)
        );

        var match3 = new SearchMatch(
            PageNumber: 1,
            CharIndex: 200,
            Length: 3,
            Text: "PDF",
            BoundingBox: new PdfRectangle(150, 100, 180, 120)
        );

        // Assert
        match1.IsValid().Should().BeTrue();
        match2.IsValid().Should().BeTrue();
        match3.IsValid().Should().BeTrue();

        // Matches on different pages don't overlap
        match1.OverlapsWith(match2).Should().BeFalse();
        match1.OverlapsWith(match3).Should().BeFalse();

        // Matches on same page don't overlap if ranges are separate
        match2.OverlapsWith(match3).Should().BeFalse();
    }

    [Fact]
    public void SearchMatch_ComplexScenario_OverlappingMatches()
    {
        // Arrange - Simulate searching for overlapping patterns
        // Text: "ababab" - searching for "aba" finds two overlapping matches
        var match1 = new SearchMatch(
            PageNumber: 0,
            CharIndex: 0,
            Length: 3,
            Text: "aba",
            BoundingBox: new PdfRectangle(10, 20, 40, 40)
        );

        var match2 = new SearchMatch(
            PageNumber: 0,
            CharIndex: 2,
            Length: 3,
            Text: "aba",
            BoundingBox: new PdfRectangle(30, 20, 60, 40)
        );

        // Assert
        match1.IsValid().Should().BeTrue();
        match2.IsValid().Should().BeTrue();
        match1.OverlapsWith(match2).Should().BeTrue();
    }

    [Fact]
    public void SearchOptions_ComplexScenario_CombinedWithSearchMatch()
    {
        // Arrange - Simulate a search workflow
        var caseSensitiveOptions = SearchOptions.CaseSensitiveSearch();
        var wholeWordOptions = SearchOptions.WholeWordSearch();

        var match = new SearchMatch(
            PageNumber: 2,
            CharIndex: 500,
            Length: 7,
            Text: "Example",
            BoundingBox: new PdfRectangle(200, 400, 270, 420)
        );

        // Assert - Verify that options can be used alongside matches
        caseSensitiveOptions.CaseSensitive.Should().BeTrue();
        wholeWordOptions.WholeWord.Should().BeTrue();
        match.IsValid().Should().BeTrue();
    }

    #endregion
}
