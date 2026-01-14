using System;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;

namespace FluentPDF.App.Services;

/// <summary>
/// Default implementation of INavigationService using WinUI Frame.
/// </summary>
/// <remarks>
/// This service wraps the WinUI Frame to enable testable navigation.
/// The Frame reference must be set via the Frame property before navigation can occur.
/// </remarks>
public sealed class NavigationService : INavigationService
{
    private readonly ILogger<NavigationService> _logger;
    private Frame? _frame;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationService"/> class.
    /// </summary>
    /// <param name="logger">Logger for navigation events.</param>
    public NavigationService(ILogger<NavigationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets or sets the Frame used for navigation.
    /// </summary>
    /// <remarks>
    /// This property must be set before any navigation operations can occur.
    /// Typically set during application initialization or window creation.
    /// </remarks>
    public Frame? Frame
    {
        get => _frame;
        set
        {
            _frame = value;
            _logger.LogDebug("Navigation frame set: {FrameSet}", value is not null);
        }
    }

    /// <inheritdoc/>
    public bool CanGoBack => _frame?.CanGoBack ?? false;

    /// <inheritdoc/>
    public void NavigateTo(Type pageType, object? parameter = null)
    {
        if (pageType is null)
        {
            throw new ArgumentNullException(nameof(pageType));
        }

        if (_frame is null)
        {
            var ex = new InvalidOperationException("Navigation frame is not initialized. Set the Frame property before navigating.");
            _logger.LogError(ex, "Navigation failed: frame not initialized");
            throw ex;
        }

        _logger.LogInformation("Navigating to {PageType} with parameter: {HasParameter}", pageType.Name, parameter is not null);

        var success = _frame.Navigate(pageType, parameter);

        if (!success)
        {
            var ex = new InvalidOperationException($"Navigation to {pageType.Name} failed.");
            _logger.LogError(ex, "Navigation failed for {PageType}", pageType.Name);
            throw ex;
        }

        _logger.LogDebug("Navigation successful to {PageType}", pageType.Name);
    }

    /// <inheritdoc/>
    public void GoBack()
    {
        if (!CanGoBack)
        {
            var ex = new InvalidOperationException("Cannot navigate back. No pages in back stack.");
            _logger.LogError(ex, "GoBack failed: no pages in back stack");
            throw ex;
        }

        _logger.LogInformation("Navigating back");
        _frame?.GoBack();
        _logger.LogDebug("Navigation back successful");
    }
}
