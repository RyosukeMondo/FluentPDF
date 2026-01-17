# Extending the Verification Framework for Other Libraries

## Overview

The FluentPDF verification framework is designed to be extensible and reusable for verifying P/Invoke declarations against any native library, not just PDFium. This guide shows you how to create verifiers for other native libraries like SkiaSharp, SQLite, LibreSSL, or any other P/Invoke dependency in your project.

## Why Extend the Framework

The verification framework catches critical issues with P/Invoke declarations:

- **Function signature mismatches** - Prevents crashes from incorrect parameter types
- **Marshalling errors** - Detects garbage values from improper marshalling
- **Missing exports** - Catches typos in function names
- **Calling convention issues** - Ensures correct stack cleanup
- **Type safety** - Validates return types and parameter types

By extending the framework, you gain these benefits for any native library dependency.

## Architecture Overview

The verification framework consists of three layers:

### 1. Core Framework Layer (`FluentPDF.Verification.Core`)

Generic, reusable components for any library:

- **`ILibraryVerifier<TResult>`** - Main interface for library verifiers
- **`IDllAnalyzer`** - Analyzes native DLL exports
- **`IVerificationReporter`** - Generates reports in various formats
- **Models** - `VerificationResult`, `FunctionSignature`, `DllInfo`, etc.

### 2. Library-Specific Layer (e.g., `FluentPDF.Verification.Pdfium`)

Library-specific verification logic:

- **Signature Verifier** - Validates P/Invoke declarations
- **Return Type Verifier** - Validates return values are reasonable
- **Behavior Verifier** - Tests functional behavior with known inputs/outputs

### 3. CLI Application Layer (`FluentPDF.Verification.Cli`)

User-facing command-line interface:

- Argument parsing
- Execution orchestration
- Report generation and output

## Step-by-Step Tutorial: Creating a SkiaSharp Verifier

Let's create a complete verifier for SkiaSharp, a popular 2D graphics library.

### Step 1: Create Library-Specific Project

Create a new class library project for your verifier:

```bash
# Create the project
dotnet new classlib -n FluentPDF.Verification.Skia -f net8.0

# Add to solution
dotnet sln add src/FluentPDF.Verification.Skia/FluentPDF.Verification.Skia.csproj

# Add framework reference
cd src/FluentPDF.Verification.Skia
dotnet add reference ../FluentPDF.Verification.Core/FluentPDF.Verification.Core.csproj

# Add necessary NuGet packages
dotnet add package FluentResults
dotnet add package Serilog
```

**Project structure:**
```
src/FluentPDF.Verification.Skia/
├── SkiaSignatureVerifier.cs       # Signature verification
├── SkiaReturnTypeVerifier.cs      # Return type validation
├── SkiaBehaviorVerifier.cs        # Functional behavior tests
├── SkiaVerificationResult.cs      # Result models
└── FluentPDF.Verification.Skia.csproj
```

### Step 2: Create Result Model

Define the result type for your verifier:

```csharp
// SkiaVerificationResult.cs
using FluentPDF.Verification.Core;

namespace FluentPDF.Verification.Skia;

/// <summary>
/// Result of SkiaSharp library verification.
/// </summary>
public record SkiaVerificationResult
{
    /// <summary>
    /// Summary of all verification tests.
    /// </summary>
    public required VerificationSummary Summary { get; init; }

    /// <summary>
    /// SkiaSharp library version detected.
    /// </summary>
    public string? SkiaVersion { get; init; }

    /// <summary>
    /// DLL information for the analyzed library.
    /// </summary>
    public required DllInfo DllInfo { get; init; }
}
```

### Step 3: Implement Signature Verifier

Create a verifier that validates P/Invoke signatures against DLL exports:

