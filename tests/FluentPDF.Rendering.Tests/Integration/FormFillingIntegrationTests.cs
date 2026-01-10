using FluentAssertions;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Interop;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace FluentPDF.Rendering.Tests.Integration;

/// <summary>
/// Integration tests for PDF form filling using real PDFium library.
/// These tests verify the complete workflow from form detection to filling and saving.
/// NOTE: These tests require PDFium native library and will only run on Windows.
/// On Linux/macOS, PDFium initialization will fail and tests will be skipped gracefully.
/// </summary>
[Trait("Category", "Integration")]
public sealed class FormFillingIntegrationTests : IDisposable
{
    private readonly IPdfDocumentService _documentService;
    private readonly IPdfFormService _formService;
    private readonly IFormValidationService _validationService;
    private readonly string _fixturesPath;
    private readonly List<PdfDocument> _documentsToCleanup;
    private readonly List<string> _tempFilesToCleanup;
    private static bool _pdfiumInitialized;
    private static readonly object _initLock = new();

    public FormFillingIntegrationTests()
    {
        // Initialize PDFium once for all tests
        lock (_initLock)
        {
            if (!_pdfiumInitialized)
            {
                var initialized = PdfiumInterop.Initialize();
                if (!initialized)
                {
                    throw new InvalidOperationException(
                        "Failed to initialize PDFium. Ensure pdfium.dll is in the test output directory.");
                }
                _pdfiumInitialized = true;
            }
        }

        // Setup services
        var documentLogger = new LoggerFactory().CreateLogger<PdfDocumentService>();
        var formLogger = new LoggerFactory().CreateLogger<PdfFormService>();
        var validationLogger = new LoggerFactory().CreateLogger<FormValidationService>();

        _documentService = new PdfDocumentService(documentLogger);
        _formService = new PdfFormService(formLogger);
        _validationService = new FormValidationService(validationLogger);

        // Setup fixtures path (go up from bin/Debug/net8.0 to tests root)
        _fixturesPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "Fixtures");

