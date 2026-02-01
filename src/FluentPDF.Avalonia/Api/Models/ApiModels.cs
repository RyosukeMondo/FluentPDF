// Copyright (c) 2025 FluentPDF. All rights reserved.

namespace FluentPDF.Avalonia.Api.Models;

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
/// <param name="ActiveSessions">Number of active document sessions.</param>
/// <param name="Timestamp">Server timestamp.</param>
public record HealthResponse(
    string Status,
    string Version,
    bool PdfiumLoaded,
    int ActiveSessions = 0,
    DateTime? Timestamp = null
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