```csharp
// SkiaSignatureVerifier.cs
using FluentPDF.Verification.Core;
using FluentResults;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FluentPDF.Verification.Skia;

/// <summary>
/// Verifies SkiaSharp P/Invoke signatures against actual DLL exports.
/// </summary>
public class SkiaSignatureVerifier : ILibraryVerifier<SkiaVerificationResult>
{
    private readonly VerificationOptions _options;
    private readonly IDllAnalyzer _dllAnalyzer;

    public string LibraryName => "SkiaSharp";

    public SkiaSignatureVerifier(VerificationOptions options, IDllAnalyzer dllAnalyzer)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _dllAnalyzer = dllAnalyzer ?? throw new ArgumentNullException(nameof(dllAnalyzer));
    }

    public Result ValidateConfiguration()
    {
        // Validate DLL path exists
        if (string.IsNullOrWhiteSpace(_options.DllPath))
        {
            return Result.Fail("DLL path is required");
        }

        if (!File.Exists(_options.DllPath))
        {
            return Result.Fail($"SkiaSharp DLL not found at: {_options.DllPath}");
        }

        return Result.Ok();
    }

    public async Task<Result<SkiaVerificationResult>> VerifyAsync(
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<VerificationResult>();

        // Analyze the DLL
        var dllAnalysisResult = await _dllAnalyzer.AnalyzeAsync(
            _options.DllPath,
            cancellationToken);

        if (dllAnalysisResult.IsFailed)
        {
            return Result.Fail<SkiaVerificationResult>(
                $"DLL analysis failed: {dllAnalysisResult.Errors[0].Message}");
        }

        var dllInfo = dllAnalysisResult.Value;

        // Get all exported functions
        var exportsResult = _dllAnalyzer.GetExportedFunctions();
        if (exportsResult.IsFailed)
        {
            return Result.Fail<SkiaVerificationResult>(
                $"Failed to get DLL exports: {exportsResult.Errors[0].Message}");
        }

        var dllExports = new HashSet<string>(exportsResult.Value, StringComparer.Ordinal);

        // Get P/Invoke declarations from your interop class
        var pInvokeSignatures = GetPInvokeDeclarations();

        // Verify each signature
        foreach (var (functionName, methodInfo) in pInvokeSignatures)
        {
            var testStopwatch = Stopwatch.StartNew();
            var result = VerifySignature(functionName, methodInfo, dllExports);
            testStopwatch.Stop();

            results.Add(new VerificationResult
            {
                TestName = $"Signature: {functionName}",
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
                SuggestedFix = result.SuggestedFix,
                Duration = testStopwatch.Elapsed,
                Metadata = result.Metadata
            });
        }

        stopwatch.Stop();

        var summary = new VerificationSummary
        {
            TotalTests = results.Count,
            PassedTests = results.Count(r => r.Success),
            FailedTests = results.Count(r => !r.Success),
            Results = results,
            TotalDuration = stopwatch.Elapsed,
            LibraryVersion = DetectSkiaVersion(dllInfo)
        };

        return Result.Ok(new SkiaVerificationResult
        {
            Summary = summary,
            SkiaVersion = summary.LibraryVersion,
            DllInfo = dllInfo
        });
    }

    /// <summary>
    /// Get all P/Invoke declarations from your SkiaInterop class.
    /// Adjust the type and namespace to match your interop class.
    /// </summary>
    private Dictionary<string, MethodInfo> GetPInvokeDeclarations()
    {
        // Replace with your actual interop class
        var interopType = Type.GetType("YourNamespace.SkiaInterop, YourAssembly");

        if (interopType == null)
        {
            return new Dictionary<string, MethodInfo>();
        }

        var methods = interopType.GetMethods(
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Static);

        return methods
            .Where(m => m.GetCustomAttribute<DllImportAttribute>() != null)
            .ToDictionary(m => m.Name, m => m);
    }

    /// <summary>
    /// Verify a single function signature.
    /// </summary>
    private VerificationResult VerifySignature(
        string functionName,
        MethodInfo methodInfo,
        HashSet<string> dllExports)
    {
        // Check if function exists in DLL
        if (!dllExports.Contains(functionName))
        {
            return new VerificationResult
            {
                TestName = functionName,
                Success = false,
                ErrorMessage = $"Function '{functionName}' not found in DLL exports",
                SuggestedFix = "Check function name spelling or library version",
                Duration = TimeSpan.Zero,
                Metadata = new Dictionary<string, object>
                {
                    ["FunctionName"] = functionName
                }
            };
        }

        // Get actual signature from DLL
        var signatureResult = _dllAnalyzer.GetFunctionSignature(functionName);
        if (signatureResult.IsFailed)
        {
            return new VerificationResult
            {
                TestName = functionName,
                Success = false,
                ErrorMessage = $"Failed to get signature: {signatureResult.Errors[0].Message}",
                Duration = TimeSpan.Zero
            };
        }

        var actualSignature = signatureResult.Value;

        // Compare signatures (implement your comparison logic)
        var matches = CompareSignatures(methodInfo, actualSignature, out var differences);

        return new VerificationResult
        {
            TestName = functionName,
            Success = matches,
            ErrorMessage = matches ? null : $"Signature mismatch: {string.Join("; ", differences)}",
            SuggestedFix = matches ? null : GenerateFix(methodInfo, actualSignature),
            Duration = TimeSpan.Zero,
            Metadata = new Dictionary<string, object>
            {
                ["DeclaredSignature"] = FormatMethodSignature(methodInfo),
                ["ActualSignature"] = actualSignature.FullSignature,
                ["FunctionName"] = functionName
            }
        };
    }

    /// <summary>
    /// Compare method signature with DLL signature.
    /// </summary>
    private bool CompareSignatures(
        MethodInfo methodInfo,
        FunctionSignature dllSignature,
        out List<string> differences)
    {
        differences = new List<string>();

        // Compare return types
        var declaredReturn = methodInfo.ReturnType.Name;
        if (declaredReturn != dllSignature.ReturnType)
        {
            differences.Add($"Return type: declared '{declaredReturn}', actual '{dllSignature.ReturnType}'");
        }

        // Compare parameter count
        var parameters = methodInfo.GetParameters();
        if (parameters.Length != dllSignature.Parameters.Count)
        {
            differences.Add($"Parameter count: declared {parameters.Length}, actual {dllSignature.Parameters.Count}");
        }

        // Compare parameter types
        for (int i = 0; i < Math.Min(parameters.Length, dllSignature.Parameters.Count); i++)
        {
            var declaredType = parameters[i].ParameterType.Name;
            var actualType = dllSignature.Parameters[i].Type;

            if (declaredType != actualType)
            {
                differences.Add($"Parameter {i}: declared '{declaredType}', actual '{actualType}'");
            }
        }

        return differences.Count == 0;
    }

    private string FormatMethodSignature(MethodInfo method)
    {
        var parameters = string.Join(", ", method.GetParameters()
            .Select(p => $"{p.ParameterType.Name} {p.Name}"));
        return $"{method.ReturnType.Name} {method.Name}({parameters})";
    }

    private string? GenerateFix(MethodInfo method, FunctionSignature actual)
    {
        // Generate suggested fix based on differences
        return $"Update signature to match: {actual.FullSignature}";
    }

    private string? DetectSkiaVersion(DllInfo dllInfo)
    {
        // Extract version from DLL info
        return dllInfo.Version ?? dllInfo.Metadata.GetValueOrDefault("Version")?.ToString();
    }
}
```

