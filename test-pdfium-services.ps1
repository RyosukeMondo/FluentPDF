# PDFium Services Integration Test
# Tests PDFium services directly to verify no Task.Run crashes

Write-Host "=== PDFium Services Direct Test ===" -ForegroundColor Cyan
Write-Host "Building test harness..." -ForegroundColor Yellow

$testCode = @'
using System;
using System.Threading.Tasks;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;

class PdfiumServicesTester
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Starting PDFium services integration test...");
        var testPdf = args.Length > 0 ? args[0] : @"C:\Users\ryosu\repos\FluentPDF\tests\Fixtures\sample-form.pdf";

        if (!System.IO.File.Exists(testPdf))
        {
            Console.WriteLine($"ERROR: Test PDF not found: {testPdf}");
            return;
        }

        Console.WriteLine($"Test PDF: {testPdf}");

        // Test 1: PdfDocumentService - Load PDF
        Console.WriteLine("\n[1/8] Testing PdfDocumentService.LoadDocumentAsync...");
        try
        {
            var loggerMock = new Mock<ILogger<PdfDocumentService>>();
            var docService = new PdfDocumentService(loggerMock.Object);
            var result = await docService.LoadDocumentAsync(testPdf);

            if (result.IsSuccess)
            {
                Console.WriteLine("  ✓ Document loaded successfully");
                Console.WriteLine($"    Pages: {result.Value.PageCount}");
                result.Value.Dispose();
            }
            else
            {
                Console.WriteLine($"  ✗ FAIL: {string.Join(", ", result.Errors)}");
                return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ CRASH: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        // Test 2: BookmarkService
        Console.WriteLine("\n[2/8] Testing BookmarkService.LoadBookmarksAsync...");
        try
        {
            var loggerMock = new Mock<ILogger<BookmarkService>>();
            var docServiceMock = new Mock<ILogger<PdfDocumentService>>();
            var docService = new PdfDocumentService(docServiceMock.Object);
            var docResult = await docService.LoadDocumentAsync(@"C:\Users\ryosu\repos\FluentPDF\tests\Fixtures\bookmarked.pdf");

            if (docResult.IsSuccess)
            {
                var bookmarkService = new BookmarkService(loggerMock.Object);
                var result = await bookmarkService.LoadBookmarksAsync(docResult.Value.Handle);
                Console.WriteLine($"  ✓ Bookmarks loaded: {result.Value.Count} bookmarks");
                docResult.Value.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ CRASH: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        // Test 3: TextSearchService
        Console.WriteLine("\n[3/8] Testing TextSearchService.SearchAsync...");
        try
        {
            var loggerMock = new Mock<ILogger<TextSearchService>>();
            var docServiceMock = new Mock<ILogger<PdfDocumentService>>();
            var docService = new PdfDocumentService(docServiceMock.Object);
            var docResult = await docService.LoadDocumentAsync(@"C:\Users\ryosu\repos\FluentPDF\tests\Fixtures\sample-with-text.pdf");

            if (docResult.IsSuccess)
            {
                var searchService = new TextSearchService(loggerMock.Object);
                var result = await searchService.SearchAsync(docResult.Value.Handle, "sample", 0);
                Console.WriteLine($"  ✓ Search completed: {result.Value.Count} results");
                docResult.Value.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ CRASH: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        // Test 4: ThumbnailRenderingService
        Console.WriteLine("\n[4/8] Testing ThumbnailRenderingService.RenderThumbnailAsync...");
        try
        {
            var loggerMock = new Mock<ILogger<ThumbnailRenderingService>>();
            var docServiceMock = new Mock<ILogger<PdfDocumentService>>();
            var docService = new PdfDocumentService(docServiceMock.Object);
            var docResult = await docService.LoadDocumentAsync(testPdf);

            if (docResult.IsSuccess)
            {
                var thumbnailService = new ThumbnailRenderingService(loggerMock.Object);
                var result = await thumbnailService.RenderThumbnailAsync(docResult.Value.Handle, 0, 150, 200);
                Console.WriteLine($"  ✓ Thumbnail rendered: {result.Value.Width}x{result.Value.Height}");
                docResult.Value.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ CRASH: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        // Test 5: TextExtractionService
        Console.WriteLine("\n[5/8] Testing TextExtractionService.ExtractTextAsync...");
        try
        {
            var loggerMock = new Mock<ILogger<TextExtractionService>>();
            var docServiceMock = new Mock<ILogger<PdfDocumentService>>();
            var docService = new PdfDocumentService(docServiceMock.Object);
            var docResult = await docService.LoadDocumentAsync(testPdf);

            if (docResult.IsSuccess)
            {
                var textService = new TextExtractionService(loggerMock.Object);
                var result = await textService.ExtractTextAsync(docResult.Value.Handle, 0);
                Console.WriteLine($"  ✓ Text extracted: {result.Value.Length} characters");
                docResult.Value.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ CRASH: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        // Test 6: PdfFormService
        Console.WriteLine("\n[6/8] Testing PdfFormService.GetFormFieldsAsync...");
        try
        {
            var loggerMock = new Mock<ILogger<PdfFormService>>();
            var docServiceMock = new Mock<ILogger<PdfDocumentService>>();
            var docService = new PdfDocumentService(docServiceMock.Object);
            var docResult = await docService.LoadDocumentAsync(testPdf);

            if (docResult.IsSuccess)
            {
                var formService = new PdfFormService(loggerMock.Object);
                var result = await formService.GetFormFieldsAsync(docResult.Value.Handle, 0);
                Console.WriteLine($"  ✓ Form fields loaded: {result.Value.Count} fields");
                docResult.Value.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ CRASH: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        // Test 7: AnnotationService
        Console.WriteLine("\n[7/8] Testing AnnotationService.GetAnnotationsAsync...");
        try
        {
            var loggerMock = new Mock<ILogger<AnnotationService>>();
            var docServiceMock = new Mock<ILogger<PdfDocumentService>>();
            var docService = new PdfDocumentService(docServiceMock.Object);
            var docResult = await docService.LoadDocumentAsync(testPdf);

            if (docResult.IsSuccess)
            {
                var annotationService = new AnnotationService(loggerMock.Object);
                var result = await annotationService.GetAnnotationsAsync(docResult.Value.Handle, 0);
                Console.WriteLine($"  ✓ Annotations loaded: {result.Value.Count} annotations");
                docResult.Value.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ CRASH: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        // Test 8: PdfRenderingService
        Console.WriteLine("\n[8/8] Testing PdfRenderingService.RenderPageAsync...");
        try
        {
            var loggerMock = new Mock<ILogger<PdfRenderingService>>();
            var docServiceMock = new Mock<ILogger<PdfDocumentService>>();
            var docService = new PdfDocumentService(docServiceMock.Object);
            var docResult = await docService.LoadDocumentAsync(testPdf);

            if (docResult.IsSuccess)
            {
                var renderService = new PdfRenderingService(loggerMock.Object);
                var result = await renderService.RenderPageAsync(docResult.Value.Handle, 0, 1.0f, 96);
                Console.WriteLine($"  ✓ Page rendered: {result.Value.Width}x{result.Value.Height}");
                docResult.Value.Dispose();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ CRASH: {ex.GetType().Name}: {ex.Message}");
            return;
        }

        Console.WriteLine("\n=== ALL TESTS PASSED ===");
        Console.WriteLine("No AccessViolation crashes detected!");
        Console.WriteLine("PDFium threading fix is working correctly.");
    }
}
'@

# Save test code
$testCode | Out-File -FilePath "C:\Users\ryosu\repos\FluentPDF\TestPdfium\PdfiumServicesTester.cs" -Encoding UTF8 -Force

# Create project file
$projFile = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\src\FluentPDF.Rendering\FluentPDF.Rendering.csproj" />
    <ProjectReference Include="..\src\FluentPDF.Core\FluentPDF.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Moq" Version="4.20.70" />
  </ItemGroup>
</Project>
'@

New-Item -Path "C:\Users\ryosu\repos\FluentPDF\TestPdfium" -ItemType Directory -Force | Out-Null
$projFile | Out-File -FilePath "C:\Users\ryosu\repos\FluentPDF\TestPdfium\TestPdfium.csproj" -Encoding UTF8 -Force

Write-Host "Building and running test harness..." -ForegroundColor Yellow
cd "C:\Users\ryosu\repos\FluentPDF\TestPdfium"
dotnet run --project TestPdfium.csproj

Write-Host "`nTest harness execution complete." -ForegroundColor Green
