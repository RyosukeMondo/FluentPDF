using System.Runtime.InteropServices;

namespace FluentPDF.Rendering.Interop;

/// <summary>
/// P/Invoke declarations for QPDF native library.
/// Provides managed wrappers for QPDF document manipulation functions.
/// See: https://qpdf.readthedocs.io/en/stable/c-api.html
/// </summary>
internal static class QpdfNative
{
    private const string DllName = "qpdf";

    private static bool _isInitialized;
    private static readonly object _lockObject = new();

    #region Library Initialization

    /// <summary>
    /// Initializes the QPDF library.
    /// Must be called once before any other QPDF functions.
    /// </summary>
    /// <returns>True if initialization succeeded; otherwise, false.</returns>
    public static bool Initialize()
    {
        lock (_lockObject)
        {
            if (_isInitialized)
            {
                return true;
            }

            try
            {
                // QPDF doesn't require explicit initialization, but we test DLL loading
                var job = qpdf_init();
                if (job != IntPtr.Zero)
                {
                    qpdf_cleanup(ref job);
                    _isInitialized = true;
                    return true;
                }
                return false;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    #endregion

    #region Job Management

    /// <summary>
    /// Creates a new QPDF job handle.
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#qpdf-init
    /// </summary>
    /// <returns>A QPDF job handle, or IntPtr.Zero on failure.</returns>
    public static SafeQpdfJobHandle CreateJob()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("QPDF library is not initialized. Call Initialize() first.");
        }

        var handle = qpdf_init();
        return new SafeQpdfJobHandle(handle);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr qpdf_init();

    /// <summary>
    /// Cleans up a QPDF job handle.
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#qpdf-cleanup
    /// </summary>
    /// <param name="job">Reference to the QPDF job handle (set to IntPtr.Zero on cleanup).</param>
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void qpdf_cleanup(ref IntPtr job);

    #endregion

    #region Document Operations

    /// <summary>
    /// Reads a PDF document from a file.
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#qpdf-read
    /// </summary>
    /// <param name="job">QPDF job handle.</param>
    /// <param name="filename">Path to the PDF file.</param>
    /// <param name="password">Password for encrypted PDFs. Pass null for unencrypted files.</param>
    /// <returns>QPDF_SUCCESS (0) on success, or an error code.</returns>
    public static int ReadDocument(SafeQpdfJobHandle job, string filename, string? password = null)
    {
        if (job == null || job.IsInvalid)
        {
            throw new ArgumentException("Invalid job handle.", nameof(job));
        }

        return qpdf_read(job, filename, password ?? string.Empty);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int qpdf_read(
        SafeQpdfJobHandle job,
        [MarshalAs(UnmanagedType.LPStr)] string filename,
        [MarshalAs(UnmanagedType.LPStr)] string password);

    /// <summary>
    /// Writes the PDF document to a file.
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#qpdf-init-write
    /// </summary>
    /// <param name="job">QPDF job handle.</param>
    /// <param name="filename">Output file path.</param>
    /// <returns>QPDF_SUCCESS (0) on success, or an error code.</returns>
    public static int WriteDocument(SafeQpdfJobHandle job, string filename)
    {
        if (job == null || job.IsInvalid)
        {
            throw new ArgumentException("Invalid job handle.", nameof(job));
        }

        var result = qpdf_init_write(job, filename);
        if (result != ErrorCodes.Success)
        {
            return result;
        }

        return qpdf_write(job);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int qpdf_init_write(
        SafeQpdfJobHandle job,
        [MarshalAs(UnmanagedType.LPStr)] string filename);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int qpdf_write(SafeQpdfJobHandle job);

    #endregion

    #region Document Information

    /// <summary>
    /// Gets the number of pages in a PDF document.
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#qpdf-get-num-pages
    /// </summary>
    /// <param name="job">QPDF job handle with a loaded document.</param>
    /// <returns>The number of pages, or 0 if the document is invalid.</returns>
    public static int GetPageCount(SafeQpdfJobHandle job)
    {
        if (job == null || job.IsInvalid)
        {
            return 0;
        }

        return qpdf_get_num_pages(job);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int qpdf_get_num_pages(SafeQpdfJobHandle job);

    #endregion

    #region Error Handling

    /// <summary>
    /// Checks if a QPDF job has errors.
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#qpdf-has-error
    /// </summary>
    /// <param name="job">QPDF job handle.</param>
    /// <returns>True if the job has errors; otherwise, false.</returns>
    public static bool HasError(SafeQpdfJobHandle job)
    {
        if (job == null || job.IsInvalid)
        {
            return true;
        }

        return qpdf_has_error(job) != 0;
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int qpdf_has_error(SafeQpdfJobHandle job);

    /// <summary>
    /// Gets the error message from a QPDF job.
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#qpdf-get-error
    /// </summary>
    /// <param name="job">QPDF job handle.</param>
    /// <returns>The error message, or an empty string if no error.</returns>
    public static string GetErrorMessage(SafeQpdfJobHandle job)
    {
        if (job == null || job.IsInvalid)
        {
            return "Invalid job handle";
        }

        var errorPtr = qpdf_get_error(job);
        if (errorPtr == IntPtr.Zero)
        {
            return string.Empty;
        }

        var message = Marshal.PtrToStringAnsi(errorPtr);
        return message ?? string.Empty;
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr qpdf_get_error(SafeQpdfJobHandle job);

    /// <summary>
    /// Translates a QPDF error code to a user-friendly message.
    /// </summary>
    /// <param name="errorCode">The QPDF error code.</param>
    /// <returns>A descriptive error message.</returns>
    public static string TranslateErrorCode(int errorCode)
    {
        return errorCode switch
        {
            ErrorCodes.Success => "Operation completed successfully",
            ErrorCodes.Internal => "Internal QPDF error",
            ErrorCodes.SystemError => "System error occurred",
            ErrorCodes.FileNotFound => "File not found or could not be opened",
            ErrorCodes.InvalidPassword => "Invalid or missing password for encrypted PDF",
            ErrorCodes.DamagedPdf => "PDF file is damaged or corrupted",
            ErrorCodes.InvalidOperation => "Invalid operation for current document state",
            ErrorCodes.OutOfMemory => "Out of memory",
            _ => $"Unknown error (code: {errorCode})"
        };
    }

    #endregion

    #region Page Operations

    /// <summary>
    /// Adds pages from one document to another.
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#page-operations
    /// </summary>
    /// <param name="targetJob">Target QPDF job handle.</param>
    /// <param name="sourceJob">Source QPDF job handle.</param>
    /// <param name="pageRange">Page range specification (e.g., "1-5" or null for all pages).</param>
    /// <returns>QPDF_SUCCESS (0) on success, or an error code.</returns>
    public static int AddPages(SafeQpdfJobHandle targetJob, SafeQpdfJobHandle sourceJob, string? pageRange = null)
    {
        if (targetJob == null || targetJob.IsInvalid)
        {
            throw new ArgumentException("Invalid target job handle.", nameof(targetJob));
        }

        if (sourceJob == null || sourceJob.IsInvalid)
        {
            throw new ArgumentException("Invalid source job handle.", nameof(sourceJob));
        }

        return qpdf_add_pages(targetJob, sourceJob, pageRange ?? string.Empty);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int qpdf_add_pages(
        SafeQpdfJobHandle target_job,
        SafeQpdfJobHandle source_job,
        [MarshalAs(UnmanagedType.LPStr)] string page_range);

    #endregion

    #region Optimization Operations

    /// <summary>
    /// Sets compression for stream data.
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#qpdf-set-compress-streams
    /// </summary>
    /// <param name="job">QPDF job handle.</param>
    /// <param name="compress">True to enable compression; false to disable.</param>
    public static void SetCompressStreams(SafeQpdfJobHandle job, bool compress)
    {
        if (job == null || job.IsInvalid)
        {
            throw new ArgumentException("Invalid job handle.", nameof(job));
        }

        qpdf_set_compress_streams(job, compress ? 1 : 0);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void qpdf_set_compress_streams(SafeQpdfJobHandle job, int compress);

    /// <summary>
    /// Sets whether to preserve unreferenced objects.
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#qpdf-set-preserve-unreferenced-objects
    /// </summary>
    /// <param name="job">QPDF job handle.</param>
    /// <param name="preserve">True to preserve; false to remove.</param>
    public static void SetPreserveUnreferencedObjects(SafeQpdfJobHandle job, bool preserve)
    {
        if (job == null || job.IsInvalid)
        {
            throw new ArgumentException("Invalid job handle.", nameof(job));
        }

        qpdf_set_preserve_unreferenced_objects(job, preserve ? 1 : 0);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void qpdf_set_preserve_unreferenced_objects(SafeQpdfJobHandle job, int preserve);

    /// <summary>
    /// Sets linearization (fast web viewing).
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#qpdf-set-linearization
    /// </summary>
    /// <param name="job">QPDF job handle.</param>
    /// <param name="linearize">True to enable linearization; false to disable.</param>
    public static void SetLinearization(SafeQpdfJobHandle job, bool linearize)
    {
        if (job == null || job.IsInvalid)
        {
            throw new ArgumentException("Invalid job handle.", nameof(job));
        }

        qpdf_set_linearization(job, linearize ? 1 : 0);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void qpdf_set_linearization(SafeQpdfJobHandle job, int linearize);

    /// <summary>
    /// Sets object stream mode for compression.
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#qpdf-set-object-stream-mode
    /// </summary>
    /// <param name="job">QPDF job handle.</param>
    /// <param name="mode">Object stream mode (0 = preserve, 1 = disable, 2 = generate).</param>
    public static void SetObjectStreamMode(SafeQpdfJobHandle job, int mode)
    {
        if (job == null || job.IsInvalid)
        {
            throw new ArgumentException("Invalid job handle.", nameof(job));
        }

        qpdf_set_object_stream_mode(job, mode);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void qpdf_set_object_stream_mode(SafeQpdfJobHandle job, int mode);

    #endregion

    #region Error Codes

    /// <summary>
    /// QPDF error codes.
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#error-codes
    /// </summary>
    public static class ErrorCodes
    {
        /// <summary>
        /// No error - operation completed successfully.
        /// </summary>
        public const int Success = 0;

        /// <summary>
        /// Internal QPDF error.
        /// </summary>
        public const int Internal = 1;

        /// <summary>
        /// System error (file I/O, permissions, etc.).
        /// </summary>
        public const int SystemError = 2;

        /// <summary>
        /// File not found or could not be opened.
        /// </summary>
        public const int FileNotFound = 3;

        /// <summary>
        /// Invalid or missing password for encrypted PDF.
        /// </summary>
        public const int InvalidPassword = 4;

        /// <summary>
        /// PDF file is damaged or corrupted.
        /// </summary>
        public const int DamagedPdf = 5;

        /// <summary>
        /// Invalid operation for current document state.
        /// </summary>
        public const int InvalidOperation = 6;

        /// <summary>
        /// Out of memory.
        /// </summary>
        public const int OutOfMemory = 7;
    }

    #endregion

    #region Object Stream Modes

    /// <summary>
    /// Object stream modes for QPDF optimization.
    /// </summary>
    public static class ObjectStreamMode
    {
        /// <summary>
        /// Preserve existing object streams.
        /// </summary>
        public const int Preserve = 0;

        /// <summary>
        /// Disable object streams.
        /// </summary>
        public const int Disable = 1;

        /// <summary>
        /// Generate object streams for compression.
        /// </summary>
        public const int Generate = 2;
    }

    #endregion
}
