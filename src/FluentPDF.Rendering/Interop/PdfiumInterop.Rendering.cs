using System.Runtime.InteropServices;

namespace FluentPDF.Rendering.Interop;

public static partial class PdfiumInterop
{
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
}
