using Microsoft.Win32.SafeHandles;

namespace FluentPDF.Rendering.Interop;

/// <summary>
/// Safe handle for PDFium annotation pointers (FPDF_ANNOTATION).
/// Automatically releases the annotation when disposed.
/// </summary>
public sealed class SafeAnnotationHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SafeAnnotationHandle"/> class.
    /// </summary>
    public SafeAnnotationHandle() : base(ownsHandle: true)
    {
    }

    /// <summary>
    /// Releases the PDFium annotation handle by calling FPDFPage_CloseAnnot.
    /// </summary>
    /// <returns>True if the handle was released successfully; otherwise, false.</returns>
    protected override bool ReleaseHandle()
    {
        if (IsInvalid)
        {
            return true;
        }

        PdfiumInterop.FPDFPage_CloseAnnot(handle);
        return true;
    }
}
