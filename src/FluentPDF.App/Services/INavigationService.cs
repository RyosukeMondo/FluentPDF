using System;

namespace FluentPDF.App.Services;

/// <summary>
/// Provides navigation capabilities for the application.
/// </summary>
/// <remarks>
/// This abstraction enables testable navigation without depending on WinUI Frame.
/// Implementations should handle page navigation, history management, and parameter passing.
/// </remarks>
public interface INavigationService
{
    /// <summary>
    /// Gets a value indicating whether backward navigation is possible.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Navigates to the specified page type.
    /// </summary>
    /// <param name="pageType">The type of page to navigate to. Must be a valid WinUI Page type.</param>
    /// <param name="parameter">Optional parameter to pass to the target page.</param>
    /// <exception cref="ArgumentNullException">Thrown when pageType is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when navigation fails or Frame is not initialized.</exception>
    void NavigateTo(Type pageType, object? parameter = null);

    /// <summary>
    /// Navigates backward to the previous page in the navigation stack.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when CanGoBack is false.</exception>
    void GoBack();
}
