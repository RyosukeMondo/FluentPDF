using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace FluentPDF.App.Views
{
    /// <summary>
    /// Dialog that prompts the user to confirm page deletion.
    /// </summary>
    public sealed partial class DeletePagesDialog : ContentDialog
    {
        private bool _confirmed = false;

        /// <summary>
        /// Initializes a new instance of the DeletePagesDialog class.
        /// </summary>
        /// <param name="pageCount">The number of pages to delete.</param>
        /// <param name="totalPages">The total number of pages in the document.</param>
        private DeletePagesDialog(int pageCount, int totalPages)
        {
            this.InitializeComponent();

            // Check if attempting to delete all pages
            if (pageCount >= totalPages)
            {
                Title = "Cannot Delete Pages";
                MessageTextBlock.Text = "Cannot delete all pages from the document. At least one page must remain.";
                PrimaryButtonText = "";
                CloseButtonText = "OK";
                Log.Warning("User attempted to delete all {TotalPages} pages from document", totalPages);
            }
            else
            {
                MessageTextBlock.Text = $"Delete {pageCount} page{(pageCount > 1 ? "s" : "")}?";
                Log.Debug("DeletePagesDialog displayed for {PageCount} page(s)", pageCount);

                // Handle primary button click to set confirmed flag
                this.PrimaryButtonClick += (sender, args) =>
                {
                    _confirmed = true;
                    Log.Debug("User confirmed deletion of {PageCount} page(s)", pageCount);
                };

                this.CloseButtonClick += (sender, args) =>
                {
                    _confirmed = false;
                    Log.Debug("User cancelled page deletion");
                };
            }
        }

        /// <summary>
        /// Shows the delete confirmation dialog and returns whether the user confirmed.
        /// </summary>
        /// <param name="xamlRoot">The XamlRoot for proper dialog hosting.</param>
        /// <param name="pageCount">The number of pages to delete.</param>
        /// <param name="totalPages">The total number of pages in the document.</param>
        /// <returns>True if the user confirmed deletion, false otherwise.</returns>
        public static async Task<bool> ShowAsync(XamlRoot xamlRoot, int pageCount, int totalPages)
        {
            var dialog = new DeletePagesDialog(pageCount, totalPages)
            {
                XamlRoot = xamlRoot
            };

            await dialog.ShowAsync();
            return dialog._confirmed;
        }
    }
}
