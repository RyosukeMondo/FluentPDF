using System.Runtime.InteropServices;

namespace FluentPDF.Rendering.Interop;

public static partial class PdfiumInterop
{
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
}
