using Microsoft.Win32.SafeHandles;

namespace FluentPDF.Rendering.Interop;

/// <summary>
/// Safe handle for PDFium form fill environment pointers (FPDF_FORMHANDLE).
/// Automatically releases the form environment when disposed.
/// </summary>
public sealed class SafePdfFormHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SafePdfFormHandle"/> class.
    /// </summary>
    public SafePdfFormHandle() : base(ownsHandle: true)
    {
    }

    /// <summary>
    /// Releases the PDFium form handle by calling FPDFDOC_ExitFormFillEnvironment.
    /// </summary>
    /// <returns>True if the handle was released successfully; otherwise, false.</returns>
    protected override bool ReleaseHandle()
    {
        if (IsInvalid)
        {
            return true;
        }

        PdfiumFormInterop.ExitFormFillEnvironment(handle);
        return true;
    }
}
