using System.IO;
using System.Runtime.InteropServices;

namespace FluentPDF.Rendering.Interop;

public static partial class PdfiumInterop
{
    #region Annotation Functions

    /// <summary>
    /// Gets the number of annotations on a page.
    /// </summary>
    /// <param name="page">Handle to the PDF page.</param>
    /// <returns>The number of annotations, or -1 if the page is invalid.</returns>
    public static int GetAnnotationCount(SafePdfPageHandle page)
    {
        if (page == null || page.IsInvalid)
        {
            return -1;
        }

        return FPDFPage_GetAnnotCount(page);
    }

    /// <summary>
    /// Gets an annotation from a page by index.
    /// </summary>
    /// <param name="page">Handle to the PDF page.</param>
    /// <param name="index">Zero-based annotation index.</param>
    /// <returns>A safe handle to the annotation, or an invalid handle if loading failed.</returns>
    public static SafeAnnotationHandle GetAnnotation(SafePdfPageHandle page, int index)
    {
        if (page == null || page.IsInvalid)
        {
            throw new ArgumentException("Invalid page handle.", nameof(page));
        }

        var handle = FPDFPage_GetAnnot(page, index);
        return handle;
    }

    /// <summary>
    /// Creates a new annotation on a page.
    /// </summary>
    /// <param name="page">Handle to the PDF page.</param>
    /// <param name="annotationType">The type of annotation to create.</param>
    /// <returns>A safe handle to the annotation, or an invalid handle if creation failed.</returns>
    public static SafeAnnotationHandle CreateAnnotation(SafePdfPageHandle page, AnnotationType annotationType)
    {
        if (page == null || page.IsInvalid)
        {
            throw new ArgumentException("Invalid page handle.", nameof(page));
        }

        var handle = FPDFPage_CreateAnnot(page, (int)annotationType);
        return handle;
    }

    /// <summary>
    /// Removes an annotation from a page.
    /// </summary>
    /// <param name="page">Handle to the PDF page.</param>
    /// <param name="index">Zero-based annotation index.</param>
    /// <returns>True if the annotation was removed successfully; otherwise, false.</returns>
    public static bool RemoveAnnotation(SafePdfPageHandle page, int index)
    {
        if (page == null || page.IsInvalid)
        {
            return false;
        }

        return FPDFPage_RemoveAnnot(page, index);
    }

    /// <summary>
    /// Gets the subtype of an annotation.
    /// </summary>
    /// <param name="annotation">Handle to the annotation.</param>
    /// <returns>The annotation type.</returns>
    public static AnnotationType GetAnnotationSubtype(SafeAnnotationHandle annotation)
    {
        if (annotation == null || annotation.IsInvalid)
        {
            return AnnotationType.Unknown;
        }

        return (AnnotationType)FPDFAnnot_GetSubtype(annotation);
    }

    /// <summary>
    /// Sets the color of an annotation.
    /// </summary>
    /// <param name="annotation">Handle to the annotation.</param>
    /// <param name="colorType">The type of color to set (fill or stroke).</param>
    /// <param name="r">Red component (0-255).</param>
    /// <param name="g">Green component (0-255).</param>
    /// <param name="b">Blue component (0-255).</param>
    /// <param name="a">Alpha component (0-255).</param>
    /// <returns>True if the color was set successfully; otherwise, false.</returns>
    public static bool SetAnnotationColor(
        SafeAnnotationHandle annotation,
        AnnotationColorType colorType,
        uint r,
        uint g,
        uint b,
        uint a)
    {
        if (annotation == null || annotation.IsInvalid)
        {
            return false;
        }

        return FPDFAnnot_SetColor(annotation, (int)colorType, r, g, b, a);
    }

    /// <summary>
    /// Gets the color of an annotation.
    /// </summary>
    /// <param name="annotation">Handle to the annotation.</param>
    /// <param name="colorType">The type of color to get (fill or stroke).</param>
    /// <param name="r">Outputs the red component (0-255).</param>
    /// <param name="g">Outputs the green component (0-255).</param>
    /// <param name="b">Outputs the blue component (0-255).</param>
    /// <param name="a">Outputs the alpha component (0-255).</param>
    /// <returns>True if the color was retrieved successfully; otherwise, false.</returns>
    public static bool GetAnnotationColor(
        SafeAnnotationHandle annotation,
        AnnotationColorType colorType,
        out uint r,
        out uint g,
        out uint b,
        out uint a)
    {
        r = 0;
        g = 0;
        b = 0;
        a = 0;

        if (annotation == null || annotation.IsInvalid)
        {
            return false;
        }

        return FPDFAnnot_GetColor(annotation, (int)colorType, out r, out g, out b, out a);
    }

    /// <summary>
    /// Sets the rectangle bounds of an annotation.
    /// </summary>
    /// <param name="annotation">Handle to the annotation.</param>
    /// <param name="left">Left coordinate in page units.</param>
    /// <param name="bottom">Bottom coordinate in page units.</param>
    /// <param name="right">Right coordinate in page units.</param>
    /// <param name="top">Top coordinate in page units.</param>
    /// <returns>True if the rectangle was set successfully; otherwise, false.</returns>
    public static bool SetAnnotationRect(
        SafeAnnotationHandle annotation,
        float left,
        float bottom,
        float right,
        float top)
    {
        if (annotation == null || annotation.IsInvalid)
        {
            return false;
        }

        var rect = new FS_RECTF { left = left, bottom = bottom, right = right, top = top };
        return FPDFAnnot_SetRect(annotation, ref rect);
    }

