using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentPDF.App.Testing.Tests;

/// <summary>
/// CLI test that verifies form field detection and rendering functionality.
/// Tests form detection, field verification, and rendering with forms visible.
/// </summary>
public sealed class FormFieldRenderCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "form-field-render";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Detects form fields in a PDF and renders page with forms visible";

    /// <summary>
    /// Executes the form field rendering test using the provided test context.
    /// Loads a test PDF, detects form fields, renders a page, and verifies form visibility.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing form field count, types, positions, and rendered output</returns>
    public async Task<CliTestResult> RunAsync(CliTestContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var result = new CliTestResult
        {
            TestName = Name,
            Success = false
        };

        var startTime = DateTime.UtcNow;

        try
        {
            context.Logger.Information("Starting form field render test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
            var formService = context.Services.GetRequiredService<IPdfFormService>();
            var renderingService = context.Services.GetRequiredService<IPdfRenderingService>();

            // Get test PDF path from context data or use default
            var testPdfPath = context.Data.TryGetValue("TestPdfPath", out var pathObj) && pathObj is string path
                ? path
                : Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "tests", "Fixtures", "sample-form.pdf"
                );
            testPdfPath = Path.GetFullPath(testPdfPath);

            // Get page number from context data or use default
            var pageNumber = context.Data.TryGetValue("PageNumber", out var pageObj) && pageObj is int page
                ? page
                : 1;

            context.Logger.Information("Loading PDF: {FilePath}", testPdfPath);

            if (!File.Exists(testPdfPath))
            {
                result.ErrorMessage = $"Test PDF not found: {testPdfPath}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Load the PDF document
            var loadResult = await documentService.LoadDocumentAsync(testPdfPath);
            if (loadResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to load PDF: {string.Join(", ", loadResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            using var document = loadResult.Value;
            context.Logger.Information("Loaded PDF with {PageCount} pages", document.PageCount);

            // Validate page number
            if (pageNumber < 1 || pageNumber > document.PageCount)
            {
                result.ErrorMessage = $"Invalid page number {pageNumber}. Document has {document.PageCount} pages.";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Get form fields on the specified page
            context.Logger.Information("Detecting form fields on page {PageNumber}", pageNumber);
            var formFieldsResult = await formService.GetFormFieldsAsync(document, pageNumber);
            if (formFieldsResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to get form fields: {string.Join(", ", formFieldsResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var formFields = formFieldsResult.Value;
            var fieldCount = formFields.Count;

            context.Logger.Information("Detected {FieldCount} form fields on page {PageNumber}", fieldCount, pageNumber);

            // Handle no forms gracefully (requirement 4.4)
            if (fieldCount == 0)
            {
                result.Success = true;
                result.Duration = DateTime.UtcNow - startTime;
                result.Outputs["FieldCount"] = 0;
                result.Outputs["Message"] = "No forms detected";
                result.Outputs["ExitCode"] = 0;

                context.Logger.Information("No forms detected on page {PageNumber}", pageNumber);
                return result;
            }

            // Collect field information (requirement 4.2)
            var fieldTypes = new Dictionary<string, int>();
            var fieldPositions = new List<Dictionary<string, object>>();

            foreach (var field in formFields)
            {
                // Count field types
                var typeName = field.Type.ToString();
                if (!fieldTypes.ContainsKey(typeName))
                {
                    fieldTypes[typeName] = 0;
                }
                fieldTypes[typeName]++;

                // Record field positions
                var fieldInfo = new Dictionary<string, object>
                {
                    ["Name"] = field.Name,
                    ["Type"] = typeName,
                    ["Bounds"] = new
                    {
                        field.Bounds.Left,
                        field.Bounds.Bottom,
                        field.Bounds.Right,
                        field.Bounds.Top
                    }
                };
                fieldPositions.Add(fieldInfo);

                context.Logger.Information(
                    "Field: {Name}, Type: {Type}, Position: ({Left}, {Bottom}, {Right}, {Top})",
                    field.Name, typeName,
                    field.Bounds.Left, field.Bounds.Bottom,
                    field.Bounds.Right, field.Bounds.Top
                );
            }

            // Record render start time
            var renderStart = DateTime.UtcNow;

            // Render the page with forms visible (requirement 4.3)
            context.Logger.Information("Rendering page {PageNumber} with forms", pageNumber);
            var renderResult = await renderingService.RenderPageAsync(document, pageNumber, zoomLevel: 1.0, dpi: 96);
            if (renderResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to render page {pageNumber}: {string.Join(", ", renderResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var renderTime = DateTime.UtcNow - renderStart;

            // Save the rendered image to a PNG file
            var outputPath = Path.Combine(context.WorkingDirectory, $"form_page_{pageNumber}.png");
            using (var imageStream = renderResult.Value)
            using (var fileStream = File.Create(outputPath))
            {
                await imageStream.CopyToAsync(fileStream);
            }

            context.Logger.Information("Saved form page {PageNumber} to: {OutputPath}", pageNumber, outputPath);

            // Get file size
            var fileInfo = new FileInfo(outputPath);
            var fileSizeKB = fileInfo.Length / 1024.0;

            // Load image to get dimensions
            int imageWidth, imageHeight;
            using (var image = await SixLabors.ImageSharp.Image.LoadAsync(outputPath))
            {
                imageWidth = image.Width;
                imageHeight = image.Height;
                context.Logger.Information("Image dimensions: {Width}x{Height}", imageWidth, imageHeight);
            }

            context.Logger.Information("File size: {FileSizeKB:F2} KB", fileSizeKB);
            context.Logger.Information("Render time: {RenderTimeMs} ms", renderTime.TotalMilliseconds);

            // Store outputs for verification
            result.Outputs["OutputFile"] = outputPath;
            result.Outputs["FieldCount"] = fieldCount;
            result.Outputs["FieldTypes"] = fieldTypes;
            result.Outputs["FieldPositions"] = fieldPositions;
            result.Outputs["ImageWidth"] = imageWidth;
            result.Outputs["ImageHeight"] = imageHeight;
            result.Outputs["FileSizeKB"] = fileSizeKB;
            result.Outputs["RenderTimeMs"] = renderTime.TotalMilliseconds;
            result.Outputs["PageNumber"] = pageNumber;
            result.Outputs["ExitCode"] = 0;

            result.Success = true;
            result.Duration = DateTime.UtcNow - startTime;

            context.Logger.Information(
                "Form field render test completed successfully. Found {FieldCount} fields, rendered {Width}x{Height}, {FileSizeKB:F2} KB, {RenderTimeMs:F0} ms",
                fieldCount, imageWidth, imageHeight, fileSizeKB, renderTime.TotalMilliseconds
            );
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Form field render test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the form field rendering test produced expected outputs.
    /// Checks that forms were detected (or gracefully reported as absent),
    /// field information was captured, and rendering succeeded if forms were present.
    /// </summary>
    /// <param name="result">The test result produced by RunAsync</param>
    /// <returns>True if all verification checks pass, false otherwise</returns>
    public Task<bool> VerifyAsync(CliTestResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        // Check basic success
        if (!result.Success)
        {
            return Task.FromResult(false);
        }

        // Verify field count was captured
        if (!result.Outputs.TryGetValue("FieldCount", out var fieldCountObj) ||
            fieldCountObj is not int fieldCount ||
            fieldCount < 0)
        {
            return Task.FromResult(false);
        }

        // If no forms were detected, verify the message is present
        if (fieldCount == 0)
        {
            if (!result.Outputs.TryGetValue("Message", out var messageObj) ||
                messageObj is not string message ||
                !message.Contains("No forms detected"))
            {
                return Task.FromResult(false);
            }

            // No forms is a valid success case (requirement 4.4)
            return Task.FromResult(true);
        }

        // If forms were detected, verify comprehensive output

        // Verify field types dictionary exists and is not empty
        if (!result.Outputs.TryGetValue("FieldTypes", out var fieldTypesObj) ||
            fieldTypesObj is not Dictionary<string, int> fieldTypes ||
            fieldTypes.Count == 0)
        {
            return Task.FromResult(false);
        }

        // Verify field positions list exists and has correct count
        if (!result.Outputs.TryGetValue("FieldPositions", out var fieldPositionsObj) ||
            fieldPositionsObj is not List<Dictionary<string, object>> fieldPositions ||
            fieldPositions.Count != fieldCount)
        {
            return Task.FromResult(false);
        }

        // Verify output file exists
        if (!result.Outputs.TryGetValue("OutputFile", out var outputFileObj) ||
            outputFileObj is not string outputFile ||
            !File.Exists(outputFile))
        {
            return Task.FromResult(false);
        }

        // Verify file size is reasonable (>1KB)
        if (!result.Outputs.TryGetValue("FileSizeKB", out var fileSizeObj) ||
            fileSizeObj is not double fileSizeKB ||
            fileSizeKB <= 1.0)
        {
            return Task.FromResult(false);
        }

        // Verify image has valid dimensions
        if (!result.Outputs.TryGetValue("ImageWidth", out var widthObj) ||
            widthObj is not int width ||
            width <= 0)
        {
            return Task.FromResult(false);
        }

        if (!result.Outputs.TryGetValue("ImageHeight", out var heightObj) ||
            heightObj is not int height ||
            height <= 0)
        {
            return Task.FromResult(false);
        }

        // Verify render time was captured
        if (!result.Outputs.TryGetValue("RenderTimeMs", out var renderTimeObj) ||
            renderTimeObj is not double renderTimeMs ||
            renderTimeMs < 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
