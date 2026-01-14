using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace FluentPDF.App.Views
{
    /// <summary>
    /// Result of save confirmation dialog.
    /// </summary>
    public enum SaveConfirmationResult
    {
        /// <summary>User chose to save the document.</summary>
        Save,

        /// <summary>User chose not to save the document.</summary>
        DontSave,

        /// <summary>User cancelled the operation.</summary>
        Cancel
    }

    /// <summary>
    /// Dialog that prompts the user to save, discard, or cancel closing a document with unsaved changes.
    /// </summary>
    public sealed partial class SaveConfirmationDialog : ContentDialog
    {
        private SaveConfirmationResult _result = SaveConfirmationResult.Cancel;

        /// <summary>
        /// Initializes a new instance of the SaveConfirmationDialog class.
        /// </summary>
        /// <param name="filename">The name of the file with unsaved changes.</param>
        public SaveConfirmationDialog(string filename)
        {
            this.InitializeComponent();

            MessageTextBlock.Text = $"Do you want to save changes to {filename}?";

            Log.Debug("SaveConfirmationDialog displayed for file: {Filename}", filename);

            // Handle button clicks to determine result
            this.PrimaryButtonClick += (sender, args) =>
            {
                _result = SaveConfirmationResult.Save;
                Log.Debug("User chose to save changes to {Filename}", filename);
            };

            this.SecondaryButtonClick += (sender, args) =>
            {
                _result = SaveConfirmationResult.DontSave;
                Log.Debug("User chose not to save changes to {Filename}", filename);
            };

            this.CloseButtonClick += (sender, args) =>
            {
                _result = SaveConfirmationResult.Cancel;
                Log.Debug("User cancelled closing {Filename}", filename);
            };
        }

        /// <summary>
        /// Shows the save confirmation dialog and returns the user's choice.
        /// </summary>
        /// <param name="filename">The name of the file with unsaved changes.</param>
        /// <param name="xamlRoot">The XamlRoot for proper dialog hosting.</param>
        /// <returns>The user's choice (Save, DontSave, or Cancel).</returns>
        public static async Task<SaveConfirmationResult> ShowAsync(string filename, XamlRoot xamlRoot)
        {
            var dialog = new SaveConfirmationDialog(filename)
            {
                XamlRoot = xamlRoot
            };

            await dialog.ShowAsync();
            return dialog._result;
        }
    }
}
