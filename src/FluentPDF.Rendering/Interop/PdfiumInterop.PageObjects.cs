using System.Runtime.InteropServices;

namespace FluentPDF.Rendering.Interop;

public static partial class PdfiumInterop
{
    #region Page Object Functions

    /// <summary>
    /// Creates a new image object.
    /// </summary>
    /// <param name="document">Handle to the PDF document.</param>
    /// <returns>Handle to the image object, or IntPtr.Zero if creation failed.</returns>
    public static IntPtr CreateImageObject(SafePdfDocumentHandle document)
    {
        if (document == null || document.IsInvalid)
        {
            throw new ArgumentException("Invalid document handle.", nameof(document));
        }

        return FPDFPageObj_NewImageObj(document);
    }

    /// <summary>
    /// Loads a JPEG image from a file into an image object.
    /// </summary>
    /// <param name="pages">Array of page handles that will use this image (can be null).</param>
    /// <param name="count">Number of pages in the array.</param>
    /// <param name="imageObject">Handle to the image object.</param>
    /// <param name="filePath">Path to the JPEG file.</param>
    /// <returns>True if the image was loaded successfully; otherwise, false.</returns>
    public static bool LoadJpegFile(IntPtr[] pages, int count, IntPtr imageObject, string filePath)
    {
        if (imageObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid image object handle.", nameof(imageObject));
        }

        return FPDFImageObj_LoadJpegFile(pages, count, imageObject, filePath);
    }

    /// <summary>
    /// Loads a JPEG image from a file inline into an image object.
    /// </summary>
    /// <param name="pages">Array of page handles that will use this image (can be null).</param>
    /// <param name="count">Number of pages in the array.</param>
    /// <param name="imageObject">Handle to the image object.</param>
    /// <param name="filePath">Path to the JPEG file.</param>
    /// <returns>True if the image was loaded successfully; otherwise, false.</returns>
    public static bool LoadJpegFileInline(IntPtr[] pages, int count, IntPtr imageObject, string filePath)
    {
        if (imageObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid image object handle.", nameof(imageObject));
        }

        return FPDFImageObj_LoadJpegFileInline(pages, count, imageObject, filePath);
    }

    /// <summary>
    /// Sets a bitmap as the image content for an image object.
    /// </summary>
    /// <param name="pages">Array of page handles that will use this image (can be null).</param>
    /// <param name="count">Number of pages in the array.</param>
    /// <param name="imageObject">Handle to the image object.</param>
    /// <param name="bitmap">Handle to the bitmap.</param>
    /// <returns>True if the bitmap was set successfully; otherwise, false.</returns>
    public static bool SetImageBitmap(IntPtr[] pages, int count, IntPtr imageObject, IntPtr bitmap)
    {
        if (imageObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid image object handle.", nameof(imageObject));
        }

        if (bitmap == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid bitmap handle.", nameof(bitmap));
        }

        return FPDFImageObj_SetBitmap(pages, count, imageObject, bitmap);
    }

    /// <summary>
    /// Transforms a page object with a transformation matrix.
    /// </summary>
    /// <param name="pageObject">Handle to the page object.</param>
    /// <param name="a">Matrix a component.</param>
    /// <param name="b">Matrix b component.</param>
    /// <param name="c">Matrix c component.</param>
    /// <param name="d">Matrix d component.</param>
    /// <param name="e">Matrix e component (x translation).</param>
    /// <param name="f">Matrix f component (y translation).</param>
    public static void TransformPageObject(IntPtr pageObject, double a, double b, double c, double d, double e, double f)
    {
        if (pageObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid page object handle.", nameof(pageObject));
        }

        FPDFPageObj_Transform(pageObject, a, b, c, d, e, f);
    }

