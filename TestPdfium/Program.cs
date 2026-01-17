using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public sealed class SafePdfDocumentHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafePdfDocumentHandle() : base(ownsHandle: true) { }

    protected override bool ReleaseHandle()
    {
        if (IsInvalid) return true;
        FPDF_CloseDocument(handle);
        return true;
    }

    [DllImport("pdfium.dll")]
    private static extern void FPDF_CloseDocument(IntPtr document);
}

public sealed class SafePdfPageHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    public SafePdfPageHandle() : base(ownsHandle: true) { }

    protected override bool ReleaseHandle()
    {
        if (IsInvalid) return true;
        FPDF_ClosePage(handle);
        return true;
    }

    [DllImport("pdfium.dll")]
    private static extern void FPDF_ClosePage(IntPtr page);
}

class Program
{
    [DllImport("pdfium.dll")]
    static extern void FPDF_InitLibrary();

    [DllImport("pdfium.dll")]
    static extern void FPDF_DestroyLibrary();

    [DllImport("pdfium.dll", CharSet = CharSet.Ansi)]
    static extern SafePdfDocumentHandle FPDF_LoadDocument(
        [MarshalAs(UnmanagedType.LPStr)] string file_path,
        [MarshalAs(UnmanagedType.LPStr)] string password);

    [DllImport("pdfium.dll")]
    static extern int FPDF_GetPageCount(SafePdfDocumentHandle document);

    [DllImport("pdfium.dll")]
    static extern SafePdfPageHandle FPDF_LoadPage(SafePdfDocumentHandle document, int page_index);

    // Float versions
    [DllImport("pdfium.dll")]
    static extern double FPDF_GetPageWidthF(SafePdfPageHandle page);

    [DllImport("pdfium.dll")]
    static extern double FPDF_GetPageHeightF(SafePdfPageHandle page);

    // Integer versions (older API)
    [DllImport("pdfium.dll")]
    static extern double FPDF_GetPageWidth(SafePdfPageHandle page);

    [DllImport("pdfium.dll")]
    static extern double FPDF_GetPageHeight(SafePdfPageHandle page);

    // Test with raw IntPtr
    [DllImport("pdfium.dll", EntryPoint = "FPDF_GetPageWidthF")]
    static extern double FPDF_GetPageWidthF_RawPtr(IntPtr page);

    [DllImport("pdfium.dll", EntryPoint = "FPDF_GetPageHeightF")]
    static extern double FPDF_GetPageHeightF_RawPtr(IntPtr page);

    [DllImport("pdfium.dll")]
    static extern uint FPDF_GetLastError();

    static void Main()
    {
        Console.WriteLine("Testing PDFium Page Dimensions");
        Console.WriteLine("================================\n");

        try
        {
            Console.WriteLine("1. Initializing PDFium...");
            FPDF_InitLibrary();
            Console.WriteLine("   SUCCESS\n");

            string pdfPath = @"..\tests\Fixtures\bookmarked.pdf";
            Console.WriteLine($"2. Loading document: {pdfPath}");
            using (var doc = FPDF_LoadDocument(pdfPath, ""))
            {
                if (doc.IsInvalid)
                {
                    var error = FPDF_GetLastError();
                    Console.WriteLine($"   FAILED: Error code: {error}");
                    return;
                }

                var pageCount = FPDF_GetPageCount(doc);
                Console.WriteLine($"   SUCCESS: Loaded {pageCount} pages\n");

                Console.WriteLine("3. Loading page 0...");
                using (var page = FPDF_LoadPage(doc, 0))
                {
                    if (page.IsInvalid)
                    {
                        Console.WriteLine("   FAILED: Invalid page handle");
                        return;
                    }

                    IntPtr pagePtr = page.DangerousGetHandle();
                    Console.WriteLine($"   Page handle: 0x{pagePtr:X}\n");

                    Console.WriteLine("4. Testing Float versions (FPDF_GetPageWidthF/HeightF)...");
                    double widthF = FPDF_GetPageWidthF(page);
                    double heightF = FPDF_GetPageHeightF(page);
                    Console.WriteLine($"   Float API: Width={widthF}, Height={heightF}");

                    Console.WriteLine("\n5. Testing Integer versions (FPDF_GetPageWidth/Height)...");
                    double widthInt = FPDF_GetPageWidth(page);
                    double heightInt = FPDF_GetPageHeight(page);
                    Console.WriteLine($"   Int API:   Width={widthInt}, Height={heightInt}");

                    Console.WriteLine("\n6. Testing with raw IntPtr parameter...");
                    double widthRaw = FPDF_GetPageWidthF_RawPtr(pagePtr);
                    double heightRaw = FPDF_GetPageHeightF_RawPtr(pagePtr);
                    Console.WriteLine($"   Raw IntPtr: Width={widthRaw}, Height={heightRaw}\n");

                    // Use whichever works
                    double width = widthInt;
                    double height = heightInt;

                    Console.WriteLine("\n7. Verdict:");
                    if (widthInt > 0 && heightInt > 0 && widthInt < 10000 && heightInt < 10000)
                    {
                        Console.WriteLine($"   SUCCESS: Integer API works! {widthInt / 72.0:F2} x {heightInt / 72.0:F2} inches");
                        width = widthInt;
                        height = heightInt;
                    }
                    else if (widthF > 0 && heightF > 0 && widthF < 10000 && heightF < 10000)
                    {
                        Console.WriteLine($"   SUCCESS: Float API works! {widthF / 72.0:F2} x {heightF / 72.0:F2} inches");
                        width = widthF;
                        height = heightF;
                    }
                    else
                    {
                        Console.WriteLine("   ERROR: Both APIs return garbage values!");
                        Console.WriteLine($"   Float: {widthF:E} x {heightF:E}");
                        Console.WriteLine($"   Int:   {widthInt:E} x {heightInt:E}");
                        Console.WriteLine($"   Raw:   {widthRaw:E} x {heightRaw:E}");
                    }
                }
            }

            Console.WriteLine("\n8. Cleanup...");
            FPDF_DestroyLibrary();
            Console.WriteLine("   Done");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nEXCEPTION: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Stack:\n{ex.StackTrace}");
        }
    }
}
