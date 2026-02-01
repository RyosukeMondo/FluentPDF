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
    /// Initializes a new instance of the <see cref="SafePdfDocumentHandle"/> class with a specific handle.
    /// Used primarily for testing purposes.
    /// </summary>
    /// <param name="handle">The handle pointer.</param>
    /// <param name="ownsHandle">True if the handle should be released when disposed.</param>
    public SafePdfDocumentHandle(IntPtr handle, bool ownsHandle) : base(ownsHandle)
    {
        SetHandle(handle);
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
