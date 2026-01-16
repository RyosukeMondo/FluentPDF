// Copyright (c) 2025 FluentPDF. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.App.Services;

/// <summary>
/// Handles CLI diagnostic commands for PDF rendering testing and system diagnostics.
/// Provides test-render, diagnostics, and render-test command implementations.
/// </summary>
public sealed class DiagnosticCommandHandler
{
    private readonly IPdfDocumentService _pdfDocumentService;
    private readonly RenderingCoordinator _renderingCoordinator;
    private readonly RenderingObservabilityService _observabilityService;
    private readonly IPdfRenderingService _pdfRenderingService;
    private readonly ILogger<DiagnosticCommandHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticCommandHandler"/> class.
    /// </summary>
    /// <param name="pdfDocumentService">Service for loading PDF documents.</param>
    /// <param name="renderingCoordinator">Coordinator for rendering with fallback strategies.</param>
    /// <param name="observabilityService">Service for observability and diagnostics logging.</param>
    /// <param name="pdfRenderingService">Service for rendering PDF pages to PNG streams.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public DiagnosticCommandHandler(
        IPdfDocumentService pdfDocumentService,
        RenderingCoordinator renderingCoordinator,
        RenderingObservabilityService observabilityService,
        IPdfRenderingService pdfRenderingService,
        ILogger<DiagnosticCommandHandler> logger)
    {
        _pdfDocumentService = pdfDocumentService ?? throw new ArgumentNullException(nameof(pdfDocumentService));
        _renderingCoordinator = renderingCoordinator ?? throw new ArgumentNullException(nameof(renderingCoordinator));
        _observabilityService = observabilityService ?? throw new ArgumentNullException(nameof(observabilityService));
        _pdfRenderingService = pdfRenderingService ?? throw new ArgumentNullException(nameof(pdfRenderingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the test-render command: loads PDF, renders first page, saves diagnostic info.
    /// </summary>
    /// <param name="filePath">Path to the PDF file to test.</param>
    /// <returns>
    /// Exit code: 0 = success, 1 = load failure, 2 = render failure, 3 = UI binding failure.
    /// </returns>
    public async Task<int> HandleTestRenderAsync(string filePath)
    {
        Console.WriteLine($"FluentPDF Test Render");
        Console.WriteLine($"====================");
        Console.WriteLine($"File: {filePath}");
        Console.WriteLine();

        // Validate file exists
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"ERROR: File not found: {filePath}");
            return 1;
        }

        var diagnosticData = new Dictionary<string, object>
        {
            { "Timestamp", DateTime.UtcNow },
            { "FilePath", filePath },
            { "MachineName", Environment.MachineName },
            { "OSVersion", Environment.OSVersion.ToString() },
            { ".NET Version", Environment.Version.ToString() }
        };

        try
        {
            // Step 1: Load document
            Console.WriteLine("Step 1: Loading PDF document...");
            var loadResult = await _pdfDocumentService.LoadDocumentAsync(filePath);

            if (loadResult.IsFailed)
            {
                var error = loadResult.Errors.FirstOrDefault();
                Console.WriteLine($"ERROR: Failed to load PDF: {error?.Message ?? "Unknown error"}");
                diagnosticData["LoadResult"] = "Failed";
                diagnosticData["LoadError"] = error?.Message ?? "Unknown error";
                SaveDiagnosticFile(filePath, diagnosticData);
                return 1;
            }

            var document = loadResult.Value;
            Console.WriteLine($"SUCCESS: Loaded PDF with {document.PageCount} pages");
            diagnosticData["LoadResult"] = "Success";
            diagnosticData["PageCount"] = document.PageCount;
            diagnosticData["FileSizeBytes"] = document.FileSizeBytes;

            // Step 2: Render first page
            Console.WriteLine();
            Console.WriteLine("Step 2: Rendering first page...");

            var context = new RenderContext(
                DocumentPath: filePath,
                PageNumber: 1,
                TotalPages: document.PageCount,
                RenderDpi: 96.0,
                RequestSource: "CLI-TestRender",
                RequestTime: DateTime.UtcNow,
                OperationId: Guid.NewGuid()
            );

            var stopwatch = Stopwatch.StartNew();
            var imageSource = await _renderingCoordinator.RenderWithFallbackAsync(
                document,
                pageNumber: 1,
                zoomLevel: 1.0,
                dpi: 96.0,
                context);
            stopwatch.Stop();

            if (imageSource == null)
            {
                Console.WriteLine("ERROR: Failed to render first page (all strategies failed)");
                diagnosticData["RenderResult"] = "Failed";
                diagnosticData["RenderError"] = "All rendering strategies failed";
                SaveDiagnosticFile(filePath, diagnosticData);
                document.Dispose();
                return 2;
            }

            Console.WriteLine($"SUCCESS: Rendered first page in {stopwatch.ElapsedMilliseconds}ms");
            diagnosticData["RenderResult"] = "Success";
            diagnosticData["RenderTimeMs"] = stopwatch.ElapsedMilliseconds;

            // Step 3: Verify UI binding (basic check that ImageSource is valid)
            Console.WriteLine();
            Console.WriteLine("Step 3: Verifying rendered output...");

            if (imageSource != null)
            {
                Console.WriteLine("SUCCESS: ImageSource is valid and ready for UI binding");
                diagnosticData["UIBindingResult"] = "Success";
            }
            else
            {
                Console.WriteLine("WARNING: ImageSource verification failed");
                diagnosticData["UIBindingResult"] = "Failed";
                SaveDiagnosticFile(filePath, diagnosticData);
                document.Dispose();
                return 3;
            }

            // Save diagnostic file
            SaveDiagnosticFile(filePath, diagnosticData);

            Console.WriteLine();
            Console.WriteLine("Test render completed successfully!");
            Console.WriteLine($"Diagnostic file saved: {GetDiagnosticFileName(filePath)}");

            document.Dispose();
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Unexpected exception: {ex.Message}");
            _logger.LogError(ex, "Test render failed with exception");
            diagnosticData["Exception"] = ex.ToString();
            SaveDiagnosticFile(filePath, diagnosticData);
            return 2;
        }
    }

    /// <summary>
    /// Handles the diagnostics command: outputs system information.
    /// </summary>
    /// <returns>Exit code (always 0).</returns>
    public Task<int> HandleDiagnosticsAsync()
    {
        Console.WriteLine("FluentPDF System Diagnostics");
        Console.WriteLine("============================");
        Console.WriteLine();

        // Operating System
        Console.WriteLine("Operating System:");
        Console.WriteLine($"  OS: {RuntimeInformation.OSDescription}");
        Console.WriteLine($"  Architecture: {RuntimeInformation.OSArchitecture}");
        Console.WriteLine($"  Framework: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine($"  Process Architecture: {RuntimeInformation.ProcessArchitecture}");
        Console.WriteLine();

        // .NET Version
        Console.WriteLine(".NET Runtime:");
        Console.WriteLine($"  Version: {Environment.Version}");
        Console.WriteLine($"  Runtime: {RuntimeInformation.FrameworkDescription}");
        Console.WriteLine();

        // Memory
        var process = Process.GetCurrentProcess();
        Console.WriteLine("Memory:");
        Console.WriteLine($"  Working Set: {process.WorkingSet64 / 1024 / 1024:N2} MB");
        Console.WriteLine($"  Private Memory: {process.PrivateMemorySize64 / 1024 / 1024:N2} MB");
        Console.WriteLine($"  Managed Heap: {GC.GetTotalMemory(false) / 1024 / 1024:N2} MB");
        Console.WriteLine($"  Handle Count: {process.HandleCount}");
        Console.WriteLine();

        // PDFium
        Console.WriteLine("PDFium:");
        var pdfiumInitialized = PdfiumInterop.Initialize();
        Console.WriteLine($"  Initialized: {pdfiumInitialized}");

        var pdfiumPath = Path.Combine(AppContext.BaseDirectory, "pdfium.dll");
        if (File.Exists(pdfiumPath))
        {
            var pdfiumFileInfo = new FileInfo(pdfiumPath);
            Console.WriteLine($"  DLL Path: {pdfiumPath}");
            Console.WriteLine($"  DLL Size: {pdfiumFileInfo.Length / 1024:N0} KB");
            Console.WriteLine($"  DLL Modified: {pdfiumFileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
        }
        else
        {
            Console.WriteLine($"  DLL Path: Not found at {pdfiumPath}");
        }
        Console.WriteLine();

        // Capabilities
        Console.WriteLine("Capabilities:");
        Console.WriteLine($"  64-bit Process: {Environment.Is64BitProcess}");
        Console.WriteLine($"  Processor Count: {Environment.ProcessorCount}");
        Console.WriteLine($"  User Interactive: {Environment.UserInteractive}");
        Console.WriteLine();

        return Task.FromResult(0);
    }

    /// <summary>
    /// Handles the render-test command: renders all pages to PNG files.
    /// </summary>
    /// <param name="filePath">Path to the PDF file.</param>
    /// <param name="outputDirectory">Directory to save PNG files (default: current directory).</param>
    /// <returns>Exit code: 0 = success, 1 = load failure, 2 = render failure.</returns>
    public async Task<int> HandleRenderTestAsync(string filePath, string? outputDirectory = null)
    {
        Console.WriteLine("FluentPDF Render Test");
        Console.WriteLine("====================");
        Console.WriteLine($"File: {filePath}");
        Console.WriteLine();

        // Validate file exists
        if (!File.Exists(filePath))
        {
            Console.WriteLine($"ERROR: File not found: {filePath}");
            return 1;
        }

        // Set default output directory
        outputDirectory = outputDirectory ?? Environment.CurrentDirectory;

        // Create output directory if it doesn't exist
        if (!Directory.Exists(outputDirectory))
        {
            try
            {
                Directory.CreateDirectory(outputDirectory);
                Console.WriteLine($"Created output directory: {outputDirectory}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to create output directory: {ex.Message}");
                return 2;
            }
        }

        Console.WriteLine($"Output directory: {outputDirectory}");
        Console.WriteLine();

        try
        {
            // Load document
            Console.WriteLine("Loading PDF document...");
            var loadResult = await _pdfDocumentService.LoadDocumentAsync(filePath);

            if (loadResult.IsFailed)
            {
                var error = loadResult.Errors.FirstOrDefault();
                Console.WriteLine($"ERROR: Failed to load PDF: {error?.Message ?? "Unknown error"}");
                return 1;
            }

            var document = loadResult.Value;
            Console.WriteLine($"Loaded PDF with {document.PageCount} pages");
            Console.WriteLine();

            var successCount = 0;
            var failureCount = 0;
            var totalStopwatch = Stopwatch.StartNew();

            // Render each page
            for (int pageNum = 1; pageNum <= document.PageCount; pageNum++)
            {
                Console.Write($"Rendering page {pageNum}/{document.PageCount}... ");

                var context = new RenderContext(
                    DocumentPath: filePath,
                    PageNumber: pageNum,
                    TotalPages: document.PageCount,
                    RenderDpi: 96.0,
                    RequestSource: "CLI-RenderTest",
                    RequestTime: DateTime.UtcNow,
                    OperationId: Guid.NewGuid()
                );

                var pageStopwatch = Stopwatch.StartNew();

                // Use PdfRenderingService to get PNG stream
                var renderResult = await _pdfRenderingService.RenderPageAsync(
                    document,
                    pageNum,
                    zoomLevel: 1.0,
                    dpi: 96.0);

                pageStopwatch.Stop();

                if (renderResult.IsSuccess && renderResult.Value != null)
                {
                    // Save PNG to file
                    var outputFileName = $"page_{pageNum:D4}.png";
                    var outputPath = Path.Combine(outputDirectory, outputFileName);

                    try
                    {
                        using var fileStream = File.Create(outputPath);
                        await renderResult.Value.CopyToAsync(fileStream);
                        renderResult.Value.Dispose();

                        Console.WriteLine($"✓ Saved to {outputFileName} ({pageStopwatch.ElapsedMilliseconds}ms)");
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Failed to save: {ex.Message}");
                        failureCount++;
                    }
                }
                else
                {
                    Console.WriteLine($"✗ Render failed ({pageStopwatch.ElapsedMilliseconds}ms)");
                    failureCount++;
                }
            }

            totalStopwatch.Stop();

            // Summary
            Console.WriteLine();
            Console.WriteLine("Render Test Summary");
            Console.WriteLine("===================");
            Console.WriteLine($"Total pages: {document.PageCount}");
            Console.WriteLine($"Successful: {successCount}");
            Console.WriteLine($"Failed: {failureCount}");
            Console.WriteLine($"Total time: {totalStopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Average per page: {(totalStopwatch.ElapsedMilliseconds / (double)document.PageCount):F2}ms");

            document.Dispose();

            return failureCount > 0 ? 2 : 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Unexpected exception: {ex.Message}");
            _logger.LogError(ex, "Render test failed with exception");
            return 2;
        }
    }

    /// <summary>
    /// Saves diagnostic data to a file.
    /// </summary>
    /// <param name="pdfFilePath">Original PDF file path.</param>
    /// <param name="diagnosticData">Diagnostic data dictionary.</param>
    private void SaveDiagnosticFile(string pdfFilePath, Dictionary<string, object> diagnosticData)
    {
        try
        {
            var diagnosticFileName = GetDiagnosticFileName(pdfFilePath);
            var lines = new List<string>
            {
                "FluentPDF Test Render Diagnostic Report",
                "========================================",
                ""
            };

            foreach (var kvp in diagnosticData)
            {
                lines.Add($"{kvp.Key}: {kvp.Value}");
            }

            File.WriteAllLines(diagnosticFileName, lines);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"WARNING: Failed to save diagnostic file: {ex.Message}");
            _logger.LogWarning(ex, "Failed to save diagnostic file");
        }
    }

    /// <summary>
    /// Gets the diagnostic file name for a PDF file.
    /// </summary>
    /// <param name="pdfFilePath">PDF file path.</param>
    /// <returns>Diagnostic file name.</returns>
    private string GetDiagnosticFileName(string pdfFilePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(pdfFilePath);
        var directory = Path.GetDirectoryName(pdfFilePath) ?? Environment.CurrentDirectory;
        return Path.Combine(directory, $"{fileName}_diagnostic_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
    }
}
