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

    #region Bookmark Functions

    /// <summary>
    /// Gets the first child bookmark of a parent bookmark.
    /// </summary>
    /// <param name="document">Handle to the PDF document.</param>
    /// <param name="parentBookmark">Handle to the parent bookmark, or IntPtr.Zero to get the first root bookmark.</param>
    /// <returns>Handle to the first child bookmark, or IntPtr.Zero if none exists.</returns>
    public static IntPtr GetFirstChildBookmark(SafePdfDocumentHandle document, IntPtr parentBookmark)
    {
        if (document == null || document.IsInvalid)
        {
            throw new ArgumentException("Invalid document handle.", nameof(document));
        }

        return FPDFBookmark_GetFirstChild(document, parentBookmark);
    }

    /// <summary>
    /// Gets the next sibling bookmark.
    /// </summary>
    /// <param name="document">Handle to the PDF document.</param>
    /// <param name="bookmark">Handle to the current bookmark.</param>
    /// <returns>Handle to the next sibling bookmark, or IntPtr.Zero if none exists.</returns>
    public static IntPtr GetNextSiblingBookmark(SafePdfDocumentHandle document, IntPtr bookmark)
    {
        if (document == null || document.IsInvalid)
        {
            throw new ArgumentException("Invalid document handle.", nameof(document));
        }

        return FPDFBookmark_GetNextSibling(document, bookmark);
    }

    /// <summary>
    /// Gets the title of a bookmark as a UTF-16LE encoded string.
    /// </summary>
    /// <param name="bookmark">Handle to the bookmark.</param>
    /// <returns>The bookmark title, or "(Untitled)" if the bookmark has no title.</returns>
    public static string GetBookmarkTitle(IntPtr bookmark)
    {
        if (bookmark == IntPtr.Zero)
        {
            return "(Untitled)";
        }

        // Get title length (includes null terminator)
        var length = FPDFBookmark_GetTitle(bookmark, null, 0);
        if (length == 0)
        {
            return "(Untitled)";
        }

        // Get title bytes (UTF-16LE)
        var buffer = new byte[length];
        FPDFBookmark_GetTitle(bookmark, buffer, length);

        // Decode UTF-16LE to string and trim null terminators
        return System.Text.Encoding.Unicode.GetString(buffer).TrimEnd('\0');
    }

    /// <summary>
    /// Gets the destination of a bookmark.
    /// </summary>
    /// <param name="document">Handle to the PDF document.</param>
    /// <param name="bookmark">Handle to the bookmark.</param>
    /// <returns>Handle to the destination, or IntPtr.Zero if the bookmark has no destination.</returns>
    public static IntPtr GetBookmarkDest(SafePdfDocumentHandle document, IntPtr bookmark)
    {
        if (document == null || document.IsInvalid)
        {
            throw new ArgumentException("Invalid document handle.", nameof(document));
        }

        if (bookmark == IntPtr.Zero)
        {
            return IntPtr.Zero;
        }

        return FPDFBookmark_GetDest(document, bookmark);
    }

    /// <summary>
    /// Gets the page index of a destination (0-based).
    /// </summary>
    /// <param name="document">Handle to the PDF document.</param>
    /// <param name="dest">Handle to the destination.</param>
    /// <returns>Zero-based page index, or -1 if invalid.</returns>
    public static int GetDestPageIndex(SafePdfDocumentHandle document, IntPtr dest)
    {
        if (document == null || document.IsInvalid)
        {
            throw new ArgumentException("Invalid document handle.", nameof(document));
        }

        if (dest == IntPtr.Zero)
        {
            return -1;
        }

        return (int)FPDFDest_GetDestPageIndex(document, dest);
    }

    /// <summary>
    /// Gets the location coordinates within a page for a destination.
    /// </summary>
    /// <param name="dest">Handle to the destination.</param>
    /// <param name="hasX">Outputs true if the destination has an X coordinate.</param>
    /// <param name="hasY">Outputs true if the destination has a Y coordinate.</param>
    /// <param name="hasZoom">Outputs true if the destination has a zoom factor.</param>
    /// <param name="x">Outputs the X coordinate.</param>
    /// <param name="y">Outputs the Y coordinate.</param>
    /// <param name="zoom">Outputs the zoom factor.</param>
    /// <returns>True if the operation succeeded; otherwise, false.</returns>
    public static bool GetDestLocationInPage(
        IntPtr dest,
        out bool hasX,
        out bool hasY,
        out bool hasZoom,
        out float x,
        out float y,
        out float zoom)
    {
        hasX = false;
        hasY = false;
        hasZoom = false;
        x = 0;
        y = 0;
        zoom = 0;

        if (dest == IntPtr.Zero)
        {
            return false;
        }

        int hasXInt, hasYInt, hasZoomInt;
        var result = FPDFDest_GetLocationInPage(dest, out hasXInt, out hasYInt, out hasZoomInt, out x, out y, out zoom);

        hasX = hasXInt != 0;
        hasY = hasYInt != 0;
        hasZoom = hasZoomInt != 0;

        return result;
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFBookmark_GetFirstChild(SafePdfDocumentHandle document, IntPtr bookmark);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFBookmark_GetNextSibling(SafePdfDocumentHandle document, IntPtr bookmark);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint FPDFBookmark_GetTitle(IntPtr bookmark, byte[]? buffer, uint buflen);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFBookmark_GetDest(SafePdfDocumentHandle document, IntPtr bookmark);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint FPDFDest_GetDestPageIndex(SafePdfDocumentHandle document, IntPtr dest);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFDest_GetLocationInPage(
        IntPtr dest,
        out int hasX,
        out int hasY,
        out int hasZoom,
        out float x,
        out float y,
        out float zoom);

    #endregion

    #region Text Extraction Functions

    /// <summary>
    /// Loads text page information from a PDF page.
    /// </summary>
    /// <param name="page">Handle to the PDF page.</param>
    /// <returns>A safe handle to the text page, or an invalid handle if loading failed.</returns>
    public static SafePdfTextPageHandle LoadTextPage(SafePdfPageHandle page)
    {
        if (page == null || page.IsInvalid)
        {
            throw new ArgumentException("Invalid page handle.", nameof(page));
        }

        var handle = FPDFText_LoadPage(page);
        return handle;
    }

    /// <summary>
    /// Gets the number of characters in a text page.
    /// </summary>
    /// <param name="textPage">Handle to the text page.</param>
    /// <returns>The number of characters, or 0 if the text page is invalid.</returns>
    public static int GetTextCharCount(SafePdfTextPageHandle textPage)
    {
        if (textPage == null || textPage.IsInvalid)
        {
            return 0;
        }

        return FPDFText_CountChars(textPage);
    }

    /// <summary>
    /// Extracts text from a text page.
    /// </summary>
    /// <param name="textPage">Handle to the text page.</param>
    /// <param name="startIndex">Zero-based index of the first character.</param>
    /// <param name="count">Number of characters to extract.</param>
    /// <returns>The extracted text, or an empty string if extraction failed.</returns>
    public static string GetText(SafePdfTextPageHandle textPage, int startIndex, int count)
    {
        if (textPage == null || textPage.IsInvalid)
        {
            return string.Empty;
        }

        if (count <= 0)
        {
            return string.Empty;
        }

        // Buffer size in bytes (UTF-16 uses 2 bytes per character + null terminator)
        var bufferSize = (count + 1) * 2;
        var buffer = new byte[bufferSize];

        // Extract text (returns number of characters including null terminator)
        var extractedCount = FPDFText_GetText(textPage, startIndex, count, buffer);
        if (extractedCount <= 0)
        {
            return string.Empty;
        }

        // Decode UTF-16LE to string and trim null terminators
        return System.Text.Encoding.Unicode.GetString(buffer, 0, (extractedCount - 1) * 2);
    }

    /// <summary>
    /// Gets the bounding box of a character.
    /// </summary>
    /// <param name="textPage">Handle to the text page.</param>
    /// <param name="charIndex">Zero-based character index.</param>
    /// <param name="left">Outputs the left coordinate.</param>
    /// <param name="top">Outputs the top coordinate.</param>
    /// <param name="right">Outputs the right coordinate.</param>
    /// <param name="bottom">Outputs the bottom coordinate.</param>
    /// <returns>True if the operation succeeded; otherwise, false.</returns>
    public static bool GetCharBox(
        SafePdfTextPageHandle textPage,
        int charIndex,
        out double left,
        out double top,
        out double right,
        out double bottom)
    {
        left = 0;
        top = 0;
        right = 0;
        bottom = 0;

        if (textPage == null || textPage.IsInvalid)
        {
            return false;
        }

        return FPDFText_GetCharBox(textPage, charIndex, out left, out top, out right, out bottom);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern SafePdfTextPageHandle FPDFText_LoadPage(SafePdfPageHandle page);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void FPDFText_ClosePage(IntPtr text_page);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFText_CountChars(SafePdfTextPageHandle text_page);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFText_GetText(
        SafePdfTextPageHandle text_page,
        int start_index,
        int count,
        [Out] byte[] result);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFText_GetCharBox(
        SafePdfTextPageHandle text_page,
        int index,
        out double left,
        out double top,
        out double right,
        out double bottom);

    #endregion

    #region Text Search Functions

    /// <summary>
    /// Starts a text search on a text page.
    /// </summary>
    /// <param name="textPage">Handle to the text page.</param>
    /// <param name="query">The search query string (UTF-16LE encoded).</param>
    /// <param name="flags">Search flags (case sensitivity, whole word, etc.).</param>
    /// <param name="startIndex">Zero-based character index to start the search.</param>
    /// <returns>Handle to the search context, or IntPtr.Zero if the search failed to start.</returns>
    public static IntPtr StartTextSearch(SafePdfTextPageHandle textPage, string query, SearchFlags flags, int startIndex = 0)
    {
        if (textPage == null || textPage.IsInvalid)
        {
            throw new ArgumentException("Invalid text page handle.", nameof(textPage));
        }

        if (string.IsNullOrEmpty(query))
        {
            throw new ArgumentException("Search query cannot be null or empty.", nameof(query));
        }

        // Convert string to UTF-16LE byte array
        var queryBytes = System.Text.Encoding.Unicode.GetBytes(query + "\0");
        return FPDFText_FindStart(textPage, queryBytes, (uint)flags, startIndex);
    }

    /// <summary>
    /// Finds the next search match.
    /// </summary>
    /// <param name="searchHandle">Handle to the search context.</param>
    /// <returns>True if a match was found; otherwise, false.</returns>
    public static bool FindNext(IntPtr searchHandle)
    {
        if (searchHandle == IntPtr.Zero)
        {
            return false;
        }

        return FPDFText_FindNext(searchHandle);
    }

    /// <summary>
    /// Finds the previous search match.
    /// </summary>
    /// <param name="searchHandle">Handle to the search context.</param>
    /// <returns>True if a match was found; otherwise, false.</returns>
    public static bool FindPrev(IntPtr searchHandle)
    {
        if (searchHandle == IntPtr.Zero)
        {
            return false;
        }

        return FPDFText_FindPrev(searchHandle);
    }

    /// <summary>
    /// Gets the character index of the current search match.
    /// </summary>
    /// <param name="searchHandle">Handle to the search context.</param>
    /// <returns>Zero-based character index of the match, or -1 if no match.</returns>
    public static int GetSearchResultIndex(IntPtr searchHandle)
    {
        if (searchHandle == IntPtr.Zero)
        {
            return -1;
        }

        return FPDFText_GetSchResultIndex(searchHandle);
    }

    /// <summary>
    /// Gets the number of characters in the current search match.
    /// </summary>
    /// <param name="searchHandle">Handle to the search context.</param>
    /// <returns>Number of characters in the match, or 0 if no match.</returns>
    public static int GetSearchResultCount(IntPtr searchHandle)
    {
        if (searchHandle == IntPtr.Zero)
        {
            return 0;
        }

        return FPDFText_GetSchCount(searchHandle);
    }

    /// <summary>
    /// Closes a search context and releases its resources.
    /// </summary>
    /// <param name="searchHandle">Handle to the search context.</param>
    public static void CloseSearch(IntPtr searchHandle)
    {
        if (searchHandle != IntPtr.Zero)
        {
            FPDFText_FindClose(searchHandle);
        }
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFText_FindStart(
        SafePdfTextPageHandle text_page,
        [MarshalAs(UnmanagedType.LPArray)] byte[] findwhat,
        uint flags,
        int start_index);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFText_FindNext(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFText_FindPrev(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFText_GetSchResultIndex(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFText_GetSchCount(IntPtr handle);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDFText_FindClose(IntPtr handle);

    #endregion

    #region Search Flags

    /// <summary>
    /// Search flags for text search operations.
    /// </summary>
    [Flags]
    public enum SearchFlags : uint
    {
        /// <summary>
        /// Default search (case-insensitive, partial word matching).
        /// </summary>
        None = 0,

        /// <summary>
        /// Match case when searching.
        /// </summary>
        MatchCase = 0x00000001,

        /// <summary>
        /// Match whole word only.
        /// </summary>
        MatchWholeWord = 0x00000002,

        /// <summary>
        /// Search consecutively (used internally by PDFium).
        /// </summary>
        Consecutive = 0x00000004
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
