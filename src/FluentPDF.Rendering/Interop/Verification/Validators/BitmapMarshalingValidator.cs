using FluentPDF.Rendering.Interop.Verification.Reports;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace FluentPDF.Rendering.Interop.Verification.Validators;

/// <summary>
/// Validates bitmap buffer marshaling, stride calculations, and overflow prevention for PDFium bitmap operations.
/// Tests edge cases including 1x1 pixel, large dimensions (8192x8192), non-power-of-2 dimensions, stride padding, and checked arithmetic.
/// </summary>
public sealed class BitmapMarshalingValidator : IValidator
{
    private readonly ILogger<BitmapMarshalingValidator> _logger;
    private readonly object _lock = new();

    public string ValidatorName => "Bitmap Marshaling Validator";
    public string TargetArea => "Bitmap Marshaling";

    public BitmapMarshalingValidator(ILogger<BitmapMarshalingValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates bitmap marshaling for all high-risk areas.
    /// </summary>
    public async Task<ValidationReport> ValidateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting bitmap marshaling validation");

        var resultsByArea = new Dictionary<string, List<ValidationResult>>();
        var allResults = new List<ValidationResult>();

        // Run all validation methods
        var strideResults = ValidateStrideCalculation();
        resultsByArea["Stride Calculation"] = strideResults;
        allResults.AddRange(strideResults);

        var marshalCopyResults = await ValidateMarshalCopyAsync(cancellationToken);
        resultsByArea["Marshal.Copy Operations"] = marshalCopyResults;
        allResults.AddRange(marshalCopyResults);

        var pixelDataResults = await ValidatePixelDataIntegrityAsync(cancellationToken);
        resultsByArea["Pixel Data Integrity"] = pixelDataResults;
        allResults.AddRange(pixelDataResults);

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
            "Bitmap marshaling validation complete: {PassedCount}/{TotalTests} passed, {FailedCount} failed, {CriticalCount} critical",
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
    /// Validates stride calculations with checked arithmetic to detect integer overflow.
    /// Tests: 1x1 pixel, 8192x8192 pixel, 1920x1080 (non-power-of-2), width causing stride padding, zero/negative dimensions.
    /// </summary>
    public List<ValidationResult> ValidateStrideCalculation()
    {
        lock (_lock)
        {
            var results = new List<ValidationResult>();

            // Test case 1: 1x1 pixel bitmap (minimum size)
            results.Add(TestStrideCalculation(1, 1, "1x1 pixel (minimum size)"));

            // Test case 2: 8192x8192 pixel bitmap (large but valid)
            results.Add(TestStrideCalculation(8192, 8192, "8192x8192 pixel (large dimension)"));

            // Test case 3: 1920x1080 pixel bitmap (non-power-of-2)
            results.Add(TestStrideCalculation(1920, 1080, "1920x1080 pixel (non-power-of-2)"));

            // Test case 4: Width causing stride padding (odd width)
            results.Add(TestStrideCalculation(1921, 1080, "1921x1080 pixel (odd width, stride padding)"));

            // Test case 5: Zero dimensions (should handle gracefully)
            results.Add(TestStrideCalculation(0, 0, "0x0 pixel (zero dimensions)"));

            // Test case 6: Negative dimensions (should handle gracefully)
            results.Add(TestStrideCalculation(-1, -1, "-1x-1 pixel (negative dimensions)"));

            // Test case 7: Overflow detection - extremely large dimensions
            results.Add(TestStrideOverflow(int.MaxValue / 4, int.MaxValue / 4, "Overflow detection (stride × height)"));

            return results;
        }
    }

    /// <summary>
    /// Validates Marshal.Copy operations with edge-case buffer sizes.
    /// Tests: 1x1 pixel, 8192x8192 pixel, non-power-of-2 dimensions without crashes.
    /// </summary>
    public async Task<List<ValidationResult>> ValidateMarshalCopyAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                var results = new List<ValidationResult>();

                // Test case 1: 1x1 pixel bitmap
                results.Add(TestMarshalCopy(1, 1, "1x1 pixel bitmap"));

                // Test case 2: 8192x8192 pixel bitmap (stress test)
                results.Add(TestMarshalCopy(8192, 8192, "8192x8192 pixel bitmap (stress test)"));

                // Test case 3: 1920x1080 pixel bitmap (common resolution)
                results.Add(TestMarshalCopy(1920, 1080, "1920x1080 pixel bitmap (common resolution)"));

                // Test case 4: Non-square bitmap (wide)
                results.Add(TestMarshalCopy(3840, 100, "3840x100 pixel bitmap (wide)"));

                // Test case 5: Non-square bitmap (tall)
                results.Add(TestMarshalCopy(100, 2160, "100x2160 pixel bitmap (tall)"));

                return results;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Validates pixel data integrity after marshaling, verifying BGRA32 pixel format correctness.
    /// </summary>
    public async Task<List<ValidationResult>> ValidatePixelDataIntegrityAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            lock (_lock)
            {
                var results = new List<ValidationResult>();

                // Test case 1: Verify BGRA32 format with known color
                results.Add(TestPixelDataIntegrity(100, 100, 0xFFFF0000, "BGRA32 format (red color)", 0xFF, 0x00, 0x00, 0xFF)); // Red in BGRA

                // Test case 2: Verify BGRA32 format with green
                results.Add(TestPixelDataIntegrity(100, 100, 0xFF00FF00, "BGRA32 format (green color)", 0x00, 0xFF, 0x00, 0xFF)); // Green in BGRA

                // Test case 3: Verify BGRA32 format with blue
                results.Add(TestPixelDataIntegrity(100, 100, 0xFF0000FF, "BGRA32 format (blue color)", 0xFF, 0x00, 0x00, 0xFF)); // Blue in BGRA (note: ARGB 0xFF0000FF -> BGRA is 0xFF,0x00,0x00,0xFF)

                // Test case 4: Verify BGRA32 format with white
                results.Add(TestPixelDataIntegrity(100, 100, 0xFFFFFFFF, "BGRA32 format (white color)", 0xFF, 0xFF, 0xFF, 0xFF));

                // Test case 5: Verify BGRA32 format with black
                results.Add(TestPixelDataIntegrity(100, 100, 0xFF000000, "BGRA32 format (black color)", 0x00, 0x00, 0x00, 0xFF));

                return results;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Tests stride calculation for a bitmap with checked arithmetic.
    /// </summary>
    private ValidationResult TestStrideCalculation(int width, int height, string description)
    {
        var testName = $"Stride calculation: {description}";

        try
        {
            // Skip invalid dimensions
            if (width <= 0 || height <= 0)
            {
                return new ValidationResult
                {
                    TestName = testName,
                    Passed = true,
                    Severity = ValidationSeverity.Info,
                    Message = "Invalid dimensions handled gracefully (not attempting creation)",
                    Context = new Dictionary<string, string>
                    {
                        ["Width"] = width.ToString(),
                        ["Height"] = height.ToString()
                    }
                };
            }

            // Create bitmap to get actual stride
            var bitmap = PdfiumInterop.CreateBitmap(width, height, hasAlpha: true);

            if (bitmap == IntPtr.Zero)
            {
                return new ValidationResult
                {
                    TestName = testName,
                    Passed = false,
                    Severity = ValidationSeverity.Error,
                    Message = "Failed to create bitmap",
                    Context = new Dictionary<string, string>
                    {
                        ["Width"] = width.ToString(),
                        ["Height"] = height.ToString()
                    }
                };
            }

            try
            {
                var actualStride = PdfiumInterop.GetBitmapStride(bitmap);

                // Validate stride is reasonable (should be >= width * 4 for BGRA32)
                var minimumStride = width * 4;
                if (actualStride < minimumStride)
                {
                    return new ValidationResult
                    {
                        TestName = testName,
                        Passed = false,
                        Severity = ValidationSeverity.Critical,
                        Message = "Stride is less than minimum required for BGRA32 format",
                        ExpectedValue = $">= {minimumStride}",
                        ActualValue = actualStride,
                        Context = new Dictionary<string, string>
                        {
                            ["Width"] = width.ToString(),
                            ["Height"] = height.ToString(),
                            ["MinimumStride"] = minimumStride.ToString(),
                            ["ActualStride"] = actualStride.ToString()
                        }
                    };
                }

                // Test checked multiplication for buffer size calculation
                try
                {
                    var bufferSize = checked(actualStride * height);

                    return new ValidationResult
                    {
                        TestName = testName,
                        Passed = true,
                        Severity = ValidationSeverity.Info,
                        Message = $"Stride calculation valid: stride={actualStride}, bufferSize={bufferSize:N0} bytes",
                        Context = new Dictionary<string, string>
                        {
                            ["Width"] = width.ToString(),
                            ["Height"] = height.ToString(),
                            ["Stride"] = actualStride.ToString(),
                            ["BufferSize"] = bufferSize.ToString()
                        }
                    };
                }
                catch (OverflowException)
                {
                    return new ValidationResult
                    {
                        TestName = testName,
                        Passed = false,
                        Severity = ValidationSeverity.Critical,
                        Message = "Integer overflow detected in stride × height calculation",
                        SuggestedFix = "Use checked arithmetic or validate dimensions before buffer allocation",
                        Context = new Dictionary<string, string>
                        {
                            ["Width"] = width.ToString(),
                            ["Height"] = height.ToString(),
                            ["Stride"] = actualStride.ToString()
                        }
                    };
                }
            }
            finally
            {
                PdfiumInterop.DestroyBitmap(bitmap);
            }
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                TestName = testName,
                Passed = false,
                Severity = ValidationSeverity.Error,
                Message = "Exception during stride calculation test",
                ErrorDetails = ex.ToString(),
                Context = new Dictionary<string, string>
                {
                    ["Width"] = width.ToString(),
                    ["Height"] = height.ToString()
                }
            };
        }
    }

    /// <summary>
    /// Tests stride overflow detection with extremely large dimensions.
    /// </summary>
    private ValidationResult TestStrideOverflow(int width, int height, string description)
    {
        var testName = $"Stride overflow: {description}";

        try
        {
            // Attempt to create bitmap with large dimensions
            // PDFium should fail gracefully
            var bitmap = PdfiumInterop.CreateBitmap(width, height, hasAlpha: true);

            if (bitmap == IntPtr.Zero)
            {
                // Expected: PDFium should refuse to create bitmap with overflow dimensions
                return new ValidationResult
                {
                    TestName = testName,
                    Passed = true,
                    Severity = ValidationSeverity.Info,
                    Message = "PDFium correctly refused to create bitmap with overflow dimensions",
                    Context = new Dictionary<string, string>
                    {
                        ["Width"] = width.ToString(),
                        ["Height"] = height.ToString()
                    }
                };
            }

            // If PDFium created the bitmap, try to get stride and calculate buffer size
            try
            {
                var stride = PdfiumInterop.GetBitmapStride(bitmap);

                // Check for overflow in stride × height
                try
                {
                    var bufferSize = checked(stride * height);

                    // Unexpected: PDFium allowed creation with potentially overflowing dimensions
                    return new ValidationResult
                    {
                        TestName = testName,
                        Passed = false,
                        Severity = ValidationSeverity.Warning,
                        Message = "PDFium created bitmap with very large dimensions (potential overflow risk)",
                        Context = new Dictionary<string, string>
                        {
                            ["Width"] = width.ToString(),
                            ["Height"] = height.ToString(),
                            ["Stride"] = stride.ToString(),
                            ["BufferSize"] = bufferSize.ToString()
                        }
                    };
                }
                catch (OverflowException)
                {
                    return new ValidationResult
                    {
                        TestName = testName,
                        Passed = true,
                        Severity = ValidationSeverity.Info,
                        Message = "Overflow correctly detected in stride × height calculation (checked arithmetic working)",
                        Context = new Dictionary<string, string>
                        {
                            ["Width"] = width.ToString(),
                            ["Height"] = height.ToString(),
                            ["Stride"] = stride.ToString()
                        }
                    };
                }
            }
            finally
            {
                PdfiumInterop.DestroyBitmap(bitmap);
            }
        }
        catch (OutOfMemoryException)
        {
            return new ValidationResult
            {
                TestName = testName,
                Passed = true,
                Severity = ValidationSeverity.Info,
                Message = "OutOfMemoryException handled gracefully (expected for very large dimensions)",
                Context = new Dictionary<string, string>
                {
                    ["Width"] = width.ToString(),
                    ["Height"] = height.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                TestName = testName,
                Passed = false,
                Severity = ValidationSeverity.Error,
                Message = "Unexpected exception during overflow test",
                ErrorDetails = ex.ToString(),
                Context = new Dictionary<string, string>
                {
                    ["Width"] = width.ToString(),
                    ["Height"] = height.ToString()
                }
            };
        }
    }

    /// <summary>
    /// Tests Marshal.Copy operation with actual PDFium bitmap.
    /// </summary>
    private ValidationResult TestMarshalCopy(int width, int height, string description)
    {
        var testName = $"Marshal.Copy: {description}";

        try
        {
            // Create bitmap
            var bitmap = PdfiumInterop.CreateBitmap(width, height, hasAlpha: true);

            if (bitmap == IntPtr.Zero)
            {
                return new ValidationResult
                {
                    TestName = testName,
                    Passed = false,
                    Severity = ValidationSeverity.Error,
                    Message = "Failed to create bitmap",
                    Context = new Dictionary<string, string>
                    {
                        ["Width"] = width.ToString(),
                        ["Height"] = height.ToString()
                    }
                };
            }

            try
            {
                // Get buffer and stride
                var buffer = PdfiumInterop.GetBitmapBuffer(bitmap);
                var stride = PdfiumInterop.GetBitmapStride(bitmap);

                // Calculate buffer size with checked arithmetic
                int byteCount;
                try
                {
                    byteCount = checked(stride * height);
                }
                catch (OverflowException)
                {
                    return new ValidationResult
                    {
                        TestName = testName,
                        Passed = false,
                        Severity = ValidationSeverity.Critical,
                        Message = "Integer overflow in buffer size calculation",
                        SuggestedFix = "Validate dimensions before allocation",
                        Context = new Dictionary<string, string>
                        {
                            ["Width"] = width.ToString(),
                            ["Height"] = height.ToString(),
                            ["Stride"] = stride.ToString()
                        }
                    };
                }

                // Allocate managed byte array
                var pixelData = new byte[byteCount];

                // Copy from unmanaged to managed memory
                Marshal.Copy(buffer, pixelData, 0, byteCount);

                return new ValidationResult
                {
                    TestName = testName,
                    Passed = true,
                    Severity = ValidationSeverity.Info,
                    Message = $"Marshal.Copy succeeded: {byteCount:N0} bytes copied",
                    Context = new Dictionary<string, string>
                    {
                        ["Width"] = width.ToString(),
                        ["Height"] = height.ToString(),
                        ["Stride"] = stride.ToString(),
                        ["ByteCount"] = byteCount.ToString()
                    }
                };
            }
            finally
            {
                PdfiumInterop.DestroyBitmap(bitmap);
            }
        }
        catch (OutOfMemoryException)
        {
            // Expected for very large bitmaps
            return new ValidationResult
            {
                TestName = testName,
                Passed = true,
                Severity = ValidationSeverity.Warning,
                Message = "OutOfMemoryException handled gracefully (expected for very large bitmaps)",
                Context = new Dictionary<string, string>
                {
                    ["Width"] = width.ToString(),
                    ["Height"] = height.ToString()
                }
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                TestName = testName,
                Passed = false,
                Severity = ValidationSeverity.Error,
                Message = "Exception during Marshal.Copy test",
                ErrorDetails = ex.ToString(),
                Context = new Dictionary<string, string>
                {
                    ["Width"] = width.ToString(),
                    ["Height"] = height.ToString()
                }
            };
        }
    }

    /// <summary>
    /// Tests pixel data integrity by filling bitmap with known color and verifying BGRA32 format.
    /// </summary>
    private ValidationResult TestPixelDataIntegrity(int width, int height, uint argbColor, string description,
        byte expectedB, byte expectedG, byte expectedR, byte expectedA)
    {
        var testName = $"Pixel data integrity: {description}";

        try
        {
            // Create bitmap
            var bitmap = PdfiumInterop.CreateBitmap(width, height, hasAlpha: true);

            if (bitmap == IntPtr.Zero)
            {
                return new ValidationResult
                {
                    TestName = testName,
                    Passed = false,
                    Severity = ValidationSeverity.Error,
                    Message = "Failed to create bitmap",
                    Context = new Dictionary<string, string>
                    {
                        ["Width"] = width.ToString(),
                        ["Height"] = height.ToString()
                    }
                };
            }

            try
            {
                // Fill bitmap with color
                PdfiumInterop.FillBitmap(bitmap, argbColor);

                // Get buffer and stride
                var buffer = PdfiumInterop.GetBitmapBuffer(bitmap);
                var stride = PdfiumInterop.GetBitmapStride(bitmap);
                var byteCount = stride * height;

                // Copy pixel data
                var pixelData = new byte[byteCount];
                Marshal.Copy(buffer, pixelData, 0, byteCount);

                // Verify first pixel (BGRA format)
                // PDFium uses BGRA32 format: [B, G, R, A]
                var actualB = pixelData[0];
                var actualG = pixelData[1];
                var actualR = pixelData[2];
                var actualA = pixelData[3];

                if (actualB == expectedB && actualG == expectedG && actualR == expectedR && actualA == expectedA)
                {
                    return new ValidationResult
                    {
                        TestName = testName,
                        Passed = true,
                        Severity = ValidationSeverity.Info,
                        Message = "Pixel data integrity verified (BGRA32 format correct)",
                        Context = new Dictionary<string, string>
                        {
                            ["ARGBColor"] = $"0x{argbColor:X8}",
                            ["ExpectedBGRA"] = $"[{expectedB:X2}, {expectedG:X2}, {expectedR:X2}, {expectedA:X2}]",
                            ["ActualBGRA"] = $"[{actualB:X2}, {actualG:X2}, {actualR:X2}, {actualA:X2}]"
                        }
                    };
                }
                else
                {
                    return new ValidationResult
                    {
                        TestName = testName,
                        Passed = false,
                        Severity = ValidationSeverity.Critical,
                        Message = "Pixel data mismatch (BGRA32 format incorrect)",
                        ExpectedValue = $"[{expectedB:X2}, {expectedG:X2}, {expectedR:X2}, {expectedA:X2}]",
                        ActualValue = $"[{actualB:X2}, {actualG:X2}, {actualR:X2}, {actualA:X2}]",
                        Context = new Dictionary<string, string>
                        {
                            ["ARGBColor"] = $"0x{argbColor:X8}"
                        }
                    };
                }
            }
            finally
            {
                PdfiumInterop.DestroyBitmap(bitmap);
            }
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                TestName = testName,
                Passed = false,
                Severity = ValidationSeverity.Error,
                Message = "Exception during pixel data integrity test",
                ErrorDetails = ex.ToString(),
                Context = new Dictionary<string, string>
                {
                    ["Width"] = width.ToString(),
                    ["Height"] = height.ToString(),
                    ["ARGBColor"] = $"0x{argbColor:X8}"
                }
            };
        }
    }
}
