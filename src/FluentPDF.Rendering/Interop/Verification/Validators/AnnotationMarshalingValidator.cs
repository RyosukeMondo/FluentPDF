using FluentPDF.Rendering.Interop.Verification.Reports;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace FluentPDF.Rendering.Interop.Verification.Validators;

/// <summary>
/// Validates annotation geometry marshaling for FS_QUADPOINTSF and FS_RECTF structs.
/// Tests edge cases including complete/partial quad points arrays, NaN, infinity, large values, negative values, and inverted rectangles.
/// </summary>
public sealed class AnnotationMarshalingValidator : IValidator
{
    private readonly ILogger<AnnotationMarshalingValidator> _logger;
    private readonly object _lock = new();

    public string ValidatorName => "Annotation Marshaling Validator";
    public string TargetArea => "Annotation Marshaling";

    public AnnotationMarshalingValidator(ILogger<AnnotationMarshalingValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates annotation marshaling for all high-risk areas.
    /// </summary>
    public async Task<ValidationReport> ValidateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting annotation marshaling validation");

        var resultsByArea = new Dictionary<string, List<ValidationResult>>();
        var allResults = new List<ValidationResult>();

        // Run all validation methods
        var quadPointsResults = await ValidateQuadPointsMarshalingAsync(cancellationToken);
        resultsByArea["Quad Points Marshaling"] = quadPointsResults;
        allResults.AddRange(quadPointsResults);

        var rectResults = await ValidateRectMarshalingAsync(cancellationToken);
        resultsByArea["Rectangle Marshaling"] = rectResults;
        allResults.AddRange(rectResults);

        // Calculate summary statistics
        var summary = new ValidationSummary
        {
            TotalTests = allResults.Count,
            PassedCount = allResults.Count(r => r.Passed),
            FailedCount = allResults.Count(r => !r.Passed && r.Severity != ValidationSeverity.Warning),
            WarningCount = allResults.Count(r => r.Severity == ValidationSeverity.Warning),
            CriticalCount = allResults.Count(r => r.Severity == ValidationSeverity.Critical)
        };

        _logger.LogInformation(
            "Annotation marshaling validation complete: {PassedCount}/{TotalTests} passed, {FailedCount} failed, {CriticalCount} critical",
            summary.PassedCount, summary.TotalTests, summary.FailedCount, summary.CriticalCount);

        return new ValidationReport
        {
            ValidatorName = ValidatorName,
            TargetArea = TargetArea,
            Summary = summary,
            ResultsByArea = resultsByArea
        };
    }

