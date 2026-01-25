// Copyright (c) 2025 FluentPDF. All rights reserved.

namespace FluentPDF.App.Api.Models;

/// <summary>
/// Request to load a PDF document.
/// </summary>
/// <param name="Path">The file path to the PDF document.</param>
/// <param name="Password">Optional password for encrypted PDFs.</param>
public record LoadDocumentRequest(
    string Path,
    string? Password = null
);

/// <summary>
/// Response after loading a PDF document.
/// </summary>
/// <param name="DocumentId">Session ID for the loaded document.</param>
/// <param name="PageCount">Number of pages in the document.</param>
/// <param name="Metadata">Document metadata.</param>
public record LoadDocumentResponse(
    string DocumentId,
    int PageCount,
    DocumentMetadataDto Metadata
);

/// <summary>
/// Document metadata DTO.
/// </summary>
/// <param name="Title">Document title.</param>
/// <param name="Author">Document author.</param>
/// <param name="Width">Page width in points.</param>
/// <param name="Height">Page height in points.</param>
public record DocumentMetadataDto(
    string? Title,
    string? Author,
    double Width,
    double Height
);

/// <summary>
/// Request to render a PDF page.
/// </summary>
/// <param name="DocumentId">Session ID of the loaded document.</param>
/// <param name="PageIndex">Zero-based page index.</param>
/// <param name="Dpi">Rendering DPI (default: 96).</param>
/// <param name="Zoom">Zoom level (default: 1.0).</param>
public record RenderRequest(
    string DocumentId,
    int PageIndex,
    int Dpi = 96,
    double Zoom = 1.0
);

/// <summary>
/// Request to verify a rendered page against a baseline hash.
/// </summary>
/// <param name="DocumentId">Session ID of the loaded document.</param>
/// <param name="PageIndex">Zero-based page index.</param>
/// <param name="BaselineHash">Optional SHA256 hash of expected rendering.</param>
/// <param name="Dpi">Rendering DPI (default: 96).</param>
public record VerifyRequest(
    string DocumentId,
    int PageIndex,
    string? BaselineHash = null,
    int Dpi = 96
);

/// <summary>
/// Response from page verification.
/// </summary>
/// <param name="Match">True if hashes match.</param>
/// <param name="Hash">SHA256 hash of rendered page.</param>
/// <param name="Ssim">Structural similarity score (0-1) if baseline provided.</param>
/// <param name="DiffUrl">URL to diff image if mismatch.</param>
public record VerifyResponse(
    bool Match,
    string Hash,
    double? Ssim,
    string? DiffUrl
);

/// <summary>
/// Request to verify multiple pages at once.
/// </summary>
/// <param name="DocumentId">Session ID of the loaded document.</param>
/// <param name="Baselines">Dictionary of page index to expected hash.</param>
/// <param name="Dpi">Rendering DPI (default: 96).</param>
public record BatchVerifyRequest(
    string DocumentId,
    Dictionary<int, string> Baselines,
    int Dpi = 96
);

/// <summary>
/// Response from batch verification.
/// </summary>
/// <param name="AllMatch">True if all pages match their baselines.</param>
/// <param name="Results">Individual results per page.</param>
/// <param name="Failures">List of failed pages.</param>
public record BatchVerifyResponse(
    bool AllMatch,
    Dictionary<int, VerifyResponse> Results,
    List<BatchVerifyFailure> Failures
);

/// <summary>
/// Details of a batch verification failure.
/// </summary>
/// <param name="PageIndex">Zero-based page index.</param>
/// <param name="ExpectedHash">Expected hash.</param>
/// <param name="ActualHash">Actual rendered hash.</param>
/// <param name="Ssim">Structural similarity score.</param>
public record BatchVerifyFailure(
    int PageIndex,
    string ExpectedHash,
    string ActualHash,
    double? Ssim
);

/// <summary>
/// Health check response.
/// </summary>
/// <param name="Status">Health status (healthy/unhealthy).</param>
/// <param name="Version">Application version.</param>
/// <param name="PdfiumLoaded">Whether PDFium is initialized.</param>
public record HealthResponse(
    string Status,
    string Version,
    bool PdfiumLoaded
);

/// <summary>
/// Response after closing a document.
/// </summary>
/// <param name="Closed">True if document was closed successfully.</param>
public record CloseDocumentResponse(bool Closed);

/// <summary>
/// Structured error response.
/// </summary>
/// <param name="Error">Error code.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="CorrelationId">Correlation ID for tracing.</param>
/// <param name="Details">Optional additional details.</param>
public record ErrorResponse(
    string Error,
    string Message,
    string? CorrelationId = null,
    Dictionary<string, object>? Details = null
);

// UI Verification Models

/// <summary>
/// Application status response.
/// </summary>
public record StatusResponse(
    bool WindowOpen,
    bool DocumentLoaded,
    string? DocumentPath,
    int CurrentPage,
    int TotalPages,
    int ZoomLevel,
    string ViewMode,
    string Theme,
    Dictionary<string, bool> Sidebars,
    bool FullScreen = false
);

/// <summary>
/// Element verification request.
/// </summary>
public record ElementVerificationRequest(
    string AutomationId,
    Dictionary<string, object>? ExpectedProperties = null
);

/// <summary>
/// Element property check result.
/// </summary>
public record PropertyCheck(
    string Property,
    object? Expected,
    object? Actual,
    bool Passed
);

/// <summary>
/// Element information.
/// </summary>
public record ElementInfo(
    string AutomationId,
    string Name,
    bool IsEnabled,
    bool IsVisible,
    double Width,
    double Height,
    double X,
    double Y
);

/// <summary>
/// Element verification response.
/// </summary>
public record ElementVerificationResponse(
    bool Found,
    bool Passed,
    ElementInfo? Element,
    List<PropertyCheck> Checks,
    List<string> Errors
);

