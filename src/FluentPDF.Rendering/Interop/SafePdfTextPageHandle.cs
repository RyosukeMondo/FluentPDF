using Microsoft.Win32.SafeHandles;

namespace FluentPDF.Rendering.Interop;

/// <summary>
/// Safe handle for PDFium text page pointers (FPDF_TEXTPAGE).
/// Automatically releases the text page when disposed.
/// </summary>
public sealed class SafePdfTextPageHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SafePdfTextPageHandle"/> class.
    /// </summary>
    public SafePdfTextPageHandle() : base(ownsHandle: true)
    {
    }

    /// <summary>
    /// Releases the PDFium text page handle by calling FPDFText_ClosePage.
    /// </summary>
    /// <returns>True if the handle was released successfully; otherwise, false.</returns>
    protected override bool ReleaseHandle()
    {
        if (IsInvalid)
        {
            return true;
        }

        PdfiumInterop.FPDFText_ClosePage(handle);
        return true;
    }
}
