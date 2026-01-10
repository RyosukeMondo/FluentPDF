using Microsoft.Win32.SafeHandles;

namespace FluentPDF.Rendering.Interop;

/// <summary>
/// Safe handle for PDFium document pointers (FPDF_DOCUMENT).
/// Automatically releases the document when disposed.
/// </summary>
public sealed class SafePdfDocumentHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SafePdfDocumentHandle"/> class.
    /// </summary>
    public SafePdfDocumentHandle() : base(ownsHandle: true)
    {
    }

    /// <summary>
    /// Releases the PDFium document handle by calling FPDF_CloseDocument.
    /// </summary>
    /// <returns>True if the handle was released successfully; otherwise, false.</returns>
    protected override bool ReleaseHandle()
    {
        if (IsInvalid)
        {
            return true;
        }

        PdfiumInterop.FPDF_CloseDocument(handle);
        return true;
    }
}
