using FlaUI.Core;
using FluentAssertions;
using FluentPDF.App.Tests.PageObjects;

namespace FluentPDF.App.Tests.E2E;

/// <summary>
/// End-to-end tests for page navigation functionality using FlaUI.
/// Validates that users can navigate between pages in a PDF document.
/// </summary>
[Trait("Category", "E2E")]
public class NavigationTests : FlaUITestBase
{
    private const string MultiPagePdfPath = "../../../../Fixtures/multi-page.pdf";

    /// <summary>
    /// Tests that the Next Page button navigates to the next page.
    /// </summary>
    [Fact(Skip = "E2E test requires built application - run manually on Windows")]
    public void NextPageButton_ShouldNavigateToNextPage()
    {
        // Arrange
        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, MultiPagePdfPath));
        if (!File.Exists(fullPdfPath))
        {
            throw new FileNotFoundException($"Multi-page PDF not found at {fullPdfPath}");
        }

        Window? mainWindow = null;

        try
        {
            // Act: Launch the application and open PDF
            mainWindow = LaunchApp(waitTimeoutMs: 10000);
            var mainWindowPage = new MainWindowPage(mainWindow);
            mainWindowPage.OpenFile(fullPdfPath);
            Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(2));

            // Verify PDF is loaded and on page 1
            var isPdfLoaded = mainWindowPage.IsPdfLoaded();
            isPdfLoaded.Should().BeTrue("PDF should be loaded");

            var initialPageNumber = mainWindowPage.GetCurrentPageNumber();
            initialPageNumber.Should().Be(1, "should start on page 1");

            // Act: Navigate to next page
            mainWindowPage.NextPage();
            Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));

            // Assert: Page number should increment
            var newPageNumber = mainWindowPage.GetCurrentPageNumber();
            newPageNumber.Should().Be(2, "should navigate to page 2");

            // Capture screenshot for visual verification
            var screenshotPath = CaptureScreenshot(mainWindow, "NavigationTests_NextPage");
            Console.WriteLine($"Screenshot saved: {screenshotPath}");
        }
        catch (Exception ex)
        {
            // Capture screenshot on failure
            if (mainWindow != null)
            {
                CaptureScreenshotOnFailure(mainWindow, "NavigationTests_NextPage", ex);
            }
            throw;
        }
        finally
        {
            // Cleanup: Close the application
            CloseApp();
        }
    }

    /// <summary>
    /// Tests that the Previous Page button navigates to the previous page.
    /// </summary>
    [Fact(Skip = "E2E test requires built application - run manually on Windows")]
    public void PreviousPageButton_ShouldNavigateToPreviousPage()
    {
        // Arrange
        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, MultiPagePdfPath));
        if (!File.Exists(fullPdfPath))
        {
            throw new FileNotFoundException($"Multi-page PDF not found at {fullPdfPath}");
        }

        Window? mainWindow = null;

        try
        {
            // Act: Launch the application and open PDF
            mainWindow = LaunchApp(waitTimeoutMs: 10000);
            var mainWindowPage = new MainWindowPage(mainWindow);
            mainWindowPage.OpenFile(fullPdfPath);
            Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(2));

            // Navigate to page 2 first
            mainWindowPage.NextPage();
            Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));

            var currentPageNumber = mainWindowPage.GetCurrentPageNumber();
            currentPageNumber.Should().Be(2, "should be on page 2");

            // Act: Navigate to previous page
            mainWindowPage.PreviousPage();
            Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));

            // Assert: Page number should decrement
            var newPageNumber = mainWindowPage.GetCurrentPageNumber();
            newPageNumber.Should().Be(1, "should navigate back to page 1");

            // Capture screenshot for visual verification
            var screenshotPath = CaptureScreenshot(mainWindow, "NavigationTests_PreviousPage");
            Console.WriteLine($"Screenshot saved: {screenshotPath}");
        }
        catch (Exception ex)
        {
            // Capture screenshot on failure
            if (mainWindow != null)
            {
                CaptureScreenshotOnFailure(mainWindow, "NavigationTests_PreviousPage", ex);
            }
            throw;
        }
        finally
        {
            // Cleanup: Close the application
            CloseApp();
        }
    }

    /// <summary>
    /// Tests that navigating to a specific page number works correctly.
    /// </summary>
    [Fact(Skip = "E2E test requires built application - run manually on Windows")]
    public void NavigateToPage_ShouldGoToSpecificPage()
    {
        // Arrange
        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, MultiPagePdfPath));
        if (!File.Exists(fullPdfPath))
        {
            throw new FileNotFoundException($"Multi-page PDF not found at {fullPdfPath}");
        }

        Window? mainWindow = null;

        try
        {
            // Act: Launch the application and open PDF
            mainWindow = LaunchApp(waitTimeoutMs: 10000);
            var mainWindowPage = new MainWindowPage(mainWindow);
            mainWindowPage.OpenFile(fullPdfPath);
            Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(2));

            // Act: Navigate to page 3
            var targetPage = 3;
            mainWindowPage.NavigateToPage(targetPage);
            Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));

            // Assert: Should be on page 3
            var currentPageNumber = mainWindowPage.GetCurrentPageNumber();
            currentPageNumber.Should().Be(targetPage, $"should navigate to page {targetPage}");

            // Capture screenshot for visual verification
            var screenshotPath = CaptureScreenshot(mainWindow, "NavigationTests_NavigateToPage");
            Console.WriteLine($"Screenshot saved: {screenshotPath}");
        }
        catch (Exception ex)
        {
            // Capture screenshot on failure
            if (mainWindow != null)
            {
                CaptureScreenshotOnFailure(mainWindow, "NavigationTests_NavigateToPage", ex);
            }
            throw;
        }
        finally
        {
            // Cleanup: Close the application
            CloseApp();
        }
    }

    /// <summary>
    /// Tests that sequential navigation through multiple pages works correctly.
    /// </summary>
    [Fact(Skip = "E2E test requires built application - run manually on Windows")]
    public void SequentialNavigation_ShouldWorkCorrectly()
    {
        // Arrange
        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, MultiPagePdfPath));
        if (!File.Exists(fullPdfPath))
        {
            throw new FileNotFoundException($"Multi-page PDF not found at {fullPdfPath}");
        }

        Window? mainWindow = null;

        try
        {
            // Act: Launch the application and open PDF
            mainWindow = LaunchApp(waitTimeoutMs: 10000);
            var mainWindowPage = new MainWindowPage(mainWindow);
            mainWindowPage.OpenFile(fullPdfPath);
            Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(2));

            // Act & Assert: Navigate through multiple pages
            var currentPageNumber = mainWindowPage.GetCurrentPageNumber();
            currentPageNumber.Should().Be(1, "should start on page 1");

            // Next to page 2
            mainWindowPage.NextPage();
            Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));
            currentPageNumber = mainWindowPage.GetCurrentPageNumber();
            currentPageNumber.Should().Be(2, "should be on page 2");

            // Next to page 3
            mainWindowPage.NextPage();
            Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));
            currentPageNumber = mainWindowPage.GetCurrentPageNumber();
            currentPageNumber.Should().Be(3, "should be on page 3");

            // Previous to page 2
            mainWindowPage.PreviousPage();
            Wait.UntilInputIsProcessed(TimeSpan.FromSeconds(1));
            currentPageNumber = mainWindowPage.GetCurrentPageNumber();
            currentPageNumber.Should().Be(2, "should be back on page 2");

            // Capture screenshot for visual verification
            var screenshotPath = CaptureScreenshot(mainWindow, "NavigationTests_Sequential");
            Console.WriteLine($"Screenshot saved: {screenshotPath}");
        }
        catch (Exception ex)
        {
            // Capture screenshot on failure
            if (mainWindow != null)
            {
                CaptureScreenshotOnFailure(mainWindow, "NavigationTests_Sequential", ex);
            }
            throw;
        }
        finally
        {
            // Cleanup: Close the application
            CloseApp();
        }
    }
}