### Step 4: Implement Return Type Verifier

Create a verifier that validates return values are reasonable:

```csharp
// SkiaReturnTypeVerifier.cs
using FluentPDF.Verification.Core;
using FluentResults;
using System.Diagnostics;

namespace FluentPDF.Verification.Skia;

/// <summary>
/// Validates SkiaSharp function return values are within expected ranges.
/// </summary>
public class SkiaReturnTypeVerifier : ILibraryVerifier<SkiaVerificationResult>
{
    private readonly VerificationOptions _options;

    public string LibraryName => "SkiaSharp";

    public SkiaReturnTypeVerifier(VerificationOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Result ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.DllPath))
        {
            return Result.Fail("DLL path is required");
        }

        if (!File.Exists(_options.DllPath))
        {
            return Result.Fail($"SkiaSharp DLL not found at: {_options.DllPath}");
        }

        return Result.Ok();
    }

    public async Task<Result<SkiaVerificationResult>> VerifyAsync(
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<VerificationResult>();

        // Test common functions with known expected ranges
        results.Add(await TestColorCreation());
        results.Add(await TestCanvasOperations());
        results.Add(await TestImageDimensions());

        stopwatch.Stop();

        var summary = new VerificationSummary
        {
            TotalTests = results.Count,
            PassedTests = results.Count(r => r.Success),
            FailedTests = results.Count(r => !r.Success),
            Results = results,
            TotalDuration = stopwatch.Elapsed
        };

        var dllInfo = new DllInfo
        {
            FilePath = _options.DllPath,
            Architecture = "x64",
            ExportCount = 0,
            FileSizeBytes = new FileInfo(_options.DllPath).Length
        };

        return Result.Ok(new SkiaVerificationResult
        {
            Summary = summary,
            DllInfo = dllInfo
        });
    }

    private async Task<VerificationResult> TestColorCreation()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Call your SkiaInterop methods to create a color
            // Example: var color = SkiaInterop.sk_color_set_argb(255, 128, 64, 32);

            // Validate the return value is reasonable
            // var isValid = color != 0 && color != IntPtr.Zero;

            stopwatch.Stop();

            return new VerificationResult
            {
                TestName = "Return Type: sk_color_set_argb",
                Success = true, // Set based on validation
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new VerificationResult
            {
                TestName = "Return Type: sk_color_set_argb",
                Success = false,
                ErrorMessage = $"Exception: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<VerificationResult> TestCanvasOperations()
    {
        // Similar pattern for canvas operations
        return new VerificationResult
        {
            TestName = "Return Type: Canvas Operations",
            Success = true,
            Duration = TimeSpan.Zero
        };
    }

    private async Task<VerificationResult> TestImageDimensions()
    {
        // Test image dimension functions return reasonable values
        return new VerificationResult
        {
            TestName = "Return Type: Image Dimensions",
            Success = true,
            Duration = TimeSpan.Zero
        };
    }
}
```