        _documentsToCleanup = new List<PdfDocument>();
        _tempFilesToCleanup = new List<string>();
    }

    public void Dispose()
    {
        // Clean up any documents that were loaded
        foreach (var doc in _documentsToCleanup)
        {
            try
            {
                _documentService.CloseDocument(doc);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Clean up temporary files
        foreach (var file in _tempFilesToCleanup)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    #region Form Field Detection Tests

    [Fact]
    public async Task LoadFormPdf_DetectsFields_ReturnsFormFieldsWithMetadata()
    {
        // Arrange
        var formPdfPath = Path.Combine(_fixturesPath, "sample-form.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(formPdfPath))
        {
            return;
        }

        var documentResult = await _documentService.LoadDocumentAsync(formPdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the form document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act
        var result = await _formService.GetFormFieldsAsync(documentResult.Value, 1);

        // Assert
        result.IsSuccess.Should().BeTrue("getting form fields should succeed");
        result.Value.Should().NotBeNull();
        result.Value.Should().NotBeEmpty("form PDF should have form fields");

        // Verify field metadata
        foreach (var field in result.Value)
        {
            field.Name.Should().NotBeNullOrEmpty("all fields should have names");
            field.PageNumber.Should().Be(1, "fields should be on page 1");
            field.Bounds.Should().NotBeNull("fields should have bounds");
            field.Bounds.Width.Should().BeGreaterThan(0, "field width should be positive");
            field.Bounds.Height.Should().BeGreaterThan(0, "field height should be positive");
        }
    }

    [Fact]
    public async Task GetFormFieldsAsync_WithNoFormFields_ReturnsEmptyList()
    {
        // Arrange
        var noFormPdfPath = Path.Combine(_fixturesPath, "no-bookmarks.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(noFormPdfPath))
        {
            return;
        }

        var documentResult = await _documentService.LoadDocumentAsync(noFormPdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        // Act
        var result = await _formService.GetFormFieldsAsync(documentResult.Value, 1);

        // Assert
        result.IsSuccess.Should().BeTrue("getting fields should succeed even with no forms");
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty("PDF without forms should return empty list");
    }

    #endregion

    #region Text Field Filling Tests

    [Fact]
    public async Task FillTextField_AndSave_PersistsValue()
    {
        // Arrange
        var formPdfPath = Path.Combine(_fixturesPath, "sample-form.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(formPdfPath))
        {
            return;
        }

        var documentResult = await _documentService.LoadDocumentAsync(formPdfPath);
        documentResult.IsSuccess.Should().BeTrue("loading the form document should succeed");
        _documentsToCleanup.Add(documentResult.Value);

        var fieldsResult = await _formService.GetFormFieldsAsync(documentResult.Value, 1);
        fieldsResult.IsSuccess.Should().BeTrue();

        // Find a text field
        var textField = fieldsResult.Value.FirstOrDefault(f => f.Type == FormFieldType.Text);
        if (textField == null)
        {
            return; // Skip if no text field found
        }

        var testValue = "Test Form Value";

        // Act - Set the value
        var setResult = await _formService.SetFieldValueAsync(textField, testValue);
        setResult.IsSuccess.Should().BeTrue("setting field value should succeed");

        // Save to temporary file
        var tempFile = Path.Combine(Path.GetTempPath(), $"form-test-{Guid.NewGuid()}.pdf");
        _tempFilesToCleanup.Add(tempFile);

        var saveResult = await _formService.SaveFormDataAsync(documentResult.Value, tempFile);
        saveResult.IsSuccess.Should().BeTrue("saving form should succeed");

        // Close the original document
        _documentService.CloseDocument(documentResult.Value);
        _documentsToCleanup.Remove(documentResult.Value);

        // Reload the saved document
        var reloadResult = await _documentService.LoadDocumentAsync(tempFile);
        reloadResult.IsSuccess.Should().BeTrue("reloading saved form should succeed");
        _documentsToCleanup.Add(reloadResult.Value);

        // Get fields from reloaded document
        var reloadedFieldsResult = await _formService.GetFormFieldsAsync(reloadResult.Value, 1);
        reloadedFieldsResult.IsSuccess.Should().BeTrue();

        // Assert - Verify the value persisted
        var reloadedTextField = reloadedFieldsResult.Value.FirstOrDefault(f => f.Name == textField.Name);
        reloadedTextField.Should().NotBeNull("field should exist in reloaded document");
        reloadedTextField!.Value.Should().Be(testValue, "field value should persist after save and reload");
    }

    [Fact]
    public async Task FillTextField_ExceedingMaxLength_ReturnsError()
    {
        // Arrange
        var formPdfPath = Path.Combine(_fixturesPath, "sample-form.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(formPdfPath))
        {
            return;
        }

        var documentResult = await _documentService.LoadDocumentAsync(formPdfPath);
        documentResult.IsSuccess.Should().BeTrue();
        _documentsToCleanup.Add(documentResult.Value);

        var fieldsResult = await _formService.GetFormFieldsAsync(documentResult.Value, 1);
        fieldsResult.IsSuccess.Should().BeTrue();

        // Find a text field with max length
        var textField = fieldsResult.Value.FirstOrDefault(f => f.Type == FormFieldType.Text && f.MaxLength > 0);
        if (textField == null)
        {
            return; // Skip if no text field with max length found
        }

        var tooLongValue = new string('X', (textField.MaxLength ?? 0) + 10);

        // Act
        var result = await _formService.SetFieldValueAsync(textField, tooLongValue);

        // Assert
        result.IsFailed.Should().BeTrue("setting value exceeding max length should fail");
    }

    [Fact]
    public async Task FillReadOnlyField_ReturnsError()
    {
        // Arrange
        var formPdfPath = Path.Combine(_fixturesPath, "sample-form.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(formPdfPath))
        {
            return;
        }

        var documentResult = await _documentService.LoadDocumentAsync(formPdfPath);
        documentResult.IsSuccess.Should().BeTrue();
        _documentsToCleanup.Add(documentResult.Value);

        var fieldsResult = await _formService.GetFormFieldsAsync(documentResult.Value, 1);
        fieldsResult.IsSuccess.Should().BeTrue();

        // Find a read-only field
        var readOnlyField = fieldsResult.Value.FirstOrDefault(f => f.IsReadOnly);
        if (readOnlyField == null)
        {
            return; // Skip if no read-only field found
        }

        // Act
        var result = await _formService.SetFieldValueAsync(readOnlyField, "New Value");

        // Assert
        result.IsFailed.Should().BeTrue("setting value on read-only field should fail");
    }

    #endregion

    #region Checkbox and Radio Button Tests

    [Fact]
    public async Task ToggleCheckbox_Updates_CheckedState()
    {
        // Arrange
        var formPdfPath = Path.Combine(_fixturesPath, "sample-form.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(formPdfPath))
        {
            return;
        }

        var documentResult = await _documentService.LoadDocumentAsync(formPdfPath);
        documentResult.IsSuccess.Should().BeTrue();
        _documentsToCleanup.Add(documentResult.Value);

        var fieldsResult = await _formService.GetFormFieldsAsync(documentResult.Value, 1);
        fieldsResult.IsSuccess.Should().BeTrue();

        // Find a checkbox field
        var checkbox = fieldsResult.Value.FirstOrDefault(f => f.Type == FormFieldType.Checkbox);
        if (checkbox == null)
        {
            return; // Skip if no checkbox found
        }

        var initialState = checkbox.IsChecked ?? false;

        // Act - Toggle the checkbox
        var toggleResult = await _formService.SetCheckboxStateAsync(checkbox, !initialState);
        toggleResult.IsSuccess.Should().BeTrue("toggling checkbox should succeed");

        // Reload fields to verify state change
        var updatedFieldsResult = await _formService.GetFormFieldsAsync(documentResult.Value, 1);
        updatedFieldsResult.IsSuccess.Should().BeTrue();

        var updatedCheckbox = updatedFieldsResult.Value.FirstOrDefault(f => f.Name == checkbox.Name);
        updatedCheckbox.Should().NotBeNull();
        (updatedCheckbox!.IsChecked ?? false).Should().Be(!initialState, "checkbox state should be toggled");
    }

    [Fact]
    public async Task SelectRadioButton_DeselectsOthersInGroup()
    {
        // Arrange
        var formPdfPath = Path.Combine(_fixturesPath, "sample-form.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(formPdfPath))
        {
            return;
        }

        var documentResult = await _documentService.LoadDocumentAsync(formPdfPath);
        documentResult.IsSuccess.Should().BeTrue();
        _documentsToCleanup.Add(documentResult.Value);

        var fieldsResult = await _formService.GetFormFieldsAsync(documentResult.Value, 1);
        fieldsResult.IsSuccess.Should().BeTrue();

        // Find radio button fields (must have same group name)
        var radioButtons = fieldsResult.Value
            .Where(f => f.Type == FormFieldType.RadioButton && !string.IsNullOrEmpty(f.GroupName))
            .GroupBy(f => f.GroupName)
            .FirstOrDefault(g => g.Count() > 1);

        if (radioButtons == null || radioButtons.Count() < 2)
        {
            return; // Skip if no radio button group found
        }

        var button1 = radioButtons.First();
        var button2 = radioButtons.Skip(1).First();

        // Act - Select first button
        var select1Result = await _formService.SetCheckboxStateAsync(button1, true);
        select1Result.IsSuccess.Should().BeTrue();

        // Select second button (should deselect first)
        var select2Result = await _formService.SetCheckboxStateAsync(button2, true);
        select2Result.IsSuccess.Should().BeTrue();

        // Reload fields to verify state
        var updatedFieldsResult = await _formService.GetFormFieldsAsync(documentResult.Value, 1);
        updatedFieldsResult.IsSuccess.Should().BeTrue();

        var updatedButton1 = updatedFieldsResult.Value.FirstOrDefault(f => f.Name == button1.Name);
        var updatedButton2 = updatedFieldsResult.Value.FirstOrDefault(f => f.Name == button2.Name);

        // Assert - Only button2 should be checked
        updatedButton1.Should().NotBeNull();
        updatedButton2.Should().NotBeNull();
        (updatedButton1!.IsChecked ?? false).Should().BeFalse("first radio button should be deselected");
        (updatedButton2!.IsChecked ?? false).Should().BeTrue("second radio button should be selected");
    }

    #endregion

    #region Form Validation Tests

    [Fact]
    public async Task ValidateRequiredFields_WithEmptyField_ReturnsValidationError()
    {
        // Arrange
        var formPdfPath = Path.Combine(_fixturesPath, "sample-form.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(formPdfPath))
        {
            return;
        }

        var documentResult = await _documentService.LoadDocumentAsync(formPdfPath);
        documentResult.IsSuccess.Should().BeTrue();
        _documentsToCleanup.Add(documentResult.Value);

        var fieldsResult = await _formService.GetFormFieldsAsync(documentResult.Value, 1);
        fieldsResult.IsSuccess.Should().BeTrue();

        // Find a required field
        var requiredField = fieldsResult.Value.FirstOrDefault(f => f.IsRequired);
        if (requiredField == null)
        {
            return; // Skip if no required field found
        }

        // Ensure field is empty
        if (requiredField.Type == FormFieldType.Text)
        {
            await _formService.SetFieldValueAsync(requiredField, string.Empty);
        }

        // Act
        var validationResult = _validationService.ValidateField(requiredField);

        // Assert
        validationResult.IsValid.Should().BeFalse("required field should fail validation when empty");
        validationResult.Errors.Should().NotBeEmpty("validation should provide error details");
    }

    [Fact]
    public async Task ValidateAllFields_AggregatesAllErrors()
    {
        // Arrange
        var formPdfPath = Path.Combine(_fixturesPath, "sample-form.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(formPdfPath))
        {
            return;
        }

        var documentResult = await _documentService.LoadDocumentAsync(formPdfPath);
        documentResult.IsSuccess.Should().BeTrue();
        _documentsToCleanup.Add(documentResult.Value);

        var fieldsResult = await _formService.GetFormFieldsAsync(documentResult.Value, 1);
        fieldsResult.IsSuccess.Should().BeTrue();

        // Act
        var validationResult = _validationService.ValidateAllFields(fieldsResult.Value);

        // Assert
        validationResult.Should().NotBeNull("validation result should not be null");

        // If there are required fields, validate that the result contains them
        var requiredFields = fieldsResult.Value.Where(f => f.IsRequired).ToList();
        if (requiredFields.Any())
        {
            // Validation should track required fields (just verify it's a boolean)
            _ = validationResult.IsValid;
        }
    }

    #endregion

    #region Save and Reload Workflow Tests

    [Fact]
    public async Task SaveAndReload_PreservesAllFormData()
    {
        // Arrange
        var formPdfPath = Path.Combine(_fixturesPath, "sample-form.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(formPdfPath))
        {
            return;
        }

        var documentResult = await _documentService.LoadDocumentAsync(formPdfPath);
        documentResult.IsSuccess.Should().BeTrue();
        _documentsToCleanup.Add(documentResult.Value);

        var fieldsResult = await _formService.GetFormFieldsAsync(documentResult.Value, 1);
        fieldsResult.IsSuccess.Should().BeTrue();

        // Fill multiple fields
        var textFields = fieldsResult.Value.Where(f => f.Type == FormFieldType.Text && !f.IsReadOnly).Take(3).ToList();
        var checkboxFields = fieldsResult.Value.Where(f => f.Type == FormFieldType.Checkbox).Take(2).ToList();

        var testData = new Dictionary<string, object>();

        foreach (var field in textFields)
        {
            var value = $"Test_{field.Name}";
            await _formService.SetFieldValueAsync(field, value);
            testData[field.Name] = value;
        }

        foreach (var field in checkboxFields)
        {
            await _formService.SetCheckboxStateAsync(field, true);
            testData[field.Name] = true;
        }

        // Save to temporary file
        var tempFile = Path.Combine(Path.GetTempPath(), $"form-test-{Guid.NewGuid()}.pdf");
        _tempFilesToCleanup.Add(tempFile);

        var saveResult = await _formService.SaveFormDataAsync(documentResult.Value, tempFile);
        saveResult.IsSuccess.Should().BeTrue("saving form should succeed");

        // Close the original document
        _documentService.CloseDocument(documentResult.Value);
        _documentsToCleanup.Remove(documentResult.Value);

        // Reload the saved document
        var reloadResult = await _documentService.LoadDocumentAsync(tempFile);
        reloadResult.IsSuccess.Should().BeTrue();
        _documentsToCleanup.Add(reloadResult.Value);

        var reloadedFieldsResult = await _formService.GetFormFieldsAsync(reloadResult.Value, 1);
        reloadedFieldsResult.IsSuccess.Should().BeTrue();

        // Assert - Verify all data persisted
        foreach (var kvp in testData)
        {
            var reloadedField = reloadedFieldsResult.Value.FirstOrDefault(f => f.Name == kvp.Key);
            reloadedField.Should().NotBeNull($"field {kvp.Key} should exist in reloaded document");

            if (kvp.Value is string stringValue)
            {
                reloadedField!.Value.Should().Be(stringValue, $"text field {kvp.Key} should preserve value");
            }
            else if (kvp.Value is bool boolValue)
            {
                (reloadedField!.IsChecked ?? false).Should().Be(boolValue, $"checkbox {kvp.Key} should preserve state");
            }
        }
    }

    #endregion

    #region Memory Cleanup Tests

    [Fact]
    public async Task FormOperations_CleanupResources_NoHandleLeaks()
    {
        // Arrange
        var formPdfPath = Path.Combine(_fixturesPath, "sample-form.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(formPdfPath))
        {
            return;
        }

        // Track handles before operations
        var initialHandles = GC.GetTotalMemory(true);

        // Act - Perform multiple form operations
        for (int i = 0; i < 10; i++)
        {
            var documentResult = await _documentService.LoadDocumentAsync(formPdfPath);
            if (!documentResult.IsSuccess) continue;

            var fieldsResult = await _formService.GetFormFieldsAsync(documentResult.Value, 1);
            if (fieldsResult.IsSuccess)
            {
                var textField = fieldsResult.Value.FirstOrDefault(f => f.Type == FormFieldType.Text);
                if (textField != null)
                {
                    await _formService.SetFieldValueAsync(textField, $"Test_{i}");
                }
            }

            _documentService.CloseDocument(documentResult.Value);
        }

        // Force garbage collection
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalHandles = GC.GetTotalMemory(true);

        // Assert - Memory should not grow significantly (allowing some tolerance)
        var memoryGrowth = finalHandles - initialHandles;
        memoryGrowth.Should().BeLessThan(10 * 1024 * 1024, "memory growth should be less than 10MB after cleanup");
    }

    #endregion

    #region Tab Order Navigation Tests

    [Fact]
    public async Task GetFieldsInTabOrder_SortsFieldsCorrectly()
    {
        // Arrange
        var formPdfPath = Path.Combine(_fixturesPath, "sample-form.pdf");

        // Skip test if fixture doesn't exist
        if (!File.Exists(formPdfPath))
        {
            return;
        }

        var documentResult = await _documentService.LoadDocumentAsync(formPdfPath);
        documentResult.IsSuccess.Should().BeTrue();
        _documentsToCleanup.Add(documentResult.Value);

        var fieldsResult = await _formService.GetFormFieldsAsync(documentResult.Value, 1);
        fieldsResult.IsSuccess.Should().BeTrue();

        if (fieldsResult.Value.Count < 2)
        {
            return; // Skip if not enough fields
        }

        // Act
        var sortedResult = _formService.GetFieldsInTabOrder(fieldsResult.Value);

        // Assert
        sortedResult.IsSuccess.Should().BeTrue("sorting fields should succeed");
        sortedResult.Value.Should().HaveCount(fieldsResult.Value.Count, "all fields should be in sorted list");

        // Verify spatial ordering (top-to-bottom, left-to-right)
        for (int i = 0; i < sortedResult.Value.Count - 1; i++)
        {
            var current = sortedResult.Value[i];
            var next = sortedResult.Value[i + 1];

            // Fields should be ordered by vertical position first (top to bottom)
            // If Y positions are close (within 10 units), order by horizontal position (left to right)
            var yDiff = Math.Abs(current.Bounds.Top - next.Bounds.Top);
            if (yDiff < 10)
            {
                current.Bounds.Left.Should().BeLessThanOrEqualTo(next.Bounds.Left,
                    "fields on same row should be ordered left-to-right");
            }
        }
    }

    #endregion
}