    /// <summary>
    /// Gets the current transformation matrix for a page object.
    /// </summary>
    public static bool GetPageObjectMatrix(IntPtr pageObject, out float a, out float b, out float c, out float d, out float e, out float f)
    {
        a = b = c = d = e = f = 0;
        if (pageObject == IntPtr.Zero) return false;
        if (!FPDFPageObj_GetMatrix(pageObject, out var matrix)) return false;
        a = matrix.a; b = matrix.b; c = matrix.c;
        d = matrix.d; e = matrix.e; f = matrix.f;
        return true;
    }

    /// <summary>
    /// Sets the transformation matrix for a page object.
    /// </summary>
    /// <param name="pageObject">Handle to the page object.</param>
    /// <param name="a">Matrix a component.</param>
    /// <param name="b">Matrix b component.</param>
    /// <param name="c">Matrix c component.</param>
    /// <param name="d">Matrix d component.</param>
    /// <param name="e">Matrix e component (x translation).</param>
    /// <param name="f">Matrix f component (y translation).</param>
    /// <returns>True if the matrix was set successfully; otherwise, false.</returns>
    public static bool SetPageObjectMatrix(IntPtr pageObject, double a, double b, double c, double d, double e, double f)
    {
        if (pageObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid page object handle.", nameof(pageObject));
        }

        var matrix = new FS_MATRIX
        {
            a = (float)a, b = (float)b, c = (float)c,
            d = (float)d, e = (float)e, f = (float)f
        };
        return FPDFPageObj_SetMatrix(pageObject, ref matrix);
    }

    /// <summary>
    /// Inserts a page object into a page.
    /// </summary>
    /// <param name="page">Handle to the PDF page.</param>
    /// <param name="pageObject">Handle to the page object to insert.</param>
    public static void InsertPageObject(SafePdfPageHandle page, IntPtr pageObject)
    {
        if (page == null || page.IsInvalid)
        {
            throw new ArgumentException("Invalid page handle.", nameof(page));
        }

        if (pageObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid page object handle.", nameof(pageObject));
        }

        FPDFPage_InsertObject(page, pageObject);
    }

    /// <summary>
    /// Removes a page object from a page.
    /// </summary>
    /// <param name="page">Handle to the PDF page.</param>
    /// <param name="pageObject">Handle to the page object to remove.</param>
    /// <returns>True if the object was removed successfully; otherwise, false.</returns>
    public static bool RemovePageObject(SafePdfPageHandle page, IntPtr pageObject)
    {
        if (page == null || page.IsInvalid)
        {
            throw new ArgumentException("Invalid page handle.", nameof(page));
        }

        if (pageObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid page object handle.", nameof(pageObject));
        }

        return FPDFPage_RemoveObject(page, pageObject);
    }

    /// <summary>
    /// Gets the bounding box of a page object.
    /// </summary>
    /// <param name="pageObject">Handle to the page object.</param>
    /// <param name="left">Outputs the left coordinate.</param>
    /// <param name="bottom">Outputs the bottom coordinate.</param>
    /// <param name="right">Outputs the right coordinate.</param>
    /// <param name="top">Outputs the top coordinate.</param>
    /// <returns>True if the bounds were retrieved successfully; otherwise, false.</returns>
    public static bool GetPageObjectBounds(
        IntPtr pageObject,
        out float left,
        out float bottom,
        out float right,
        out float top)
    {
        left = 0;
        bottom = 0;
        right = 0;
        top = 0;

        if (pageObject == IntPtr.Zero)
        {
            return false;
        }

        return FPDFPageObj_GetBounds(pageObject, out left, out bottom, out right, out top);
    }

    /// <summary>
    /// Destroys a page object and releases its resources.
    /// </summary>
    /// <param name="pageObject">Handle to the page object.</param>
    public static void DestroyPageObject(IntPtr pageObject)
    {
        if (pageObject != IntPtr.Zero)
        {
            FPDFPageObj_Destroy(pageObject);
        }
    }

