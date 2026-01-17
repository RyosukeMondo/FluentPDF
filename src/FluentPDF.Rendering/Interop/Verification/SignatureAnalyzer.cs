using System.Reflection;
using System.Runtime.InteropServices;

namespace FluentPDF.Rendering.Interop.Verification;

/// <summary>
/// Analyzes P/Invoke function signatures using reflection and validates them against expected PDFium API specifications.
/// Thread-safe implementation that does not execute any P/Invoke functions during analysis.
/// </summary>
public class SignatureAnalyzer
{
    private readonly Type _interopType;
    private readonly object _lock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SignatureAnalyzer"/> class.
    /// </summary>
    /// <param name="interopType">The type containing DllImport methods to analyze (e.g., typeof(PdfiumInterop)).</param>
    /// <exception cref="ArgumentNullException">Thrown when interopType is null.</exception>
    public SignatureAnalyzer(Type interopType)
    {
        _interopType = interopType ?? throw new ArgumentNullException(nameof(interopType));
    }

    /// <summary>
    /// Analyzes all DllImport methods in the interop type and extracts their signature details.
    /// </summary>
    /// <returns>A list of signature details for all DllImport methods found.</returns>
    public List<SignatureDetails> AnalyzeAllSignatures()
    {
        lock (_lock)
        {
            var signatures = new List<SignatureDetails>();
            var methods = GetAllDllImportMethods();

            foreach (var method in methods)
            {
                var signature = ExtractSignature(method);
                if (signature != null)
                {
                    signatures.Add(signature);
                }
            }

            return signatures;
        }
    }

    /// <summary>
    /// Analyzes a specific DllImport method by name.
    /// </summary>
    /// <param name="methodName">The name of the method to analyze.</param>
    /// <returns>Signature details for the method, or null if the method was not found or is not a DllImport method.</returns>
    public SignatureDetails? AnalyzeSignature(string methodName)
    {
        if (string.IsNullOrWhiteSpace(methodName))
        {
            throw new ArgumentException("Method name cannot be null or empty.", nameof(methodName));
        }

        lock (_lock)
        {
            var method = FindDllImportMethod(methodName);
            return method != null ? ExtractSignature(method) : null;
        }
    }

    /// <summary>
    /// Gets a list of all DllImport method names in the interop type.
    /// </summary>
    /// <returns>A list of method names.</returns>
    public List<string> GetAllDllImportMethodNames()
    {
        lock (_lock)
        {
            return GetAllDllImportMethods()
                .Select(m => m.Name)
                .OrderBy(n => n)
                .ToList();
        }
    }

    /// <summary>
    /// Validates a method signature against an expected signature specification.
    /// </summary>
    /// <param name="methodName">The name of the method to validate.</param>
    /// <param name="expectedSignature">The expected signature details from PDFium API specification.</param>
    /// <returns>A verification result indicating whether the signature matches.</returns>
    public VerificationResult ValidateSignature(string methodName, SignatureDetails expectedSignature)
    {
        if (string.IsNullOrWhiteSpace(methodName))
        {
            throw new ArgumentException("Method name cannot be null or empty.", nameof(methodName));
        }

        if (expectedSignature == null)
        {
            throw new ArgumentNullException(nameof(expectedSignature));
        }

        lock (_lock)
        {
            var actualSignature = AnalyzeSignature(methodName);

            if (actualSignature == null)
            {
                return new VerificationResult
                {
                    FunctionName = methodName,
                    SignatureValid = false,
                    ErrorMessage = $"Method '{methodName}' not found or is not a DllImport method.",
                    Signature = null
                };
            }

            var validationErrors = new List<string>();

            // Validate return type
            if (!CompareTypes(actualSignature.ReturnType, expectedSignature.ReturnType))
            {
                validationErrors.Add($"Return type mismatch: expected '{expectedSignature.ReturnType}', actual '{actualSignature.ReturnType}'");
            }

            // Validate calling convention
            if (!actualSignature.CallingConvention.Equals(expectedSignature.CallingConvention, StringComparison.OrdinalIgnoreCase))
            {
                validationErrors.Add($"Calling convention mismatch: expected '{expectedSignature.CallingConvention}', actual '{actualSignature.CallingConvention}'");
            }

            // Validate entry point
            if (!string.IsNullOrEmpty(expectedSignature.EntryPoint) &&
                !actualSignature.EntryPoint.Equals(expectedSignature.EntryPoint, StringComparison.Ordinal))
            {
                validationErrors.Add($"Entry point mismatch: expected '{expectedSignature.EntryPoint}', actual '{actualSignature.EntryPoint}'");
            }

            // Validate parameters
            if (actualSignature.Parameters.Count != expectedSignature.Parameters.Count)
            {
                validationErrors.Add($"Parameter count mismatch: expected {expectedSignature.Parameters.Count}, actual {actualSignature.Parameters.Count}");
            }
            else
            {
                for (int i = 0; i < actualSignature.Parameters.Count; i++)
                {
                    var actualParam = actualSignature.Parameters[i];
                    var expectedParam = expectedSignature.Parameters[i];

                    if (!CompareTypes(actualParam.Type, expectedParam.Type))
                    {
                        validationErrors.Add($"Parameter {i} type mismatch: expected '{expectedParam.Type}', actual '{actualParam.Type}'");
                    }

                    if (actualParam.IsOut != expectedParam.IsOut)
                    {
                        validationErrors.Add($"Parameter {i} 'out' modifier mismatch: expected {expectedParam.IsOut}, actual {actualParam.IsOut}");
                    }

                    if (actualParam.IsRef != expectedParam.IsRef)
                    {
                        validationErrors.Add($"Parameter {i} 'ref' modifier mismatch: expected {expectedParam.IsRef}, actual {actualParam.IsRef}");
                    }
                }
            }

            return new VerificationResult
            {
                FunctionName = methodName,
                SignatureValid = validationErrors.Count == 0,
                ErrorMessage = validationErrors.Count > 0 ? string.Join("; ", validationErrors) : null,
                Signature = actualSignature
            };
        }
    }