    /// <summary>
    /// Validates FS_QUADPOINTSF struct marshaling with edge cases.
    /// Tests: complete array (8 floats), partial array (&lt; 8 elements), NaN, infinity, large values, negative values.
    /// </summary>
    public async Task<List<ValidationResult>> ValidateQuadPointsMarshalingAsync(
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                var results = new List<ValidationResult>();

                // Test case 1: Complete quad points array (8 floats)
                results.Add(TestQuadPointsMarshaling(
                    new[] { 100f, 200f, 300f, 200f, 300f, 100f, 100f, 100f },
                    "Complete quad points array (8 floats)"));

                // Test case 2: Partial array (< 8 elements) - should be handled
                results.Add(TestQuadPointsMarshaling(
                    new[] { 100f, 200f, 300f },
                    "Partial quad points array (< 8 elements)"));

                // Test case 3: Empty array
                results.Add(TestQuadPointsMarshaling(
                    Array.Empty<float>(),
                    "Empty quad points array"));

                // Test case 4: Array with NaN values
                results.Add(TestQuadPointsMarshaling(
                    new[] { float.NaN, 200f, 300f, 200f, 300f, 100f, 100f, 100f },
                    "Quad points with NaN values"));

                // Test case 5: Array with infinity values
                results.Add(TestQuadPointsMarshaling(
                    new[] { float.PositiveInfinity, 200f, 300f, float.NegativeInfinity, 300f, 100f, 100f, 100f },
                    "Quad points with infinity values"));

                // Test case 6: Very large coordinate values
                results.Add(TestQuadPointsMarshaling(
                    new[] { 1000000f, 2000000f, 3000000f, 2000000f, 3000000f, 1000000f, 1000000f, 1000000f },
                    "Quad points with very large values"));

                // Test case 7: Negative coordinate values
                results.Add(TestQuadPointsMarshaling(
                    new[] { -100f, -200f, -300f, -200f, -300f, -100f, -100f, -100f },
                    "Quad points with negative values"));

                return results;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Validates FS_RECTF struct marshaling with edge cases.
    /// Tests: normal rectangle, inverted coordinates (left > right), zero-size, negative coordinates, NaN, infinity.
    /// </summary>
    public async Task<List<ValidationResult>> ValidateRectMarshalingAsync(
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                var results = new List<ValidationResult>();

                // Test case 1: Normal rectangle
                results.Add(TestRectMarshaling(
                    left: 100f, bottom: 100f, right: 200f, top: 200f,
                    "Normal rectangle (valid coordinates)"));

                // Test case 2: Inverted rectangle (left > right)
                results.Add(TestRectMarshaling(
                    left: 200f, bottom: 100f, right: 100f, top: 200f,
                    "Inverted rectangle (left > right)"));

                // Test case 3: Inverted rectangle (bottom > top)
                results.Add(TestRectMarshaling(
                    left: 100f, bottom: 200f, right: 200f, top: 100f,
                    "Inverted rectangle (bottom > top)"));

                // Test case 4: Zero-size rectangle
                results.Add(TestRectMarshaling(
                    left: 100f, bottom: 100f, right: 100f, top: 100f,
                    "Zero-size rectangle (point)"));

                // Test case 5: Negative coordinates
                results.Add(TestRectMarshaling(
                    left: -200f, bottom: -200f, right: -100f, top: -100f,
                    "Rectangle with negative coordinates"));

                // Test case 6: NaN values
                results.Add(TestRectMarshaling(
                    left: float.NaN, bottom: 100f, right: 200f, top: 200f,
                    "Rectangle with NaN values"));

                // Test case 7: Infinity values
                results.Add(TestRectMarshaling(
                    left: 100f, bottom: float.NegativeInfinity, right: float.PositiveInfinity, top: 200f,
                    "Rectangle with infinity values"));

                // Test case 8: Very large coordinates
                results.Add(TestRectMarshaling(
                    left: 1000000f, bottom: 1000000f, right: 2000000f, top: 2000000f,
                    "Rectangle with very large coordinates"));

                return results;
            }
        }, cancellationToken);
    }

    private ValidationResult TestQuadPointsMarshaling(float[] quadPoints, string testCase)
    {
        try
        {
            // Create FS_QUADPOINTSF struct
            var quadStruct = new FS_QUADPOINTSF
            {
                x1 = quadPoints.Length > 0 ? quadPoints[0] : 0,
                y1 = quadPoints.Length > 1 ? quadPoints[1] : 0,
                x2 = quadPoints.Length > 2 ? quadPoints[2] : 0,
                y2 = quadPoints.Length > 3 ? quadPoints[3] : 0,
                x3 = quadPoints.Length > 4 ? quadPoints[4] : 0,
                y3 = quadPoints.Length > 5 ? quadPoints[5] : 0,
                x4 = quadPoints.Length > 6 ? quadPoints[6] : 0,
                y4 = quadPoints.Length > 7 ? quadPoints[7] : 0
            };

            // Validate struct layout and size
            int structSize = Marshal.SizeOf<FS_QUADPOINTSF>();
            int expectedSize = sizeof(float) * 8; // 8 floats

            if (structSize != expectedSize)
            {
                return new ValidationResult
                {
                    TestName = testCase,
                    Passed = false,
                    Severity = ValidationSeverity.Critical,
                    Message = $"Struct size mismatch: expected {expectedSize} bytes, got {structSize} bytes",
                    SuggestedFix = "Verify FS_QUADPOINTSF struct layout with [StructLayout(LayoutKind.Sequential)]",
                    Context = new Dictionary<string, string>
                    {
                        ["QuadPointsCount"] = quadPoints.Length.ToString(),
                        ["ExpectedSize"] = expectedSize.ToString(),
                        ["ActualSize"] = structSize.ToString()
                    }
                };
            }

            // Validate float values
            var hasNaN = float.IsNaN(quadStruct.x1) || float.IsNaN(quadStruct.y1) ||
                        float.IsNaN(quadStruct.x2) || float.IsNaN(quadStruct.y2) ||
                        float.IsNaN(quadStruct.x3) || float.IsNaN(quadStruct.y3) ||
                        float.IsNaN(quadStruct.x4) || float.IsNaN(quadStruct.y4);

            var hasInfinity = float.IsInfinity(quadStruct.x1) || float.IsInfinity(quadStruct.y1) ||
                            float.IsInfinity(quadStruct.x2) || float.IsInfinity(quadStruct.y2) ||
                            float.IsInfinity(quadStruct.x3) || float.IsInfinity(quadStruct.y3) ||
                            float.IsInfinity(quadStruct.x4) || float.IsInfinity(quadStruct.y4);

            if (hasNaN || hasInfinity)
            {
                return new ValidationResult
                {
                    TestName = testCase,
                    Passed = false,
                    Severity = ValidationSeverity.Warning,
                    Message = hasNaN ? "Quad points contain NaN values" : "Quad points contain infinity values",
                    SuggestedFix = "Validate float values before creating FS_QUADPOINTSF struct to prevent undefined behavior",
                    Context = new Dictionary<string, string>
                    {
                        ["HasNaN"] = hasNaN.ToString(),
                        ["HasInfinity"] = hasInfinity.ToString(),
                        ["QuadPoints"] = $"[{quadStruct.x1}, {quadStruct.y1}, {quadStruct.x2}, {quadStruct.y2}, {quadStruct.x3}, {quadStruct.y3}, {quadStruct.x4}, {quadStruct.y4}]"
                    }
                };
            }

            return new ValidationResult
            {
                TestName = testCase,
                Passed = true,
                Severity = ValidationSeverity.Info,
                Message = $"Quad points marshaling successful ({quadPoints.Length} values provided)",
                Context = new Dictionary<string, string>
                {
                    ["QuadPointsCount"] = quadPoints.Length.ToString(),
                    ["StructSize"] = structSize.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                TestName = testCase,
                Passed = false,
                Severity = ValidationSeverity.Critical,
                Message = $"Quad points marshaling failed: {ex.Message}",
                SuggestedFix = "Investigate exception cause and ensure struct marshaling is correct",
                Context = new Dictionary<string, string>
                {
                    ["ExceptionType"] = ex.GetType().Name,
                    ["QuadPointsCount"] = quadPoints.Length.ToString()
                }
            };
        }
    }

    private ValidationResult TestRectMarshaling(float left, float bottom, float right, float top, string testCase)
    {
        try
        {
            // Create FS_RECTF struct
            var rect = new FS_RECTF
            {
                left = left,
                bottom = bottom,
                right = right,
                top = top
            };

            // Validate struct layout and size
            int structSize = Marshal.SizeOf<FS_RECTF>();
            int expectedSize = sizeof(float) * 4; // 4 floats

            if (structSize != expectedSize)
            {
                return new ValidationResult
                {
                    TestName = testCase,
                    Passed = false,
                    Severity = ValidationSeverity.Critical,
                    Message = $"Struct size mismatch: expected {expectedSize} bytes, got {structSize} bytes",
                    SuggestedFix = "Verify FS_RECTF struct layout with [StructLayout(LayoutKind.Sequential)]",
                    Context = new Dictionary<string, string>
                    {
                        ["ExpectedSize"] = expectedSize.ToString(),
                        ["ActualSize"] = structSize.ToString(),
                        ["Rect"] = $"{{left={left}, bottom={bottom}, right={right}, top={top}}}"
                    }
                };
            }

            // Validate float values
            var hasNaN = float.IsNaN(rect.left) || float.IsNaN(rect.bottom) ||
                        float.IsNaN(rect.right) || float.IsNaN(rect.top);

            var hasInfinity = float.IsInfinity(rect.left) || float.IsInfinity(rect.bottom) ||
                            float.IsInfinity(rect.right) || float.IsInfinity(rect.top);

            if (hasNaN || hasInfinity)
            {
                return new ValidationResult
                {
                    TestName = testCase,
                    Passed = false,
                    Severity = ValidationSeverity.Warning,
                    Message = hasNaN ? "Rectangle contains NaN values" : "Rectangle contains infinity values",
                    SuggestedFix = "Validate float values before creating FS_RECTF struct to prevent undefined behavior",
                    Context = new Dictionary<string, string>
                    {
                        ["HasNaN"] = hasNaN.ToString(),
                        ["HasInfinity"] = hasInfinity.ToString(),
                        ["Rect"] = $"{{left={rect.left}, bottom={rect.bottom}, right={rect.right}, top={rect.top}}}"
                    }
                };
            }

            // Detect inverted coordinates
            var isInvertedX = rect.left > rect.right;
            var isInvertedY = rect.bottom > rect.top;

            if (isInvertedX || isInvertedY)
            {
                return new ValidationResult
                {
                    TestName = testCase,
                    Passed = false,
                    Severity = ValidationSeverity.Warning,
                    Message = "Rectangle has inverted coordinates",
                    SuggestedFix = isInvertedX && isInvertedY
                        ? "Normalize rectangle: swap left/right and bottom/top"
                        : isInvertedX
                            ? "Normalize rectangle: swap left/right"
                            : "Normalize rectangle: swap bottom/top",
                    Context = new Dictionary<string, string>
                    {
                        ["IsInvertedX"] = isInvertedX.ToString(),
                        ["IsInvertedY"] = isInvertedY.ToString(),
                        ["Rect"] = $"{{left={left}, bottom={bottom}, right={right}, top={top}}}"
                    }
                };
            }

            return new ValidationResult
            {
                TestName = testCase,
                Passed = true,
                Severity = ValidationSeverity.Info,
                Message = "Rectangle marshaling successful",
                Context = new Dictionary<string, string>
                {
                    ["StructSize"] = structSize.ToString(),
                    ["Rect"] = $"{{left={left}, bottom={bottom}, right={right}, top={top}}}"
                }
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                TestName = testCase,
                Passed = false,
                Severity = ValidationSeverity.Critical,
                Message = $"Rectangle marshaling failed: {ex.Message}",
                SuggestedFix = "Investigate exception cause and ensure struct marshaling is correct",
                Context = new Dictionary<string, string>
                {
                    ["ExceptionType"] = ex.GetType().Name,
                    ["Rect"] = $"{{left={left}, bottom={bottom}, right={right}, top={top}}}"
                }
            };
        }
    }

    #region Struct Definitions (matching PdfiumInterop.cs)

    /// <summary>
    /// Rectangle structure for PDFium (uses floats).
    /// Must match PdfiumInterop.cs FS_RECTF definition.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct FS_RECTF
    {
        public float left;
        public float bottom;
        public float right;
        public float top;
    }

    /// <summary>
    /// Quad points structure for text markup annotations.
    /// Represents four points defining a quadrilateral.
    /// Must match PdfiumInterop.cs FS_QUADPOINTSF definition.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct FS_QUADPOINTSF
    {
        public float x1;
        public float y1;
        public float x2;
        public float y2;
        public float x3;
        public float y3;
        public float x4;
        public float y4;
    }

    #endregion
}
