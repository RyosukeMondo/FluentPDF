using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FluentAssertions;

namespace FluentPDF.App.Tests.E2E;

/// <summary>
/// End-to-end tests for PDF form filling functionality using FlaUI.
/// Tests the complete user workflow from opening a form PDF to filling and saving.
/// </summary>
[Trait("Category", "E2E")]
public class FormFillingE2ETests : UITestBase
{
    private const string SampleFormPath = "../../../../Fixtures/sample-form.pdf";

    /// <summary>
    /// Tests the complete form filling workflow:
    /// 1. Open form PDF
    /// 2. Tab through form fields
    /// 3. Fill text fields
    /// 4. Check checkboxes
    /// 5. Select radio buttons
    /// 6. Attempt save with validation errors
    /// 7. Fix errors
    /// 8. Save successfully
    /// 9. Reopen and verify data persists
    /// </summary>
    [Fact(Skip = "E2E test requires built application - run manually")]
    public void CompleteFormFillingWorkflow_ShouldWork()
    {
        // Arrange: Get the executable path (assumes app is built)
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var executablePath = Path.Combine(
            solutionRoot,
            "src/FluentPDF.App/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/FluentPDF.App.exe"
        );

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException(
                $"FluentPDF.App.exe not found at {executablePath}. " +
                "Build the application with 'dotnet build src/FluentPDF.App -p:Platform=x64' first."
            );
        }

