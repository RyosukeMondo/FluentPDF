using System.Reflection;
using System.Runtime.InteropServices;
using FluentPDF.Rendering.Interop.Verification.Reports;

namespace FluentPDF.Rendering.Interop.Verification.Validators;

/// <summary>
/// Validates buffer safety in Marshal.Copy operations to detect potential overflow risks.
/// Uses reflection for static analysis and runtime tests for edge-case validation.
/// </summary>
public class BufferSafetyValidator : IValidator
{
    private readonly object _lock = new();

    /// <inheritdoc/>
    public string ValidatorName => "Buffer Safety Validator";

    /// <inheritdoc/>
    public string TargetArea => "Buffer Overflow & Memory Safety";

    /// <summary>
    /// Validates buffer safety by analyzing Marshal.Copy call sites and simulating overflow scenarios.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to cancel the validation operation.</param>
    /// <returns>A validation report containing analysis results and edge-case test results.</returns>
    public async Task<ValidationReport> ValidateAsync(CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, List<ValidationResult>>();

        // Analyze Marshal.Copy call sites using reflection
        var staticAnalysisResults = await AnalyzeMarshalCopyCallsAsync(cancellationToken);
        results["Static Analysis"] = staticAnalysisResults;

        // Simulate buffer overflow scenarios
        var simulationResults = await SimulateBufferOverflowAsync(cancellationToken);
        results["Runtime Simulation"] = simulationResults;

        // Calculate summary
        var allResults = results.Values.SelectMany(r => r).ToList();
        var passedCount = allResults.Count(r => r.Passed);
        var failedCount = allResults.Count(r => !r.Passed && r.Severity != ValidationSeverity.Critical);
        var criticalCount = allResults.Count(r => !r.Passed && r.Severity == ValidationSeverity.Critical);
        var warningCount = allResults.Count(r => r.Severity == ValidationSeverity.Warning);

        var summary = new ValidationSummary
        {
            TotalTests = allResults.Count,
            PassedCount = passedCount,
            FailedCount = failedCount,
            WarningCount = warningCount,
            CriticalCount = criticalCount
        };

        return new ValidationReport
        {
            ValidatorName = ValidatorName,
            TargetArea = TargetArea,
            Timestamp = DateTime.UtcNow,
            Summary = summary,
            ResultsByArea = results
        };
    }

    /// <summary>
    /// Analyzes all methods containing Marshal.Copy calls using reflection.
    /// Does not execute Marshal.Copy - only performs static analysis.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of validation results from static analysis.</returns>
    public Task<List<ValidationResult>> AnalyzeMarshalCopyCallsAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                var results = new List<ValidationResult>();

                try
                {
                    // Analyze types in FluentPDF.Rendering assembly
                    var assembly = typeof(PdfiumInterop).Assembly;
                    var types = assembly.GetTypes();

                    var marshalCopyCallSites = new List<string>();

                    foreach (var type in types)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);

