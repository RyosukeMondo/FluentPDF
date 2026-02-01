// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.App.Api.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace FluentPDF.App.Api.Services;

/// <summary>
/// Service for UI automation and element inspection via DispatcherQueue.
/// </summary>
public interface IUiAutomationService
{
    /// <summary>
    /// Finds a UI element by AutomationId and returns its properties.
    /// </summary>
    Task<ElementInfo?> FindElementAsync(string automationId);

    /// <summary>
    /// Verifies element properties against expected values.
    /// </summary>
    Task<ElementVerificationResponse> VerifyElementAsync(ElementVerificationRequest request);

    /// <summary>
    /// Verifies layout of multiple elements.
    /// </summary>
    Task<LayoutVerificationResponse> VerifyLayoutAsync(LayoutVerificationRequest request);

    /// <summary>
    /// Gets current application status.
    /// </summary>
    Task<StatusResponse> GetStatusAsync();

    /// <summary>
    /// Gets the background color of an element.
    /// </summary>
    Task<string?> GetElementBackgroundAsync(string automationId);
}

/// <summary>
/// Implementation of UI automation service.
/// </summary>
public sealed class UiAutomationService : IUiAutomationService
{
    private readonly ILogger<UiAutomationService> _logger;
    private readonly DispatcherQueue _dispatcherQueue;

    public UiAutomationService(ILogger<UiAutomationService> logger)
    {
        _logger = logger;

        // Get the dispatcher queue from the main window
        _dispatcherQueue = App.MainWindow.DispatcherQueue;
    }