### Step 5: Implement Behavior Verifier

Create a verifier that tests functional behavior:

```csharp
// SkiaBehaviorVerifier.cs
using FluentPDF.Verification.Core;
using FluentResults;
using System.Diagnostics;

namespace FluentPDF.Verification.Skia;

/// <summary>
/// Verifies SkiaSharp functional behavior with known inputs/outputs.
/// </summary>
public class SkiaBehaviorVerifier : ILibraryVerifier<SkiaVerificationResult>
{
    private readonly VerificationOptions _options;

    public string LibraryName => "SkiaSharp";

    public SkiaBehaviorVerifier(VerificationOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public Result ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.DllPath))
        {
            return Result.Fail("DLL path is required");
        }

        if (!File.Exists(_options.DllPath))
        {
            return Result.Fail($"SkiaSharp DLL not found at: {_options.DllPath}");
        }

        return Result.Ok();
    }

    public async Task<Result<SkiaVerificationResult>> VerifyAsync(
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new List<VerificationResult>();

        // Test library initialization
        results.Add(await TestLibraryInitialization());

        // Test basic drawing operations
        results.Add(await TestBasicDrawing());

        // Test image loading/saving
        results.Add(await TestImageOperations());

        // Test resource cleanup
        results.Add(await TestResourceCleanup());

        stopwatch.Stop();

        var summary = new VerificationSummary
        {
            TotalTests = results.Count,
            PassedTests = results.Count(r => r.Success),
            FailedTests = results.Count(r => !r.Success),
            Results = results,
            TotalDuration = stopwatch.Elapsed
        };

        var dllInfo = new DllInfo
        {
            FilePath = _options.DllPath,
            Architecture = "x64",
            ExportCount = 0,
            FileSizeBytes = new FileInfo(_options.DllPath).Length
        };

        return Result.Ok(new SkiaVerificationResult
        {
            Summary = summary,
            DllInfo = dllInfo
        });
    }

    private async Task<VerificationResult> TestLibraryInitialization()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Initialize SkiaSharp library
            // Example: SkiaInterop.Initialize();

            stopwatch.Stop();

            return new VerificationResult
            {
                TestName = "Behavior: Library Initialization",
                Success = true,
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            return new VerificationResult
            {
                TestName = "Behavior: Library Initialization",
                Success = false,
                ErrorMessage = $"Initialization failed: {ex.Message}",
                Duration = stopwatch.Elapsed
            };
        }
    }

    private async Task<VerificationResult> TestBasicDrawing()
    {
        // Test basic drawing operations (create canvas, draw shapes, etc.)
        return new VerificationResult
        {
            TestName = "Behavior: Basic Drawing",
            Success = true,
            Duration = TimeSpan.Zero
        };
    }

    private async Task<VerificationResult> TestImageOperations()
    {
        // Test image loading, manipulation, and saving
        return new VerificationResult
        {
            TestName = "Behavior: Image Operations",
            Success = true,
            Duration = TimeSpan.Zero
        };
    }

    private async Task<VerificationResult> TestResourceCleanup()
    {
        // Verify proper resource disposal
        return new VerificationResult
        {
            TestName = "Behavior: Resource Cleanup",
            Success = true,
            Duration = TimeSpan.Zero
        };
    }
}
```