    /// <summary>
    /// Creates a new text object.
    /// </summary>
    /// <param name="document">Handle to the PDF document.</param>
    /// <param name="font">Handle to the font.</param>
    /// <param name="fontSize">Font size in points.</param>
    /// <returns>Handle to the text object, or IntPtr.Zero if creation failed.</returns>
    public static IntPtr CreateTextObject(SafePdfDocumentHandle document, IntPtr font, float fontSize)
    {
        if (document == null || document.IsInvalid)
        {
            throw new ArgumentException("Invalid document handle.", nameof(document));
        }

        if (font == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid font handle.", nameof(font));
        }

        return FPDFPageObj_CreateTextObj(document, font, fontSize);
    }

    /// <summary>
    /// Loads a standard font for use in text objects.
    /// </summary>
    /// <param name="document">Handle to the PDF document.</param>
    /// <param name="fontName">Name of the standard font (e.g., "Arial", "Times-Roman", "Helvetica").</param>
    /// <returns>Handle to the font, or IntPtr.Zero if loading failed.</returns>
    public static IntPtr LoadStandardFont(SafePdfDocumentHandle document, string fontName)
    {
        if (document == null || document.IsInvalid)
        {
            throw new ArgumentException("Invalid document handle.", nameof(document));
        }

        if (string.IsNullOrWhiteSpace(fontName))
        {
            throw new ArgumentException("Font name cannot be null or empty.", nameof(fontName));
        }

        return FPDFText_LoadStandardFont(document, fontName);
    }

    /// <summary>
    /// Sets the text content for a text object.
    /// </summary>
    /// <param name="textObject">Handle to the text object.</param>
    /// <param name="text">Text content to set.</param>
    /// <returns>True if the text was set successfully; otherwise, false.</returns>
    public static bool SetTextObjectText(IntPtr textObject, string text)
    {
        if (textObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid text object handle.", nameof(textObject));
        }

        return FPDFText_SetText(textObject, text);
    }

    /// <summary>
    /// Sets the fill color for a page object (text or path).
    /// </summary>
    /// <param name="pageObject">Handle to the page object.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="a">Alpha component (0-255).</param>
    /// <returns>True if the color was set successfully; otherwise, false.</returns>
    public static bool SetPageObjectFillColor(IntPtr pageObject, uint r, uint g, uint b, uint a)
    {
        if (pageObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid page object handle.", nameof(pageObject));
        }

        return FPDFPageObj_SetFillColor(pageObject, r, g, b, a);
    }

    /// <summary>
    /// Sets the stroke color for a page object.
    /// </summary>
    /// <param name="pageObject">Handle to the page object.</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="a">Alpha component (0-255).</param>
    /// <returns>True if the color was set successfully; otherwise, false.</returns>
    public static bool SetPageObjectStrokeColor(IntPtr pageObject, uint r, uint g, uint b, uint a)
    {
        if (pageObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid page object handle.", nameof(pageObject));
        }

        return FPDFPageObj_SetStrokeColor(pageObject, r, g, b, a);
    }

    /// <summary>
    /// Marks a page object for modification in the content stream.
    /// Must be called after modifying a page object.
    /// </summary>
    /// <param name="page">Handle to the page.</param>
    /// <param name="pageObject">Handle to the page object.</param>
    public static void MarkPageObjectDirty(SafePdfPageHandle page, IntPtr pageObject)
    {
        if (page == null || page.IsInvalid)
        {
            throw new ArgumentException("Invalid page handle.", nameof(page));
        }

        if (pageObject == IntPtr.Zero)
        {
            throw new ArgumentException("Invalid page object handle.", nameof(pageObject));
        }

        FPDFPage_GenerateContent(page);
    }

    /// <summary>
    /// Regenerates the content stream for a page after modifications.
    /// </summary>
    public static bool GenerateContent(SafePdfPageHandle page)
    {
        if (page == null || page.IsInvalid)
            throw new ArgumentException("Invalid page handle.", nameof(page));
        return FPDFPage_GenerateContent(page);
    }

    /// <summary>
    /// Gets the number of page objects on a page.
    /// </summary>
    /// <param name="page">Handle to the page.</param>
    /// <returns>Number of page objects.</returns>
    public static int GetPageObjectCount(SafePdfPageHandle page)
    {
        if (page == null || page.IsInvalid)
        {
            throw new ArgumentException("Invalid page handle.", nameof(page));
        }

        return FPDFPage_CountObjects(page);
    }