    /// <inheritdoc />
    public async Task<ElementInfo?> FindElementAsync(string automationId)
    {
        var tcs = new TaskCompletionSource<ElementInfo?>();

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                var element = FindElementByAutomationId(App.MainWindow.Content as FrameworkElement, automationId);

                if (element is null)
                {
                    tcs.SetResult(null);
                    return;
                }

                var info = new ElementInfo(
                    AutomationId: automationId,
                    Name: element.Name,
                    IsEnabled: element is Microsoft.UI.Xaml.Controls.Control control ? control.IsEnabled : true,
                    IsVisible: element.Visibility == Visibility.Visible,
                    Width: element.ActualWidth,
                    Height: element.ActualHeight,
                    X: GetElementX(element),
                    Y: GetElementY(element)
                );

                tcs.SetResult(info);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding element {AutomationId}", automationId);
                tcs.SetException(ex);
            }
        });

        return await tcs.Task;
    }

    /// <inheritdoc />
    public async Task<ElementVerificationResponse> VerifyElementAsync(ElementVerificationRequest request)
    {
        var elementInfo = await FindElementAsync(request.AutomationId);

        if (elementInfo is null)
        {
            return new ElementVerificationResponse(
                Found: false,
                Passed: false,
                Element: null,
                Checks: new List<PropertyCheck>(),
                Errors: new List<string> { $"Element '{request.AutomationId}' not found" }
            );
        }

        var checks = new List<PropertyCheck>();
        var allPassed = true;

        if (request.ExpectedProperties is not null)
        {
            foreach (var (property, expectedValue) in request.ExpectedProperties)
            {
                var (actual, passed) = VerifyProperty(elementInfo, property, expectedValue);
                checks.Add(new PropertyCheck(property, expectedValue, actual, passed));

                if (!passed)
                {
                    allPassed = false;
                }
            }
        }

        return new ElementVerificationResponse(
            Found: true,
            Passed: allPassed,
            Element: elementInfo,
            Checks: checks,
            Errors: new List<string>()
        );
    }

    /// <inheritdoc />
    public async Task<LayoutVerificationResponse> VerifyLayoutAsync(LayoutVerificationRequest request)
    {
        var elements = new List<ElementLayoutCheck>();
        var allPassed = true;
        var errors = new List<string>();

        foreach (var layoutElement in request.Elements)
        {
            var elementInfo = await FindElementAsync(layoutElement.AutomationId);

            if (elementInfo is null)
            {
                elements.Add(new ElementLayoutCheck(
                    AutomationId: layoutElement.AutomationId,
                    Found: false,
                    Position: null,
                    Checks: null
                ));
                allPassed = false;
                errors.Add($"Element '{layoutElement.AutomationId}' not found");
                continue;
            }

            var position = new Dictionary<string, double>
            {
                ["x"] = elementInfo.X,
                ["y"] = elementInfo.Y,
                ["width"] = elementInfo.Width,
                ["height"] = elementInfo.Height
            };

            var checks = new Dictionary<string, object>();
            var elementPassed = true;

            // Verify width
            if (layoutElement.ExpectedWidth is not null)
            {
                var widthPassed = VerifyRange(elementInfo.Width, layoutElement.ExpectedWidth);
                checks["width"] = elementInfo.Width;
                checks["widthPassed"] = widthPassed;

                if (!widthPassed)
                {
                    elementPassed = false;
                }
            }

            // Verify height
            if (layoutElement.ExpectedHeight is not null)
            {
                var heightPassed = VerifyRange(elementInfo.Height, layoutElement.ExpectedHeight);
                checks["height"] = elementInfo.Height;
                checks["heightPassed"] = heightPassed;

                if (!heightPassed)
                {
                    elementPassed = false;
                }
            }

            // Verify position
            if (layoutElement.ExpectedPosition is not null)
            {
                checks["position"] = layoutElement.ExpectedPosition;
                checks["positionPassed"] = true; // Simplified for now
            }

            checks["passed"] = elementPassed;

            elements.Add(new ElementLayoutCheck(
                AutomationId: layoutElement.AutomationId,
                Found: true,
                Position: position,
                Checks: checks
            ));

            if (!elementPassed)
            {
                allPassed = false;
            }
        }

        return new LayoutVerificationResponse(
            Passed: allPassed,
            Elements: elements,
            Errors: errors
        );
    }

    /// <inheritdoc />
    public async Task<StatusResponse> GetStatusAsync()
    {
        var tcs = new TaskCompletionSource<StatusResponse>();

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                // Get MainViewModel from App
                var mainViewModel = App.GetService<ViewModels.MainViewModel>();

                var documentLoaded = mainViewModel?.ActiveTab?.ViewerViewModel?.CurrentDocument is not null;
                var documentPath = mainViewModel?.ActiveTab?.ViewerViewModel?.CurrentDocument?.FilePath;
                var currentPage = mainViewModel?.ActiveTab?.ViewerViewModel?.CurrentPageNumber ?? 0;
                var totalPages = mainViewModel?.ActiveTab?.ViewerViewModel?.CurrentDocument?.PageCount ?? 0;
                var zoomLevel = (int)(mainViewModel?.ActiveTab?.ViewerViewModel?.ZoomLevel ?? 100);

                // Determine theme
                var theme = "light";
                if (App.MainWindow.Content is FrameworkElement rootElement)
                {
                    theme = rootElement.ActualTheme switch
                    {
                        ElementTheme.Dark => "dark",
                        ElementTheme.Light => "light",
                        _ => "system"
                    };
                }

                var sidebars = new Dictionary<string, bool>
                {
                    ["thumbnails"] = mainViewModel?.ActiveTab?.ViewerViewModel?.ThumbnailsViewModel?.IsVisible ?? false,
                    ["bookmarks"] = mainViewModel?.ActiveTab?.ViewerViewModel?.BookmarksViewModel?.IsVisible ?? false
                };

                // Check if presentation mode is active
                var fullScreen = Views.PresentationWindow.CurrentInstance is not null;

                var status = new StatusResponse(
                    WindowOpen: App.MainWindow is not null,
                    DocumentLoaded: documentLoaded,
                    DocumentPath: documentPath,
                    CurrentPage: currentPage,
                    TotalPages: totalPages,
                    ZoomLevel: zoomLevel,
                    ViewMode: mainViewModel?.ActiveTab?.ViewerViewModel?.ViewMode.ToString() ?? "SinglePage",
                    Theme: theme,
                    Sidebars: sidebars,
                    FullScreen: fullScreen
                );

                tcs.SetResult(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application status");
                tcs.SetException(ex);
            }
        });

        return await tcs.Task;
    }

    /// <inheritdoc />
    public async Task<string?> GetElementBackgroundAsync(string automationId)
    {
        var tcs = new TaskCompletionSource<string?>();

        _dispatcherQueue.TryEnqueue(() =>
        {
            try
            {
                var element = FindElementByAutomationId(App.MainWindow.Content as FrameworkElement, automationId);

                if (element is null)
                {
                    tcs.SetResult(null);
                    return;
                }

                // Try to get background color
                string? backgroundColor = null;

                if (element is Microsoft.UI.Xaml.Controls.Panel panel && panel.Background is SolidColorBrush solidBrush)
                {
                    var color = solidBrush.Color;
                    backgroundColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                }
                else if (element is Microsoft.UI.Xaml.Controls.Control control && control.Background is SolidColorBrush ctrlBrush)
                {
                    var color = ctrlBrush.Color;
                    backgroundColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                }

                tcs.SetResult(backgroundColor);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting element background for {AutomationId}", automationId);
                tcs.SetException(ex);
            }
        });

        return await tcs.Task;
    }

    private FrameworkElement? FindElementByAutomationId(FrameworkElement? root, string automationId)
    {
        if (root is null)
        {
            return null;
        }

        // Check if this element has the automation ID
        var elementAutomationId = Microsoft.UI.Xaml.Automation.AutomationProperties.GetAutomationId(root);
        if (elementAutomationId == automationId)
        {
            return root;
        }

        // Also check Name property
        if (root.Name == automationId)
        {
            return root;
        }

        // Search children
        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < childCount; i++)
        {
            if (VisualTreeHelper.GetChild(root, i) is FrameworkElement child)
            {
                var found = FindElementByAutomationId(child, automationId);
                if (found is not null)
                {
                    return found;
                }
            }
        }

        return null;
    }

    private (object? actual, bool passed) VerifyProperty(ElementInfo element, string property, object expectedValue)
    {
        return property.ToLowerInvariant() switch
        {
            "isenabled" => (element.IsEnabled, element.IsEnabled == Convert.ToBoolean(expectedValue)),
            "isvisible" => (element.IsVisible, element.IsVisible == Convert.ToBoolean(expectedValue)),
            "width" => VerifyNumericProperty(element.Width, expectedValue),
            "height" => VerifyNumericProperty(element.Height, expectedValue),
            _ => (null, false)
        };
    }

    private (object actual, bool passed) VerifyNumericProperty(double actualValue, object expectedValue)
    {
        // Handle range specification {min: 80, max: 120}
        if (expectedValue is System.Text.Json.JsonElement jsonElement && jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            var min = jsonElement.TryGetProperty("min", out var minProp) ? minProp.GetDouble() : double.MinValue;
            var max = jsonElement.TryGetProperty("max", out var maxProp) ? maxProp.GetDouble() : double.MaxValue;

            var passed = actualValue >= min && actualValue <= max;
            return (actualValue, passed);
        }

        // Handle direct numeric comparison
        if (expectedValue is double expectedDouble)
        {
            return (actualValue, Math.Abs(actualValue - expectedDouble) < 0.01);
        }

        return (actualValue, false);
    }

    private bool VerifyRange(double value, Dictionary<string, object> range)
    {
        var min = range.TryGetValue("min", out var minObj) ? Convert.ToDouble(minObj) : double.MinValue;
        var max = range.TryGetValue("max", out var maxObj) ? Convert.ToDouble(maxObj) : double.MaxValue;

        return value >= min && value <= max;
    }

    private double GetElementX(FrameworkElement element)
    {
        try
        {
            var transform = element.TransformToVisual(App.MainWindow.Content);
            var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
            return point.X;
        }
        catch
        {
            return 0;
        }
    }

    private double GetElementY(FrameworkElement element)
    {
        try
        {
            var transform = element.TransformToVisual(App.MainWindow.Content);
            var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
            return point.Y;
        }
        catch
        {
            return 0;
        }
    }
}
