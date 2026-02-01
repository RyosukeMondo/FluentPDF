using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Default implementation of INavigationService using Avalonia ContentControl.
/// </summary>
/// <remarks>
/// This service wraps Avalonia ContentControl to enable testable navigation.
/// The ContentControl reference must be set via the NavigationFrame property before navigation can occur.
/// </remarks>
public sealed class AvaloniaNavigationService : INavigationService
{
    private readonly ILogger<AvaloniaNavigationService> _logger;
    private ContentControl? _navigationFrame;
    private readonly Stack<(Type PageType, object? Parameter)> _navigationStack = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaNavigationService"/> class.
    /// </summary>
    /// <param name="logger">Logger for navigation events.</param>
    public AvaloniaNavigationService(ILogger<AvaloniaNavigationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets or sets the ContentControl used for navigation.
    /// </summary>
    /// <remarks>
    /// This property must be set before any navigation operations can occur.
    /// Typically set during application initialization or window creation.
    /// </remarks>
    public ContentControl? NavigationFrame
    {
        get => _navigationFrame;
        set
        {
            _navigationFrame = value;
            _logger.LogDebug("Navigation frame set: {FrameSet}", value is not null);
        }
    }

    /// <inheritdoc/>
    public bool CanGoBack => _navigationStack.Count > 0;

    /// <inheritdoc/>
    public void NavigateTo(Type pageType, object? parameter = null)
    {
        if (pageType is null)
        {
            throw new ArgumentNullException(nameof(pageType));
        }

        if (_navigationFrame is null)
        {
            var ex = new InvalidOperationException("Navigation frame is not initialized. Set the NavigationFrame property before navigating.");
            _logger.LogError(ex, "Navigation failed: frame not initialized");
            throw ex;
        }

        _logger.LogInformation("Navigating to {PageType} with parameter: {HasParameter}", pageType.Name, parameter is not null);

        try
        {
            // Store current page in navigation stack if one exists
            if (_navigationFrame.Content != null)
            {
                var currentType = _navigationFrame.Content.GetType();
                _navigationStack.Push((currentType, null));
            }

            // Create new page instance
            var page = Activator.CreateInstance(pageType);
            if (page == null)
            {
                throw new InvalidOperationException($"Failed to create instance of {pageType.Name}");
            }

            // Set the content
            _navigationFrame.Content = page;

            // If the page has a DataContext property and we have a parameter, set it
            if (parameter != null && page is Control control)
            {
                control.DataContext = parameter;
            }

            _logger.LogDebug("Navigation successful to {PageType}", pageType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation failed for {PageType}", pageType.Name);
            throw new InvalidOperationException($"Navigation to {pageType.Name} failed.", ex);
        }
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

        if (_navigationFrame is null)
        {
            var ex = new InvalidOperationException("Navigation frame is not initialized.");
            _logger.LogError(ex, "GoBack failed: frame not initialized");
            throw ex;
        }

        _logger.LogInformation("Navigating back");

        var (pageType, parameter) = _navigationStack.Pop();

        try
        {
            var page = Activator.CreateInstance(pageType);
            if (page == null)
            {
                throw new InvalidOperationException($"Failed to create instance of {pageType.Name}");
            }

            _navigationFrame.Content = page;

            if (parameter != null && page is Control control)
            {
                control.DataContext = parameter;
            }

            _logger.LogDebug("Navigation back successful to {PageType}", pageType.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GoBack failed for {PageType}", pageType.Name);
            // Re-add to stack if navigation failed
            _navigationStack.Push((pageType, parameter));
            throw;
        }
    }
}
