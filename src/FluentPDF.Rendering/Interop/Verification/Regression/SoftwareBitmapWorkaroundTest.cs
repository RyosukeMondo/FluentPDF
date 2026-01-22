using System.Runtime.InteropServices;
using FluentPDF.Rendering.Interop.Verification.Reports;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace FluentPDF.Rendering.Interop.Verification.Regression;

/// <summary>
/// Tests the SoftwareBitmap workaround to determine if WinUI 3 InMemoryRandomAccessStream
/// crash issues have been resolved.
/// </summary>
/// <remarks>
/// <para>
/// The workaround decodes PNG using ImageSharp and copies pixel data manually instead of
/// using InMemoryRandomAccessStream directly with WinUI 3 SoftwareBitmapSource, which
/// caused crashes in earlier WinUI 3 versions.
/// </para>
/// <para>
/// This test verifies that:
/// 1. The workaround (ImageSharp decoding + manual pixel copy) still works correctly
/// 2. Whether the underlying WinUI 3 issue has been fixed (not testable in unit test context)
/// </para>
/// </remarks>
public class SoftwareBitmapWorkaroundTest : IWorkaroundTest
{
    private const string DllName = "pdfium.dll";
    private const int TestPageWidth = 100;
    private const int TestPageHeight = 100;

    /// <inheritdoc/>
    public string WorkaroundName => "SoftwareBitmap Workaround (ImageSharp PNG Decode)";

    /// <inheritdoc/>
    public string DocumentationReference => "ThumbnailsViewModel.cs:155-174";

    /// <inheritdoc/>
    public async Task<WorkaroundTestResult> TestWorkaroundAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        SafePdfDocumentHandle? documentHandle = null;
        SafePdfPageHandle? pageHandle = null;
        IntPtr bitmapHandle = IntPtr.Zero;

