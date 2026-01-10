using FluentAssertions;
using FluentPDF.Core.ErrorHandling;
using FluentPDF.Core.Models;
using FluentPDF.Core.Utilities;
using Xunit;

namespace FluentPDF.Core.Tests.Utilities;

/// <summary>
/// Comprehensive tests for PageRangeParser covering all valid formats and edge cases.
/// Tests validate parsing of single pages, ranges, multiple ranges, and error scenarios.
/// </summary>
public class PageRangeParserTests
{
    #region Valid Range Parsing Tests

    [Theory]
    [InlineData("1", 1, 1)]
    [InlineData("5", 5, 5)]
    [InlineData("10", 10, 10)]
    [InlineData("100", 100, 100)]
    [InlineData("999", 999, 999)]
    public void Parse_SinglePage_ShouldReturnSingleRange(string input, int expectedStart, int expectedEnd)
    {
        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].StartPage.Should().Be(expectedStart);
        result.Value[0].EndPage.Should().Be(expectedEnd);
        result.Value[0].PageCount.Should().Be(1);
    }

    [Theory]
    [InlineData("1-5", 1, 5, 5)]
    [InlineData("1-10", 1, 10, 10)]
    [InlineData("5-10", 5, 10, 6)]
    [InlineData("10-20", 10, 20, 11)]
    [InlineData("1-100", 1, 100, 100)]
    public void Parse_SimpleRange_ShouldReturnCorrectRange(string input, int expectedStart, int expectedEnd, int expectedCount)
    {
        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].StartPage.Should().Be(expectedStart);
        result.Value[0].EndPage.Should().Be(expectedEnd);
        result.Value[0].PageCount.Should().Be(expectedCount);
    }

    [Fact]
    public void Parse_TwoRanges_ShouldReturnBothRanges()
    {
        // Arrange
        const string input = "1-5, 10";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].StartPage.Should().Be(1);
        result.Value[0].EndPage.Should().Be(5);
        result.Value[1].StartPage.Should().Be(10);
        result.Value[1].EndPage.Should().Be(10);
    }

    [Fact]
    public void Parse_ThreeRanges_ShouldReturnAllRanges()
    {
        // Arrange
        const string input = "1-5, 10, 15-20";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].StartPage.Should().Be(1);
        result.Value[0].EndPage.Should().Be(5);
        result.Value[1].StartPage.Should().Be(10);
        result.Value[1].EndPage.Should().Be(10);
        result.Value[2].StartPage.Should().Be(15);
        result.Value[2].EndPage.Should().Be(20);
    }

    [Fact]
    public void Parse_ComplexMultipleRanges_ShouldReturnAllRanges()
    {
        // Arrange
        const string input = "1-3, 5, 7-10, 15, 20-25, 30";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(6);

        result.Value[0].Should().BeEquivalentTo(new PageRange { StartPage = 1, EndPage = 3 });
        result.Value[1].Should().BeEquivalentTo(new PageRange { StartPage = 5, EndPage = 5 });
        result.Value[2].Should().BeEquivalentTo(new PageRange { StartPage = 7, EndPage = 10 });
        result.Value[3].Should().BeEquivalentTo(new PageRange { StartPage = 15, EndPage = 15 });
        result.Value[4].Should().BeEquivalentTo(new PageRange { StartPage = 20, EndPage = 25 });
        result.Value[5].Should().BeEquivalentTo(new PageRange { StartPage = 30, EndPage = 30 });
    }

    [Theory]
    [InlineData("1-5,10")]
    [InlineData("1-5 , 10")]
    [InlineData(" 1-5 , 10 ")]
    [InlineData("  1-5  ,  10  ")]
    [InlineData("1 - 5, 10")]
    [InlineData("1- 5,10")]
    [InlineData(" 1 -5 , 10 ")]
    public void Parse_WithWhitespace_ShouldIgnoreWhitespace(string input)
    {
        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Should().BeEquivalentTo(new PageRange { StartPage = 1, EndPage = 5 });
        result.Value[1].Should().BeEquivalentTo(new PageRange { StartPage = 10, EndPage = 10 });
    }

    [Fact]
    public void Parse_SinglePageAsRange_ShouldWork()
    {
        // Arrange - Same start and end page
        const string input = "5-5";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].StartPage.Should().Be(5);
        result.Value[0].EndPage.Should().Be(5);
        result.Value[0].PageCount.Should().Be(1);
    }

    #endregion

    #region Null and Empty String Tests

    [Fact]
    public void Parse_NullString_ShouldReturnValidationError()
    {
        // Act
        var result = PageRangeParser.Parse(null!);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_VALIDATION_PAGE_RANGE_EMPTY");
        error.Category.Should().Be(ErrorCategory.Validation);
        error.Severity.Should().Be(ErrorSeverity.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("  \t  \n  ")]
    public void Parse_EmptyOrWhitespaceString_ShouldReturnValidationError(string input)
    {
        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_VALIDATION_PAGE_RANGE_EMPTY");
        error.Category.Should().Be(ErrorCategory.Validation);
    }

    [Theory]
    [InlineData(",")]
    [InlineData(",,")]
    [InlineData(",,,,,")]
    public void Parse_OnlyCommas_ShouldReturnValidationError(string input)
    {
        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        // Empty parts after split result in empty string error
        error!.ErrorCode.Should().Be("PDF_VALIDATION_PAGE_RANGE_EMPTY");
        error.Category.Should().Be(ErrorCategory.Validation);
    }

    [Fact]
    public void Parse_CommasWithWhitespace_ShouldReturnValidationError()
    {
        // Arrange - spaces between commas result in all empty parts after trimming
        const string input = " , , ";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        // After splitting and trimming, all parts are empty, so no valid parts found
        error!.ErrorCode.Should().Be("PDF_VALIDATION_PAGE_RANGE_NO_VALID_PARTS");
        error.Category.Should().Be(ErrorCategory.Validation);
    }

    #endregion

    #region Invalid Number Format Tests

    [Theory]
    [InlineData("abc")]
    [InlineData("1a5")]
    [InlineData("page5")]
    [InlineData("five")]
    [InlineData("1.5")]
    [InlineData("1,5")] // This is treated as two separate parts: "1" (valid) and "5" (valid)
    [InlineData("@#$")]
    public void Parse_InvalidNumberFormat_ShouldReturnValidationError(string input)
    {
        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        // Note: "1,5" would actually succeed as it parses as two separate pages
        if (input == "1,5")
        {
            result.IsSuccess.Should().BeTrue();
        }
        else
        {
            result.IsFailed.Should().BeTrue();
            result.Errors.Should().HaveCount(1);
            var error = result.Errors[0] as PdfError;
            error.Should().NotBeNull();
            error!.ErrorCode.Should().Be("PDF_VALIDATION_PAGE_RANGE_INVALID_NUMBER");
            error.Category.Should().Be(ErrorCategory.Validation);
        }
    }

    [Theory]
    [InlineData("1-abc")]
    [InlineData("abc-5")]
    [InlineData("abc-def")]
    [InlineData("1-5-10")]
    [InlineData("1.5-10")]
    [InlineData("1-10.5")]
    public void Parse_InvalidRangeFormat_ShouldReturnValidationError(string input)
    {
        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.Category.Should().Be(ErrorCategory.Validation);

        // Could be either INVALID_NUMBER or INVALID_FORMAT depending on the specific case
        error.ErrorCode.Should().Match(code =>
            code == "PDF_VALIDATION_PAGE_RANGE_INVALID_NUMBER" ||
            code == "PDF_VALIDATION_PAGE_RANGE_INVALID_FORMAT");
    }

    [Theory]
    [InlineData("-")]
    [InlineData("1-")]
    [InlineData("-5")]
    [InlineData(" - ")]
    [InlineData("1 -")]
    [InlineData("- 5")]
    public void Parse_MissingRangeNumbers_ShouldReturnValidationError(string input)
    {
        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.Category.Should().Be(ErrorCategory.Validation);
    }

    #endregion

    #region Negative and Zero Page Number Tests

    [Fact]
    public void Parse_ZeroPageNumber_ShouldReturnValidationError()
    {
        // Act
        var result = PageRangeParser.Parse("0");

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_VALIDATION_PAGE_RANGE_INVALID_NUMBER");
        error.Category.Should().Be(ErrorCategory.Validation);
        error.Message.Should().Contain("greater than 0");
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("-5")]
    [InlineData("-100")]
    public void Parse_NegativePageNumber_ShouldReturnValidationError(string input)
    {
        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.Category.Should().Be(ErrorCategory.Validation);
        // Negative numbers like "-5" have a dash, so they're treated as a range format
        // This results in INVALID_FORMAT error (empty start part)
        error.ErrorCode.Should().Be("PDF_VALIDATION_PAGE_RANGE_INVALID_FORMAT");
    }

    [Theory]
    [InlineData("0-5")]
    public void Parse_ZeroOrNegativeStartPage_ShouldReturnValidationError(string input)
    {
        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_VALIDATION_PAGE_RANGE_INVALID_NUMBER");
        error.Category.Should().Be(ErrorCategory.Validation);
        error.Message.Should().Contain("greater than 0");
    }

    [Theory]
    [InlineData("-1-5")]
    [InlineData("-5-10")]
    public void Parse_NegativeStartPage_ShouldReturnValidationError(string input)
    {
        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.Category.Should().Be(ErrorCategory.Validation);
        // Negative numbers like "-1-5" are parsed as empty start (before first dash) and then "-5" fails
        // This is an invalid format error
    }

    [Theory]
    [InlineData("1-0")]
    [InlineData("5--1")]
    [InlineData("10--5")]
    public void Parse_ZeroOrNegativeEndPage_ShouldReturnValidationError(string input)
    {
        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.Category.Should().Be(ErrorCategory.Validation);
    }

    #endregion

    #region Reverse Range Tests

    [Theory]
    [InlineData("5-1")]
    [InlineData("10-5")]
    [InlineData("100-1")]
    [InlineData("20-15")]
    public void Parse_ReverseRange_ShouldReturnValidationError(string input)
    {
        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_VALIDATION_PAGE_RANGE_REVERSE");
        error.Category.Should().Be(ErrorCategory.Validation);
        error.Message.Should().Contain("start page");
        error.Message.Should().Contain("greater than end page");
    }

    [Fact]
    public void Parse_ReverseRangeInMultipleRanges_ShouldReturnValidationError()
    {
        // Arrange - Valid ranges followed by an invalid reverse range
        const string input = "1-5, 10-5";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_VALIDATION_PAGE_RANGE_REVERSE");
    }

    #endregion

    #region Multiple Dashes Tests

    [Theory]
    [InlineData("1-5-10")]
    [InlineData("1--5")]
    [InlineData("1---5")]
    public void Parse_MultipleDashes_ShouldReturnValidationError(string input)
    {
        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.ErrorCode.Should().Be("PDF_VALIDATION_PAGE_RANGE_INVALID_FORMAT");
        error.Category.Should().Be(ErrorCategory.Validation);
        error.Message.Should().Contain("dashes");
    }

    #endregion

    #region Error Context Tests

    [Fact]
    public void Parse_InvalidInput_ShouldIncludeContextInError()
    {
        // Arrange
        const string input = "abc";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.Context.Should().ContainKey("Part");
        error.Context["Part"].Should().Be("abc");
    }

    [Fact]
    public void Parse_InvalidRangeInMultipleRanges_ShouldIncludeFullRangeStringInContext()
    {
        // Arrange
        const string input = "1-5, abc, 10-15";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.Context.Should().ContainKey("FullRangeString");
        error.Context["FullRangeString"].Should().Be(input);
    }

    [Fact]
    public void Parse_ReverseRange_ShouldIncludeStartAndEndPagesInContext()
    {
        // Arrange
        const string input = "10-5";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        var error = result.Errors[0] as PdfError;
        error.Should().NotBeNull();
        error!.Context.Should().ContainKey("StartPage");
        error!.Context.Should().ContainKey("EndPage");
        error.Context["StartPage"].Should().Be(10);
        error.Context["EndPage"].Should().Be(5);
    }

    #endregion

    #region Mixed Valid and Invalid Tests

    [Fact]
    public void Parse_FirstPartInvalid_ShouldFailImmediately()
    {
        // Arrange
        const string input = "abc, 1-5, 10";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public void Parse_MiddlePartInvalid_ShouldFailAtMiddlePart()
    {
        // Arrange
        const string input = "1-5, xyz, 10-15";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
    }

    [Fact]
    public void Parse_LastPartInvalid_ShouldFailAtLastPart()
    {
        // Arrange
        const string input = "1-5, 10, invalid";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(1);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Parse_VeryLargePageNumber_ShouldWork()
    {
        // Arrange
        const string input = "999999-1000000";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].StartPage.Should().Be(999999);
        result.Value[0].EndPage.Should().Be(1000000);
        result.Value[0].PageCount.Should().Be(2);
    }

    [Fact]
    public void Parse_ManyRanges_ShouldHandleAll()
    {
        // Arrange - 10 different ranges
        const string input = "1, 2, 3, 4, 5, 6, 7, 8, 9, 10";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(10);
        for (int i = 0; i < 10; i++)
        {
            result.Value[i].StartPage.Should().Be(i + 1);
            result.Value[i].EndPage.Should().Be(i + 1);
        }
    }

    [Fact]
    public void Parse_TrailingComma_ShouldIgnoreTrailingComma()
    {
        // Arrange
        const string input = "1-5, 10,";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_LeadingComma_ShouldIgnoreLeadingComma()
    {
        // Arrange
        const string input = ", 1-5, 10";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_MultipleConsecutiveCommas_ShouldIgnoreEmptyParts()
    {
        // Arrange
        const string input = "1-5,,, 10";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    #endregion

    #region Overlapping Ranges Tests
    // Note: The parser does not validate for overlapping ranges as per requirements.
    // It's the caller's responsibility to handle overlaps if needed.

    [Fact]
    public void Parse_OverlappingRanges_ShouldAcceptWithoutValidation()
    {
        // Arrange - Overlapping ranges are allowed
        const string input = "1-10, 5-15";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Should().BeEquivalentTo(new PageRange { StartPage = 1, EndPage = 10 });
        result.Value[1].Should().BeEquivalentTo(new PageRange { StartPage = 5, EndPage = 15 });
    }

    [Fact]
    public void Parse_DuplicateRanges_ShouldAcceptWithoutValidation()
    {
        // Arrange - Duplicate ranges are allowed
        const string input = "1-5, 1-5";

        // Act
        var result = PageRangeParser.Parse(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Should().BeEquivalentTo(new PageRange { StartPage = 1, EndPage = 5 });
        result.Value[1].Should().BeEquivalentTo(new PageRange { StartPage = 1, EndPage = 5 });
    }

    #endregion
}
