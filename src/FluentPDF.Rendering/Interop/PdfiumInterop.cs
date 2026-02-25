using System.Runtime.InteropServices;

namespace FluentPDF.Rendering.Interop;

/// <summary>
/// P/Invoke declarations for PDFium native library.
/// Provides managed wrappers for PDFium document and page rendering functions.
/// Split into partial classes by functional area.
/// </summary>
public static partial class PdfiumInterop
{
    private const string DllName = "pdfium.dll";

    private static bool _isInitialized;
    private static readonly object _lockObject = new();

    /// <summary>
    /// Gets whether the PDFium library is initialized.
    /// </summary>
    public static bool IsInitialized => _isInitialized;

    #region Library Initialization

    /// <summary>
    /// Initializes the PDFium library.
    /// Must be called once before any other PDFium functions.
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
                FPDF_InitLibrary();
                _isInitialized = true;
                return true;
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

    /// <summary>
    /// Shuts down the PDFium library.
    /// Should be called once when the application exits.
    /// </summary>
    public static void Shutdown()
    {
        lock (_lockObject)
        {
            if (!_isInitialized)
            {
                return;
            }

            FPDF_DestroyLibrary();
            _isInitialized = false;
        }
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDF_InitLibrary();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDF_DestroyLibrary();

    #endregion

    #region Document Functions

    /// <summary>
    /// Loads a PDF document from a file.
    /// </summary>
    /// <param name="filePath">Path to the PDF file.</param>
    /// <param name="password">Password for encrypted PDFs. Pass null for unencrypted files.</param>
    /// <returns>A safe handle to the PDF document, or an invalid handle if loading failed.</returns>
    public static SafePdfDocumentHandle LoadDocument(string filePath, string? password = null)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("PDFium library is not initialized. Call Initialize() first.");
        }

        var handle = FPDF_LoadDocument(filePath, password);
        return handle;
    }

    /// <summary>
    /// Gets the number of pages in a PDF document.
    /// </summary>
    /// <param name="document">Handle to the PDF document.</param>
    /// <returns>The number of pages, or 0 if the document is invalid.</returns>
    public static int GetPageCount(SafePdfDocumentHandle document)
    {
        if (document == null || document.IsInvalid)
        {
            return 0;
        }

        return FPDF_GetPageCount(document);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern SafePdfDocumentHandle FPDF_LoadDocument(
        [MarshalAs(UnmanagedType.LPStr)] string file_path,
        [MarshalAs(UnmanagedType.LPStr)] string? password);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void FPDF_CloseDocument(IntPtr document);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDF_GetPageCount(SafePdfDocumentHandle document);

    #endregion

    #region Page Functions

    /// <summary>
    /// Loads a page from a PDF document.
    /// </summary>
    /// <param name="document">Handle to the PDF document.</param>
    /// <param name="pageIndex">Zero-based page index.</param>
    /// <returns>A safe handle to the page, or an invalid handle if loading failed.</returns>
    public static SafePdfPageHandle LoadPage(SafePdfDocumentHandle document, int pageIndex)
    {
        if (document == null || document.IsInvalid)
        {
            throw new ArgumentException("Invalid document handle.", nameof(document));
        }

        var handle = FPDF_LoadPage(document, pageIndex);
        return handle;
    }

    /// <summary>
    /// Gets the width of a page in points (1/72 inch).
    /// </summary>
    /// <param name="page">Handle to the page.</param>
    /// <returns>The page width in points.</returns>
    public static double GetPageWidth(SafePdfPageHandle page)
    {
        if (page == null || page.IsInvalid)
        {
            return 0;
        }

        return FPDF_GetPageWidth_Internal(page);
    }

    /// <summary>
    /// Gets the height of a page in points (1/72 inch).
    /// </summary>
    /// <param name="page">Handle to the page.</param>
    /// <returns>The page height in points.</returns>
    public static double GetPageHeight(SafePdfPageHandle page)
    {
        if (page == null || page.IsInvalid)
        {
            return 0;
        }

        return FPDF_GetPageHeight_Internal(page);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern SafePdfPageHandle FPDF_LoadPage(SafePdfDocumentHandle document, int page_index);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void FPDF_ClosePage(IntPtr page);

    // Use the integer-based API (FPDF_GetPageWidth/Height) instead of float API (FPDF_GetPageWidthF/HeightF)
    // The float API returns garbage values with this version of PDFium
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FPDF_GetPageWidth")]
    private static extern double FPDF_GetPageWidth_Internal(SafePdfPageHandle page);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "FPDF_GetPageHeight")]
    private static extern double FPDF_GetPageHeight_Internal(SafePdfPageHandle page);

    #endregion

    #region Error Functions

    /// <summary>
    /// Gets the last error code from PDFium.
    /// </summary>
    /// <returns>The error code.</returns>
    public static uint GetLastError()
    {
        return FPDF_GetLastError();
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint FPDF_GetLastError();

    #endregion

    #region Error Codes

    /// <summary>
    /// PDFium error codes.
    /// </summary>
    public static class ErrorCodes
    {
        /// <summary>
        /// No error.
        /// </summary>
        public const uint Success = 0;

        /// <summary>
        /// Unknown error.
        /// </summary>
        public const uint Unknown = 1;

        /// <summary>
        /// File not found or could not be opened.
        /// </summary>
        public const uint File = 2;

        /// <summary>
        /// File not in PDF format or corrupted.
        /// </summary>
        public const uint Format = 3;

        /// <summary>
        /// Password required or incorrect password.
        /// </summary>
        public const uint Password = 4;

        /// <summary>
        /// Unsupported security scheme.
        /// </summary>
        public const uint Security = 5;

        /// <summary>
        /// Page not found or content error.
        /// </summary>
        public const uint Page = 6;
    }

    #endregion
}
