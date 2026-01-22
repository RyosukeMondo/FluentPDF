namespace FluentPDF.Rendering.Interop.Verification.Reports;

/// <summary>
/// Represents a comprehensive validation report that includes signature verification,
/// marshaling tests, high-risk area validators, performance profiling, and workaround regression tests.
/// </summary>
public record ComprehensiveValidationReport
{
    /// <summary>
    /// Gets the timestamp when this report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the legacy coverage report containing signature verification and basic marshaling tests.
    /// </summary>
    public required CoverageReport CoverageReport { get; init; }

    /// <summary>
    /// Gets the list of validation reports from high-risk area validators.
    /// </summary>
    public required List<ValidationReport> ValidationReports { get; init; }

    /// <summary>
    /// Gets the performance profiling report.
    /// </summary>
    public ProfilingReport? ProfilingReport { get; init; }

    /// <summary>
    /// Gets the list of workaround test results.
    /// </summary>
    public required List<WorkaroundTestResult> WorkaroundTestResults { get; init; }

    /// <summary>
    /// Gets the overall validation status based on all reports.
    /// </summary>
    public ValidationStatus OverallStatus
    {
        get
        {
            // Check if any validation reports have critical failures
            if (ValidationReports.Any(r => r.HasCriticalFailures))
            {
                return ValidationStatus.Critical;
            }

            // Check if any validation reports have failures
            if (ValidationReports.Any(r => !r.AllPassed) || CoverageReport.HasCriticalGaps)
            {
                return ValidationStatus.Failed;
            }

            // Check if any workarounds are broken
            if (WorkaroundTestResults.Any(w => w.Status == WorkaroundStatus.Broken))
            {
                return ValidationStatus.Critical;
            }

            // Check if there are any warnings
            if (ValidationReports.Any(r => r.Summary.WarningCount > 0))
            {
                return ValidationStatus.Warning;
            }

            return ValidationStatus.Passed;
        }
    }

    /// <summary>
    /// Gets a summary of all validation results.
    /// </summary>
    public ComprehensiveValidationSummary Summary => new()
    {
        TotalValidators = ValidationReports.Count,
        PassedValidators = ValidationReports.Count(r => r.AllPassed),
        FailedValidators = ValidationReports.Count(r => !r.AllPassed),
        CriticalValidators = ValidationReports.Count(r => r.HasCriticalFailures),
        TotalWorkarounds = WorkaroundTestResults.Count,
        WorkaroundsStillNeeded = WorkaroundTestResults.Count(w => w.Status == WorkaroundStatus.StillNeeded),
        WorkaroundsCanBeRemoved = WorkaroundTestResults.Count(w => w.Status == WorkaroundStatus.CanBeRemoved),
        WorkaroundsBroken = WorkaroundTestResults.Count(w => w.Status == WorkaroundStatus.Broken),
        HasPerformanceProfiling = ProfilingReport != null,
        HasRegressions = ProfilingReport?.RegressedFunctions != null && ProfilingReport.RegressedFunctions.Count > 0,
        RegressedFunctionsCount = ProfilingReport?.RegressedFunctions?.Count ?? 0
    };
}

/// <summary>
/// Contains summary statistics for comprehensive validation.
/// </summary>
public record ComprehensiveValidationSummary
{
    /// <summary>
    /// Gets the total number of validators that were run.
    /// </summary>
    public int TotalValidators { get; init; }

    /// <summary>
    /// Gets the number of validators that passed all tests.
    /// </summary>
    public int PassedValidators { get; init; }

    /// <summary>
    /// Gets the number of validators that had failures.
    /// </summary>
    public int FailedValidators { get; init; }

    /// <summary>
    /// Gets the number of validators with critical failures.
    /// </summary>
    public int CriticalValidators { get; init; }

    /// <summary>
    /// Gets the total number of workarounds tested.
    /// </summary>
    public int TotalWorkarounds { get; init; }

    /// <summary>
    /// Gets the number of workarounds that are still needed.
    /// </summary>
    public int WorkaroundsStillNeeded { get; init; }

    /// <summary>
    /// Gets the number of workarounds that can be removed.
    /// </summary>
    public int WorkaroundsCanBeRemoved { get; init; }

    /// <summary>
    /// Gets the number of workarounds that are broken.
    /// </summary>
    public int WorkaroundsBroken { get; init; }

    /// <summary>
    /// Gets whether performance profiling was performed.
    /// </summary>
    public bool HasPerformanceProfiling { get; init; }

    /// <summary>
    /// Gets whether performance regressions were detected.
    /// </summary>
    public bool HasRegressions { get; init; }

    /// <summary>
    /// Gets the number of functions with performance regressions.
    /// </summary>
    public int RegressedFunctionsCount { get; init; }
}
