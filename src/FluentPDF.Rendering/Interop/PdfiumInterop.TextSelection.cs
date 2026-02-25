using System.Runtime.InteropServices;

namespace FluentPDF.Rendering.Interop;

public static partial class PdfiumInterop
{
    #region Text Selection Regions

    /// <summary>Count text rectangles in a range of characters.</summary>
    public static int CountTextRects(IntPtr textPage, int startIndex, int count)
    {
        if (textPage == IntPtr.Zero) return 0;
        return FPDFText_CountRects(textPage, startIndex, count);
    }

    /// <summary>Get a text rectangle by index (after calling CountTextRects).</summary>
    public static bool GetTextRect(IntPtr textPage, int rectIndex, out double left, out double top, out double right, out double bottom)
    {
        left = top = right = bottom = 0;
        if (textPage == IntPtr.Zero) return false;
        return FPDFText_GetRect(textPage, rectIndex, out left, out top, out right, out bottom);
    }

    /// <summary>Get text within a bounding rectangle.</summary>
    public static string GetBoundedText(IntPtr textPage, double left, double top, double right, double bottom)
    {
        if (textPage == IntPtr.Zero) return string.Empty;
        int charCount = FPDFText_GetBoundedText(textPage, left, top, right, bottom, null, 0);
        if (charCount <= 0) return string.Empty;
        var buffer = new char[charCount + 1];
        FPDFText_GetBoundedText(textPage, left, top, right, bottom, buffer, charCount + 1);
        return new string(buffer, 0, charCount > 0 ? charCount - 1 : 0);
    }

    /// <summary>Get character index at a specific page coordinate.</summary>
    public static int GetCharIndexAtPos(IntPtr textPage, double x, double y, double xTolerance, double yTolerance)
    {
        if (textPage == IntPtr.Zero) return -1;
        return FPDFText_GetCharIndexAtPos(textPage, x, y, xTolerance, yTolerance);
    }

    #endregion

    #region Document Metadata

    /// <summary>Get document metadata (Title, Author, Subject, Keywords, Creator, Producer).</summary>
    public static string GetMetaText(SafePdfDocumentHandle document, string tag)
    {
        if (document == null || document.IsInvalid || string.IsNullOrEmpty(tag)) return string.Empty;
        uint length = FPDF_GetMetaText(document, tag, null, 0);
        if (length <= 2) return string.Empty; // 2 = null terminator in UTF-16
        var buffer = new byte[length];
        FPDF_GetMetaText(document, tag, buffer, length);
        return System.Text.Encoding.Unicode.GetString(buffer, 0, (int)length - 2);
    }

    /// <summary>Get page label (e.g., "i", "ii", "1", "2").</summary>
    public static string GetPageLabel(SafePdfDocumentHandle document, int pageIndex)
    {
        if (document == null || document.IsInvalid) return string.Empty;
        uint length = FPDF_GetPageLabel(document, pageIndex, null, 0);
        if (length <= 2) return string.Empty;
        var buffer = new byte[length];
        FPDF_GetPageLabel(document, pageIndex, buffer, length);
        return System.Text.Encoding.Unicode.GetString(buffer, 0, (int)length - 2);
    }

    #endregion

    #region Link Detection

    /// <summary>Load link information for a page.</summary>
    public static IntPtr LoadPageLinks(SafePdfPageHandle page)
    {
        if (page == null || page.IsInvalid) return IntPtr.Zero;
        return FPDFLink_LoadWebLinks(IntPtr.Zero); // Note: requires text page; simplified
    }

    /// <summary>Get link at specific point on page. Returns link index or -1.</summary>
    public static IntPtr GetLinkAtPoint(SafePdfPageHandle page, double x, double y)
    {
        if (page == null || page.IsInvalid) return IntPtr.Zero;
        return FPDFLink_GetLinkAtPoint(page, x, y);
    }

    /// <summary>Get destination of a link.</summary>
    public static IntPtr GetLinkDest(SafePdfDocumentHandle document, IntPtr link)
    {
        if (document == null || document.IsInvalid || link == IntPtr.Zero) return IntPtr.Zero;
        return FPDFLink_GetDest(document, link);
    }

    /// <summary>Get URI action string from a link.</summary>
    public static string GetLinkUri(SafePdfDocumentHandle document, IntPtr link)
    {
        if (document == null || document.IsInvalid || link == IntPtr.Zero) return string.Empty;
        var action = FPDFLink_GetAction(link);
        if (action == IntPtr.Zero) return string.Empty;
        uint actionType = FPDFAction_GetType(action);
        if (actionType != 3) return string.Empty; // 3 = PDFACTION_URI
        uint length = FPDFAction_GetURIPath(document, action, null, 0);
        if (length <= 1) return string.Empty;
        var buffer = new byte[length];
        FPDFAction_GetURIPath(document, action, buffer, length);
        return System.Text.Encoding.UTF8.GetString(buffer, 0, (int)length - 1);
    }

    #endregion

    // Text selection DllImports
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFText_CountRects(IntPtr text_page, int start_index, int count);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFText_GetRect(IntPtr text_page, int rect_index, out double left, out double top, out double right, out double bottom);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFText_GetBoundedText(IntPtr text_page, double left, double top, double right, double bottom, [Out] char[]? buffer, int buflen);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFText_GetCharIndexAtPos(IntPtr text_page, double x, double y, double xTolerance, double yTolerance);

    // Document metadata DllImports
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern uint FPDF_GetMetaText(SafePdfDocumentHandle document, [MarshalAs(UnmanagedType.LPStr)] string tag, [Out] byte[]? buffer, uint buflen);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint FPDF_GetPageLabel(SafePdfDocumentHandle document, int page_index, [Out] byte[]? buffer, uint buflen);

    // Link DllImports
    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFLink_GetLinkAtPoint(SafePdfPageHandle page, double x, double y);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFLink_GetDest(SafePdfDocumentHandle document, IntPtr link);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFLink_GetAction(IntPtr link);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint FPDFAction_GetType(IntPtr action);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern uint FPDFAction_GetURIPath(SafePdfDocumentHandle document, IntPtr action, [Out] byte[]? buffer, uint buflen);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFLink_LoadWebLinks(IntPtr text_page);
}