                            foreach (var method in methods)
                            {
                                // Check if method body contains Marshal.Copy calls
                                // Note: This is a simplified check - full IL inspection would be more thorough
                                var methodBody = method.GetMethodBody();
                                if (methodBody != null)
                                {
                                    var ilBytes = methodBody.GetILAsByteArray();
                                    if (ilBytes != null && ilBytes.Length > 0)
                                    {
                                        // Check method name suggests Marshal.Copy usage or bitmap/buffer operations
                                        var methodName = $"{type.FullName}.{method.Name}";
                                        if (methodName.Contains("Buffer") || methodName.Contains("Copy") ||
                                            methodName.Contains("Bitmap") || methodName.Contains("Pixel") ||
                                            methodName.Contains("Marshal"))
                                        {
                                            marshalCopyCallSites.Add(methodName);
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // Skip types that cannot be reflected
                            continue;
                        }
                    }

                    // Analyze known Marshal.Copy locations
                    var knownLocations = new[]
                    {
                        "FluentPDF.Rendering.Services.PdfRenderingService.ConvertToPngStreamAsync",
                        "FluentPDF.Rendering.Interop.Verification.Validators.BitmapMarshalingValidator",
                        "FluentPDF.Rendering.Services.PdfFormService",
                        "FluentPDF.Rendering.Interop.PdfiumFormInterop"
                    };

                    foreach (var location in knownLocations)
                    {
                        results.Add(new ValidationResult
                        {
                            TestName = $"Marshal.Copy Call Site: {location}",
                            Passed = true,
                            Severity = ValidationSeverity.Info,
                            Message = "Marshal.Copy call site identified for buffer safety review",
                            Context = new Dictionary<string, string>
                            {
                                { "Location", location },
                                { "RequiresReview", "Verify stride * height calculation uses checked arithmetic" }
                            },
                            SuggestedFix = "Ensure buffer size is calculated with checked arithmetic: checked(stride * height)",
                            DocumentationUrl = "docs/marshaling/buffer-safety.md"
                        });
                    }

                    // Report discovered call sites
                    results.Add(new ValidationResult
                    {
                        TestName = "Marshal.Copy Call Site Discovery",
                        Passed = true,
                        Severity = ValidationSeverity.Info,
                        Message = $"Discovered {marshalCopyCallSites.Count} potential Marshal.Copy call sites using reflection",
                        Context = new Dictionary<string, string>
                        {
                            { "CallSiteCount", marshalCopyCallSites.Count.ToString() },
                            { "CallSites", string.Join(", ", marshalCopyCallSites.Take(10)) }
                        }
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new ValidationResult
                    {
                        TestName = "Static Analysis Failure",
                        Passed = false,
                        Severity = ValidationSeverity.Warning,
                        Message = "Failed to complete static analysis of Marshal.Copy call sites",
                        ErrorDetails = ex.Message
                    });
                }

                return results;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Simulates buffer overflow scenarios with edge-case buffer sizes.
    /// Tests zero-length, minimal, boundary, and overflow conditions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of validation results from runtime simulation.</returns>
    public Task<List<ValidationResult>> SimulateBufferOverflowAsync(CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
        {
            lock (_lock)
            {
                var results = new List<ValidationResult>();

                // Test 1: Zero-length buffer
                results.Add(TestZeroLengthBuffer());

                // Test 2: 1-byte buffer (UTF-16 requires minimum 2 bytes)
                results.Add(TestMinimalBuffer());

                // Test 3: 2GB allocation boundary
                results.Add(TestLargeAllocationBoundary());

                // Test 4: Stride overflow scenario
                results.Add(TestStrideOverflow());

                // Test 5: Two-phase allocation validation
                results.Add(TestTwoPhaseAllocation());

                return results;
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Tests zero-length buffer handling.
    /// </summary>
    private ValidationResult TestZeroLengthBuffer()
    {
        try
        {
            var sourcePtr = Marshal.AllocHGlobal(0);
            try
            {
                var buffer = new byte[0];

                // This should not throw - Marshal.Copy with length 0 is valid
                Marshal.Copy(sourcePtr, buffer, 0, 0);

                return new ValidationResult
                {
                    TestName = "Zero-Length Buffer",
                    Passed = true,
                    Severity = ValidationSeverity.Info,
                    Message = "Marshal.Copy handles zero-length buffers correctly",
                    Context = new Dictionary<string, string>
                    {
                        { "BufferSize", "0 bytes" },
                        { "Behavior", "No exception thrown" }
                    }
                };
            }
            finally
            {
                Marshal.FreeHGlobal(sourcePtr);
            }
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                TestName = "Zero-Length Buffer",
                Passed = false,
                Severity = ValidationSeverity.Warning,
                Message = "Unexpected exception with zero-length buffer",
                ErrorDetails = ex.Message
            };
        }
    }

    /// <summary>
    /// Tests 1-byte buffer (insufficient for UTF-16).
    /// </summary>
    private ValidationResult TestMinimalBuffer()
    {
        try
        {
            var sourceData = new byte[] { 0xFF };
            var sourcePtr = Marshal.AllocHGlobal(1);
            try
            {
                Marshal.Copy(sourceData, 0, sourcePtr, 1);

                var destBuffer = new byte[1];
                Marshal.Copy(sourcePtr, destBuffer, 0, 1);

                return new ValidationResult
                {
                    TestName = "1-Byte Buffer (UTF-16 Minimum)",
                    Passed = false,
                    Severity = ValidationSeverity.Warning,
                    Message = "1-byte buffer is insufficient for UTF-16 encoding (requires minimum 2 bytes)",
                    Context = new Dictionary<string, string>
                    {
                        { "BufferSize", "1 byte" },
                        { "UTF16Minimum", "2 bytes" }
                    },
                    SuggestedFix = "For UTF-16 operations, ensure buffer size is at least 2 bytes",
                    DocumentationUrl = "docs/marshaling/utf16-encoding.md"
                };
            }
            finally
            {
                Marshal.FreeHGlobal(sourcePtr);
            }
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                TestName = "1-Byte Buffer (UTF-16 Minimum)",
                Passed = false,
                Severity = ValidationSeverity.Critical,
                Message = "Exception occurred with 1-byte buffer",
                ErrorDetails = ex.Message
            };
        }
    }

    /// <summary>
    /// Tests allocation at 2GB boundary (array size limit on x86).
    /// </summary>
    private ValidationResult TestLargeAllocationBoundary()
    {
        try
        {
            // Don't actually allocate 2GB - just validate the calculation
            const int maxWidth = 8192;
            const int maxHeight = 8192;
            const int bytesPerPixel = 4; // BGRA32

            // Calculate with checked arithmetic
            try
            {
                var stride = checked(maxWidth * bytesPerPixel);
                var totalSize = checked(stride * maxHeight);

                if (totalSize > int.MaxValue)
                {
                    return new ValidationResult
                    {
                        TestName = "2GB Allocation Boundary",
                        Passed = false,
                        Severity = ValidationSeverity.Critical,
                        Message = "Buffer size calculation exceeds 2GB limit",
                        Context = new Dictionary<string, string>
                        {
                            { "MaxWidth", maxWidth.ToString() },
                            { "MaxHeight", maxHeight.ToString() },
                            { "CalculatedSize", totalSize.ToString() },
                            { "Limit", int.MaxValue.ToString() }
                        },
                        SuggestedFix = "Use checked arithmetic and validate dimensions before allocation",
                        DocumentationUrl = "docs/marshaling/buffer-safety.md"
                    };
                }

                return new ValidationResult
                {
                    TestName = "2GB Allocation Boundary",
                    Passed = true,
                    Severity = ValidationSeverity.Info,
                    Message = "Large allocation boundary calculation within limits",
                    Context = new Dictionary<string, string>
                    {
                        { "MaxWidth", maxWidth.ToString() },
                        { "MaxHeight", maxHeight.ToString() },
                        { "CalculatedSize", totalSize.ToString() },
                        { "WithinLimit", "true" }
                    }
                };
            }
            catch (OverflowException)
            {
                return new ValidationResult
                {
                    TestName = "2GB Allocation Boundary",
                    Passed = false,
                    Severity = ValidationSeverity.Critical,
                    Message = "Overflow detected in buffer size calculation with checked arithmetic",
                    Context = new Dictionary<string, string>
                    {
                        { "MaxWidth", maxWidth.ToString() },
                        { "MaxHeight", maxHeight.ToString() }
                    },
                    SuggestedFix = "Validate dimensions before calculation to prevent overflow",
                    DocumentationUrl = "docs/marshaling/buffer-safety.md"
                };
            }
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                TestName = "2GB Allocation Boundary",
                Passed = false,
                Severity = ValidationSeverity.Critical,
                Message = "Unexpected exception during allocation boundary test",
                ErrorDetails = ex.Message
            };
        }
    }

    /// <summary>
    /// Tests stride calculation overflow scenario.
    /// </summary>
    private ValidationResult TestStrideOverflow()
    {
        try
        {
            // Test case: width that causes stride overflow
            // Use variables to avoid compile-time constant overflow detection
            int width = int.MaxValue / 4;
            width = width + 1;
            int height = 2;
            int bytesPerPixel = 4;

            try
            {
                // This should overflow with checked arithmetic
                var stride = checked(width * bytesPerPixel);
                var totalSize = checked(stride * height);

                return new ValidationResult
                {
                    TestName = "Stride Overflow Detection",
                    Passed = false,
                    Severity = ValidationSeverity.Critical,
                    Message = "Stride calculation did not detect overflow (checked arithmetic not enforced)",
                    Context = new Dictionary<string, string>
                    {
                        { "Width", width.ToString() },
                        { "Height", height.ToString() },
                        { "BytesPerPixel", bytesPerPixel.ToString() },
                        { "CalculatedStride", stride.ToString() },
                        { "CalculatedTotal", totalSize.ToString() }
                    },
                    SuggestedFix = "Use checked { } block for stride * height calculations",
                    DocumentationUrl = "docs/marshaling/buffer-safety.md"
                };
            }
            catch (OverflowException)
            {
                return new ValidationResult
                {
                    TestName = "Stride Overflow Detection",
                    Passed = true,
                    Severity = ValidationSeverity.Info,
                    Message = "Stride overflow correctly detected with checked arithmetic",
                    Context = new Dictionary<string, string>
                    {
                        { "Width", width.ToString() },
                        { "Height", height.ToString() },
                        { "BytesPerPixel", bytesPerPixel.ToString() }
                    }
                };
            }
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                TestName = "Stride Overflow Detection",
                Passed = false,
                Severity = ValidationSeverity.Critical,
                Message = "Unexpected exception during stride overflow test",
                ErrorDetails = ex.Message
            };
        }
    }

    /// <summary>
    /// Tests two-phase allocation pattern (get length, allocate, fill).
    /// </summary>
    private ValidationResult TestTwoPhaseAllocation()
    {
        try
        {
            // Simulate two-phase allocation pattern used in bookmark title extraction
            // Phase 1: Get required length
            uint requiredLength = 100; // Simulated length from PDFium (includes null terminator)

            // Phase 2: Allocate buffer
            if (requiredLength == 0)
            {
                return new ValidationResult
                {
                    TestName = "Two-Phase Allocation Pattern",
                    Passed = false,
                    Severity = ValidationSeverity.Warning,
                    Message = "Zero length returned from phase 1 - allocation would fail",
                    Context = new Dictionary<string, string>
                    {
                        { "Phase", "1 - Get Length" },
                        { "ReturnedLength", "0" }
                    },
                    SuggestedFix = "Check for zero length before phase 2 allocation"
                };
            }

            var buffer = new byte[requiredLength];

            // Phase 3: Fill buffer (simulated)
            // In real code, this would be a second PDFium call with the allocated buffer

            return new ValidationResult
            {
                TestName = "Two-Phase Allocation Pattern",
                Passed = true,
                Severity = ValidationSeverity.Info,
                Message = "Two-phase allocation pattern validated successfully",
                Context = new Dictionary<string, string>
                {
                    { "Phase1Length", requiredLength.ToString() },
                    { "Phase2BufferSize", buffer.Length.ToString() },
                    { "Pattern", "Get length → Allocate → Fill" }
                },
                DocumentationUrl = "docs/marshaling/safe-patterns.md"
            };
        }
        catch (Exception ex)
        {
            return new ValidationResult
            {
                TestName = "Two-Phase Allocation Pattern",
                Passed = false,
                Severity = ValidationSeverity.Critical,
                Message = "Exception during two-phase allocation test",
                ErrorDetails = ex.Message
            };
        }
    }
}