    /// <summary>
    /// Gets the rectangle bounds of an annotation.
    /// </summary>
    /// <param name="annotation">Handle to the annotation.</param>
    /// <param name="left">Outputs the left coordinate in page units.</param>
    /// <param name="bottom">Outputs the bottom coordinate in page units.</param>
    /// <param name="right">Outputs the right coordinate in page units.</param>
    /// <param name="top">Outputs the top coordinate in page units.</param>
    /// <returns>True if the rectangle was retrieved successfully; otherwise, false.</returns>
    public static bool GetAnnotationRect(
        SafeAnnotationHandle annotation,
        out float left,
        out float bottom,
        out float right,
        out float top)
    {
        left = 0;
        bottom = 0;
        right = 0;
        top = 0;

        if (annotation == null || annotation.IsInvalid)
        {
            return false;
        }

        var rect = new FS_RECTF();
        var result = FPDFAnnot_GetRect(annotation, ref rect);
        if (result)
        {
            left = rect.left;
            bottom = rect.bottom;
            right = rect.right;
            top = rect.top;
        }
        return result;
    }

    /// <summary>
    /// Sets the contents (text) of an annotation.
    /// </summary>
    /// <param name="annotation">Handle to the annotation.</param>
    /// <param name="contents">The text content to set.</param>
    /// <returns>True if the contents were set successfully; otherwise, false.</returns>
    public static bool SetAnnotationContents(SafeAnnotationHandle annotation, string contents)
    {
        if (annotation == null || annotation.IsInvalid)
        {
            return false;
        }

        // Convert string to UTF-16LE byte array
        var contentsBytes = System.Text.Encoding.Unicode.GetBytes(contents + "\0");
        return FPDFAnnot_SetStringValue(annotation, "Contents", contentsBytes);
    }

    /// <summary>
    /// Gets the contents (text) of an annotation.
    /// </summary>
    /// <param name="annotation">Handle to the annotation.</param>
    /// <returns>The annotation contents, or an empty string if retrieval failed.</returns>
    public static string GetAnnotationContents(SafeAnnotationHandle annotation)
    {
        if (annotation == null || annotation.IsInvalid)
        {
            return string.Empty;
        }

        // Get the length of the contents string (in bytes, including null terminator)
        var length = FPDFAnnot_GetStringValue(annotation, "Contents", null, 0);
        if (length <= 2) // Empty or just null terminator
        {
            return string.Empty;
        }

        // Get the contents
        var buffer = new byte[length];
        FPDFAnnot_GetStringValue(annotation, "Contents", buffer, length);

        // Decode UTF-16LE to string and trim null terminators
        return System.Text.Encoding.Unicode.GetString(buffer).TrimEnd('\0');
    }

    /// <summary>
    /// Sets the quad points for a text markup annotation (highlight, underline, strikethrough).
    /// </summary>
    /// <param name="annotation">Handle to the annotation.</param>
    /// <param name="quadPoints">Array of quad points (must be a multiple of 8 values: x1,y1,x2,y2,x3,y3,x4,y4 for each quad).</param>
    /// <returns>True if the quad points were set successfully; otherwise, false.</returns>
    public static bool SetAnnotationQuadPoints(SafeAnnotationHandle annotation, float[] quadPoints)
    {
        if (annotation == null || annotation.IsInvalid)
        {
            return false;
        }

        if (quadPoints == null || quadPoints.Length % 8 != 0)
        {
            throw new ArgumentException("Quad points must be a multiple of 8 values.", nameof(quadPoints));
        }

        return FPDFAnnot_SetAttachmentPoints(annotation, 0, new FS_QUADPOINTSF
        {
            x1 = quadPoints.Length > 0 ? quadPoints[0] : 0,
            y1 = quadPoints.Length > 1 ? quadPoints[1] : 0,
            x2 = quadPoints.Length > 2 ? quadPoints[2] : 0,
            y2 = quadPoints.Length > 3 ? quadPoints[3] : 0,
            x3 = quadPoints.Length > 4 ? quadPoints[4] : 0,
            y3 = quadPoints.Length > 5 ? quadPoints[5] : 0,
            x4 = quadPoints.Length > 6 ? quadPoints[6] : 0,
            y4 = quadPoints.Length > 7 ? quadPoints[7] : 0
        });
    }

