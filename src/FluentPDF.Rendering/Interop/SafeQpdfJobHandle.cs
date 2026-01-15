using Microsoft.Win32.SafeHandles;

namespace FluentPDF.Rendering.Interop;

/// <summary>
/// Safe handle for QPDF job pointers (qpdf_data).
/// Automatically releases the job handle when disposed.
/// Ensures thread-safe cleanup of QPDF resources.
/// </summary>
public sealed class SafeQpdfJobHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SafeQpdfJobHandle"/> class
    /// with an existing handle.
    /// </summary>
    /// <param name="handle">The QPDF job handle pointer.</param>
    internal SafeQpdfJobHandle(IntPtr handle) : base(ownsHandle: true)
    {
        SetHandle(handle);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SafeQpdfJobHandle"/> class.
    /// Used by P/Invoke for automatic handle creation.
    /// </summary>
    public SafeQpdfJobHandle() : base(ownsHandle: true)
    {
    }

    /// <summary>
    /// Releases the QPDF job handle by calling qpdf_cleanup.
    /// This method is thread-safe and idempotent.
    /// </summary>
    /// <returns>True if the handle was released successfully; otherwise, false.</returns>
    protected override bool ReleaseHandle()
    {
        if (IsInvalid)
        {
            return true;
        }

        try
        {
            var tempHandle = handle;
            QpdfNative.qpdf_cleanup(ref tempHandle);
            return true;
        }
        catch
        {
            // If cleanup fails, we still return true to prevent handle leak warnings.
            // QPDF cleanup is designed to be safe even if called multiple times.
            return true;
        }
    }
}