### Step 6: Add CLI Support (Optional)

If you want a standalone CLI for your verifier, extend the existing CLI or create a new one:

```csharp
// In your CLI Program.cs or create a new project
using FluentPDF.Verification.Core;
using FluentPDF.Verification.Skia;
using System.CommandLine;

var rootCommand = new RootCommand("SkiaSharp Library Verification Tool");

var dllOption = new Option<FileInfo>(
    aliases: new[] { "--dll", "-d" },
    description: "Path to SkiaSharp DLL")
{
    IsRequired = true
};

var formatOption = new Option<ReportFormat>(
    aliases: new[] { "--format", "-f" },
    getDefaultValue: () => ReportFormat.Console,
    description: "Output format (Console, Json, Html)");

rootCommand.AddOption(dllOption);
rootCommand.AddOption(formatOption);

rootCommand.SetHandler(async (dllFile, format) =>
{
    var options = new VerificationOptions
    {
        DllPath = dllFile.FullName,
        OutputFormat = format
    };

    var dllAnalyzer = new DllAnalyzer(); // Use framework's DllAnalyzer
    var verifier = new SkiaSignatureVerifier(options, dllAnalyzer);

    var result = await verifier.VerifyAsync();

    if (result.IsSuccess)
    {
        Console.WriteLine($"Verification completed: {result.Value.Summary.PassedTests}/{result.Value.Summary.TotalTests} passed");
        Environment.Exit(result.Value.Summary.Success ? 0 : 1);
    }
    else
    {
        Console.Error.WriteLine($"Verification failed: {result.Errors[0].Message}");
        Environment.Exit(3);
    }
}, dllOption, formatOption);

return await rootCommand.InvokeAsync(args);
```

## Framework Extension Points

### Core Interfaces

#### `ILibraryVerifier<TResult>`

Main interface for all verifiers. Implement this for each verification type.

**Key methods:**
- `ValidateConfiguration()` - Check configuration before running
- `VerifyAsync(CancellationToken)` - Execute verification tests
- `LibraryName { get; }` - Name of library being verified

**When to use:**
- Create one implementation per verification category (Signature, ReturnType, Behavior)
- Use generic `TResult` to specify your result type

#### `IDllAnalyzer`

Analyzes native DLL exports. The framework provides `DllAnalyzer` implementation.

**Key methods:**
- `AnalyzeAsync(string dllPath)` - Load and analyze DLL
- `GetFunctionSignature(string functionName)` - Get specific function signature
- `GetExportedFunctions()` - Get all exported function names

**When to use:**
- Use the framework's `DllAnalyzer` implementation directly
- Only implement custom analyzer for special DLL formats

#### `IVerificationReporter`

Generates reports in various formats. The framework provides base `ReportGenerator` class.

**Key methods:**
- `GenerateConsoleReport(VerificationSummary)` - Human-readable console output
- `GenerateJsonReport(VerificationSummary)` - Machine-readable JSON
- `GenerateHtmlReport(VerificationSummary)` - Formatted HTML report

**When to use:**
- Extend `ReportGenerator` for custom formatting
- Use framework reporters for standard output

### Core Models

#### `VerificationResult`

Result of a single test. Required fields:

```csharp
public record VerificationResult
{
    public required string TestName { get; init; }
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? SuggestedFix { get; init; }
    public required TimeSpan Duration { get; init; }
    public Dictionary<string, object> Metadata { get; init; }
}
```

#### `VerificationSummary`

Summary of all verification results:

```csharp
public record VerificationSummary
{
    public required int TotalTests { get; init; }
    public required int PassedTests { get; init; }
    public required int FailedTests { get; init; }
    public required IReadOnlyList<VerificationResult> Results { get; init; }
    public required TimeSpan TotalDuration { get; init; }
    public string? LibraryVersion { get; init; }
    public bool Success => FailedTests == 0;
}
```