        var fullFormPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SampleFormPath));
        if (!File.Exists(fullFormPath))
        {
            throw new FileNotFoundException($"Sample form PDF not found at {fullFormPath}");
        }

        // Act & Assert: Launch application
        var mainWindow = LaunchApplication(executablePath, waitTimeoutMs: 10000);
        mainWindow.Should().NotBeNull("main window should launch");
        mainWindow.Title.Should().Be("FluentPDF", "main window should have correct title");

        try
        {
            // Step 1: Open the form PDF
            OpenFormPdf(mainWindow, fullFormPath);

            // Step 2: Wait for form fields to load
            Wait.UntilResponsive(mainWindow, TimeSpan.FromSeconds(5));

            // Step 3: Verify form fields are detected
            var formFieldsDetected = VerifyFormFieldsDetected(mainWindow);
            formFieldsDetected.Should().BeTrue("form fields should be detected");

            // Step 4: Tab through fields and verify navigation
            TestTabNavigation(mainWindow);

            // Step 5: Fill text fields
            FillTextFields(mainWindow);

            // Step 6: Check checkboxes
            CheckCheckboxes(mainWindow);

            // Step 7: Select radio buttons
            SelectRadioButtons(mainWindow);

            // Step 8: Attempt save with validation errors (leave required field empty)
            var validationErrorDisplayed = AttemptSaveWithErrors(mainWindow);
            validationErrorDisplayed.Should().BeTrue("validation error should be displayed");

            // Step 9: Fix validation errors
            FixValidationErrors(mainWindow);

            // Step 10: Save successfully
            SaveForm(mainWindow);

            // Step 11: Close and reopen to verify persistence
            var dataPath = Path.Combine(Path.GetTempPath(), "test-form-filled.pdf");
            CloseApplication();

            // Reopen and verify
            mainWindow = LaunchApplication(executablePath, waitTimeoutMs: 10000);
            OpenFormPdf(mainWindow, dataPath);

            // Step 12: Verify data persists
            VerifyDataPersists(mainWindow);
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Tests that form fields are correctly detected when opening a form PDF.
    /// </summary>
    [Fact(Skip = "E2E test requires built application - run manually")]
    public void OpenFormPdf_ShouldDetectFormFields()
    {
        // Arrange
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var executablePath = Path.Combine(
            solutionRoot,
            "src/FluentPDF.App/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/FluentPDF.App.exe"
        );

        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException($"FluentPDF.App.exe not found. Build the app first.");
        }

        var fullFormPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SampleFormPath));

        // Act
        var mainWindow = LaunchApplication(executablePath, waitTimeoutMs: 10000);

        try
        {
            OpenFormPdf(mainWindow, fullFormPath);
            Wait.UntilResponsive(mainWindow, TimeSpan.FromSeconds(5));

            // Assert
            var formFieldsDetected = VerifyFormFieldsDetected(mainWindow);
            formFieldsDetected.Should().BeTrue("form fields should be detected and displayed");
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Tests keyboard navigation through form fields using Tab and Shift+Tab.
    /// </summary>
    [Fact(Skip = "E2E test requires built application - run manually")]
    public void TabNavigation_ShouldMoveAcrossFields()
    {
        // Arrange
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var executablePath = Path.Combine(
            solutionRoot,
            "src/FluentPDF.App/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/FluentPDF.App.exe"
        );

        var fullFormPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SampleFormPath));

        // Act
        var mainWindow = LaunchApplication(executablePath, waitTimeoutMs: 10000);

        try
        {
            OpenFormPdf(mainWindow, fullFormPath);
            Wait.UntilResponsive(mainWindow, TimeSpan.FromSeconds(5));

            // Assert
            TestTabNavigation(mainWindow);
        }
        finally
        {
            CloseApplication();
        }
    }

    /// <summary>
    /// Tests form validation with required fields.
    /// </summary>
    [Fact(Skip = "E2E test requires built application - run manually")]
    public void SaveWithMissingRequiredFields_ShouldShowValidationError()
    {
        // Arrange
        var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var executablePath = Path.Combine(
            solutionRoot,
            "src/FluentPDF.App/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/FluentPDF.App.exe"
        );

        var fullFormPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SampleFormPath));

        // Act
        var mainWindow = LaunchApplication(executablePath, waitTimeoutMs: 10000);

        try
        {
            OpenFormPdf(mainWindow, fullFormPath);
            Wait.UntilResponsive(mainWindow, TimeSpan.FromSeconds(5));

            // Attempt to save without filling required fields
            var validationError = AttemptSaveWithErrors(mainWindow);

            // Assert
            validationError.Should().BeTrue("validation error should be displayed for missing required fields");
        }
        finally
        {
            CloseApplication();
        }
    }

    #region Helper Methods

    private void OpenFormPdf(Window mainWindow, string pdfPath)
    {
        // Find and click the Open button or use File menu
        var openButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByName("Open")));

        if (openButton != null)
        {
            openButton.Click();

            // Wait for file dialog
            var fileDialog = Retry.WhileNull(() =>
                Automation.GetDesktop().FindFirstChild(cf => cf.ByControlType(ControlType.Window)
                    .And(cf.ByName("Open"))),
                TimeSpan.FromSeconds(5)
            ).Result;

            if (fileDialog != null)
            {
                // Enter file path in the file name textbox
                var fileNameTextBox = fileDialog.FindFirstDescendant(cf =>
                    cf.ByControlType(ControlType.Edit).And(cf.ByName("File name:")));

                if (fileNameTextBox != null)
                {
                    fileNameTextBox.AsTextBox().Text = pdfPath;
                    Keyboard.Press(VirtualKeyShort.ENTER);
                }
            }
        }
        else
        {
            // Fallback: Use keyboard shortcut Ctrl+O
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_O);

            // Wait for file dialog and enter path
            var fileDialog = Retry.WhileNull(() =>
                Automation.GetDesktop().FindFirstChild(cf => cf.ByControlType(ControlType.Window)),
                TimeSpan.FromSeconds(5)
            ).Result;

            if (fileDialog != null)
            {
                Keyboard.Type(pdfPath);
                Keyboard.Press(VirtualKeyShort.ENTER);
            }
        }

        // Wait for PDF to load
        Wait.UntilResponsive(mainWindow, TimeSpan.FromSeconds(5));
    }

    private bool VerifyFormFieldsDetected(Window mainWindow)
    {
        // Look for form field controls (TextBox, CheckBox, etc.)
        var textFields = mainWindow.FindAllDescendants(cf =>
            cf.ByControlType(ControlType.Edit).And(cf.ByClassName("FormFieldControl")));

        var checkboxes = mainWindow.FindAllDescendants(cf =>
            cf.ByControlType(ControlType.CheckBox).And(cf.ByClassName("FormFieldControl")));

        var radioButtons = mainWindow.FindAllDescendants(cf =>
            cf.ByControlType(ControlType.RadioButton).And(cf.ByClassName("FormFieldControl")));

        // At least one form field should be detected
        return textFields.Length > 0 || checkboxes.Length > 0 || radioButtons.Length > 0;
    }

    private void TestTabNavigation(Window mainWindow)
    {
        // Get initial focused element
        var initialFocus = mainWindow.FocusedElement();

        // Press Tab
        Keyboard.Press(VirtualKeyShort.TAB);
        Wait.UntilResponsive(mainWindow, TimeSpan.FromMilliseconds(500));

        // Get new focused element
        var afterTabFocus = mainWindow.FocusedElement();

        // Focus should have changed
        afterTabFocus.Should().NotBe(initialFocus, "Tab should move focus to next field");

        // Press Shift+Tab to go back
        Keyboard.TypeSimultaneously(VirtualKeyShort.SHIFT, VirtualKeyShort.TAB);
        Wait.UntilResponsive(mainWindow, TimeSpan.FromMilliseconds(500));

        var afterShiftTabFocus = mainWindow.FocusedElement();

        // Should be back to original or previous field
        afterShiftTabFocus.Should().NotBe(afterTabFocus, "Shift+Tab should move focus to previous field");
    }

    private void FillTextFields(Window mainWindow)
    {
        // Find text fields (Edit controls)
        var textFields = mainWindow.FindAllDescendants(cf =>
            cf.ByControlType(ControlType.Edit).And(cf.ByClassName("FormFieldControl")));

        foreach (var field in textFields)
        {
            if (field.IsEnabled)
            {
                field.Focus();
                var textBox = field.AsTextBox();
                textBox.Text = "Test Value";
                Wait.UntilResponsive(mainWindow, TimeSpan.FromMilliseconds(200));
            }
        }
    }

    private void CheckCheckboxes(Window mainWindow)
    {
        // Find checkboxes
        var checkboxes = mainWindow.FindAllDescendants(cf =>
            cf.ByControlType(ControlType.CheckBox).And(cf.ByClassName("FormFieldControl")));

        foreach (var checkbox in checkboxes)
        {
            if (checkbox.IsEnabled)
            {
                checkbox.AsCheckBox().IsChecked = true;
                Wait.UntilResponsive(mainWindow, TimeSpan.FromMilliseconds(200));
            }
        }
    }

    private void SelectRadioButtons(Window mainWindow)
    {
        // Find radio buttons
        var radioButtons = mainWindow.FindAllDescendants(cf =>
            cf.ByControlType(ControlType.RadioButton).And(cf.ByClassName("FormFieldControl")));

        // Select the first radio button in each group
        var selectedGroups = new HashSet<string>();

        foreach (var radioButton in radioButtons)
        {
            if (radioButton.IsEnabled)
            {
                // Get group name (if available via automation properties)
                var groupName = radioButton.Name;

                if (!selectedGroups.Contains(groupName))
                {
                    radioButton.AsRadioButton().IsChecked = true;
                    selectedGroups.Add(groupName);
                    Wait.UntilResponsive(mainWindow, TimeSpan.FromMilliseconds(200));
                }
            }
        }
    }

    private bool AttemptSaveWithErrors(Window mainWindow)
    {
        // Find Save button
        var saveButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByName("Save")));

        if (saveButton != null)
        {
            saveButton.Click();
            Wait.UntilResponsive(mainWindow, TimeSpan.FromSeconds(2));

            // Look for validation error message (InfoBar)
            var errorMessage = mainWindow.FindFirstDescendant(cf =>
                cf.ByControlType(ControlType.Text).And(cf.ByName("ValidationErrorPanel")));

            return errorMessage != null;
        }

        // Fallback: Use Ctrl+S
        Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_S);
        Wait.UntilResponsive(mainWindow, TimeSpan.FromSeconds(2));

        // Check for validation error
        var error = mainWindow.FindFirstDescendant(cf =>
            cf.ByClassName("InfoBar"));

        return error != null;
    }

    private void FixValidationErrors(Window mainWindow)
    {
        // Find any empty required fields and fill them
        var textFields = mainWindow.FindAllDescendants(cf =>
            cf.ByControlType(ControlType.Edit).And(cf.ByClassName("FormFieldControl")));

        foreach (var field in textFields)
        {
            if (field.IsEnabled)
            {
                var textBox = field.AsTextBox();
                if (string.IsNullOrEmpty(textBox.Text))
                {
                    field.Focus();
                    textBox.Text = "Required Value";
                    Wait.UntilResponsive(mainWindow, TimeSpan.FromMilliseconds(200));
                }
            }
        }
    }

    private void SaveForm(Window mainWindow)
    {
        // Find Save button
        var saveButton = mainWindow.FindFirstDescendant(cf =>
            cf.ByControlType(ControlType.Button).And(cf.ByName("Save")));

        if (saveButton != null)
        {
            saveButton.Click();
        }
        else
        {
            // Fallback: Use Ctrl+S
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_S);
        }

        // Wait for save dialog
        Wait.UntilResponsive(mainWindow, TimeSpan.FromSeconds(2));

        // Handle save file dialog if it appears
        var saveDialog = Automation.GetDesktop().FindFirstChild(cf =>
            cf.ByControlType(ControlType.Window).And(cf.ByName("Save As")));

        if (saveDialog != null)
        {
            var fileNameTextBox = saveDialog.FindFirstDescendant(cf =>
                cf.ByControlType(ControlType.Edit).And(cf.ByName("File name:")));

            if (fileNameTextBox != null)
            {
                var savePath = Path.Combine(Path.GetTempPath(), "test-form-filled.pdf");
                fileNameTextBox.AsTextBox().Text = savePath;
                Keyboard.Press(VirtualKeyShort.ENTER);
            }
        }

        Wait.UntilResponsive(mainWindow, TimeSpan.FromSeconds(2));
    }

    private void VerifyDataPersists(Window mainWindow)
    {
        // Verify text fields contain saved values
        var textFields = mainWindow.FindAllDescendants(cf =>
            cf.ByControlType(ControlType.Edit).And(cf.ByClassName("FormFieldControl")));

        foreach (var field in textFields)
        {
            var textBox = field.AsTextBox();
            textBox.Text.Should().NotBeNullOrEmpty("saved data should persist");
        }

        // Verify checkboxes are checked
        var checkboxes = mainWindow.FindAllDescendants(cf =>
            cf.ByControlType(ControlType.CheckBox).And(cf.ByClassName("FormFieldControl")));

        foreach (var checkbox in checkboxes)
        {
            checkbox.AsCheckBox().IsChecked.Should().BeTrue("saved checkbox state should persist");
        }
    }

    #endregion
}
