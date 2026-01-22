using System.Runtime.InteropServices;
using FluentPDF.Rendering.Interop.Verification.Reports;

namespace FluentPDF.Rendering.Interop.Verification.Regression;

/// <summary>
/// Tests the float dimension workaround to determine if PDFium's float API (FPDF_GetPageWidthF/HeightF)
/// has been fixed and the workaround can be removed.
/// </summary>
/// <remarks>
/// <para>
/// The workaround uses integer-based API (FPDF_GetPageWidth/Height) instead of float API
/// because the float API returns garbage values in certain PDFium versions.
/// </para>
/// <para>
/// This test compares both APIs to detect if the float API now returns valid values,
/// indicating the underlying bug has been fixed.
/// </para>
/// </remarks>
public class FloatDimensionWorkaroundTest : IWorkaroundTest
{
    private const string DllName = "pdfium.dll";
    private const double FloatApiTolerancePoints = 1.0; // Allow 1 point difference due to rounding

    /// <inheritdoc/>
    public string WorkaroundName => "Float Dimension Workaround";

    /// <inheritdoc/>
    public string DocumentationReference => "PdfiumInterop.cs:177-183";

    /// <inheritdoc/>
    public async Task<WorkaroundTestResult> TestWorkaroundAsync(CancellationToken cancellationToken = default)
    {
        await Task.Yield();

        SafePdfDocumentHandle? documentHandle = null;
        SafePdfPageHandle? pageHandle = null;

        try
        {
            // Create a simple test PDF in memory to test dimension APIs
            var testPdfBytes = CreateSimpleTestPdf();

            // Load document
            documentHandle = LoadDocumentFromMemory(testPdfBytes);
            if (documentHandle == null || documentHandle.IsInvalid)
            {
                return new WorkaroundTestResult
                {
                    WorkaroundName = WorkaroundName,
                    DocumentationReference = DocumentationReference,
                    Status = WorkaroundStatus.Broken,
                    Details = "Failed to load test document for float dimension testing",
                    RecommendedAction = "Investigate why test document creation or loading failed",
                    Context = new Dictionary<string, string>
                    {
                        ["TestMethod"] = "FloatDimensionWorkaroundTest.TestWorkaroundAsync",
                        ["Error"] = "Document load failed"
                    }
                };
            }

            // Load first page
            pageHandle = FPDF_LoadPage(documentHandle, 0);
            if (pageHandle == null || pageHandle.IsInvalid)
            {
                return new WorkaroundTestResult
                {
                    WorkaroundName = WorkaroundName,
                    DocumentationReference = DocumentationReference,
                    Status = WorkaroundStatus.Broken,
                    Details = "Failed to load page 0 for dimension testing",
                    RecommendedAction = "Investigate why page loading failed",
                    Context = new Dictionary<string, string>
                    {
                        ["TestMethod"] = "FloatDimensionWorkaroundTest.TestWorkaroundAsync",
                        ["PageIndex"] = "0",
                        ["Error"] = "Page load failed"
                    }
                };
            }

            // Get dimensions using integer API (current workaround)
            var intWidth = FPDF_GetPageWidth(pageHandle);
            var intHeight = FPDF_GetPageHeight(pageHandle);

            // Get dimensions using float API (potentially broken)
            var floatWidth = FPDF_GetPageWidthF(pageHandle);
            var floatHeight = FPDF_GetPageHeightF(pageHandle);

            // Check if float API returns valid values (within tolerance of integer API)
            var widthDifference = Math.Abs(floatWidth - intWidth);
            var heightDifference = Math.Abs(floatHeight - intHeight);

            var isFloatApiValid = widthDifference <= FloatApiTolerancePoints &&
                                  heightDifference <= FloatApiTolerancePoints &&
                                  !double.IsNaN(floatWidth) &&
                                  !double.IsInfinity(floatWidth) &&
                                  !double.IsNaN(floatHeight) &&
                                  !double.IsInfinity(floatHeight) &&
                                  floatWidth > 0 &&
                                  floatHeight > 0;

            var context = new Dictionary<string, string>
            {
                ["IntegerWidth"] = intWidth.ToString("F2"),
                ["IntegerHeight"] = intHeight.ToString("F2"),
                ["FloatWidth"] = floatWidth.ToString("F2"),
                ["FloatHeight"] = floatHeight.ToString("F2"),
                ["WidthDifference"] = widthDifference.ToString("F2"),
                ["HeightDifference"] = heightDifference.ToString("F2"),
                ["Tolerance"] = FloatApiTolerancePoints.ToString("F2")
            };

            if (isFloatApiValid)
            {
                // Float API appears to be working correctly now
                return new WorkaroundTestResult
                {
                    WorkaroundName = WorkaroundName,
                    DocumentationReference = DocumentationReference,
                    Status = WorkaroundStatus.CanBeRemoved,
                    Details = $"Float API (FPDF_GetPageWidthF/HeightF) now returns valid values within tolerance. " +
                             $"Integer API: {intWidth:F2} x {intHeight:F2}, Float API: {floatWidth:F2} x {floatHeight:F2}, " +
                             $"Difference: {widthDifference:F2} x {heightDifference:F2} (tolerance: {FloatApiTolerancePoints:F2})",
                    RecommendedAction = "Consider removing workaround and migrating to float API (FPDF_GetPageWidthF/HeightF) " +
                                       "after thorough testing with production PDFs",
                    Context = context
                };
            }
            else
            {
                // Float API still returns invalid values - workaround still needed
                return new WorkaroundTestResult
                {
                    WorkaroundName = WorkaroundName,
                    DocumentationReference = DocumentationReference,
                    Status = WorkaroundStatus.StillNeeded,
                    Details = $"Float API (FPDF_GetPageWidthF/HeightF) still returns invalid/garbage values. " +
                             $"Integer API: {intWidth:F2} x {intHeight:F2}, Float API: {floatWidth:F2} x {floatHeight:F2}, " +
                             $"Difference: {widthDifference:F2} x {heightDifference:F2} (tolerance: {FloatApiTolerancePoints:F2})",
                    RecommendedAction = "Continue using integer API (FPDF_GetPageWidth/Height) until PDFium float API is fixed",
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
            pageHandle?.Dispose();
            documentHandle?.Dispose();
        }
    }

    #region PDFium P/Invoke Declarations

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern SafePdfDocumentHandle FPDF_LoadMemDocument(IntPtr data_buf, int size, string? password);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern SafePdfPageHandle FPDF_LoadPage(SafePdfDocumentHandle document, int page_index);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern double FPDF_GetPageWidth(SafePdfPageHandle page);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern double FPDF_GetPageHeight(SafePdfPageHandle page);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern float FPDF_GetPageWidthF(SafePdfPageHandle page);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern float FPDF_GetPageHeightF(SafePdfPageHandle page);

    #endregion

    #region Test PDF Creation

    /// <summary>
    /// Creates a minimal valid PDF for testing dimension APIs.
    /// </summary>
    /// <returns>Byte array containing a valid PDF document.</returns>
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

    /// <summary>
    /// Loads a PDF document from memory.
    /// </summary>
    private static SafePdfDocumentHandle LoadDocumentFromMemory(byte[] pdfBytes)
    {
        var unmanagedPointer = Marshal.AllocHGlobal(pdfBytes.Length);
        try
        {
            Marshal.Copy(pdfBytes, 0, unmanagedPointer, pdfBytes.Length);
            return FPDF_LoadMemDocument(unmanagedPointer, pdfBytes.Length, null);
        }
        finally
        {
            Marshal.FreeHGlobal(unmanagedPointer);
        }
    }

    #endregion
}