#### `FunctionSignature`

Represents a function signature from DLL:

```csharp
public record FunctionSignature
{
    public required string Name { get; init; }
    public required string ReturnType { get; init; }
    public required IReadOnlyList<ParameterInfo> Parameters { get; init; }
    public string? CallingConvention { get; init; }
    public string FullSignature { get; } // Auto-formatted
}
```

## Best Practices

### 1. Follow Single Responsibility Principle

Create separate verifiers for different concerns:

- **SignatureVerifier** - Only validates P/Invoke signatures
- **ReturnTypeVerifier** - Only validates return values
- **BehaviorVerifier** - Only tests functional behavior

### 2. Use FluentResults for Error Handling

Always return `Result<T>` or `Result` from methods:

```csharp
// Good
public Result ValidateConfiguration()
{
    if (!File.Exists(_options.DllPath))
    {
        return Result.Fail($"DLL not found: {_options.DllPath}");
    }
    return Result.Ok();
}

// Avoid throwing exceptions
public void ValidateConfiguration()
{
    if (!File.Exists(_options.DllPath))
    {
        throw new FileNotFoundException(_options.DllPath); // Don't do this
    }
}
```

### 3. Make Tests Deterministic

Ensure verification tests produce consistent results:

```csharp
// Good - deterministic
private VerificationResult TestColorCreation()
{
    var color = SkiaInterop.sk_color_set_argb(255, 128, 64, 32);
    var expected = 0xFF804020; // Known expected value
    return new VerificationResult
    {
        TestName = "Color Creation",
        Success = color == expected,
        ErrorMessage = color != expected ? $"Expected {expected:X}, got {color:X}" : null
    };
}

// Avoid - non-deterministic
private VerificationResult TestRandomColor()
{
    var random = new Random(); // Results vary each run
    var r = random.Next(256);
    var color = SkiaInterop.sk_color_set_argb(255, r, 0, 0);
    return new VerificationResult
    {
        Success = color != 0 // Too vague
    };
}
```

### 4. Include Metadata in Results

Add relevant metadata to help diagnose failures:

```csharp
new VerificationResult
{
    TestName = $"Signature: {functionName}",
    Success = matches,
    ErrorMessage = matches ? null : "Signature mismatch",
    Metadata = new Dictionary<string, object>
    {
        ["DeclaredSignature"] = declaredSig,
        ["ActualSignature"] = actualSig,
        ["FunctionName"] = functionName,
        ["ParameterCount"] = paramCount,
        ["ReturnType"] = returnType
    }
};
```

### 5. Provide Actionable Suggested Fixes

When tests fail, suggest specific fixes:

```csharp
// Good - specific fix
SuggestedFix = "Change return type from 'float' to 'double' in MyInterop.cs:42"

// Good - actionable
SuggestedFix = "Add [MarshalAs(UnmanagedType.LPStr)] attribute to 'filename' parameter"

// Avoid - too vague
SuggestedFix = "Fix the signature"
```

### 6. Validate Configuration Early

Check configuration before starting verification:

```csharp
public async Task<Result<MyResult>> VerifyAsync(CancellationToken ct)
{
    // Validate first
    var configResult = ValidateConfiguration();
    if (configResult.IsFailed)
    {
        return Result.Fail<MyResult>(configResult.Errors[0].Message);
    }

    // Then proceed with verification
    // ...
}
```

### 7. Use Async Throughout

Make all I/O operations async:

```csharp
// Good
public async Task<Result<DllInfo>> AnalyzeAsync(string dllPath, CancellationToken ct)
{
    await Task.Run(() => {
        // DLL analysis work
    }, ct);
}

// Avoid
public DllInfo Analyze(string dllPath)
{
    // Blocks calling thread
}
```

### 8. Support Cancellation

Accept and respect cancellation tokens:

```csharp
public async Task<Result<T>> VerifyAsync(CancellationToken cancellationToken = default)
{
    foreach (var test in tests)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await RunTestAsync(test, cancellationToken);
    }
}
```

## Common Patterns

### Pattern 1: Reflection-Based Signature Discovery

