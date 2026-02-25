using System.Runtime.InteropServices;

namespace FluentPDF.Rendering.Interop;

public static partial class PdfiumInterop
{
    #region Shape Creation

    /// <summary>
    /// Creates a new rectangle page object.
    /// </summary>
    /// <param name="x">Left coordinate.</param>
    /// <param name="y">Bottom coordinate.</param>
    /// <param name="width">Width of the rectangle.</param>
    /// <param name="height">Height of the rectangle.</param>
    /// <returns>Handle to the rectangle object, or IntPtr.Zero if creation failed.</returns>
    public static IntPtr CreateRectObject(float x, float y, float width, float height)
    {
        return FPDFPageObj_CreateNewRect(x, y, width, height);
    }

    /// <summary>
    /// Creates a new path object starting at the specified point.
    /// </summary>
    /// <param name="x">Starting X coordinate.</param>
    /// <param name="y">Starting Y coordinate.</param>
    /// <returns>Handle to the path object, or IntPtr.Zero if creation failed.</returns>
    public static IntPtr CreatePathObject(float x, float y)
    {
        return FPDFPageObj_CreateNewPath(x, y);
    }

    #endregion

    #region Path Operations

    /// <summary>
    /// Sets the fill and stroke mode for a path object.
    /// </summary>
    /// <param name="pathObject">Handle to the path object.</param>
    /// <param name="fillMode">Fill mode: 0=none, 1=alternate, 2=winding.</param>
    /// <param name="stroke">Whether to stroke the path.</param>
    /// <returns>True if the draw mode was set successfully; otherwise, false.</returns>
    public static bool SetPathDrawMode(IntPtr pathObject, int fillMode, bool stroke)
    {
        if (pathObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid path object handle.", nameof(pathObject));
        }

        return FPDFPath_SetDrawMode(pathObject, fillMode, stroke);
    }

    /// <summary>
    /// Adds a line segment from the current point to the specified point.
    /// </summary>
    /// <param name="pathObject">Handle to the path object.</param>
    /// <param name="x">X coordinate of the line endpoint.</param>
    /// <param name="y">Y coordinate of the line endpoint.</param>
    /// <returns>True if the line segment was added successfully; otherwise, false.</returns>
    public static bool PathLineTo(IntPtr pathObject, float x, float y)
    {
        if (pathObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid path object handle.", nameof(pathObject));
        }

        return FPDFPath_LineTo(pathObject, x, y);
    }

    /// <summary>
    /// Adds a cubic Bezier curve to the path.
    /// </summary>
    /// <param name="pathObject">Handle to the path object.</param>
    /// <param name="x1">X coordinate of the first control point.</param>
    /// <param name="y1">Y coordinate of the first control point.</param>
    /// <param name="x2">X coordinate of the second control point.</param>
    /// <param name="y2">Y coordinate of the second control point.</param>
    /// <param name="x3">X coordinate of the endpoint.</param>
    /// <param name="y3">Y coordinate of the endpoint.</param>
    /// <returns>True if the curve was added successfully; otherwise, false.</returns>
    public static bool PathBezierTo(
        IntPtr pathObject,
        float x1,
        float y1,
        float x2,
        float y2,
        float x3,
        float y3)
    {
        if (pathObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid path object handle.", nameof(pathObject));
        }

        return FPDFPath_BezierTo(pathObject, x1, y1, x2, y2, x3, y3);
    }

    /// <summary>
    /// Moves the current point to the specified coordinates without drawing.
    /// </summary>
    /// <param name="pathObject">Handle to the path object.</param>
    /// <param name="x">X coordinate to move to.</param>
    /// <param name="y">Y coordinate to move to.</param>
    /// <returns>True if the move was successful; otherwise, false.</returns>
    public static bool PathMoveTo(IntPtr pathObject, float x, float y)
    {
        if (pathObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid path object handle.", nameof(pathObject));
        }

        return FPDFPath_MoveTo(pathObject, x, y);
    }

    /// <summary>
    /// Closes the current path by connecting the last point to the first point.
    /// </summary>
    /// <param name="pathObject">Handle to the path object.</param>
    /// <returns>True if the path was closed successfully; otherwise, false.</returns>
    public static bool PathClose(IntPtr pathObject)
    {
        if (pathObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid path object handle.", nameof(pathObject));
        }

        return FPDFPath_Close(pathObject);
    }

    #endregion

    #region Line Styling

    /// <summary>
    /// Sets the stroke width for a page object.
    /// </summary>
    /// <param name="pageObject">Handle to the page object.</param>
    /// <param name="width">Stroke width in points.</param>
    /// <returns>True if the width was set successfully; otherwise, false.</returns>
    public static bool SetStrokeWidth(IntPtr pageObject, float width)
    {
        if (pageObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid page object handle.", nameof(pageObject));
        }

        return FPDFPageObj_SetStrokeWidth(pageObject, width);
    }

    /// <summary>
    /// Gets the stroke width of a page object.
    /// </summary>
    /// <param name="pageObject">Handle to the page object.</param>
    /// <param name="width">Outputs the stroke width in points.</param>
    /// <returns>True if the width was retrieved successfully; otherwise, false.</returns>
    public static bool GetStrokeWidth(IntPtr pageObject, out float width)
    {
        width = 0;

        if (pageObject == IntPtr.Zero)
        {
            return false;
        }

        return FPDFPageObj_GetStrokeWidth(pageObject, out width);
    }

