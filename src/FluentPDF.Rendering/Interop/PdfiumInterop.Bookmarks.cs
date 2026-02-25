using System.Runtime.InteropServices;

namespace FluentPDF.Rendering.Interop;

public static partial class PdfiumInterop
{
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
}
