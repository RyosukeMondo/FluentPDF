namespace FluentPDF.Core.ViewModels;

/// <summary>
/// Defines the page view modes for the PDF viewer.
/// </summary>
public enum PageViewMode
{
    /// <summary>Single page view - one page at a time with navigation.</summary>
    SinglePage,

    /// <summary>Continuous scroll - all pages in a vertical scrolling list.</summary>
    ContinuousScroll,

    /// <summary>Two-page view - displays two pages side-by-side like a book.</summary>
    TwoPage
}

/// <summary>
/// Message sent when navigating to a specific page via thumbnail click.
/// </summary>
/// <param name="PageNumber">The 1-based page number to navigate to.</param>
public record NavigateToPageMessage(int PageNumber);

/// <summary>
/// Message sent to raise an accessibility notification for screen readers.
/// </summary>
/// <param name="Message">The message to announce to screen readers.</param>
public record AccessibilityNotificationMessage(string Message);

/// <summary>
/// Message sent when pages have been modified (rotated, deleted, reordered, or inserted).
/// </summary>
public record PageModifiedMessage();
