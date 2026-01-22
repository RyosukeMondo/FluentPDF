using FluentPDF.Rendering.Interop.Verification.Reports;
using FluentPDF.Rendering.Interop.Verification.Validators;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FluentPDF.Rendering.Tests.Interop.Verification;

public sealed class Utf16MarshalingValidatorTests
{
    private readonly Utf16MarshalingValidator _validator;

    public Utf16MarshalingValidatorTests()
    {
        var logger = NullLogger<Utf16MarshalingValidator>.Instance;
        _validator = new Utf16MarshalingValidator(logger);
    }

    [Fact]
    public void ValidatorName_ShouldReturnCorrectValue()
    {
        Assert.Equal("UTF-16 Marshaling Validator", _validator.ValidatorName);
    }

    [Fact]
    public void TargetArea_ShouldReturnCorrectValue()
    {
        Assert.Equal("UTF-16 Marshaling", _validator.TargetArea);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnValidationReport()
    {
        var report = await _validator.ValidateAsync();

        Assert.NotNull(report);
        Assert.Equal("UTF-16 Marshaling Validator", report.ValidatorName);
        Assert.Equal("UTF-16 Marshaling", report.TargetArea);
        Assert.NotNull(report.Summary);
        Assert.NotNull(report.ResultsByArea);
    }

    [Fact]
    public async Task ValidateAsync_ShouldIncludeAllAreas()
    {
        var report = await _validator.ValidateAsync();

        Assert.True(report.ResultsByArea.ContainsKey("Bookmarks"));
        Assert.True(report.ResultsByArea.ContainsKey("Text Search"));
        Assert.True(report.ResultsByArea.ContainsKey("Form Fields"));
    }

    [Fact]
    public async Task ValidateAsync_ShouldHaveCorrectTestCount()
    {
        var report = await _validator.ValidateAsync();

        // 6 bookmark tests + 6 text search tests + 6 form field tests = 18 total
        Assert.Equal(18, report.Summary.TotalTests);
    }

    [Fact]
    public async Task ValidateBookmarkMarshalingAsync_ShouldTestEmoji()
    {
        var results = await _validator.ValidateBookmarkMarshalingAsync();

        var emojiTest = results.FirstOrDefault(r => r.TestName.Contains("Emoji"));
        Assert.NotNull(emojiTest);
        Assert.True(emojiTest.Passed, "Emoji test should pass");
        Assert.NotNull(emojiTest.Context);
    }

    [Fact]
    public async Task ValidateBookmarkMarshalingAsync_ShouldTestNullCharacters()
    {
        var results = await _validator.ValidateBookmarkMarshalingAsync();

        var nullTest = results.FirstOrDefault(r => r.TestName.Contains("Null characters"));
        Assert.NotNull(nullTest);
        Assert.NotNull(nullTest.Context);
    }

    [Fact]
    public async Task ValidateBookmarkMarshalingAsync_ShouldTestCombiningDiacritics()
    {
        var results = await _validator.ValidateBookmarkMarshalingAsync();

        var diacriticsTest = results.FirstOrDefault(r => r.TestName.Contains("Combining diacritics"));
        Assert.NotNull(diacriticsTest);
        Assert.True(diacriticsTest.Passed, "Combining diacritics test should pass");
    }

    [Fact]
    public async Task ValidateBookmarkMarshalingAsync_ShouldTestNewlines()
    {
        var results = await _validator.ValidateBookmarkMarshalingAsync();

        var newlineTest = results.FirstOrDefault(r => r.TestName.Contains("CRLF vs LF"));
        Assert.NotNull(newlineTest);
        Assert.True(newlineTest.Passed, "Newline test should pass");
    }

    [Fact]
    public async Task ValidateBookmarkMarshalingAsync_ShouldTestEmptyString()
    {
        var results = await _validator.ValidateBookmarkMarshalingAsync();

        var emptyTest = results.FirstOrDefault(r => r.TestName.Contains("Empty string"));
        Assert.NotNull(emptyTest);
        Assert.True(emptyTest.Passed, "Empty string test should pass");
    }

    [Fact]
    public async Task ValidateBookmarkMarshalingAsync_ShouldTestLargeString()
    {
        var results = await _validator.ValidateBookmarkMarshalingAsync();

        var largeTest = results.FirstOrDefault(r => r.TestName.Contains("Maximum length"));
        Assert.NotNull(largeTest);
        Assert.True(largeTest.Passed, "Large string test should pass");
        Assert.NotNull(largeTest.Context);
    }

    [Fact]
    public async Task ValidateBookmarkMarshalingAsync_ShouldReturn6Tests()
    {
        var results = await _validator.ValidateBookmarkMarshalingAsync();

        Assert.Equal(6, results.Count);
    }

    [Fact]
    public async Task ValidateTextSearchMarshalingAsync_ShouldTestEmoji()
    {
        var results = await _validator.ValidateTextSearchMarshalingAsync();

        var emojiTest = results.FirstOrDefault(r => r.TestName.Contains("Emoji"));
        Assert.NotNull(emojiTest);
        Assert.True(emojiTest.Passed, "Emoji search test should pass");
    }

    [Fact]
    public async Task ValidateTextSearchMarshalingAsync_ShouldTestNullCharacter()
    {
        var results = await _validator.ValidateTextSearchMarshalingAsync();

        var nullTest = results.FirstOrDefault(r => r.TestName.Contains("Null character"));
        Assert.NotNull(nullTest);
    }

    [Fact]
    public async Task ValidateTextSearchMarshalingAsync_ShouldTestCombiningDiacritics()
    {
        var results = await _validator.ValidateTextSearchMarshalingAsync();

        var diacriticsTest = results.FirstOrDefault(r => r.TestName.Contains("Combining diacritics"));
        Assert.NotNull(diacriticsTest);
        Assert.True(diacriticsTest.Passed, "Combining diacritics search test should pass");
    }

    [Fact]
    public async Task ValidateTextSearchMarshalingAsync_ShouldTestNewlines()
    {
        var results = await _validator.ValidateTextSearchMarshalingAsync();

        var newlineTest = results.FirstOrDefault(r => r.TestName.Contains("Newline"));
        Assert.NotNull(newlineTest);
        Assert.True(newlineTest.Passed, "Newline search test should pass");
    }

    [Fact]
    public async Task ValidateTextSearchMarshalingAsync_ShouldHandleEmptyQuery()
    {
        var results = await _validator.ValidateTextSearchMarshalingAsync();

        var emptyTest = results.FirstOrDefault(r => r.TestName.Contains("Empty query"));
        Assert.NotNull(emptyTest);
        Assert.True(emptyTest.Passed, "Empty query should be handled gracefully");
        Assert.Contains("handled gracefully", emptyTest.Message);
    }

    [Fact]
    public async Task ValidateTextSearchMarshalingAsync_ShouldTestLongQuery()
    {
        var results = await _validator.ValidateTextSearchMarshalingAsync();

        var longTest = results.FirstOrDefault(r => r.TestName.Contains("Long query"));
        Assert.NotNull(longTest);
        Assert.True(longTest.Passed, "Long query test should pass");
    }

    [Fact]
    public async Task ValidateTextSearchMarshalingAsync_ShouldReturn6Tests()
    {
        var results = await _validator.ValidateTextSearchMarshalingAsync();

        Assert.Equal(6, results.Count);
    }

    [Fact]
    public async Task ValidateFormFieldMarshalingAsync_ShouldTestEmoji()
    {
        var results = await _validator.ValidateFormFieldMarshalingAsync();

        var emojiTest = results.FirstOrDefault(r => r.TestName.Contains("Emoji"));
        Assert.NotNull(emojiTest);
        Assert.True(emojiTest.Passed, "Emoji form field test should pass");
    }

    [Fact]
    public async Task ValidateFormFieldMarshalingAsync_ShouldTestNullCharacters()
    {
        var results = await _validator.ValidateFormFieldMarshalingAsync();

        var nullTest = results.FirstOrDefault(r => r.TestName.Contains("Null characters"));
        Assert.NotNull(nullTest);
    }

    [Fact]
    public async Task ValidateFormFieldMarshalingAsync_ShouldTestCombiningDiacritics()
    {
        var results = await _validator.ValidateFormFieldMarshalingAsync();

        var diacriticsTest = results.FirstOrDefault(r => r.TestName.Contains("Combining diacritics"));
        Assert.NotNull(diacriticsTest);
        Assert.True(diacriticsTest.Passed, "Combining diacritics form field test should pass");
    }

    [Fact]
    public async Task ValidateFormFieldMarshalingAsync_ShouldTestSpecialCharacters()
    {
        var results = await _validator.ValidateFormFieldMarshalingAsync();

        var specialTest = results.FirstOrDefault(r => r.TestName.Contains("Special characters"));
        Assert.NotNull(specialTest);
        Assert.True(specialTest.Passed, "Special characters (CJK) test should pass");
    }

    [Fact]
    public async Task ValidateFormFieldMarshalingAsync_ShouldTestEmptyFieldName()
    {
        var results = await _validator.ValidateFormFieldMarshalingAsync();

        var emptyTest = results.FirstOrDefault(r => r.TestName.Contains("Empty field"));
        Assert.NotNull(emptyTest);
        Assert.True(emptyTest.Passed, "Empty field name test should pass");
    }

    [Fact]
    public async Task ValidateFormFieldMarshalingAsync_ShouldTestLongFieldName()
    {
        var results = await _validator.ValidateFormFieldMarshalingAsync();

        var longTest = results.FirstOrDefault(r => r.TestName.Contains("Long field"));
        Assert.NotNull(longTest);
        Assert.True(longTest.Passed, "Long field name test should pass");
    }

    [Fact]
    public async Task ValidateFormFieldMarshalingAsync_ShouldReturn6Tests()
    {
        var results = await _validator.ValidateFormFieldMarshalingAsync();

        Assert.Equal(6, results.Count);
    }

    [Fact]
    public async Task ValidationResults_ShouldIncludeContext()
    {
        var results = await _validator.ValidateBookmarkMarshalingAsync();

        foreach (var result in results)
        {
            Assert.NotNull(result.Context);
            Assert.True(result.Context.Count > 0, "Context should contain data");
        }
    }

    [Fact]
    public async Task ValidationResults_ShouldIncludeDocumentationUrl_OnFailure()
    {
        // This test verifies that failed validations include documentation URLs
        // Since all current tests pass, we verify the structure is correct
        var results = await _validator.ValidateBookmarkMarshalingAsync();

        Assert.All(results, result =>
        {
            if (!result.Passed)
            {
                Assert.NotNull(result.DocumentationUrl);
            }
        });
    }

    [Fact]
    public async Task ValidateAsync_ShouldCalculateCorrectSummary()
    {
        var report = await _validator.ValidateAsync();

        Assert.True(report.Summary.TotalTests >= 18, "Should have at least 18 tests");
        Assert.Equal(
            report.Summary.TotalTests,
            report.Summary.PassedCount + report.Summary.FailedCount + report.Summary.WarningCount);
    }

    [Fact]
    public async Task ValidateAsync_WithCancellation_ShouldRespectToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await _validator.ValidateAsync(cts.Token));
    }

    [Fact]
    public async Task ValidateAsync_ShouldBeThreadSafe()
    {
        // Run multiple validations concurrently
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _validator.ValidateAsync())
            .ToArray();

        var reports = await Task.WhenAll(tasks);

        // All reports should be valid
        Assert.All(reports, report =>
        {
            Assert.NotNull(report);
            Assert.Equal(18, report.Summary.TotalTests);
        });
    }
}
