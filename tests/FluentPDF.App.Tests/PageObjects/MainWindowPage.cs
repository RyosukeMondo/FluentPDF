using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;

namespace FluentPDF.App.Tests.PageObjects;

/// <summary>
/// Page object representing the main window of FluentPDF application.
/// Provides high-level methods for interacting with main window UI elements.
/// </summary>
public class MainWindowPage
{
    private readonly Window _mainWindow;

    /// <summary>
    /// Initializes a new instance of MainWindowPage.
    /// </summary>
    /// <param name="mainWindow">The main window automation element.</param>
    public MainWindowPage(Window mainWindow)
    {
        _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));
    }

    /// <summary>
    /// Opens a PDF file using the File menu.
    /// </summary>
    /// <param name="filePath">Path to the PDF file to open.</param>
    /// <param name="timeoutMs">Timeout in milliseconds to wait for file dialog.</param>
    public void OpenFile(string filePath, int timeoutMs = 5000)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        // Click File > Open menu item
        var fileMenu = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("FileMenuBarItem"));
        if (fileMenu == null)
        {
            throw new InvalidOperationException("File menu not found. Ensure AutomationId is set on FileMenuBarItem.");
        }

        fileMenu.Click();
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(200));

        var openMenuItem = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("OpenMenuItem"));
        if (openMenuItem == null)
        {
            throw new InvalidOperationException("Open menu item not found. Ensure AutomationId is set on OpenMenuItem.");
        }

        openMenuItem.Click();
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));

        // Type file path in the file dialog
        // Note: This is a simplified approach. In production, you'd interact with the actual file dialog
        Keyboard.Type(filePath);
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(200));
        Keyboard.Type(VirtualKeyShort.RETURN);
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(1000));
    }

    /// <summary>
    /// Gets the current page number from the PDF viewer.
    /// </summary>
    /// <returns>The current page number, or -1 if not found.</returns>
    public int GetCurrentPageNumber()
    {
        var pageNumberElement = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("CurrentPageNumber"));
        if (pageNumberElement == null)
        {
            return -1;
        }

        var text = pageNumberElement.Name ?? pageNumberElement.AsTextBox()?.Text;
        if (int.TryParse(text, out var pageNumber))
        {
            return pageNumber;
        }

        return -1;
    }

    /// <summary>
    /// Navigates to a specific page in the PDF.
    /// </summary>
    /// <param name="pageNumber">The page number to navigate to (1-based).</param>
    public void NavigateToPage(int pageNumber)
    {
        if (pageNumber < 1)
        {
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));
        }

        var pageNavigator = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("PageNavigator"));
        if (pageNavigator == null)
        {
            throw new InvalidOperationException("Page navigator not found. Ensure AutomationId is set on PageNavigator.");
        }

        // Click on the page navigator input
        pageNavigator.Click();
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(200));

        // Select all and type new page number
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_A);
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(100));
        Keyboard.Type(pageNumber.ToString());
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(100));
        Keyboard.Type(VirtualKeyShort.RETURN);
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Clicks the next page button.
    /// </summary>
    public void NextPage()
    {
        var nextButton = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("NextPageButton"));
        if (nextButton == null)
        {
            throw new InvalidOperationException("Next page button not found. Ensure AutomationId is set on NextPageButton.");
        }

        nextButton.Click();
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Clicks the previous page button.
    /// </summary>
    public void PreviousPage()
    {
        var previousButton = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("PreviousPageButton"));
        if (previousButton == null)
        {
            throw new InvalidOperationException("Previous page button not found. Ensure AutomationId is set on PreviousPageButton.");
        }

        previousButton.Click();
        Wait.UntilInputIsProcessed(TimeSpan.FromMilliseconds(500));
    }

    /// <summary>
    /// Verifies that a PDF is currently loaded by checking if tabs are visible.
    /// </summary>
    /// <returns>True if a PDF appears to be loaded, false otherwise.</returns>
    public bool IsPdfLoaded()
    {
        var tabView = _mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("MainTabView"));
        if (tabView == null)
        {
            return false;
        }

        // Check if there are any tab items
        var tabs = tabView.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem));
        return tabs.Length > 0;
    }

    /// <summary>
    /// Gets the title of the main window.
    /// </summary>
    /// <returns>The window title.</returns>
    public string GetWindowTitle()
    {
        return _mainWindow.Title;
    }
}