/// <summary>
/// Layout element specification.
/// </summary>
public record LayoutElement(
    string AutomationId,
    string? ExpectedPosition = null,
    Dictionary<string, object>? ExpectedWidth = null,
    Dictionary<string, object>? ExpectedHeight = null
);

/// <summary>
/// Layout verification request.
/// </summary>
public record LayoutVerificationRequest(
    List<LayoutElement> Elements
);

/// <summary>
/// Element layout check result.
/// </summary>
public record ElementLayoutCheck(
    string AutomationId,
    bool Found,
    Dictionary<string, double>? Position,
    Dictionary<string, object>? Checks
);

/// <summary>
/// Layout verification response.
/// </summary>
public record LayoutVerificationResponse(
    bool Passed,
    List<ElementLayoutCheck> Elements,
    List<string> Errors
);

/// <summary>
/// Click action request.
/// </summary>
public record ClickActionRequest(
    string AutomationId,
    bool WaitForDialog = false,
    string? DialogAutomationId = null
);

/// <summary>
/// Click action response.
/// </summary>
public record ClickActionResponse(
    bool Success,
    bool Clicked,
    bool DialogAppeared,
    int DurationMs,
    List<string> Errors
);

/// <summary>
/// Input action request.
/// </summary>
public record InputActionRequest(
    string AutomationId,
    string Text,
    bool ClearFirst = true,
    bool PressEnter = false
);

/// <summary>
/// Input action response.
/// </summary>
public record InputActionResponse(
    bool Success,
    bool ValueSet,
    string ActualValue,
    List<string> Errors
);

/// <summary>
/// Navigate action request.
/// </summary>
public record NavigateActionRequest(
    string Action,
    int ExpectedPage
);

/// <summary>
/// Navigate action response.
/// </summary>
public record NavigateActionResponse(
    bool Success,
    int PreviousPage,
    int CurrentPage,
    int ExpectedPage,
    bool Passed,
    List<string> Errors
);

/// <summary>
/// Theme element specification.
/// </summary>
public record ThemeElement(
    string AutomationId,
    string ExpectedBackground
);

/// <summary>
/// Theme verification request.
/// </summary>
public record ThemeVerificationRequest(
    string SetTheme,
    List<ThemeElement> VerifyElements
);

/// <summary>
/// Theme element result.
/// </summary>
public record ThemeElementResult(
    string AutomationId,
    string Background,
    string ExpectedBackground,
    bool Passed
);

/// <summary>
/// Theme verification response.
/// </summary>
public record ThemeVerificationResponse(
    string ThemeSet,
    bool Passed,
    List<ThemeElementResult> Elements,
    List<string> Errors
);

/// <summary>
/// Annotation verification request.
/// </summary>
public record AnnotationVerificationRequest(
    string Tool,
    string Action,
    int Page,
    List<double> Rect,
    string Color,
    bool VerifyPresence = true
);

/// <summary>
/// Annotation properties.
/// </summary>
public record AnnotationProperties(
    string Type,
    string Color,
    List<double> Rect
);

/// <summary>
/// Annotation presence verification.
/// </summary>
public record AnnotationPresenceCheck(
    bool Verified,
    bool Found,
    AnnotationProperties? Properties
);

/// <summary>
/// Annotation verification response.
/// </summary>
public record AnnotationVerificationResponse(
    bool Success,
    bool AnnotationCreated,
    string? AnnotationId,
    AnnotationPresenceCheck? Presence,
    List<string> Errors
);

/// <summary>
/// Form validation result.
/// </summary>
public record FormValidationResult(
    bool Valid,
    List<string> Errors
);

/// <summary>
/// Form field verification request.
/// </summary>
public record FormVerificationRequest(
    string Field,
    string Action,
    string Value,
    bool VerifyValidation = true,
    bool ExpectedValid = true
);

/// <summary>
/// Form field verification response.
/// </summary>
public record FormVerificationResponse(
    bool Success,
    bool Filled,
    string Value,
    FormValidationResult Validation,
    bool Passed
);

/// <summary>
/// Document merge verification request.
/// </summary>
public record MergeVerificationRequest(
    List<string> Files,
    string Output,
    bool VerifyPageCount = true
);

/// <summary>
/// Merge verification details.
/// </summary>
public record MergeVerificationDetails(
    int ExpectedPages,
    int ActualPages,
    bool Passed
);

/// <summary>
/// Document merge verification response.
/// </summary>
public record MergeVerificationResponse(
    bool Success,
    bool Merged,
    string Output,
    MergeVerificationDetails Verification,
    List<string> Errors
);

/// <summary>
/// Text replacement verification request.
/// </summary>
/// <param name="DocumentId">Session ID of the loaded document.</param>
/// <param name="FindText">Text to find.</param>
/// <param name="ReplaceText">Text to replace with.</param>
/// <param name="CaseSensitive">Whether to match case.</param>
/// <param name="WholeWord">Whether to match whole words only.</param>
/// <param name="Preview">Whether to preview changes without applying them.</param>
public record ReplaceRequest(
    string DocumentId,
    string FindText,
    string ReplaceText,
    bool CaseSensitive = false,
    bool WholeWord = false,
    bool Preview = false
);

/// <summary>
/// Text replacement verification response.
/// </summary>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="TotalMatches">Total number of matches found.</param>
/// <param name="ReplacementCount">Number of replacements made.</param>
/// <param name="Preview">Whether this was a preview operation.</param>
/// <param name="Message">Human-readable result message.</param>
public record ReplaceResponse(
    bool Success,
    int TotalMatches,
    int ReplacementCount,
    bool Preview,
    string Message
);
