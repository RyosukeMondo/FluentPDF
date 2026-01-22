namespace FluentPDF.Rendering.Interop.Verification.Reports;

/// <summary>
/// Represents a comprehensive validation report from a marshaling validator.
/// </summary>
public record ValidationReport
{
    /// <summary>
    /// Gets the name of the validator that generated this report.
    /// </summary>
    public required string ValidatorName { get; init; }

    /// <summary>
    /// Gets the target area being validated (e.g., "UTF-16 Marshaling", "Bitmap Marshaling").
    /// </summary>
    public required string TargetArea { get; init; }

    /// <summary>
    /// Gets the timestamp when this validation was performed.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the summary statistics for this validation.
    /// </summary>
    public required ValidationSummary Summary { get; init; }

    /// <summary>
    /// Gets the validation results organized by sub-area or test category.
    /// Key: sub-area name (e.g., "Bookmarks", "Text Search", "Form Fields")
    /// Value: list of validation results for that sub-area
    /// </summary>
    public required Dictionary<string, List<ValidationResult>> ResultsByArea { get; init; }

    /// <summary>
    /// Gets a value indicating whether all validations passed.
    /// </summary>
    public bool AllPassed => Summary.FailedCount == 0 && Summary.CriticalCount == 0;

    /// <summary>
    /// Gets a value indicating whether any critical failures occurred.
    /// </summary>
    public bool HasCriticalFailures => Summary.CriticalCount > 0;
}

/// <summary>
/// Represents summary statistics for a validation report.
/// </summary>
public record ValidationSummary
{
    /// <summary>
    /// Gets the total number of validation tests performed.
    /// </summary>
    public int TotalTests { get; init; }

    /// <summary>
    /// Gets the number of tests that passed.
    /// </summary>
    public int PassedCount { get; init; }

    /// <summary>
    /// Gets the number of tests that failed.
    /// </summary>
    public int FailedCount { get; init; }

    /// <summary>
    /// Gets the number of warnings generated.
    /// </summary>
    public int WarningCount { get; init; }

    /// <summary>
    /// Gets the number of critical failures.
    /// </summary>
    public int CriticalCount { get; init; }

    /// <summary>
    /// Gets the percentage of tests that passed (0-100).
    /// </summary>
    public double PassPercentage => TotalTests > 0 ? (double)PassedCount / TotalTests * 100 : 0;

    /// <summary>
    /// Gets the overall validation status based on the results.
    /// </summary>
    public ValidationStatus OverallStatus
    {
        get
        {
            if (CriticalCount > 0) return ValidationStatus.Critical;
            if (FailedCount > 0) return ValidationStatus.Failed;
            if (WarningCount > 0) return ValidationStatus.Warning;
            return ValidationStatus.Passed;
        }
    }
}

/// <summary>
/// Represents the overall status of a validation.
/// </summary>
public enum ValidationStatus
{
    /// <summary>
    /// All validations passed without warnings.
    /// </summary>
    Passed,

    /// <summary>
    /// Validations passed but with warnings.
    /// </summary>
    Warning,

    /// <summary>
    /// One or more validations failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Critical failures occurred that may cause crashes or data corruption.
    /// </summary>
    Critical
}