    /// <summary>
    /// Gets all methods with DllImport attribute from the interop type.
    /// </summary>
    private MethodInfo[] GetAllDllImportMethods()
    {
        return _interopType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)
            .Where(m => m.GetCustomAttribute<DllImportAttribute>() != null)
            .ToArray();
    }

    /// <summary>
    /// Finds a specific DllImport method by name.
    /// </summary>
    private MethodInfo? FindDllImportMethod(string methodName)
    {
        return GetAllDllImportMethods()
            .FirstOrDefault(m => m.Name.Equals(methodName, StringComparison.Ordinal));
    }

    /// <summary>
    /// Extracts signature details from a MethodInfo object.
    /// </summary>
    private SignatureDetails? ExtractSignature(MethodInfo method)
    {
        var dllImport = method.GetCustomAttribute<DllImportAttribute>();
        if (dllImport == null)
        {
            return null;
        }

        var parameters = method.GetParameters()
            .Select(ParameterDetails.FromParameterInfo)
            .ToList();

        return new SignatureDetails
        {
            ReturnType = method.ReturnType.FullName ?? method.ReturnType.Name,
            Parameters = parameters,
            CallingConvention = dllImport.CallingConvention.ToString(),
            EntryPoint = dllImport.EntryPoint ?? method.Name,
            CharSet = dllImport.CharSet != CharSet.None ? dllImport.CharSet.ToString() : null
        };
    }

    /// <summary>
    /// Compares two type names, handling common variations and aliases.
    /// </summary>
    private bool CompareTypes(string type1, string type2)
    {
        // Normalize both types
        var normalized1 = NormalizeTypeName(type1);
        var normalized2 = NormalizeTypeName(type2);

        return normalized1.Equals(normalized2, StringComparison.Ordinal);
    }

    /// <summary>
    /// Normalizes type names to handle common variations (e.g., System.Int32 vs int).
    /// </summary>
    private string NormalizeTypeName(string typeName)
    {
        if (string.IsNullOrWhiteSpace(typeName))
        {
            return string.Empty;
        }

        // Handle C# type aliases
        var typeAliases = new Dictionary<string, string>
        {
            { "System.Void", "void" },
            { "System.Boolean", "bool" },
            { "System.Byte", "byte" },
            { "System.SByte", "sbyte" },
            { "System.Char", "char" },
            { "System.Decimal", "decimal" },
            { "System.Double", "double" },
            { "System.Single", "float" },
            { "System.Int32", "int" },
            { "System.UInt32", "uint" },
            { "System.Int64", "long" },
            { "System.UInt64", "ulong" },
            { "System.Object", "object" },
            { "System.Int16", "short" },
            { "System.UInt16", "ushort" },
            { "System.String", "string" },
            { "System.IntPtr", "IntPtr" },
            { "System.UIntPtr", "UIntPtr" }
        };

        // Try to find an alias
        foreach (var alias in typeAliases)
        {
            if (typeName.Equals(alias.Key, StringComparison.Ordinal))
            {
                return alias.Value;
            }
            if (typeName.Equals(alias.Value, StringComparison.Ordinal))
            {
                return alias.Value; // Return the alias (canonical form)
            }
        }

        return typeName;
    }
}