Extract P/Invoke declarations using reflection:

```csharp
private Dictionary<string, MethodInfo> GetPInvokeDeclarations()
{
    var interopType = typeof(MyLibraryInterop);

    var methods = interopType.GetMethods(
        BindingFlags.Public |
        BindingFlags.NonPublic |
        BindingFlags.Static);

    return methods
        .Where(m => m.GetCustomAttribute<DllImportAttribute>() != null)
        .ToDictionary(m => GetExportName(m), m => m);
}

private string GetExportName(MethodInfo method)
{
    var dllImport = method.GetCustomAttribute<DllImportAttribute>();
    return dllImport?.EntryPoint ?? method.Name;
}
```

### Pattern 2: Range Validation for Return Values

Validate numeric return values are within expected ranges:

```csharp
private VerificationResult ValidateNumericReturn(
    string functionName,
    Func<double> function,
    double min,
    double max)
{
    var stopwatch = Stopwatch.StartNew();
    var value = function();
    stopwatch.Stop();

    var isValid = value >= min && value <= max;

    return new VerificationResult
    {
        TestName = $"Return Value Range: {functionName}",
        Success = isValid,
        ErrorMessage = isValid ? null :
            $"Value {value} outside expected range [{min}, {max}]",
        SuggestedFix = isValid ? null :
            $"Check P/Invoke signature for {functionName}",
        Duration = stopwatch.Elapsed,
        Metadata = new Dictionary<string, object>
        {
            ["ActualValue"] = value,
            ["MinValue"] = min,
            ["MaxValue"] = max
        }
    };
}
```

### Pattern 3: Test File-Based Behavior Verification

Use test files for functional verification:

```csharp
public async Task<Result<VerificationResult>> VerifyAsync(CancellationToken ct)
{
    var results = new List<VerificationResult>();

    if (_options.TestFileDirectory != null)
    {
        var testFiles = Directory.GetFiles(
            _options.TestFileDirectory,
            "*.test"); // Your test file extension

        foreach (var testFile in testFiles)
        {
            ct.ThrowIfCancellationRequested();

            var result = await VerifyWithTestFile(testFile, ct);
            results.Add(result);
        }
    }

    return CreateSummary(results);
}
```

### Pattern 4: Parallel Test Execution

Execute tests in parallel for performance:

```csharp
public async Task<Result<VerificationResult>> VerifyAsync(CancellationToken ct)
{
    var tests = GetAllTests();

    List<VerificationResult> results;

    if (_options.ParallelExecution)
    {
        results = await Task.WhenAll(
            tests.Select(test => RunTestAsync(test, ct)));
    }
    else
    {
        results = new List<VerificationResult>();
        foreach (var test in tests)
        {
            results.Add(await RunTestAsync(test, ct));
        }
    }

    return CreateSummary(results);
}
```

## Testing Your Verifier

Create unit tests for your verifier:

```csharp
// tests/FluentPDF.Verification.Skia.Tests/SkiaSignatureVerifierTests.cs
using Xunit;
using FluentAssertions;
using FluentPDF.Verification.Skia;

public class SkiaSignatureVerifierTests
{
    [Fact]
    public async Task VerifyAsync_ValidDll_ReturnsSuccess()
    {
        // Arrange
        var options = new VerificationOptions
        {
            DllPath = "path/to/libSkiaSharp.dll"
        };
        var analyzer = new DllAnalyzer();
        var verifier = new SkiaSignatureVerifier(options, analyzer);

        // Act
        var result = await verifier.VerifyAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Summary.FailedTests.Should().Be(0);
    }

    [Fact]
    public void ValidateConfiguration_MissingDll_ReturnsFailure()
    {
        // Arrange
        var options = new VerificationOptions
        {
            DllPath = "nonexistent.dll"
        };
        var analyzer = new DllAnalyzer();
        var verifier = new SkiaSignatureVerifier(options, analyzer);

        // Act
        var result = verifier.ValidateConfiguration();

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Contain("not found");
    }
}
```

## Complete Example Projects

### Example 1: SQLite Verifier

Minimal verifier for SQLite native library:

**Project structure:**
```
src/FluentPDF.Verification.Sqlite/
├── SqliteSignatureVerifier.cs
├── SqliteReturnTypeVerifier.cs
├── SqliteBehaviorVerifier.cs
└── SqliteVerificationResult.cs
```

