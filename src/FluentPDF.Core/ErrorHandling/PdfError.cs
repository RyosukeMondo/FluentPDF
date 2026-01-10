using FluentResults;

namespace FluentPDF.Core.ErrorHandling;

/// <summary>
/// Represents a structured PDF-related error with categorization and context for AI analysis.
/// Extends FluentResults.Error to provide type-safe error handling with rich metadata.
/// </summary>
public class PdfError : Error
{
    /// <summary>
    /// Gets the unique error code identifying the specific error condition.
    /// Format: {COMPONENT}_{OPERATION}_{CONDITION} (e.g., "PDF_LOAD_FILE_NOT_FOUND").
    /// </summary>
    public string ErrorCode { get; init; }

    /// <summary>
    /// Gets the error category for classification and routing.
    /// </summary>
    public ErrorCategory Category { get; init; }

    /// <summary>
    /// Gets the severity level for prioritization and handling decisions.
    /// </summary>
    public ErrorSeverity Severity { get; init; }

    /// <summary>
    /// Gets additional context data specific to this error instance.
    /// Used to capture runtime information such as file paths, page numbers, or user actions.
    /// </summary>
    public Dictionary<string, object> Context { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PdfError"/> class.
    /// </summary>
    /// <param name="errorCode">Unique error code identifying the error condition.</param>
    /// <param name="message">Human-readable error message.</param>
    /// <param name="category">Error category for classification.</param>
    /// <param name="severity">Severity level for prioritization.</param>
    public PdfError(
        string errorCode,
        string message,
        ErrorCategory category,
        ErrorSeverity severity)
        : base(message)
    {
        ErrorCode = errorCode;
        Category = category;
        Severity = severity;
        Context = new Dictionary<string, object>();

        // Populate Metadata for AI analysis
        Metadata.Add("ErrorCode", errorCode);
        Metadata.Add("Category", category.ToString());
        Metadata.Add("Severity", severity.ToString());
    }

    /// <summary>
    /// Adds contextual information to the error.
    /// </summary>
    /// <param name="key">Context key.</param>
    /// <param name="value">Context value.</param>
    /// <returns>The current PdfError instance for fluent chaining.</returns>
    public PdfError WithContext(string key, object value)
    {
        Context[key] = value;
        Metadata.Add($"Context.{key}", value);
        return this;
    }
}
