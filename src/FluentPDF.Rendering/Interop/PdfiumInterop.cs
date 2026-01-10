using System.Runtime.InteropServices;

namespace FluentPDF.Rendering.Interop;

/// <summary>
/// P/Invoke declarations for PDFium native library.
/// Provides managed wrappers for PDFium document and page rendering functions.
/// </summary>
public static class PdfiumInterop
{
    private const string DllName = "pdfium.dll";

    private static bool _isInitialized;
    private static readonly object _lockObject = new();

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

        return FPDF_GetPageWidthF(page);
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

        return FPDF_GetPageHeightF(page);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern SafePdfPageHandle FPDF_LoadPage(SafePdfDocumentHandle document, int page_index);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void FPDF_ClosePage(IntPtr page);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern double FPDF_GetPageWidthF(SafePdfPageHandle page);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern double FPDF_GetPageHeightF(SafePdfPageHandle page);

    #endregion

    #region Bitmap Functions

    /// <summary>
    /// Creates a bitmap for rendering.
    /// </summary>
    /// <param name="width">Width of the bitmap in pixels.</param>
    /// <param name="height">Height of the bitmap in pixels.</param>
    /// <param name="hasAlpha">True to include an alpha channel; otherwise, false.</param>
    /// <returns>Handle to the bitmap, or IntPtr.Zero if creation failed.</returns>
    public static IntPtr CreateBitmap(int width, int height, bool hasAlpha)
    {
        return FPDFBitmap_Create(width, height, hasAlpha ? 1 : 0);
    }

    /// <summary>
    /// Destroys a bitmap and frees its memory.
    /// </summary>
    /// <param name="bitmap">Handle to the bitmap.</param>
    public static void DestroyBitmap(IntPtr bitmap)
    {
        if (bitmap != IntPtr.Zero)
        {
            FPDFBitmap_Destroy(bitmap);
        }
    }

    /// <summary>
    /// Gets the buffer pointer for a bitmap.
    /// </summary>
    /// <param name="bitmap">Handle to the bitmap.</param>
    /// <returns>Pointer to the bitmap buffer.</returns>
    public static IntPtr GetBitmapBuffer(IntPtr bitmap)
    {
        return FPDFBitmap_GetBuffer(bitmap);
    }

    /// <summary>
    /// Gets the stride (bytes per row) of a bitmap.
    /// </summary>
    /// <param name="bitmap">Handle to the bitmap.</param>
    /// <returns>The bitmap stride in bytes.</returns>
    public static int GetBitmapStride(IntPtr bitmap)
    {
        return FPDFBitmap_GetStride(bitmap);
    }

    /// <summary>
    /// Fills a bitmap with a color.
    /// </summary>
    /// <param name="bitmap">Handle to the bitmap.</param>
    /// <param name="color">ARGB color value.</param>
    public static void FillBitmap(IntPtr bitmap, uint color)
    {
        FPDFBitmap_FillRect(bitmap, 0, 0, int.MaxValue, int.MaxValue, color);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFBitmap_Create(int width, int height, int alpha);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDFBitmap_Destroy(IntPtr bitmap);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFBitmap_GetBuffer(IntPtr bitmap);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFBitmap_GetStride(IntPtr bitmap);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDFBitmap_FillRect(IntPtr bitmap, int left, int top, int width, int height, uint color);

    #endregion

    #region Rendering Functions

    /// <summary>
    /// Renders a page to a bitmap.
    /// </summary>
    /// <param name="bitmap">Handle to the bitmap.</param>
    /// <param name="page">Handle to the page.</param>
    /// <param name="startX">Left pixel position of the display area in bitmap coordinates.</param>
    /// <param name="startY">Top pixel position of the display area in bitmap coordinates.</param>
    /// <param name="sizeX">Horizontal size (in pixels) for displaying the page.</param>
    /// <param name="sizeY">Vertical size (in pixels) for displaying the page.</param>
    /// <param name="rotate">Page rotation: 0 (normal), 1 (90 degrees), 2 (180 degrees), 3 (270 degrees).</param>
    /// <param name="flags">Rendering flags (0 for normal rendering with antialiasing).</param>
    public static void RenderPageBitmap(
        IntPtr bitmap,
        SafePdfPageHandle page,
        int startX,
        int startY,
        int sizeX,
        int sizeY,
        int rotate,
        int flags)
    {
        if (bitmap == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid bitmap handle.", nameof(bitmap));
        }

        if (page == null || page.IsInvalid)
        {
            throw new ArgumentException("Invalid page handle.", nameof(page));
        }

        FPDF_RenderPageBitmap(bitmap, page, startX, startY, sizeX, sizeY, rotate, flags);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDF_RenderPageBitmap(
        IntPtr bitmap,
        SafePdfPageHandle page,
        int start_x,
        int start_y,
        int size_x,
        int size_y,
        int rotate,
        int flags);

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

    #region Rendering Flags

    /// <summary>
    /// Rendering flags for FPDF_RenderPageBitmap.
    /// </summary>
    public static class RenderFlags
    {
        /// <summary>
        /// Normal rendering with antialiasing.
        /// </summary>
        public const int Normal = 0;

        /// <summary>
        /// Set to render annotations.
        /// </summary>
        public const int Annotations = 0x01;

        /// <summary>
        /// Set to use LCD text optimization.
        /// </summary>
        public const int LcdText = 0x02;

        /// <summary>
        /// Disable anti-aliasing on text.
        /// </summary>
        public const int NoTextSmooth = 0x08;

        /// <summary>
        /// Disable anti-aliasing on images.
        /// </summary>
        public const int NoImageSmooth = 0x10;

        /// <summary>
        /// Disable anti-aliasing on paths.
        /// </summary>
        public const int NoPathSmooth = 0x20;

        /// <summary>
        /// Grayscale output.
        /// </summary>
        public const int Grayscale = 0x40;

        /// <summary>
        /// Limit image cache size.
        /// </summary>
        public const int LimitImageCache = 0x200;

        /// <summary>
        /// Always use halftone for image stretching.
        /// </summary>
        public const int ForceHalftone = 0x400;

        /// <summary>
        /// Render for printing.
        /// </summary>
        public const int Printing = 0x800;

        /// <summary>
        /// Disable the native text output available on some platforms.
        /// </summary>
        public const int NoNativeText = 0x1000;
    }

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
