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

    /// <summary>
    /// Removes pages from a document.
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#page-operations
    /// </summary>
    /// <param name="job">QPDF job handle.</param>
    /// <param name="pageRange">Page range specification (e.g., "1,3,5-7").</param>
    /// <returns>QPDF_SUCCESS (0) on success, or an error code.</returns>
    public static int RemovePages(SafeQpdfJobHandle job, string pageRange)
    {
        if (job == null || job.IsInvalid)
        {
            throw new ArgumentException("Invalid job handle.", nameof(job));
        }

        if (string.IsNullOrWhiteSpace(pageRange))
        {
            throw new ArgumentException("Page range cannot be null or empty.", nameof(pageRange));
        }

        return qpdf_remove_page_range(job, pageRange);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern int qpdf_remove_page_range(
        SafeQpdfJobHandle job,
        [MarshalAs(UnmanagedType.LPStr)] string page_range);

    /// <summary>
    /// Gets a page object handle for the specified page number (1-based).
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#page-operations
    /// </summary>
    /// <param name="job">QPDF job handle.</param>
    /// <param name="pageNumber">1-based page number.</param>
    /// <returns>Page object handle, or 0 on error.</returns>
    public static ulong GetPageHandle(SafeQpdfJobHandle job, int pageNumber)
    {
        if (job == null || job.IsInvalid)
        {
            throw new ArgumentException("Invalid job handle.", nameof(job));
        }

        return qpdf_get_page_n(job, pageNumber);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong qpdf_get_page_n(SafeQpdfJobHandle job, int page_num);

    /// <summary>
    /// Rotates a page by the specified angle (must be 0, 90, 180, or 270).
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#page-operations
    /// </summary>
    /// <param name="job">QPDF job handle.</param>
    /// <param name="pageHandle">Page object handle from GetPageHandle.</param>
    /// <param name="angle">Rotation angle (0, 90, 180, or 270 degrees).</param>
    /// <param name="relative">If true, rotation is relative to current; if false, absolute.</param>
    /// <returns>QPDF_SUCCESS (0) on success, or an error code.</returns>
    public static int RotatePage(SafeQpdfJobHandle job, ulong pageHandle, int angle, bool relative)
    {
        if (job == null || job.IsInvalid)
        {
            throw new ArgumentException("Invalid job handle.", nameof(job));
        }

        return qpdf_oh_rotate_page(job, pageHandle, angle, relative ? 1 : 0);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int qpdf_oh_rotate_page(SafeQpdfJobHandle job, ulong page_oh, int angle, int relative);

    /// <summary>
    /// Gets the media box for a page (returns array: [llx, lly, urx, ury]).
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#page-operations
    /// </summary>
    /// <param name="job">QPDF job handle.</param>
    /// <param name="pageHandle">Page object handle from GetPageHandle.</param>
    /// <returns>Array of 4 doubles [llx, lly, urx, ury] or null on error.</returns>
    public static double[]? GetPageMediaBox(SafeQpdfJobHandle job, ulong pageHandle)
    {
        if (job == null || job.IsInvalid)
        {
            return null;
        }

        var box = new double[4];
        var result = qpdf_oh_get_media_box(job, pageHandle, box);

        return result == ErrorCodes.Success ? box : null;
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int qpdf_oh_get_media_box(SafeQpdfJobHandle job, ulong page_oh, [Out] double[] box);

    /// <summary>
    /// Adds a new blank page to the document.
    /// See: https://qpdf.readthedocs.io/en/stable/c-api.html#page-operations
    /// </summary>
    /// <param name="job">QPDF job handle.</param>
    /// <param name="mediaBox">Media box array [llx, lly, urx, ury].</param>
    /// <param name="position">Position to insert (1-based), or 0 to append.</param>
    /// <returns>Page object handle for the new page, or 0 on error.</returns>
    public static ulong AddBlankPage(SafeQpdfJobHandle job, double[] mediaBox, int position)
    {
        if (job == null || job.IsInvalid)
        {
            throw new ArgumentException("Invalid job handle.", nameof(job));
        }

        if (mediaBox == null || mediaBox.Length != 4)
        {
            throw new ArgumentException("Media box must be an array of 4 doubles.", nameof(mediaBox));
        }

        return qpdf_add_blank_page(job, mediaBox[0], mediaBox[1], mediaBox[2], mediaBox[3], position);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong qpdf_add_blank_page(
        SafeQpdfJobHandle job,
        double llx, double lly, double urx, double ury,
        int position);

    #endregion

    #region Object Handle - Stream Operations

    /// <summary>
    /// Gets a dictionary key from an object handle.
    /// </summary>
    public static ulong GetObjectKey(SafeQpdfJobHandle job, ulong oh, string key)
    {
        return qpdf_oh_get_key(job, oh, key);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern ulong qpdf_oh_get_key(
        SafeQpdfJobHandle qpdf, ulong oh,
        [MarshalAs(UnmanagedType.LPStr)] string key);

    /// <summary>Checks if an object handle refers to a stream.</summary>
    public static bool IsStream(SafeQpdfJobHandle job, ulong oh) => qpdf_oh_is_stream(job, oh) != 0;

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int qpdf_oh_is_stream(SafeQpdfJobHandle qpdf, ulong oh);

    /// <summary>Checks if an object handle refers to an array.</summary>
    public static bool IsArray(SafeQpdfJobHandle job, ulong oh) => qpdf_oh_is_array(job, oh) != 0;

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int qpdf_oh_is_array(SafeQpdfJobHandle qpdf, ulong oh);

    /// <summary>Gets number of items in an array object.</summary>
    public static int GetArrayNItems(SafeQpdfJobHandle job, ulong oh) => qpdf_oh_get_array_n_items(job, oh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int qpdf_oh_get_array_n_items(SafeQpdfJobHandle qpdf, ulong oh);

    /// <summary>Gets an item from an array object.</summary>
    public static ulong GetArrayItem(SafeQpdfJobHandle job, ulong oh, int n) => qpdf_oh_get_array_item(job, oh, n);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong qpdf_oh_get_array_item(SafeQpdfJobHandle qpdf, ulong oh, int n);

    /// <summary>
    /// Gets decoded (decompressed) stream data. Returns buffer pointer and length.
    /// Buffer is valid until next QPDF oh call.
    /// decode_level: 0=none, 1=generalized, 2=specialized, 3=all
    /// </summary>
    public static (IntPtr buffer, int length, bool filtered) GetStreamData(
        SafeQpdfJobHandle job, ulong oh, int decodeLevel = 3)
    {
        int filtered = 0;
        IntPtr bufp = IntPtr.Zero;
        IntPtr len = IntPtr.Zero;
        qpdf_oh_get_binary_stream_data(job, oh, decodeLevel, ref filtered, ref bufp, ref len);
        return (bufp, (int)len, filtered != 0);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void qpdf_oh_get_binary_stream_data(
        SafeQpdfJobHandle qpdf, ulong stream_oh,
        int decode_level,
        ref int filtered,
        ref IntPtr bufp,
        ref IntPtr len);

    /// <summary>
    /// Replaces stream data. Pass filter=0 and decode_parms=0 for no compression.
    /// </summary>
    public static void ReplaceStreamData(
        SafeQpdfJobHandle job, ulong oh, byte[] data, ulong filter, ulong decodeParms)
    {
        qpdf_oh_replace_stream_data(job, oh, data, (IntPtr)data.Length, filter, decodeParms);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void qpdf_oh_replace_stream_data(
        SafeQpdfJobHandle qpdf, ulong stream_oh,
        byte[] data, IntPtr length,
        ulong filter, ulong decode_parms);

    /// <summary>Creates a null object handle (for optional parameters).</summary>
    public static ulong NewNull(SafeQpdfJobHandle job) => qpdf_oh_new_null(job);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong qpdf_oh_new_null(SafeQpdfJobHandle qpdf);

    /// <summary>Appends an item to an array object handle.</summary>
    public static void AppendArrayItem(SafeQpdfJobHandle job, ulong arrayOh, ulong itemOh) =>
        qpdf_oh_append_item(job, arrayOh, itemOh);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void qpdf_oh_append_item(SafeQpdfJobHandle qpdf, ulong array_oh, ulong item);

    /// <summary>Creates a new stream with given data and no filter.</summary>
    public static ulong NewStream(SafeQpdfJobHandle job, byte[] data)
    {
        return qpdf_oh_new_binary_stream(job, data, (IntPtr)data.Length);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern ulong qpdf_oh_new_binary_stream(
        SafeQpdfJobHandle qpdf, byte[] data, IntPtr length);

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