    /// <summary>
    /// Saves a PDF document to a file path.
    /// </summary>
    /// <param name="document">Handle to the PDF document.</param>
    /// <param name="filePath">Path where the PDF should be saved.</param>
    /// <param name="flags">Save flags (0 for default).</param>
    /// <returns>True if the save was successful; otherwise, false.</returns>
    public static bool SaveDocument(SafePdfDocumentHandle document, string filePath, int flags = 0)
    {
        if (document == null || document.IsInvalid)
            return false;

        // PDFium's FPDF_SaveAsCopy requires an FPDF_FILEWRITE struct with a callback.
        // We implement it by opening a FileStream and writing via the callback.
        FileStream? fs = null;
        try
        {
            fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            var capturedFs = fs;

            WriteBlockDelegate writeBlock = (pThis, pData, size) =>
            {
                try
                {
                    var buffer = new byte[(int)size];
                    System.Runtime.InteropServices.Marshal.Copy(pData, buffer, 0, (int)size);
                    capturedFs.Write(buffer, 0, (int)size);
                    return 1;
                }
                catch
                {
                    return 0;
                }
            };

            // FPDF_FILEWRITE struct: first field is version (int), second is WriteBlock function pointer
            var fileWrite = new FPDF_FILEWRITE
            {
                version = 1,
                WriteBlock = System.Runtime.InteropServices.Marshal.GetFunctionPointerForDelegate(writeBlock)
            };

            // Pin the struct and call FPDF_SaveAsCopy
            var handle = GCHandle.Alloc(fileWrite, GCHandleType.Pinned);
            try
            {
                var result = FPDF_SaveAsCopy(document, handle.AddrOfPinnedObject(), flags);
                // Keep delegate alive until after the call
                GC.KeepAlive(writeBlock);
                return result;
            }
            finally
            {
                handle.Free();
            }
        }
        catch
        {
            return false;
        }
        finally
        {
            fs?.Dispose();
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int WriteBlockDelegate(IntPtr pThis, IntPtr pData, uint size);

    [StructLayout(LayoutKind.Sequential)]
    private struct FPDF_FILEWRITE
    {
        public int version;
        public IntPtr WriteBlock;
    }

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFPage_GetAnnotCount(SafePdfPageHandle page);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern SafeAnnotationHandle FPDFPage_GetAnnot(SafePdfPageHandle page, int index);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern SafeAnnotationHandle FPDFPage_CreateAnnot(SafePdfPageHandle page, int subtype);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFPage_RemoveAnnot(SafePdfPageHandle page, int index);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void FPDFPage_CloseAnnot(IntPtr annotation);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFAnnot_GetSubtype(SafeAnnotationHandle annotation);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFAnnot_SetColor(
        SafeAnnotationHandle annotation,
        int color_type,
        uint R,
        uint G,
        uint B,
        uint A);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFAnnot_GetColor(
        SafeAnnotationHandle annotation,
        int color_type,
        out uint R,
        out uint G,
        out uint B,
        out uint A);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFAnnot_SetRect(SafeAnnotationHandle annotation, ref FS_RECTF rect);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFAnnot_GetRect(SafeAnnotationHandle annotation, ref FS_RECTF rect);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFAnnot_SetStringValue(
        SafeAnnotationHandle annotation,
        [MarshalAs(UnmanagedType.LPStr)] string key,
        [MarshalAs(UnmanagedType.LPArray)] byte[] value);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern uint FPDFAnnot_GetStringValue(
        SafeAnnotationHandle annotation,
        [MarshalAs(UnmanagedType.LPStr)] string key,
        byte[]? buffer,
        uint buflen);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDFAnnot_SetAttachmentPoints(
        SafeAnnotationHandle annotation,
        int quad_index,
        FS_QUADPOINTSF quad_points);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FPDF_SaveAsCopy(
        SafePdfDocumentHandle document,
        IntPtr pFileWrite,
        int flags);

    #endregion

    #region Annotation Types and Structures

    /// <summary>
    /// PDFium annotation types.
    /// </summary>
    public enum AnnotationType
    {
        Unknown = 0,
        Text = 1,
        Link = 2,
        FreeText = 3,
        Line = 4,
        Square = 5,
        Circle = 6,
        Polygon = 7,
        PolyLine = 8,
        Highlight = 9,
        Underline = 10,
        Squiggly = 11,
        StrikeOut = 12,
        Stamp = 13,
        Caret = 14,
        Ink = 15,
        Popup = 16,
        FileAttachment = 17,
        Sound = 18,
        Movie = 19,
        Widget = 20,
        Screen = 21,
        PrinterMark = 22,
        TrapNet = 23,
        Watermark = 24,
        ThreeD = 25,
        Redact = 26
    }

    /// <summary>
    /// Annotation color type.
    /// </summary>
    public enum AnnotationColorType
    {
        /// <summary>
        /// Fill color for the annotation.
        /// </summary>
        Fill = 0,

        /// <summary>
        /// Stroke (border) color for the annotation.
        /// </summary>
        Stroke = 1
    }

    /// <summary>
    /// Rectangle structure for PDFium (uses floats).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct FS_RECTF
    {
        public float left;
        public float bottom;
        public float right;
        public float top;
    }

    /// <summary>
    /// Quad points structure for text markup annotations.
    /// Represents four points defining a quadrilateral.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    private struct FS_QUADPOINTSF
    {
        public float x1;
        public float y1;
        public float x2;
        public float y2;
        public float x3;
        public float y3;
        public float x4;
        public float y4;
    }

    #endregion
}