**Usage:**
```csharp
var options = new VerificationOptions
{
    DllPath = "sqlite3.dll",
    TestFileDirectory = "tests/Fixtures/Databases"
};

var analyzer = new DllAnalyzer();
var verifier = new SqliteSignatureVerifier(options, analyzer);

var result = await verifier.VerifyAsync();

if (result.IsSuccess && result.Value.Summary.Success)
{
    Console.WriteLine("SQLite verification passed!");
}
```

### Example 2: OpenSSL Verifier

Verifier for OpenSSL cryptography library:

**Unique considerations:**
- Multiple DLLs (libssl.dll, libcrypto.dll)
- Version-specific function availability
- Security-sensitive validation (no crashes, no leaks)

**Implementation:**
```csharp
public class OpenSslSignatureVerifier : ILibraryVerifier<OpenSslVerificationResult>
{
    private readonly string[] _dllPaths; // Multiple DLLs

    public OpenSslSignatureVerifier(params string[] dllPaths)
    {
        _dllPaths = dllPaths;
    }

    public async Task<Result<OpenSslVerificationResult>> VerifyAsync(CancellationToken ct)
    {
        var allResults = new List<VerificationResult>();

        // Verify each DLL
        foreach (var dllPath in _dllPaths)
        {
            var results = await VerifyDll(dllPath, ct);
            allResults.AddRange(results);
        }

        return CreateSummary(allResults);
    }
}
```

## Troubleshooting

### Issue: DllAnalyzer Can't Read DLL

**Problem:** `DllAnalyzer.AnalyzeAsync()` fails to read DLL exports.

**Solution:**
1. Verify DLL is not corrupted: `dumpbin /exports mydll.dll` (Windows)
2. Check DLL architecture matches (x64 vs x86)
3. Ensure DLL is not in use by another process
4. Try copying DLL to a temporary location first

### Issue: P/Invoke Functions Not Found by Reflection

**Problem:** `GetMethods()` doesn't find P/Invoke declarations.

**Solution:**
1. Verify interop class is public
2. Check binding flags include `Static`
3. Ensure methods have `[DllImport]` attribute
4. Try using fully qualified type name

### Issue: Signature Comparison Always Fails

**Problem:** All signature comparisons report mismatches.

**Solution:**
1. Check type name mapping (CLR type vs C type)
2. Normalize type names (e.g., `Int32` vs `int`)
3. Account for pointer types (`IntPtr` vs `void*`)
4. Consider platform-specific types (`long` = 64-bit on x64, 32-bit on x86)

### Issue: Test Files Not Found

**Problem:** Behavior tests can't find test files.

**Solution:**
1. Use absolute paths in tests
2. Copy test files to output directory in `.csproj`:
   ```xml
   <ItemGroup>
     <None Include="TestFiles\**" CopyToOutputDirectory="PreserveNewest" />
   </ItemGroup>
   ```
3. Check file permissions

## Additional Resources

- **PDFium Verifier Source** - Reference implementation in `src/FluentPDF.Verification.Pdfium/`
- **Core Framework** - Generic interfaces and models in `src/FluentPDF.Verification.Core/`
- **CLI Reference** - Command-line tool in `src/FluentPDF.Verification.Cli/`
- **Tests** - Example tests in `tests/FluentPDF.Verification.*.Tests/`

## Support

If you encounter issues extending the framework:

1. Review the PDFium verifier implementation as a reference
2. Check the core framework interfaces and models
3. Create an issue on GitHub with:
   - Your library name and version
   - Verifier code snippet
   - Error messages or unexpected behavior
   - Sample DLL (if shareable)

## Summary

To create a verifier for a new library:

1. **Create library-specific project** - Reference Core framework
2. **Implement signature verifier** - Validate P/Invoke declarations
3. **Implement return type verifier** - Validate return values
4. **Implement behavior verifier** - Test functional behavior
5. **Create result model** - Define verification result type
6. **Add CLI support** (optional) - Create user-friendly interface
7. **Write tests** - Ensure verifier reliability

The framework handles the heavy lifting (DLL analysis, reporting, orchestration). You focus on library-specific validation logic.
