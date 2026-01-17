using FluentResults;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace FluentPDF.Verification.Core;

/// <summary>
/// Analyzes native DLL files to extract function signatures and export information.
/// Uses P/Invoke to access native APIs for DLL analysis.
/// </summary>
public class DllAnalyzer : IDllAnalyzer
{
    private DllInfo? _cachedDllInfo;
    private Dictionary<string, FunctionSignature>? _cachedSignatures;
    private readonly object _lock = new();

    /// <summary>
    /// Loads and analyzes a native DLL file.
    /// </summary>
    public async Task<Result<DllInfo>> AnalyzeAsync(string dllPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dllPath))
        {
            return Result.Fail<DllInfo>("DLL path cannot be null or empty");
        }

        if (!File.Exists(dllPath))
        {
            return Result.Fail<DllInfo>($"DLL file not found: {dllPath}");
        }

        return await Task.Run(() =>
        {
            lock (_lock)
            {
                if (_cachedDllInfo != null && _cachedDllInfo.FilePath == dllPath)
                {
                    return Result.Ok(_cachedDllInfo);
                }

                try
                {
                    var fileInfo = new FileInfo(dllPath);
                    var versionInfo = FileVersionInfo.GetVersionInfo(dllPath);

                    // Load the DLL to get its exports
                    var exports = GetDllExports(dllPath);
                    if (exports.IsFailed)
                    {
                        return Result.Fail<DllInfo>($"Failed to analyze DLL exports: {exports.Errors[0].Message}");
                    }

                    // Determine architecture
                    var architecture = GetDllArchitecture(dllPath);
                    if (architecture.IsFailed)
                    {
                        return Result.Fail<DllInfo>($"Failed to determine DLL architecture: {architecture.Errors[0].Message}");
                    }

                    _cachedDllInfo = new DllInfo
                    {
                        FilePath = dllPath,
                        Version = versionInfo.FileVersion ?? "Unknown",
                        Architecture = architecture.Value,
                        ExportCount = exports.Value.Count,
                        FileSizeBytes = fileInfo.Length,
                        Metadata = new Dictionary<string, object>
                        {
                            ["ProductName"] = versionInfo.ProductName ?? "Unknown",
                            ["FileDescription"] = versionInfo.FileDescription ?? "Unknown",
                            ["LastModified"] = fileInfo.LastWriteTime
                        }
                    };

                    // Cache function signatures
                    _cachedSignatures = new Dictionary<string, FunctionSignature>();
                    foreach (var exportName in exports.Value)
                    {
                        // For now, we'll create basic signatures - full signature extraction
                        // requires parsing debug info or type libraries
                        _cachedSignatures[exportName] = new FunctionSignature
                        {
                            Name = exportName,
                            ReturnType = "void*",  // Default - needs type library for accurate info
                            Parameters = Array.Empty<ParameterInfo>(),
                            CallingConvention = "Cdecl"
                        };
                    }

                    return Result.Ok(_cachedDllInfo);
                }
                catch (BadImageFormatException ex)
                {
                    return Result.Fail<DllInfo>($"Invalid DLL format (corrupted or wrong architecture): {ex.Message}");
                }
                catch (Exception ex)
                {
                    return Result.Fail<DllInfo>($"Error analyzing DLL: {ex.Message}");
                }
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Extracts the signature of a specific exported function.
    /// </summary>
    public Result<FunctionSignature> GetFunctionSignature(string functionName)
    {
        if (_cachedSignatures == null)
        {
            return Result.Fail<FunctionSignature>("DLL has not been analyzed. Call AnalyzeAsync first.");
        }

        if (string.IsNullOrWhiteSpace(functionName))
        {
            return Result.Fail<FunctionSignature>("Function name cannot be null or empty");
        }

        lock (_lock)
        {
            if (_cachedSignatures.TryGetValue(functionName, out var signature))
            {
                return Result.Ok(signature);
            }

            return Result.Fail<FunctionSignature>($"Function '{functionName}' not found in DLL exports");
        }
    }

    /// <summary>
    /// Gets all exported function names from the analyzed DLL.
    /// </summary>
    public Result<IReadOnlyList<string>> GetExportedFunctions()
    {
        if (_cachedSignatures == null)
        {
            return Result.Fail<IReadOnlyList<string>>("DLL has not been analyzed. Call AnalyzeAsync first.");
        }

        lock (_lock)
        {
            return Result.Ok<IReadOnlyList<string>>(_cachedSignatures.Keys.ToList());
        }
    }

    /// <summary>
    /// Gets the list of exported functions from a DLL.
    /// Uses LoadLibraryEx with LOAD_LIBRARY_AS_DATAFILE to avoid executing DLL code.
    /// </summary>
    private Result<List<string>> GetDllExports(string dllPath)
    {
        IntPtr hModule = IntPtr.Zero;
        try
        {
            // Load as data file to prevent executing any code
            hModule = NativeMethods.LoadLibraryEx(dllPath, IntPtr.Zero, NativeMethods.LOAD_LIBRARY_AS_DATAFILE);
            if (hModule == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                return Result.Fail<List<string>>($"Failed to load DLL: Error code {error}");
            }

            var exports = new List<string>();

            // Parse PE header to find export directory
            var exportDirectory = GetExportDirectory(hModule);
            if (exportDirectory == null)
            {
                return Result.Ok(exports); // No exports found
            }

            // Read export names
            var numberOfNames = exportDirectory.Value.NumberOfNames;
            var namesRva = exportDirectory.Value.AddressOfNames;

            for (int i = 0; i < numberOfNames; i++)
            {
                var nameRva = Marshal.ReadInt32(hModule + (int)namesRva + (i * 4));
                var namePtr = hModule + nameRva;
                var name = Marshal.PtrToStringAnsi(namePtr);
                if (!string.IsNullOrEmpty(name))
                {
                    exports.Add(name);
                }
            }

            return Result.Ok(exports);
        }
        catch (Exception ex)
        {
            return Result.Fail<List<string>>($"Error reading DLL exports: {ex.Message}");
        }
        finally
        {
            if (hModule != IntPtr.Zero)
            {
                NativeMethods.FreeLibrary(hModule);
            }
        }
    }

    /// <summary>
    /// Extracts the export directory from a loaded PE file.
    /// </summary>
    private unsafe IMAGE_EXPORT_DIRECTORY? GetExportDirectory(IntPtr hModule)
    {
        try
        {
            var dosHeader = Marshal.PtrToStructure<IMAGE_DOS_HEADER>(hModule);
            if (dosHeader.e_magic != 0x5A4D) // "MZ"
            {
                return null;
            }

            var ntHeadersPtr = hModule + dosHeader.e_lfanew;
            var ntHeaders = Marshal.PtrToStructure<IMAGE_NT_HEADERS64>(ntHeadersPtr);

            if (ntHeaders.Signature != 0x00004550) // "PE\0\0"
            {
                return null;
            }

            var exportDirRva = ntHeaders.OptionalHeader.ExportTable.VirtualAddress;
            if (exportDirRva == 0)
            {
                return null;
            }

            var exportDir = Marshal.PtrToStructure<IMAGE_EXPORT_DIRECTORY>(hModule + (int)exportDirRva);
            return exportDir;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Determines whether a DLL is x86, x64, ARM64, etc.
    /// </summary>
    private Result<string> GetDllArchitecture(string dllPath)
    {
        try
        {
            using var stream = File.OpenRead(dllPath);
            using var reader = new BinaryReader(stream);

            // Read DOS header
            var dosSignature = reader.ReadUInt16();
            if (dosSignature != 0x5A4D) // "MZ"
            {
                return Result.Fail<string>("Invalid DOS signature");
            }

            // Jump to PE header offset
            stream.Seek(0x3C, SeekOrigin.Begin);
            var peOffset = reader.ReadInt32();

            stream.Seek(peOffset, SeekOrigin.Begin);
            var peSignature = reader.ReadUInt32();
            if (peSignature != 0x00004550) // "PE\0\0"
            {
                return Result.Fail<string>("Invalid PE signature");
            }

            // Read machine type
            var machine = reader.ReadUInt16();

            return machine switch
            {
                0x014c => Result.Ok("x86"),
                0x8664 => Result.Ok("x64"),
                0xAA64 => Result.Ok("ARM64"),
                0x01c4 => Result.Ok("ARM"),
                _ => Result.Ok($"Unknown (0x{machine:X4})")
            };
        }
        catch (Exception ex)
        {
            return Result.Fail<string>($"Error reading DLL architecture: {ex.Message}");
        }
    }

    #region Native Structures and Methods

    private static class NativeMethods
    {
        public const uint LOAD_LIBRARY_AS_DATAFILE = 0x00000002;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGE_DOS_HEADER
    {
        public ushort e_magic;
        public ushort e_cblp;
        public ushort e_cp;
        public ushort e_crlc;
        public ushort e_cparhdr;
        public ushort e_minalloc;
        public ushort e_maxalloc;
        public ushort e_ss;
        public ushort e_sp;
        public ushort e_csum;
        public ushort e_ip;
        public ushort e_cs;
        public ushort e_lfarlc;
        public ushort e_ovno;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] e_res1;
        public ushort e_oemid;
        public ushort e_oeminfo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public ushort[] e_res2;
        public int e_lfanew;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGE_NT_HEADERS64
    {
        public uint Signature;
        public IMAGE_FILE_HEADER FileHeader;
        public IMAGE_OPTIONAL_HEADER64 OptionalHeader;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGE_FILE_HEADER
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGE_OPTIONAL_HEADER64
    {
        public ushort Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public ulong ImageBase;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort MajorOperatingSystemVersion;
        public ushort MinorOperatingSystemVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public uint Win32VersionValue;
        public uint SizeOfImage;
        public uint SizeOfHeaders;
        public uint CheckSum;
        public ushort Subsystem;
        public ushort DllCharacteristics;
        public ulong SizeOfStackReserve;
        public ulong SizeOfStackCommit;
        public ulong SizeOfHeapReserve;
        public ulong SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberOfRvaAndSizes;
        public IMAGE_DATA_DIRECTORY ExportTable;
        // Additional data directories omitted for brevity
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGE_DATA_DIRECTORY
    {
        public uint VirtualAddress;
        public uint Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGE_EXPORT_DIRECTORY
    {
        public uint Characteristics;
        public uint TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public uint Name;
        public uint Base;
        public uint NumberOfFunctions;
        public uint NumberOfNames;
        public uint AddressOfFunctions;
        public uint AddressOfNames;
        public uint AddressOfNameOrdinals;
    }

    #endregion
}
