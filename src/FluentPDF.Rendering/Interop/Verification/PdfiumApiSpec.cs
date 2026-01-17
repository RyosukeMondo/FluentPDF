using System.Runtime.InteropServices;

namespace FluentPDF.Rendering.Interop.Verification;

/// <summary>
/// PDFium API specification data for marshalling verification.
/// Provides ground truth for all P/Invoke function signatures based on official PDFium headers.
/// </summary>
/// <remarks>
/// Specifications are derived from PDFium headers available at:
/// https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/
/// Header files referenced: fpdfview.h, fpdf_doc.h, fpdf_text.h, fpdf_edit.h, fpdf_annot.h
/// </remarks>
public static class PdfiumApiSpec
{
    /// <summary>
    /// Gets all PDFium API function specifications.
    /// </summary>
    public static Dictionary<string, FunctionSpec> GetAllSpecs()
    {
        return new Dictionary<string, FunctionSpec>
        {
            // Library Initialization (fpdfview.h)
            ["FPDF_InitLibrary"] = new FunctionSpec
            {
                FunctionName = "FPDF_InitLibrary",
                ReturnType = typeof(void),
                Parameters = Array.Empty<ParameterSpec>(),
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdfview.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdfview.h",
                Description = "Initialize the FPDF library. Must be called once before using any PDFium functions."
            },

            ["FPDF_DestroyLibrary"] = new FunctionSpec
            {
                FunctionName = "FPDF_DestroyLibrary",
                ReturnType = typeof(void),
                Parameters = Array.Empty<ParameterSpec>(),
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdfview.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdfview.h",
                Description = "Destroy the FPDF library. Should be called when the application exits."
            },

            // Document Functions (fpdfview.h)
            ["FPDF_LoadDocument"] = new FunctionSpec
            {
                FunctionName = "FPDF_LoadDocument",
                ReturnType = typeof(IntPtr), // FPDF_DOCUMENT
                Parameters = new[]
                {
                    new ParameterSpec { Name = "file_path", Type = typeof(string), MarshalAs = UnmanagedType.LPStr },
                    new ParameterSpec { Name = "password", Type = typeof(string), MarshalAs = UnmanagedType.LPStr, IsNullable = true }
                },
                CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Ansi,
                HeaderFile = "fpdfview.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdfview.h",
                Description = "Load a PDF document from a file path."
            },

            ["FPDF_CloseDocument"] = new FunctionSpec
            {
                FunctionName = "FPDF_CloseDocument",
                ReturnType = typeof(void),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "document", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdfview.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdfview.h",
                Description = "Close a PDF document and free all associated resources."
            },

            ["FPDF_GetPageCount"] = new FunctionSpec
            {
                FunctionName = "FPDF_GetPageCount",
                ReturnType = typeof(int),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "document", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdfview.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdfview.h",
                Description = "Get the number of pages in a document."
            },

            // Page Functions (fpdfview.h)
            ["FPDF_LoadPage"] = new FunctionSpec
            {
                FunctionName = "FPDF_LoadPage",
                ReturnType = typeof(IntPtr), // FPDF_PAGE
                Parameters = new[]
                {
                    new ParameterSpec { Name = "document", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "page_index", Type = typeof(int) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdfview.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdfview.h",
                Description = "Load a page from a document by zero-based index."
            },

            ["FPDF_ClosePage"] = new FunctionSpec
            {
                FunctionName = "FPDF_ClosePage",
                ReturnType = typeof(void),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdfview.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdfview.h",
                Description = "Close a page and free all associated resources."
            },

            // Page Dimension Functions (fpdfview.h)
            // CRITICAL: Use FPDF_GetPageWidth/Height (returns double) NOT FPDF_GetPageWidthF/HeightF (returns float)
            // The 'F' variants return garbage values with some PDFium versions
            ["FPDF_GetPageWidth"] = new FunctionSpec
            {
                FunctionName = "FPDF_GetPageWidth",
                ReturnType = typeof(double), // Returns double, NOT float
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdfview.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdfview.h",
                Description = "Get the width of a page in points (1/72 inch). Returns double precision value.",
                Notes = "Use this instead of FPDF_GetPageWidthF. The float variant has known marshalling issues."
            },

            ["FPDF_GetPageHeight"] = new FunctionSpec
            {
                FunctionName = "FPDF_GetPageHeight",
                ReturnType = typeof(double), // Returns double, NOT float
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdfview.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdfview.h",
                Description = "Get the height of a page in points (1/72 inch). Returns double precision value.",
                Notes = "Use this instead of FPDF_GetPageHeightF. The float variant has known marshalling issues."
            },

            // Bitmap Functions (fpdfview.h)
            ["FPDFBitmap_Create"] = new FunctionSpec
            {
                FunctionName = "FPDFBitmap_Create",
                ReturnType = typeof(IntPtr), // FPDF_BITMAP
                Parameters = new[]
                {
                    new ParameterSpec { Name = "width", Type = typeof(int) },
                    new ParameterSpec { Name = "height", Type = typeof(int) },
                    new ParameterSpec { Name = "alpha", Type = typeof(int) } // 0 = no alpha, 1 = with alpha
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdfview.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdfview.h",
                Description = "Create a bitmap for rendering. Alpha parameter: 0=no alpha, 1=with alpha."
            },

            ["FPDFBitmap_Destroy"] = new FunctionSpec
            {
                FunctionName = "FPDFBitmap_Destroy",
                ReturnType = typeof(void),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "bitmap", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdfview.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdfview.h",
                Description = "Destroy a bitmap and free its memory."
            },

            ["FPDFBitmap_GetBuffer"] = new FunctionSpec
            {
                FunctionName = "FPDFBitmap_GetBuffer",
                ReturnType = typeof(IntPtr), // void*
                Parameters = new[]
                {
                    new ParameterSpec { Name = "bitmap", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdfview.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdfview.h",
                Description = "Get the pointer to the bitmap buffer."
            },

            ["FPDFBitmap_GetStride"] = new FunctionSpec
            {
                FunctionName = "FPDFBitmap_GetStride",
                ReturnType = typeof(int),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "bitmap", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdfview.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdfview.h",
                Description = "Get the stride (bytes per row) of a bitmap."
            },

            ["FPDFBitmap_FillRect"] = new FunctionSpec
            {
                FunctionName = "FPDFBitmap_FillRect",
                ReturnType = typeof(void),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "bitmap", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "left", Type = typeof(int) },
                    new ParameterSpec { Name = "top", Type = typeof(int) },
                    new ParameterSpec { Name = "width", Type = typeof(int) },
                    new ParameterSpec { Name = "height", Type = typeof(int) },
                    new ParameterSpec { Name = "color", Type = typeof(uint) } // ARGB color
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdfview.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdfview.h",
                Description = "Fill a rectangle in a bitmap with a color (ARGB format)."
            },

            // Rendering Functions (fpdfview.h)
            ["FPDF_RenderPageBitmap"] = new FunctionSpec
            {
                FunctionName = "FPDF_RenderPageBitmap",
                ReturnType = typeof(void),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "bitmap", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "page", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "start_x", Type = typeof(int) },
                    new ParameterSpec { Name = "start_y", Type = typeof(int) },
                    new ParameterSpec { Name = "size_x", Type = typeof(int) },
                    new ParameterSpec { Name = "size_y", Type = typeof(int) },
                    new ParameterSpec { Name = "rotate", Type = typeof(int) }, // 0=0°, 1=90°, 2=180°, 3=270°
                    new ParameterSpec { Name = "flags", Type = typeof(int) } // Rendering flags
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdfview.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdfview.h",
                Description = "Render a page to a bitmap."
            },

            // Error Functions (fpdfview.h)
            ["FPDF_GetLastError"] = new FunctionSpec
            {
                FunctionName = "FPDF_GetLastError",
                ReturnType = typeof(uint),
                Parameters = Array.Empty<ParameterSpec>(),
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdfview.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdfview.h",
                Description = "Get the last error code from PDFium."
            },

            // Bookmark Functions (fpdf_doc.h)
            ["FPDFBookmark_GetFirstChild"] = new FunctionSpec
            {
                FunctionName = "FPDFBookmark_GetFirstChild",
                ReturnType = typeof(IntPtr), // FPDF_BOOKMARK
                Parameters = new[]
                {
                    new ParameterSpec { Name = "document", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "bookmark", Type = typeof(IntPtr) } // NULL for root
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_doc.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_doc.h",
                Description = "Get the first child bookmark. Pass NULL for bookmark to get first root bookmark."
            },

            ["FPDFBookmark_GetNextSibling"] = new FunctionSpec
            {
                FunctionName = "FPDFBookmark_GetNextSibling",
                ReturnType = typeof(IntPtr), // FPDF_BOOKMARK
                Parameters = new[]
                {
                    new ParameterSpec { Name = "document", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "bookmark", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_doc.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_doc.h",
                Description = "Get the next sibling bookmark."
            },

            ["FPDFBookmark_GetTitle"] = new FunctionSpec
            {
                FunctionName = "FPDFBookmark_GetTitle",
                ReturnType = typeof(uint), // Returns buffer size needed
                Parameters = new[]
                {
                    new ParameterSpec { Name = "bookmark", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "buffer", Type = typeof(byte[]), IsNullable = true },
                    new ParameterSpec { Name = "buflen", Type = typeof(uint) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_doc.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_doc.h",
                Description = "Get bookmark title as UTF-16LE. Pass NULL buffer to get required buffer size."
            },

            ["FPDFBookmark_GetDest"] = new FunctionSpec
            {
                FunctionName = "FPDFBookmark_GetDest",
                ReturnType = typeof(IntPtr), // FPDF_DEST
                Parameters = new[]
                {
                    new ParameterSpec { Name = "document", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "bookmark", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_doc.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_doc.h",
                Description = "Get the destination of a bookmark."
            },

            ["FPDFDest_GetDestPageIndex"] = new FunctionSpec
            {
                FunctionName = "FPDFDest_GetDestPageIndex",
                ReturnType = typeof(uint), // unsigned long (page index)
                Parameters = new[]
                {
                    new ParameterSpec { Name = "document", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "dest", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_doc.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_doc.h",
                Description = "Get the page index of a destination."
            },

            ["FPDFDest_GetLocationInPage"] = new FunctionSpec
            {
                FunctionName = "FPDFDest_GetLocationInPage",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "dest", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "hasX", Type = typeof(int), IsByRef = true }, // FPDF_BOOL* (out int)
                    new ParameterSpec { Name = "hasY", Type = typeof(int), IsByRef = true }, // FPDF_BOOL* (out int)
                    new ParameterSpec { Name = "hasZoom", Type = typeof(int), IsByRef = true }, // FPDF_BOOL* (out int)
                    new ParameterSpec { Name = "x", Type = typeof(float), IsByRef = true }, // FS_FLOAT* (out float)
                    new ParameterSpec { Name = "y", Type = typeof(float), IsByRef = true }, // FS_FLOAT* (out float)
                    new ParameterSpec { Name = "zoom", Type = typeof(float), IsByRef = true } // FS_FLOAT* (out float)
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_doc.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_doc.h",
                Description = "Get the location coordinates within a page for a destination.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            // Text Extraction Functions (fpdf_text.h)
            ["FPDFText_LoadPage"] = new FunctionSpec
            {
                FunctionName = "FPDFText_LoadPage",
                ReturnType = typeof(IntPtr), // FPDF_TEXTPAGE
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_text.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_text.h",
                Description = "Load text page information from a PDF page."
            },

            ["FPDFText_ClosePage"] = new FunctionSpec
            {
                FunctionName = "FPDFText_ClosePage",
                ReturnType = typeof(void),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "text_page", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_text.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_text.h",
                Description = "Close a text page and free resources."
            },

            ["FPDFText_CountChars"] = new FunctionSpec
            {
                FunctionName = "FPDFText_CountChars",
                ReturnType = typeof(int),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "text_page", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_text.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_text.h",
                Description = "Get the number of characters in a text page."
            },

            ["FPDFText_GetText"] = new FunctionSpec
            {
                FunctionName = "FPDFText_GetText",
                ReturnType = typeof(int), // Number of characters copied
                Parameters = new[]
                {
                    new ParameterSpec { Name = "text_page", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "start_index", Type = typeof(int) },
                    new ParameterSpec { Name = "count", Type = typeof(int) },
                    new ParameterSpec { Name = "result", Type = typeof(byte[]), MarshalAs = UnmanagedType.LPArray } // UTF-16LE buffer
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_text.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_text.h",
                Description = "Extract text from a text page as UTF-16LE."
            },

            ["FPDFText_GetCharBox"] = new FunctionSpec
            {
                FunctionName = "FPDFText_GetCharBox",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "text_page", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "index", Type = typeof(int) },
                    new ParameterSpec { Name = "left", Type = typeof(double), IsByRef = true },
                    new ParameterSpec { Name = "top", Type = typeof(double), IsByRef = true },
                    new ParameterSpec { Name = "right", Type = typeof(double), IsByRef = true },
                    new ParameterSpec { Name = "bottom", Type = typeof(double), IsByRef = true }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_text.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_text.h",
                Description = "Get the bounding box of a character.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            // Text Search Functions (fpdf_text.h)
            ["FPDFText_FindStart"] = new FunctionSpec
            {
                FunctionName = "FPDFText_FindStart",
                ReturnType = typeof(IntPtr), // FPDF_SCHHANDLE
                Parameters = new[]
                {
                    new ParameterSpec { Name = "text_page", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "findwhat", Type = typeof(byte[]), MarshalAs = UnmanagedType.LPArray }, // UTF-16LE
                    new ParameterSpec { Name = "flags", Type = typeof(uint) },
                    new ParameterSpec { Name = "start_index", Type = typeof(int) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_text.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_text.h",
                Description = "Start a text search on a text page."
            },

            ["FPDFText_FindNext"] = new FunctionSpec
            {
                FunctionName = "FPDFText_FindNext",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "handle", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_text.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_text.h",
                Description = "Find the next search match.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFText_FindPrev"] = new FunctionSpec
            {
                FunctionName = "FPDFText_FindPrev",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "handle", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_text.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_text.h",
                Description = "Find the previous search match.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFText_GetSchResultIndex"] = new FunctionSpec
            {
                FunctionName = "FPDFText_GetSchResultIndex",
                ReturnType = typeof(int),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "handle", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_text.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_text.h",
                Description = "Get the character index of the current search match."
            },

            ["FPDFText_GetSchCount"] = new FunctionSpec
            {
                FunctionName = "FPDFText_GetSchCount",
                ReturnType = typeof(int),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "handle", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_text.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_text.h",
                Description = "Get the number of characters in the current search match."
            },

            ["FPDFText_FindClose"] = new FunctionSpec
            {
                FunctionName = "FPDFText_FindClose",
                ReturnType = typeof(void),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "handle", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_text.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_text.h",
                Description = "Close a search context and release resources."
            },

            // Annotation Functions (fpdf_annot.h)
            ["FPDFPage_GetAnnotCount"] = new FunctionSpec
            {
                FunctionName = "FPDFPage_GetAnnotCount",
                ReturnType = typeof(int),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_annot.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_annot.h",
                Description = "Get the number of annotations on a page."
            },

            ["FPDFPage_GetAnnot"] = new FunctionSpec
            {
                FunctionName = "FPDFPage_GetAnnot",
                ReturnType = typeof(IntPtr), // FPDF_ANNOTATION
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "index", Type = typeof(int) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_annot.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_annot.h",
                Description = "Get an annotation from a page by index."
            },

            ["FPDFPage_CreateAnnot"] = new FunctionSpec
            {
                FunctionName = "FPDFPage_CreateAnnot",
                ReturnType = typeof(IntPtr), // FPDF_ANNOTATION
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "subtype", Type = typeof(int) } // FPDF_ANNOTATION_SUBTYPE
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_annot.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_annot.h",
                Description = "Create a new annotation on a page."
            },

            ["FPDFPage_RemoveAnnot"] = new FunctionSpec
            {
                FunctionName = "FPDFPage_RemoveAnnot",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "index", Type = typeof(int) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_annot.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_annot.h",
                Description = "Remove an annotation from a page.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFPage_CloseAnnot"] = new FunctionSpec
            {
                FunctionName = "FPDFPage_CloseAnnot",
                ReturnType = typeof(void),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "annotation", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_annot.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_annot.h",
                Description = "Close an annotation and free resources."
            },

            ["FPDFAnnot_GetSubtype"] = new FunctionSpec
            {
                FunctionName = "FPDFAnnot_GetSubtype",
                ReturnType = typeof(int), // FPDF_ANNOTATION_SUBTYPE
                Parameters = new[]
                {
                    new ParameterSpec { Name = "annotation", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_annot.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_annot.h",
                Description = "Get the subtype of an annotation."
            },

            ["FPDFAnnot_SetColor"] = new FunctionSpec
            {
                FunctionName = "FPDFAnnot_SetColor",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "annotation", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "color_type", Type = typeof(int) }, // FPDFANNOT_COLORTYPE
                    new ParameterSpec { Name = "R", Type = typeof(uint) },
                    new ParameterSpec { Name = "G", Type = typeof(uint) },
                    new ParameterSpec { Name = "B", Type = typeof(uint) },
                    new ParameterSpec { Name = "A", Type = typeof(uint) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_annot.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_annot.h",
                Description = "Set the color of an annotation.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFAnnot_GetColor"] = new FunctionSpec
            {
                FunctionName = "FPDFAnnot_GetColor",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "annotation", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "color_type", Type = typeof(int) },
                    new ParameterSpec { Name = "R", Type = typeof(uint), IsByRef = true },
                    new ParameterSpec { Name = "G", Type = typeof(uint), IsByRef = true },
                    new ParameterSpec { Name = "B", Type = typeof(uint), IsByRef = true },
                    new ParameterSpec { Name = "A", Type = typeof(uint), IsByRef = true }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_annot.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_annot.h",
                Description = "Get the color of an annotation.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFAnnot_SetRect"] = new FunctionSpec
            {
                FunctionName = "FPDFAnnot_SetRect",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "annotation", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "rect", Type = typeof(object), IsByRef = true } // const FS_RECTF* (struct by ref)
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_annot.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_annot.h",
                Description = "Set the rectangle bounds of an annotation.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFAnnot_GetRect"] = new FunctionSpec
            {
                FunctionName = "FPDFAnnot_GetRect",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "annotation", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "rect", Type = typeof(object), IsByRef = true } // FS_RECTF* (struct by ref)
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_annot.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_annot.h",
                Description = "Get the rectangle bounds of an annotation.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFAnnot_SetStringValue"] = new FunctionSpec
            {
                FunctionName = "FPDFAnnot_SetStringValue",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "annotation", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "key", Type = typeof(string), MarshalAs = UnmanagedType.LPStr },
                    new ParameterSpec { Name = "value", Type = typeof(byte[]), MarshalAs = UnmanagedType.LPArray } // UTF-16LE
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_annot.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_annot.h",
                Description = "Set a string value for an annotation property.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFAnnot_GetStringValue"] = new FunctionSpec
            {
                FunctionName = "FPDFAnnot_GetStringValue",
                ReturnType = typeof(uint), // Buffer size needed
                Parameters = new[]
                {
                    new ParameterSpec { Name = "annotation", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "key", Type = typeof(string), MarshalAs = UnmanagedType.LPStr },
                    new ParameterSpec { Name = "buffer", Type = typeof(byte[]), IsNullable = true },
                    new ParameterSpec { Name = "buflen", Type = typeof(uint) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_annot.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_annot.h",
                Description = "Get a string value from an annotation property as UTF-16LE."
            },

            ["FPDFAnnot_SetAttachmentPoints"] = new FunctionSpec
            {
                FunctionName = "FPDFAnnot_SetAttachmentPoints",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "annotation", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "quad_index", Type = typeof(int) },
                    new ParameterSpec { Name = "quad_points", Type = typeof(object) } // const FS_QUADPOINTSF (struct)
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_annot.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_annot.h",
                Description = "Set quad points for text markup annotations.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            // Document Save Functions (fpdf_save.h)
            ["FPDF_SaveAsCopy"] = new FunctionSpec
            {
                FunctionName = "FPDF_SaveAsCopy",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "document", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "file_path", Type = typeof(string), MarshalAs = UnmanagedType.LPStr },
                    new ParameterSpec { Name = "flags", Type = typeof(int) }
                },
                CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Ansi,
                HeaderFile = "fpdf_save.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_save.h",
                Description = "Save a PDF document to a file path.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            // Page Object Functions (fpdf_edit.h)
            ["FPDFPageObj_NewImageObj"] = new FunctionSpec
            {
                FunctionName = "FPDFPageObj_NewImageObj",
                ReturnType = typeof(IntPtr), // FPDF_PAGEOBJECT
                Parameters = new[]
                {
                    new ParameterSpec { Name = "document", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Create a new image object."
            },

            ["FPDFImageObj_LoadJpegFile"] = new FunctionSpec
            {
                FunctionName = "FPDFImageObj_LoadJpegFile",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "pages", Type = typeof(IntPtr[]) },
                    new ParameterSpec { Name = "nCount", Type = typeof(int) },
                    new ParameterSpec { Name = "image_object", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "file_path", Type = typeof(string), MarshalAs = UnmanagedType.LPStr }
                },
                CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Ansi,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Load a JPEG image from a file into an image object.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFImageObj_LoadJpegFileInline"] = new FunctionSpec
            {
                FunctionName = "FPDFImageObj_LoadJpegFileInline",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "pages", Type = typeof(IntPtr[]) },
                    new ParameterSpec { Name = "nCount", Type = typeof(int) },
                    new ParameterSpec { Name = "image_object", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "file_path", Type = typeof(string), MarshalAs = UnmanagedType.LPStr }
                },
                CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Ansi,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Load a JPEG image inline into an image object.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFImageObj_SetBitmap"] = new FunctionSpec
            {
                FunctionName = "FPDFImageObj_SetBitmap",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "pages", Type = typeof(IntPtr[]) },
                    new ParameterSpec { Name = "nCount", Type = typeof(int) },
                    new ParameterSpec { Name = "image_object", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "bitmap", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Set a bitmap as the image content for an image object.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFPageObj_Transform"] = new FunctionSpec
            {
                FunctionName = "FPDFPageObj_Transform",
                ReturnType = typeof(void),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page_object", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "a", Type = typeof(double) },
                    new ParameterSpec { Name = "b", Type = typeof(double) },
                    new ParameterSpec { Name = "c", Type = typeof(double) },
                    new ParameterSpec { Name = "d", Type = typeof(double) },
                    new ParameterSpec { Name = "e", Type = typeof(double) },
                    new ParameterSpec { Name = "f", Type = typeof(double) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Transform a page object with a transformation matrix."
            },

            ["FPDFPageObj_SetMatrix"] = new FunctionSpec
            {
                FunctionName = "FPDFPageObj_SetMatrix",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page_object", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "a", Type = typeof(double) },
                    new ParameterSpec { Name = "b", Type = typeof(double) },
                    new ParameterSpec { Name = "c", Type = typeof(double) },
                    new ParameterSpec { Name = "d", Type = typeof(double) },
                    new ParameterSpec { Name = "e", Type = typeof(double) },
                    new ParameterSpec { Name = "f", Type = typeof(double) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Set the transformation matrix for a page object.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFPage_InsertObject"] = new FunctionSpec
            {
                FunctionName = "FPDFPage_InsertObject",
                ReturnType = typeof(void),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "page_obj", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Insert a page object into a page."
            },

            ["FPDFPage_RemoveObject"] = new FunctionSpec
            {
                FunctionName = "FPDFPage_RemoveObject",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "page_obj", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Remove a page object from a page.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFPageObj_GetBounds"] = new FunctionSpec
            {
                FunctionName = "FPDFPageObj_GetBounds",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page_object", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "left", Type = typeof(float), IsByRef = true },
                    new ParameterSpec { Name = "bottom", Type = typeof(float), IsByRef = true },
                    new ParameterSpec { Name = "right", Type = typeof(float), IsByRef = true },
                    new ParameterSpec { Name = "top", Type = typeof(float), IsByRef = true }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Get the bounding box of a page object.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFPageObj_Destroy"] = new FunctionSpec
            {
                FunctionName = "FPDFPageObj_Destroy",
                ReturnType = typeof(void),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page_object", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Destroy a page object and release resources."
            },

            // Text Object Functions (fpdf_edit.h)
            ["FPDFPageObj_CreateTextObj"] = new FunctionSpec
            {
                FunctionName = "FPDFPageObj_CreateTextObj",
                ReturnType = typeof(IntPtr), // FPDF_PAGEOBJECT
                Parameters = new[]
                {
                    new ParameterSpec { Name = "document", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "font", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "font_size", Type = typeof(float) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Create a new text object."
            },

            ["FPDFText_LoadStandardFont"] = new FunctionSpec
            {
                FunctionName = "FPDFText_LoadStandardFont",
                ReturnType = typeof(IntPtr), // FPDF_FONT
                Parameters = new[]
                {
                    new ParameterSpec { Name = "document", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "font", Type = typeof(string), MarshalAs = UnmanagedType.LPStr }
                },
                CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Ansi,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Load a standard font for use in text objects."
            },

            ["FPDFText_SetText"] = new FunctionSpec
            {
                FunctionName = "FPDFText_SetText",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "text_object", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "text", Type = typeof(string), MarshalAs = UnmanagedType.LPWStr } // UTF-16
                },
                CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Unicode,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Set the text content for a text object.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFPageObj_SetFillColor"] = new FunctionSpec
            {
                FunctionName = "FPDFPageObj_SetFillColor",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page_object", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "R", Type = typeof(uint) },
                    new ParameterSpec { Name = "G", Type = typeof(uint) },
                    new ParameterSpec { Name = "B", Type = typeof(uint) },
                    new ParameterSpec { Name = "A", Type = typeof(uint) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Set the fill color for a page object.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFPageObj_SetStrokeColor"] = new FunctionSpec
            {
                FunctionName = "FPDFPageObj_SetStrokeColor",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page_object", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "R", Type = typeof(uint) },
                    new ParameterSpec { Name = "G", Type = typeof(uint) },
                    new ParameterSpec { Name = "B", Type = typeof(uint) },
                    new ParameterSpec { Name = "A", Type = typeof(uint) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Set the stroke color for a page object.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFPage_GenerateContent"] = new FunctionSpec
            {
                FunctionName = "FPDFPage_GenerateContent",
                ReturnType = typeof(bool), // FPDF_BOOL
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Generate content stream for page after modifications.",
                ReturnMarshalAs = UnmanagedType.Bool
            },

            ["FPDFPage_CountObjects"] = new FunctionSpec
            {
                FunctionName = "FPDFPage_CountObjects",
                ReturnType = typeof(int),
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page", Type = typeof(IntPtr) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Get the number of page objects on a page."
            },

            ["FPDFPage_GetObject"] = new FunctionSpec
            {
                FunctionName = "FPDFPage_GetObject",
                ReturnType = typeof(IntPtr), // FPDF_PAGEOBJECT
                Parameters = new[]
                {
                    new ParameterSpec { Name = "page", Type = typeof(IntPtr) },
                    new ParameterSpec { Name = "index", Type = typeof(int) }
                },
                CallingConvention = CallingConvention.Cdecl,
                HeaderFile = "fpdf_edit.h",
                DocumentationUrl = "https://pdfium.googlesource.com/pdfium/+/refs/heads/main/public/fpdf_edit.h",
                Description = "Get a page object by index."
            }
        };
    }
}

/// <summary>
/// Specification for a PDFium API function.
/// </summary>
public class FunctionSpec
{
    /// <summary>
    /// The function name as it appears in the PDFium API.
    /// </summary>
    public required string FunctionName { get; init; }

    /// <summary>
    /// The return type of the function.
    /// </summary>
    public required Type ReturnType { get; init; }

    /// <summary>
    /// The parameters of the function.
    /// </summary>
    public required ParameterSpec[] Parameters { get; init; }

    /// <summary>
    /// The calling convention (typically Cdecl for PDFium).
    /// </summary>
    public CallingConvention CallingConvention { get; init; } = CallingConvention.Cdecl;

    /// <summary>
    /// The character set for string parameters (if applicable).
    /// </summary>
    public CharSet? CharSet { get; init; }

    /// <summary>
    /// The PDFium header file where this function is declared.
    /// </summary>
    public required string HeaderFile { get; init; }

    /// <summary>
    /// URL to the PDFium documentation or header file.
    /// </summary>
    public required string DocumentationUrl { get; init; }

    /// <summary>
    /// Description of what the function does.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Additional notes about the function (e.g., known issues, alternatives).
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// MarshalAs attribute for the return type (if applicable).
    /// </summary>
    public UnmanagedType? ReturnMarshalAs { get; init; }
}

/// <summary>
/// Specification for a function parameter.
/// </summary>
public class ParameterSpec
{
    /// <summary>
    /// The parameter name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The managed type of the parameter.
    /// </summary>
    public required Type Type { get; init; }

    /// <summary>
    /// Whether the parameter is passed by reference (out/ref).
    /// </summary>
    public bool IsByRef { get; init; }

    /// <summary>
    /// Whether the parameter can be null.
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// MarshalAs attribute for the parameter (if applicable).
    /// </summary>
    public UnmanagedType? MarshalAs { get; init; }
}
