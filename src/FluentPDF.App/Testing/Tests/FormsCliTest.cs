using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;

namespace FluentPDF.App.Testing.Tests;

/// <summary>
/// CLI test that verifies PDF form field functionality.
/// Tests filling text fields, checkboxes, and radio buttons, then verifies persistence.
/// </summary>
public sealed class FormsCliTest : ICliTest
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    public string Name => "forms";

    /// <summary>
    /// Gets a human-readable description of what this test verifies.
    /// </summary>
    public string Description => "Fills text fields, checkboxes, radio buttons, saves and reloads to verify persistence";

    /// <summary>
    /// Executes the forms test using the provided test context.
    /// Fills form fields, saves, and reloads to verify persistence.
    /// </summary>
    /// <param name="context">Isolated test execution environment with services and working directory</param>
    /// <returns>Test result containing form fill metrics and output file path</returns>
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
            context.Logger.Information("Starting PDF forms test");

            // Get required services
            var documentService = context.Services.GetRequiredService<IPdfDocumentService>();
            var formService = context.Services.GetRequiredService<IPdfFormService>();

            // Get test PDF path from context data or use default
            var inputPath = context.Data.TryGetValue("InputPath", out var pathObj) && pathObj is string path
                ? path
                : Path.GetFullPath(Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "..", "..", "..", "..", "..",
                    "tests", "Fixtures", "sample-form.pdf"
                ));

            context.Logger.Information("Loading form PDF: {FileName}", Path.GetFileName(inputPath));

            if (!File.Exists(inputPath))
            {
                result.ErrorMessage = $"Input PDF not found: {inputPath}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            // Load the PDF document
            var loadResult = await documentService.LoadDocumentAsync(inputPath);
            if (loadResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to load PDF: {string.Join(", ", loadResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            using var document = loadResult.Value;
            context.Logger.Information("Loaded PDF with {PageCount} pages", document.PageCount);

            // Extract form fields (page 0 = all pages)
            var fieldsResult = await formService.GetFormFieldsAsync(document, 0);
            if (fieldsResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to get form fields: {string.Join(", ", fieldsResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var fields = fieldsResult.Value;
            context.Logger.Information("Found {Count} form fields", fields.Count);

            // Track filled fields
            var textFieldsFilled = 0;
            var checkboxesFilled = 0;
            var radioButtonsFilled = 0;
            var operationStart = DateTime.UtcNow;

            // Fill text fields
            var textFields = fields.Where(f => f.Type == FormFieldType.Text).ToList();
            foreach (var field in textFields)
            {
                var fillResult = await formService.SetFieldValueAsync(field, $"Test Value {textFieldsFilled + 1}");
                if (fillResult.IsSuccess)
                {
                    textFieldsFilled++;
                    context.Logger.Debug("Filled text field: {FieldName}", field.Name);
                }
            }

            // Fill checkboxes
            var checkboxes = fields.Where(f => f.Type == FormFieldType.Checkbox).ToList();
            for (int i = 0; i < checkboxes.Count; i++)
            {
                var field = checkboxes[i];
                var checked_value = i % 2 == 0; // Alternate checked/unchecked
                var fillResult = await formService.SetFieldValueAsync(field, checked_value.ToString());
                if (fillResult.IsSuccess)
                {
                    checkboxesFilled++;
                    context.Logger.Debug("Filled checkbox: {FieldName} = {Value}", field.Name, checked_value);
                }
            }

            // Fill radio buttons (select first option in each group)
            var radioButtons = fields.Where(f => f.Type == FormFieldType.RadioButton)
                .GroupBy(f => f.Name)
                .ToList();

            foreach (var group in radioButtons)
            {
                var field = group.First();
                if (field.Options?.Any() == true)
                {
                    var fillResult = await formService.SetFieldValueAsync(field, field.Options[0]);
                    if (fillResult.IsSuccess)
                    {
                        radioButtonsFilled++;
                        context.Logger.Debug("Filled radio button: {FieldName} = {Value}", field.Name, field.Options[0]);
                    }
                }
            }

            var fillTime = DateTime.UtcNow - operationStart;

            context.Logger.Information(
                "Filled {Text} text fields, {Checkboxes} checkboxes, {Radio} radio button groups",
                textFieldsFilled,
                checkboxesFilled,
                radioButtonsFilled
            );

            // Save document
            var outputPath = Path.Combine(context.WorkingDirectory, "filled_form_output.pdf");
            var saveResult = await documentService.SaveDocumentAsync(document, outputPath);

            if (saveResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to save PDF: {string.Join(", ", saveResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            context.Logger.Information("Saved filled form to: {OutputPath}", outputPath);

            // Verify persistence: reload and check field values
            var verifyStart = DateTime.UtcNow;
            var reloadResult = await documentService.LoadDocumentAsync(outputPath);
            if (reloadResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to reload saved PDF: {string.Join(", ", reloadResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            using var reloadedDocument = reloadResult.Value;
            var reloadedFieldsResult = await formService.GetFormFieldsAsync(reloadedDocument, 0);
            if (reloadedFieldsResult.IsFailed)
            {
                result.ErrorMessage = $"Failed to get form fields from reloaded PDF: {string.Join(", ", reloadedFieldsResult.Errors.Select(e => e.Message))}";
                result.Duration = DateTime.UtcNow - startTime;
                return result;
            }

            var reloadedFields = reloadedFieldsResult.Value;
            var verifyTime = DateTime.UtcNow - verifyStart;

            // Verify text fields
            var textFieldsVerified = 0;
            foreach (var field in reloadedFields.Where(f => f.Type == FormFieldType.Text))
            {
                if (!string.IsNullOrEmpty(field.Value) && field.Value.StartsWith("Test Value"))
                {
                    textFieldsVerified++;
                }
            }

            // Verify checkboxes
            var checkboxesVerified = 0;
            foreach (var field in reloadedFields.Where(f => f.Type == FormFieldType.Checkbox))
            {
                if (!string.IsNullOrEmpty(field.Value))
                {
                    checkboxesVerified++;
                }
            }

            // Verify radio buttons
            var radioButtonsVerified = 0;
            foreach (var group in reloadedFields.Where(f => f.Type == FormFieldType.RadioButton).GroupBy(f => f.Name))
            {
                var field = group.First();
                if (!string.IsNullOrEmpty(field.Value))
                {
                    radioButtonsVerified++;
                }
            }

            context.Logger.Information(
                "Verified persistence: {Text}/{TotalText} text fields, {Checkboxes}/{TotalCheckboxes} checkboxes, {Radio}/{TotalRadio} radio buttons",
                textFieldsVerified, textFieldsFilled,
                checkboxesVerified, checkboxesFilled,
                radioButtonsVerified, radioButtonsFilled
            );

            // Check if persistence verification passed
            var persistenceVerified =
                textFieldsVerified == textFieldsFilled &&
                checkboxesVerified == checkboxesFilled &&
                radioButtonsVerified == radioButtonsFilled;

            // Create summary report
            var reportPath = Path.Combine(context.WorkingDirectory, "forms_report.txt");
            var report = new StringBuilder();
            report.AppendLine("PDF Forms Test Report");
            report.AppendLine($"Input File: {Path.GetFileName(inputPath)}");
            report.AppendLine($"Total Fields: {fields.Count}");
            report.AppendLine($"Text Fields Filled: {textFieldsFilled}");
            report.AppendLine($"Checkboxes Filled: {checkboxesFilled}");
            report.AppendLine($"Radio Button Groups Filled: {radioButtonsFilled}");
            report.AppendLine($"Fill Time: {fillTime.TotalMilliseconds:F2} ms");
            report.AppendLine($"Text Fields Verified: {textFieldsVerified}/{textFieldsFilled}");
            report.AppendLine($"Checkboxes Verified: {checkboxesVerified}/{checkboxesFilled}");
            report.AppendLine($"Radio Buttons Verified: {radioButtonsVerified}/{radioButtonsFilled}");
            report.AppendLine($"Persistence Verified: {persistenceVerified}");
            report.AppendLine($"Verify Time: {verifyTime.TotalMilliseconds:F2} ms");
            report.AppendLine($"Output: {outputPath}");

            await File.WriteAllTextAsync(reportPath, report.ToString());
            context.Logger.Information("Saved forms report to: {ReportPath}", reportPath);

            // Create detailed field data JSON
            var fieldDataPath = Path.Combine(context.WorkingDirectory, "form_fields.json");
            var fieldData = reloadedFields.Select(f => new
            {
                Name = f.Name,
                Type = f.Type.ToString(),
                Value = f.Value ?? "",
                PageNumber = f.PageNumber,
                IsReadOnly = f.IsReadOnly,
                IsRequired = f.IsRequired
            });

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(fieldData, jsonOptions);
            await File.WriteAllTextAsync(fieldDataPath, json);

            // Store outputs for verification
            result.Outputs["OutputFile"] = outputPath;
            result.Outputs["ReportFile"] = reportPath;
            result.Outputs["FieldDataFile"] = fieldDataPath;
            result.Outputs["TotalFields"] = fields.Count;
            result.Outputs["TextFieldsFilled"] = textFieldsFilled;
            result.Outputs["CheckboxesFilled"] = checkboxesFilled;
            result.Outputs["RadioButtonsFilled"] = radioButtonsFilled;
            result.Outputs["TextFieldsVerified"] = textFieldsVerified;
            result.Outputs["CheckboxesVerified"] = checkboxesVerified;
            result.Outputs["RadioButtonsVerified"] = radioButtonsVerified;
            result.Outputs["PersistenceVerified"] = persistenceVerified;
            result.Outputs["FillTimeMs"] = fillTime.TotalMilliseconds;
            result.Outputs["VerifyTimeMs"] = verifyTime.TotalMilliseconds;
            result.Outputs["ExitCode"] = persistenceVerified ? 0 : 1;

            result.Success = persistenceVerified;
            result.Duration = DateTime.UtcNow - startTime;

            if (persistenceVerified)
            {
                context.Logger.Information(
                    "Forms test completed successfully. Filled and verified {Total} fields, {FillTimeMs:F0} ms",
                    textFieldsFilled + checkboxesFilled + radioButtonsFilled,
                    fillTime.TotalMilliseconds
                );
            }
            else
            {
                result.ErrorMessage = "Persistence verification failed: not all filled fields were preserved after save/reload";
            }
        }
        catch (Exception ex)
        {
            context.Logger.Error(ex, "Forms test failed with exception");
            result.ErrorMessage = $"Exception during test execution: {ex.Message}";
            result.Duration = DateTime.UtcNow - startTime;
        }

        return result;
    }

    /// <summary>
    /// Verifies that the forms test produced expected outputs.
    /// </summary>
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

        // Verify output file exists
        if (!result.Outputs.TryGetValue("OutputFile", out var outputFileObj) ||
            outputFileObj is not string outputFile ||
            !File.Exists(outputFile))
        {
            return Task.FromResult(false);
        }

        // Verify persistence was verified
        if (!result.Outputs.TryGetValue("PersistenceVerified", out var persistenceObj) ||
            persistenceObj is not bool persistence ||
            !persistence)
        {
            return Task.FromResult(false);
        }

        // Verify at least some fields were filled
        if (!result.Outputs.TryGetValue("TotalFields", out var totalFieldsObj) ||
            totalFieldsObj is not int totalFields ||
            totalFields == 0)
        {
            return Task.FromResult(false);
        }

        // Verify field data JSON exists
        if (!result.Outputs.TryGetValue("FieldDataFile", out var fieldDataObj) ||
            fieldDataObj is not string fieldDataFile ||
            !File.Exists(fieldDataFile))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }
}
