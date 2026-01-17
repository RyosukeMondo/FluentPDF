using FluentPDF.Verification.Core;
using FluentResults;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FluentPDF.Verification.Pdfium;

/// <summary>
/// Verifies PDFium P/Invoke signatures against actual DLL exports.
/// Validates calling conventions, parameter types, return types, and marshalling attributes.
/// </summary>
public class PdfiumSignatureVerifier : ILibraryVerifier<PdfiumVerificationResult>
{
    private readonly VerificationOptions _options;
    private readonly IDllAnalyzer _dllAnalyzer;

    public string LibraryName => "PDFium";

    public PdfiumSignatureVerifier(VerificationOptions options, IDllAnalyzer dllAnalyzer)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _dllAnalyzer = dllAnalyzer ?? throw new ArgumentNullException(nameof(dllAnalyzer));
    }

    public Result ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.DllPath))
        {
            return Result.Fail("DLL path is required");
        }

        if (!File.Exists(_options.DllPath))
        {
            return Result.Fail($"PDFium DLL not found at: {_options.DllPath}");
        }

        return Result.Ok();
    }

    public async Task<Result<PdfiumVerificationResult>> VerifyAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<VerificationResult>();

        // Analyze the DLL
        var dllAnalysisResult = await _dllAnalyzer.AnalyzeAsync(_options.DllPath, cancellationToken);
        if (dllAnalysisResult.IsFailed)
        {
            return Result.Fail<PdfiumVerificationResult>($"DLL analysis failed: {dllAnalysisResult.Errors[0].Message}");
        }

        var dllInfo = dllAnalysisResult.Value;

        // Get all exported functions from the DLL
        var exportsResult = _dllAnalyzer.GetExportedFunctions();
        if (exportsResult.IsFailed)
        {
            return Result.Fail<PdfiumVerificationResult>($"Failed to get DLL exports: {exportsResult.Errors[0].Message}");
        }

        var dllExports = new HashSet<string>(exportsResult.Value, StringComparer.Ordinal);

        // Get all P/Invoke declarations from PdfiumInterop
        var pInvokeSignatures = GetPInvokeDeclarations();

        // Verify each P/Invoke declaration
        foreach (var (functionName, methodInfo) in pInvokeSignatures)
        {
            var testStopwatch = Stopwatch.StartNew();
            var verificationResult = VerifySignature(functionName, methodInfo, dllExports);
            testStopwatch.Stop();

            results.Add(new VerificationResult
            {
                TestName = $"Signature: {functionName}",
                Success = verificationResult.Matches,
                ErrorMessage = verificationResult.Matches ? null : $"Signature mismatch: {string.Join("; ", verificationResult.Differences)}",
                SuggestedFix = verificationResult.SuggestedFix,
                Duration = testStopwatch.Elapsed,
                Metadata = new Dictionary<string, object>
                {
                    ["DeclaredSignature"] = verificationResult.DeclaredSignature,
                    ["ActualSignature"] = verificationResult.ActualSignature,
                    ["FunctionName"] = functionName
                }
            });
        }

        // Check for missing exports
        foreach (var (functionName, _) in pInvokeSignatures)
        {
            if (!dllExports.Contains(functionName))
            {
                results.Add(new VerificationResult
                {
                    TestName = $"Export exists: {functionName}",
                    Success = false,
                    ErrorMessage = $"Function '{functionName}' is declared in P/Invoke but not exported by the DLL",
                    SuggestedFix = $"Remove the P/Invoke declaration for '{functionName}' or ensure the correct PDFium DLL is being used",
                    Duration = TimeSpan.Zero
                });
            }
        }

        stopwatch.Stop();

        var summary = new VerificationSummary
        {
            TotalTests = results.Count,
            PassedTests = results.Count(r => r.Success),
            FailedTests = results.Count(r => !r.Success),
            Results = results,
            TotalDuration = stopwatch.Elapsed,
            LibraryVersion = dllInfo.Version
        };

        var signatureMatches = results
            .Where(r => r.TestName.StartsWith("Signature:"))
            .Select(r => new SignatureMatch
            {
                FunctionName = (string)r.Metadata["FunctionName"],
                Matches = r.Success,
                DeclaredSignature = (string)r.Metadata["DeclaredSignature"],
                ActualSignature = (string)r.Metadata["ActualSignature"],
                SuggestedFix = r.SuggestedFix,
                Differences = r.ErrorMessage?.Split("; ").ToList() ?? new List<string>()
            })
            .ToList();

        var pdfiumResult = new PdfiumVerificationResult
        {
            Summary = summary,
            DllInfo = dllInfo,
            SignatureMatches = signatureMatches,
            MissingExports = results
                .Where(r => r.TestName.StartsWith("Export exists:") && !r.Success)
                .Select(r => r.TestName.Replace("Export exists: ", ""))
                .ToList()
        };

        return Result.Ok(pdfiumResult);
    }

    /// <summary>
    /// Gets all P/Invoke declarations from the PdfiumInterop class using reflection.
    /// </summary>
    private Dictionary<string, MethodInfo> GetPInvokeDeclarations()
    {
        var pInvokeMethods = new Dictionary<string, MethodInfo>();

        // Get the PdfiumInterop type
        var pdfiumInteropType = typeof(FluentPDF.Rendering.Interop.PdfiumInterop);

        // Get all methods (public and private)
        var methods = pdfiumInteropType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

        foreach (var method in methods)
        {
            // Check if method has DllImport attribute
            var dllImportAttr = method.GetCustomAttribute<DllImportAttribute>();
            if (dllImportAttr != null)
            {
                // Get the entry point name (function name in the DLL)
                var entryPoint = dllImportAttr.EntryPoint ?? method.Name;
                pInvokeMethods[entryPoint] = method;
            }
        }

        return pInvokeMethods;
    }

    /// <summary>
    /// Verifies a single P/Invoke signature against the DLL export.
    /// </summary>
    private SignatureMatch VerifySignature(string functionName, MethodInfo methodInfo, HashSet<string> dllExports)
    {
        var differences = new List<string>();
        var dllImportAttr = methodInfo.GetCustomAttribute<DllImportAttribute>()!;

        // Check if function is exported
        if (!dllExports.Contains(functionName))
        {
            differences.Add($"Function not exported by DLL");
        }

        // Get calling convention
        var callingConvention = dllImportAttr.CallingConvention;
        var expectedConvention = CallingConvention.Cdecl; // PDFium uses Cdecl

        if (callingConvention != expectedConvention)
        {
            differences.Add($"Calling convention mismatch: declared={callingConvention}, expected={expectedConvention}");
        }

        // Build signature strings
        var declaredSignature = BuildDeclaredSignature(methodInfo, dllImportAttr);
        var actualSignature = BuildActualSignature(functionName, methodInfo);

        // Validate return type
        var returnType = methodInfo.ReturnType;
        var returnTypeValidation = ValidateReturnType(returnType, functionName);
        if (!string.IsNullOrEmpty(returnTypeValidation))
        {
            differences.Add(returnTypeValidation);
        }

        // Validate parameters
        var parameters = methodInfo.GetParameters();
        foreach (var param in parameters)
        {
            var paramValidation = ValidateParameter(param, functionName);
            if (!string.IsNullOrEmpty(paramValidation))
            {
                differences.Add(paramValidation);
            }
        }

        // Generate suggested fix if there are differences
        string? suggestedFix = null;
        if (differences.Count > 0)
        {
            suggestedFix = GenerateSuggestedFix(functionName, methodInfo, differences);
        }

        return new SignatureMatch
        {
            FunctionName = functionName,
            Matches = differences.Count == 0,
            DeclaredSignature = declaredSignature,
            ActualSignature = actualSignature,
            Differences = differences,
            SuggestedFix = suggestedFix
        };
    }

    /// <summary>
    /// Builds a signature string from the declared P/Invoke method.
    /// </summary>
    private string BuildDeclaredSignature(MethodInfo methodInfo, DllImportAttribute dllImportAttr)
    {
        var returnType = GetTypeDisplayName(methodInfo.ReturnType);
        var parameters = methodInfo.GetParameters();
        var paramStrings = parameters.Select(p =>
        {
            var marshalAsAttr = p.GetCustomAttribute<MarshalAsAttribute>();
            var marshalInfo = marshalAsAttr != null ? $"[MarshalAs({marshalAsAttr.Value})] " : "";
            return $"{marshalInfo}{GetTypeDisplayName(p.ParameterType)} {p.Name}";
        });

        return $"[CallingConvention={dllImportAttr.CallingConvention}] {returnType} {methodInfo.Name}({string.Join(", ", paramStrings)})";
    }

    /// <summary>
    /// Builds the actual signature string (simplified, as we can't get full native signature without debug info).
    /// </summary>
    private string BuildActualSignature(string functionName, MethodInfo methodInfo)
    {
        // In a real implementation, we would parse the DLL's debug info or type library
        // For now, we'll just note that the function is exported
        return $"[Exported] {functionName}";
    }

    /// <summary>
    /// Validates that a return type is appropriate for P/Invoke.
    /// </summary>
    private string? ValidateReturnType(Type returnType, string functionName)
    {
        // Check for common issues with return types
        if (returnType == typeof(string))
        {
            // Returning strings from native code is problematic - should use StringBuilder or IntPtr
            return "Return type 'string' may cause marshalling issues; consider using IntPtr or SafeHandle";
        }

        if (returnType.IsClass && !returnType.IsSubclassOf(typeof(SafeHandle)) && returnType != typeof(string))
        {
            // Non-SafeHandle classes as return types can be problematic
            return $"Return type '{returnType.Name}' is a class but not a SafeHandle; may cause marshalling issues";
        }

        return null;
    }

    /// <summary>
    /// Validates that a parameter is correctly marshalled for P/Invoke.
    /// </summary>
    private string? ValidateParameter(System.Reflection.ParameterInfo param, string functionName)
    {
        var paramType = param.ParameterType;

        // Check for string parameters without marshalling attributes
        if (paramType == typeof(string))
        {
            var marshalAsAttr = param.GetCustomAttribute<MarshalAsAttribute>();
            if (marshalAsAttr == null)
            {
                return $"Parameter '{param.Name}' is string without MarshalAs attribute; marshalling behavior is platform-dependent";
            }
        }

        // Check for arrays without proper marshalling
        if (paramType.IsArray)
        {
            var marshalAsAttr = param.GetCustomAttribute<MarshalAsAttribute>();
            if (marshalAsAttr == null && !param.ParameterType.GetElementType()!.IsValueType)
            {
                return $"Parameter '{param.Name}' is array without MarshalAs attribute; may cause marshalling issues";
            }
        }

        return null;
    }

    /// <summary>
    /// Generates a suggested fix for signature mismatches.
    /// </summary>
    private string GenerateSuggestedFix(string functionName, MethodInfo methodInfo, List<string> differences)
    {
        var fixes = new List<string>();

        foreach (var difference in differences)
        {
            if (difference.Contains("Calling convention mismatch"))
            {
                fixes.Add($"Change [DllImport] CallingConvention to CallingConvention.Cdecl");
            }
            else if (difference.Contains("Return type 'string'"))
            {
                fixes.Add("Change return type from 'string' to 'IntPtr' and marshal manually");
            }
            else if (difference.Contains("without MarshalAs"))
            {
                fixes.Add($"Add [MarshalAs(UnmanagedType.LPStr)] or [MarshalAs(UnmanagedType.LPWStr)] attribute");
            }
            else if (difference.Contains("Function not exported"))
            {
                fixes.Add($"Verify PDFium DLL version or remove unused P/Invoke declaration");
            }
        }

        if (fixes.Count == 0)
        {
            return $"Review P/Invoke declaration for {functionName} against PDFium documentation";
        }

        return string.Join("; ", fixes);
    }

    /// <summary>
    /// Gets a display name for a type (handles common P/Invoke types).
    /// </summary>
    private string GetTypeDisplayName(Type type)
    {
        if (type == typeof(void))
            return "void";
        if (type == typeof(int))
            return "int";
        if (type == typeof(uint))
            return "uint";
        if (type == typeof(bool))
            return "bool";
        if (type == typeof(IntPtr))
            return "IntPtr";
        if (type == typeof(string))
            return "string";
        if (type == typeof(byte[]))
            return "byte[]";
        if (type == typeof(float))
            return "float";
        if (type == typeof(double))
            return "double";

        return type.Name;
    }
}

/// <summary>
/// Result of PDFium verification, including signature matches and DLL information.
/// </summary>
public record PdfiumVerificationResult
{
    /// <summary>
    /// Overall verification summary.
    /// </summary>
    public required VerificationSummary Summary { get; init; }

    /// <summary>
    /// Information about the analyzed PDFium DLL.
    /// </summary>
    public required DllInfo DllInfo { get; init; }

    /// <summary>
    /// Detailed signature match results for each P/Invoke declaration.
    /// </summary>
    public required IReadOnlyList<SignatureMatch> SignatureMatches { get; init; }

    /// <summary>
    /// List of P/Invoke declarations that reference functions not exported by the DLL.
    /// </summary>
    public required IReadOnlyList<string> MissingExports { get; init; }
}