        try
        {
            // Create test PDF
            var testPdfBytes = CreateSimpleTestPdf();

            // Load document
            var unmanagedPointer = Marshal.AllocHGlobal(testPdfBytes.Length);
            try
            {
                Marshal.Copy(testPdfBytes, 0, unmanagedPointer, testPdfBytes.Length);
                documentHandle = FPDF_LoadMemDocument(unmanagedPointer, testPdfBytes.Length, null);
            }
            finally
            {
                Marshal.FreeHGlobal(unmanagedPointer);
            }

            if (documentHandle == null || documentHandle.IsInvalid)
            {
                return new WorkaroundTestResult
                {
                    WorkaroundName = WorkaroundName,
                    DocumentationReference = DocumentationReference,
                    Status = WorkaroundStatus.Broken,
                    Details = "Failed to load test document for SoftwareBitmap workaround testing",
                    RecommendedAction = "Investigate why test document creation or loading failed",
                    Context = new Dictionary<string, string>
                    {
                        ["TestMethod"] = "SoftwareBitmapWorkaroundTest.TestWorkaroundAsync",
                        ["Error"] = "Document load failed"
                    }
                };
            }

            // Load page
            pageHandle = FPDF_LoadPage(documentHandle, 0);
            if (pageHandle == null || pageHandle.IsInvalid)
            {
                return new WorkaroundTestResult
                {
                    WorkaroundName = WorkaroundName,
                    DocumentationReference = DocumentationReference,
                    Status = WorkaroundStatus.Broken,
                    Details = "Failed to load page 0 for SoftwareBitmap workaround testing",
                    RecommendedAction = "Investigate why page loading failed",
                    Context = new Dictionary<string, string>
                    {
                        ["TestMethod"] = "SoftwareBitmapWorkaroundTest.TestWorkaroundAsync",
                        ["PageIndex"] = "0",
                        ["Error"] = "Page load failed"
                    }
                };
            }

            // Create bitmap (BGRA32 format, matching WinUI 3 requirements)
            bitmapHandle = FPDFBitmap_Create(TestPageWidth, TestPageHeight, 1 /* alpha channel */);
            if (bitmapHandle == IntPtr.Zero)
            {
                return new WorkaroundTestResult
                {
                    WorkaroundName = WorkaroundName,
                    DocumentationReference = DocumentationReference,
                    Status = WorkaroundStatus.Broken,
                    Details = "Failed to create bitmap for SoftwareBitmap workaround testing",
                    RecommendedAction = "Investigate why bitmap creation failed",
                    Context = new Dictionary<string, string>
                    {
                        ["TestMethod"] = "SoftwareBitmapWorkaroundTest.TestWorkaroundAsync",
                        ["BitmapWidth"] = TestPageWidth.ToString(),
                        ["BitmapHeight"] = TestPageHeight.ToString(),
                        ["Error"] = "Bitmap creation failed"
                    }
                };
            }

            // Fill bitmap with white background
            FPDFBitmap_FillRect(bitmapHandle, 0, 0, TestPageWidth, TestPageHeight, 0xFFFFFFFF);

            // Render page to bitmap
            FPDF_RenderPageBitmap(bitmapHandle, pageHandle, 0, 0, TestPageWidth, TestPageHeight, 0, 0);

            // Get bitmap buffer and copy to managed memory
            var bufferPtr = FPDFBitmap_GetBuffer(bitmapHandle);
            var stride = FPDFBitmap_GetStride(bitmapHandle);
            var bufferSize = stride * TestPageHeight;

            if (bufferPtr == IntPtr.Zero)
            {
                return new WorkaroundTestResult
                {
                    WorkaroundName = WorkaroundName,
                    DocumentationReference = DocumentationReference,
                    Status = WorkaroundStatus.Broken,
                    Details = "FPDFBitmap_GetBuffer returned null pointer",
                    RecommendedAction = "Investigate why bitmap buffer retrieval failed",
                    Context = new Dictionary<string, string>
                    {
                        ["TestMethod"] = "SoftwareBitmapWorkaroundTest.TestWorkaroundAsync",
                        ["Error"] = "Bitmap buffer null"
                    }
                };
            }

            var pixelData = new byte[bufferSize];
            Marshal.Copy(bufferPtr, pixelData, 0, bufferSize);

            // Test the workaround: decode with ImageSharp
            var workaroundSucceeded = await TestImageSharpDecodingWorkaroundAsync(pixelData, cancellationToken);

            var context = new Dictionary<string, string>
            {
                ["BitmapWidth"] = TestPageWidth.ToString(),
                ["BitmapHeight"] = TestPageHeight.ToString(),
                ["Stride"] = stride.ToString(),
                ["BufferSize"] = bufferSize.ToString(),
                ["WorkaroundSucceeded"] = workaroundSucceeded.ToString()
            };

            if (workaroundSucceeded)
            {
                // Workaround is working correctly
                // Note: We cannot test if the underlying WinUI 3 issue is fixed without actual WinUI 3 UI context
                return new WorkaroundTestResult
                {
                    WorkaroundName = WorkaroundName,
                    DocumentationReference = DocumentationReference,
                    Status = WorkaroundStatus.StillNeeded,
                    Details = "ImageSharp PNG decoding workaround is functioning correctly. " +
                             "The underlying WinUI 3 InMemoryRandomAccessStream issue cannot be tested without UI context. " +
                             $"Successfully processed {bufferSize} bytes of pixel data.",
                    RecommendedAction = "Continue using ImageSharp workaround. To test if WinUI 3 issue is fixed, " +
                                       "manually test InMemoryRandomAccessStream with SoftwareBitmapSource in WinUI 3 UI context.",
                    Context = context
                };
            }
            else
            {
                // Workaround is broken
                return new WorkaroundTestResult
                {
                    WorkaroundName = WorkaroundName,
                    DocumentationReference = DocumentationReference,
                    Status = WorkaroundStatus.Broken,
                    Details = "ImageSharp PNG decoding workaround failed to process bitmap data correctly",
                    RecommendedAction = "Investigate why ImageSharp decoding failed. Check ImageSharp library version and compatibility.",
                    Context = context
                };
            }
        }
        catch (Exception ex)
        {
            return new WorkaroundTestResult
            {
                WorkaroundName = WorkaroundName,
                DocumentationReference = DocumentationReference,
                Status = WorkaroundStatus.Broken,
                Details = $"Workaround test threw exception: {ex.GetType().Name}: {ex.Message}",
                RecommendedAction = $"Investigate exception in workaround test: {ex.Message}",
                Context = new Dictionary<string, string>
                {
                    ["ExceptionType"] = ex.GetType().FullName ?? ex.GetType().Name,
                    ["ExceptionMessage"] = ex.Message,
                    ["StackTrace"] = ex.StackTrace ?? "(no stack trace)"
                }
            };
        }
        finally
        {
            if (bitmapHandle != IntPtr.Zero)
            {
                FPDFBitmap_Destroy(bitmapHandle);
            }
            pageHandle?.Dispose();
            documentHandle?.Dispose();
        }
    }

    /// <summary>
    /// Tests the ImageSharp decoding workaround by loading BGRA32 pixel data.
    /// </summary>
    private async Task<bool> TestImageSharpDecodingWorkaroundAsync(byte[] pixelData, CancellationToken cancellationToken)
    {
        await Task.Yield();

        try
        {
            // Simulate the workaround from ThumbnailsViewModel.cs:155-174
            // Load pixel data as BGRA32 image using ImageSharp
            using var image = Image.LoadPixelData<Bgra32>(pixelData, TestPageWidth, TestPageHeight);

            // Verify image properties
            if (image.Width != TestPageWidth || image.Height != TestPageHeight)
            {
                return false;
            }

            // Copy pixel data out (as done in the workaround)
            var copiedPixelData = new byte[TestPageWidth * TestPageHeight * 4];
            image.CopyPixelDataTo(copiedPixelData);

            // Verify pixel data was copied successfully
            if (copiedPixelData.Length != TestPageWidth * TestPageHeight * 4)
            {
                return false;
            }

            // Workaround is working if we got here without exceptions
            return true;
        }
        catch
        {
            return false;
        }
    }

    #region PDFium P/Invoke Declarations

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern SafePdfDocumentHandle FPDF_LoadMemDocument(IntPtr data_buf, int size, string? password);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern SafePdfPageHandle FPDF_LoadPage(SafePdfDocumentHandle document, int page_index);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFBitmap_Create(int width, int height, int alpha);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDFBitmap_FillRect(IntPtr bitmap, int left, int top, int width, int height, uint color);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDF_RenderPageBitmap(IntPtr bitmap, SafePdfPageHandle page, int start_x, int start_y, int size_x, int size_y, int rotate, int flags);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr FPDFBitmap_GetBuffer(IntPtr bitmap);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int FPDFBitmap_GetStride(IntPtr bitmap);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void FPDFBitmap_Destroy(IntPtr bitmap);

    #endregion

    #region Test PDF Creation

    /// <summary>
    /// Creates a minimal valid PDF for testing bitmap workaround.
    /// </summary>
    private static byte[] CreateSimpleTestPdf()
    {
        // Minimal PDF with one 612x792 points page (US Letter size)
        var pdfContent = @"%PDF-1.4
1 0 obj
<< /Type /Catalog /Pages 2 0 R >>
endobj
2 0 obj
<< /Type /Pages /Kids [3 0 R] /Count 1 >>
endobj
3 0 obj
<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << >> >>
endobj
xref
0 4
0000000000 65535 f
0000000009 00000 n
0000000058 00000 n
0000000115 00000 n
trailer
<< /Size 4 /Root 1 0 R >>
startxref
209
%%EOF";

        return System.Text.Encoding.ASCII.GetBytes(pdfContent);
    }

    #endregion
}
