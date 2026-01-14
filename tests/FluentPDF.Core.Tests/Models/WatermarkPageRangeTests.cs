using FluentPDF.Core.Models;
using FluentAssertions;

namespace FluentPDF.Core.Tests.Models;

public class WatermarkPageRangeTests
{
    [Fact]
    public void All_ShouldReturnAllPagesType()
    {
        var range = WatermarkPageRange.All;

        range.Type.Should().Be(PageRangeType.All);
    }

    [Fact]
    public void OddPages_ShouldReturnOddPagesType()
    {
        var range = WatermarkPageRange.OddPages;

        range.Type.Should().Be(PageRangeType.OddPages);
    }

    [Fact]
    public void EvenPages_ShouldReturnEvenPagesType()
    {
        var range = WatermarkPageRange.EvenPages;

        range.Type.Should().Be(PageRangeType.EvenPages);
    }

    [Fact]
    public void Current_ShouldReturnCurrentPageTypeWithCorrectPageNumber()
    {
        var range = WatermarkPageRange.Current(5);

        range.Type.Should().Be(PageRangeType.CurrentPage);
        range.CurrentPage.Should().Be(5);
    }

    [Theory]
    [InlineData("1-5", new[] { 1, 2, 3, 4, 5 })]
    [InlineData("1,3,5", new[] { 1, 3, 5 })]
    [InlineData("1-3,7-9", new[] { 1, 2, 3, 7, 8, 9 })]
    [InlineData("10, 5, 1-3", new[] { 1, 2, 3, 5, 10 })]
    [InlineData("1-3, 2-4", new[] { 1, 2, 3, 4 })]
    [InlineData("5", new[] { 5 })]
    public void Parse_WithValidRange_ShouldReturnCorrectPages(string rangeString, int[] expectedPages)
    {
        var range = WatermarkPageRange.Parse(rangeString);

        range.Type.Should().Be(PageRangeType.Custom);
        range.SpecificPages.Should().BeEquivalentTo(expectedPages);
        range.SpecificPages.Should().BeInAscendingOrder();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Parse_WithEmptyString_ShouldThrowArgumentException(string? rangeString)
    {
        var act = () => WatermarkPageRange.Parse(rangeString!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("1-0")]
    [InlineData("5-3")]
    [InlineData("abc")]
    [InlineData("1,abc,3")]
    [InlineData("1--5")]
    [InlineData("1-2-3")]
    public void Parse_WithInvalidRange_ShouldThrowArgumentException(string rangeString)
    {
        var act = () => WatermarkPageRange.Parse(rangeString);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid*");
    }

    [Fact]
    public void GetPages_WithAllType_ShouldReturnAllPages()
    {
        var range = WatermarkPageRange.All;
        var totalPages = 10;

        var pages = range.GetPages(totalPages);

        pages.Should().HaveCount(10);
        pages.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
    }

    [Fact]
    public void GetPages_WithCurrentPageType_ShouldReturnCurrentPage()
    {
        var range = WatermarkPageRange.Current(5);
        var totalPages = 10;

        var pages = range.GetPages(totalPages);

        pages.Should().HaveCount(1);
        pages.Should().BeEquivalentTo(new[] { 5 });
    }

    [Fact]
    public void GetPages_WithOddPagesType_ShouldReturnOddPages()
    {
        var range = WatermarkPageRange.OddPages;
        var totalPages = 10;

        var pages = range.GetPages(totalPages);

        pages.Should().HaveCount(5);
        pages.Should().BeEquivalentTo(new[] { 1, 3, 5, 7, 9 });
    }

    [Fact]
    public void GetPages_WithEvenPagesType_ShouldReturnEvenPages()
    {
        var range = WatermarkPageRange.EvenPages;
        var totalPages = 10;

        var pages = range.GetPages(totalPages);

        pages.Should().HaveCount(5);
        pages.Should().BeEquivalentTo(new[] { 2, 4, 6, 8, 10 });
    }

    [Fact]
    public void GetPages_WithCustomType_ShouldReturnSpecifiedPages()
    {
        var range = WatermarkPageRange.Parse("1-3,7,9-10");
        var totalPages = 10;

        var pages = range.GetPages(totalPages);

        pages.Should().HaveCount(6);
        pages.Should().BeEquivalentTo(new[] { 1, 2, 3, 7, 9, 10 });
    }

    [Fact]
    public void GetPages_WithCustomTypeBeyondDocumentLength_ShouldFilterOutOfRangePages()
    {
        var range = WatermarkPageRange.Parse("1-5,8-15");
        var totalPages = 10;

        var pages = range.GetPages(totalPages);

        pages.Should().HaveCount(8);
        pages.Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5, 8, 9, 10 });
    }

    [Fact]
    public void GetPages_WithZeroTotalPages_ShouldReturnEmptyArray()
    {
        var range = WatermarkPageRange.All;
        var totalPages = 0;

        var pages = range.GetPages(totalPages);

        pages.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WithWhitespaceInRange_ShouldHandleCorrectly()
    {
        var range = WatermarkPageRange.Parse("  1 - 3  ,  7  ,  9 - 10  ");

        range.SpecificPages.Should().BeEquivalentTo(new[] { 1, 2, 3, 7, 9, 10 });
    }

    [Fact]
    public void Parse_WithDuplicatePages_ShouldNotDuplicate()
    {
        var range = WatermarkPageRange.Parse("1,2,2,3,1-3");

        range.SpecificPages.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }
}
