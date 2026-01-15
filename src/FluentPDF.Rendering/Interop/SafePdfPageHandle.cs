using Microsoft.Win32.SafeHandles;

namespace FluentPDF.Rendering.Interop;

/// <summary>
/// Safe handle for PDFium page pointers (FPDF_PAGE).
/// Automatically releases the page when disposed.
/// </summary>
public sealed class SafePdfPageHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SafePdfPageHandle"/> class.
    /// </summary>
    public SafePdfPageHandle() : base(ownsHandle: true)
    {
    }

    /// <summary>
    /// Releases the PDFium page handle by calling FPDF_ClosePage.
    /// </summary>
    /// <returns>True if the handle was released successfully; otherwise, false.</returns>
    protected override bool ReleaseHandle()
    {
        if (IsInvalid)
        {
            return true;
        }

        PdfiumInterop.FPDF_ClosePage(handle);
        return true;
    }
}