    /// <summary>
    /// Gets a page object by index.
    /// </summary>
    /// <param name="page">Handle to the page.</param>
    /// <param name="index">Zero-based index of the page object.</param>
    /// <returns>Handle to the page object, or IntPtr.Zero if index is out of range.</returns>
    public static IntPtr GetPageObject(SafePdfPageHandle page, int index)
    {
        if (page == null || page.IsInvalid)
        {
            throw new ArgumentException("Invalid page handle.", nameof(page));
        }

        return FPDFPage_GetObject(page, index);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFPageObj_NewImageObj(SafePdfDocumentHandle document);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFImageObj_LoadJpegFile(
        IntPtr[] pages,
        int nCount,
        IntPtr image_object,
        [MarshalAs(UnmanagedType.LPStr)] string file_path);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFImageObj_LoadJpegFileInline(
        IntPtr[] pages,
        int nCount,
        IntPtr image_object,
        [MarshalAs(UnmanagedType.LPStr)] string file_path);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFImageObj_SetBitmap(
        IntPtr[] pages,
        int nCount,
        IntPtr image_object,
        IntPtr bitmap);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDFPageObj_Transform(
        IntPtr page_object,
        double a,
        double b,
        double c,
        double d,
        double e,
        double f);

    [StructLayout(LayoutKind.Sequential)]
    private struct FS_MATRIX
    {
        public float a, b, c, d, e, f;
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPageObj_SetMatrix(
        IntPtr page_object,
        ref FS_MATRIX matrix);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPageObj_GetMatrix(
        IntPtr page_object,
        out FS_MATRIX matrix);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDFPage_InsertObject(SafePdfPageHandle page, IntPtr page_obj);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPage_RemoveObject(SafePdfPageHandle page, IntPtr page_obj);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPageObj_GetBounds(
        IntPtr page_object,
        out float left,
        out float bottom,
        out float right,
        out float top);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDFPageObj_Destroy(IntPtr page_object);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFPageObj_CreateTextObj(
        SafePdfDocumentHandle document,
        IntPtr font,
        float font_size);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr FPDFText_LoadStandardFont(
        SafePdfDocumentHandle document,
        [MarshalAs(UnmanagedType.LPStr)] string font);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFText_SetText(
        IntPtr text_object,
        [MarshalAs(UnmanagedType.LPWStr)] string text);

    /// <summary>
    /// Gets the fill color of a page object.
    /// </summary>
    public static bool GetPageObjectFillColor(IntPtr pageObject, out uint r, out uint g, out uint b, out uint a)
    {
        r = g = b = a = 0;
        if (pageObject == IntPtr.Zero) return false;
        return FPDFPageObj_GetFillColor(pageObject, out r, out g, out b, out a);
    }

    /// <summary>
    /// Gets the stroke color of a page object.
    /// </summary>
    public static bool GetPageObjectStrokeColor(IntPtr pageObject, out uint r, out uint g, out uint b, out uint a)
    {
        r = g = b = a = 0;
        if (pageObject == IntPtr.Zero) return false;
        return FPDFPageObj_GetStrokeColor(pageObject, out r, out g, out b, out a);
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPageObj_GetFillColor(
        IntPtr page_object,
        out uint R,
        out uint G,
        out uint B,
        out uint A);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPageObj_GetStrokeColor(
        IntPtr page_object,
        out uint R,
        out uint G,
        out uint B,
        out uint A);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPageObj_SetFillColor(
        IntPtr page_object,
        uint R,
        uint G,
        uint B,
        uint A);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPageObj_SetStrokeColor(
        IntPtr page_object,
        uint R,
        uint G,
        uint B,
        uint A);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPage_GenerateContent(SafePdfPageHandle page);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFPage_CountObjects(SafePdfPageHandle page);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFPage_GetObject(SafePdfPageHandle page, int index);

    #endregion
}