    /// <summary>
    /// Sets the line cap style for a page object.
    /// </summary>
    /// <param name="pageObject">Handle to the page object.</param>
    /// <param name="lineCap">Line cap style: 0=butt, 1=round, 2=square.</param>
    /// <returns>True if the line cap was set successfully; otherwise, false.</returns>
    public static bool SetLineCap(IntPtr pageObject, int lineCap)
    {
        if (pageObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid page object handle.", nameof(pageObject));
        }

        return FPDFPageObj_SetLineCap(pageObject, lineCap);
    }

    /// <summary>
    /// Sets the line join style for a page object.
    /// </summary>
    /// <param name="pageObject">Handle to the page object.</param>
    /// <param name="lineJoin">Line join style: 0=miter, 1=round, 2=bevel.</param>
    /// <returns>True if the line join was set successfully; otherwise, false.</returns>
    public static bool SetLineJoin(IntPtr pageObject, int lineJoin)
    {
        if (pageObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid page object handle.", nameof(pageObject));
        }

        return FPDFPageObj_SetLineJoin(pageObject, lineJoin);
    }

    /// <summary>
    /// Gets the type of a page object.
    /// </summary>
    /// <param name="pageObject">Handle to the page object.</param>
    /// <returns>Object type: 1=text, 2=path, 3=image, 4=shading, 5=form. Returns 0 if invalid.</returns>
    public static int GetPageObjectType(IntPtr pageObject)
    {
        if (pageObject == IntPtr.Zero)
        {
            return 0;
        }

        return FPDFPageObj_GetType(pageObject);
    }

    #endregion

    #region Page Management

    /// <summary>
    /// Creates a new blank page in the document.
    /// </summary>
    /// <param name="document">Handle to the PDF document.</param>
    /// <param name="pageIndex">Zero-based index where the page should be inserted.</param>
    /// <param name="width">Page width in points.</param>
    /// <param name="height">Page height in points.</param>
    /// <returns>Handle to the new page.</returns>
    public static SafePdfPageHandle CreateNewPage(
        SafePdfDocumentHandle document,
        int pageIndex,
        double width,
        double height)
    {
        if (document == null || document.IsInvalid)
        {
            throw new ArgumentException("Invalid document handle.", nameof(document));
        }

        return FPDFPage_New(document, pageIndex, width, height);
    }

    /// <summary>
    /// Deletes a page from the document.
    /// </summary>
    /// <param name="document">Handle to the PDF document.</param>
    /// <param name="pageIndex">Zero-based index of the page to delete.</param>
    public static void DeletePage(SafePdfDocumentHandle document, int pageIndex)
    {
        if (document == null || document.IsInvalid)
        {
            throw new ArgumentException("Invalid document handle.", nameof(document));
        }

        FPDFPage_Delete(document, pageIndex);
    }

    /// <summary>
    /// Flattens annotations and form fields into page content.
    /// </summary>
    /// <param name="page">Handle to the PDF page.</param>
    /// <param name="flag">Flatten flag: 0=normal display, 1=print.</param>
    /// <returns>Result code: 0=fail, 1=success, 2=nothing to flatten.</returns>
    public static int FlattenPage(SafePdfPageHandle page, int flag = 0)
    {
        if (page == null || page.IsInvalid)
        {
            throw new ArgumentException("Invalid page handle.", nameof(page));
        }

        return FPDFPage_Flatten(page, flag);
    }

    #endregion

    #region Document Metadata

    /// <summary>
    /// Gets the PDF file version of the document.
    /// </summary>
    /// <param name="document">Handle to the PDF document.</param>
    /// <param name="version">Outputs the file version (e.g., 14 for PDF 1.4, 17 for PDF 1.7).</param>
    /// <returns>True if the version was retrieved successfully; otherwise, false.</returns>
    public static bool GetFileVersion(SafePdfDocumentHandle document, out int version)
    {
        version = 0;

        if (document == null || document.IsInvalid)
        {
            return false;
        }

        return FPDF_GetFileVersion(document, out version);
    }

    /// <summary>
    /// Gets the permission flags for the document.
    /// </summary>
    /// <param name="document">Handle to the PDF document.</param>
    /// <returns>Permission flags as a bitmask, or 0 if the document is invalid.</returns>
    public static uint GetDocPermissions(SafePdfDocumentHandle document)
    {
        if (document == null || document.IsInvalid)
        {
            return 0;
        }

        return FPDF_GetDocPermissions(document);
    }

    #endregion

    #region Shape DllImports

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFPageObj_CreateNewRect(float x, float y, float w, float h);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFPageObj_CreateNewPath(float x, float y);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPath_SetDrawMode(
        IntPtr path,
        int fillmode,
        [MarshalAs(UnmanagedType.Bool)] bool stroke);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPath_LineTo(IntPtr path, float x, float y);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPath_BezierTo(
        IntPtr path,
        float x1,
        float y1,
        float x2,
        float y2,
        float x3,
        float y3);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPath_MoveTo(IntPtr path, float x, float y);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPath_Close(IntPtr path);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPageObj_SetStrokeWidth(IntPtr page_object, float width);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPageObj_GetStrokeWidth(IntPtr page_object, out float width);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPageObj_SetLineCap(IntPtr page_object, int line_cap);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPageObj_SetLineJoin(IntPtr page_object, int line_join);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFPageObj_GetType(IntPtr page_object);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern SafePdfPageHandle FPDFPage_New(
        SafePdfDocumentHandle document,
        int page_index,
        double width,
        double height);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDFPage_Delete(SafePdfDocumentHandle document, int page_index);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFPage_Flatten(SafePdfPageHandle page, int nFlag);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDF_GetFileVersion(
        SafePdfDocumentHandle document,
        out int fileVersion);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint FPDF_GetDocPermissions(SafePdfDocumentHandle document);

    #endregion
}
