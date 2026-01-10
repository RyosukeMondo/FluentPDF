using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace FluentPDF.App.Views
{
    /// <summary>
    /// Error dialog that displays user-friendly error messages with correlation IDs.
    /// </summary>
    public sealed partial class ErrorDialog : ContentDialog
    {
        /// <summary>
        /// Initializes a new instance of the ErrorDialog class.
        /// </summary>
        /// <param name="message">The user-friendly error message to display.</param>
        /// <param name="correlationId">The correlation ID for support tracking.</param>
        public ErrorDialog(string message, string correlationId)
        {
            this.InitializeComponent();

            MessageTextBlock.Text = message;
            CorrelationIdTextBlock.Text = correlationId;

            Log.Debug("ErrorDialog displayed with CorrelationId: {CorrelationId}", correlationId);

            // Log when user closes the dialog
            this.PrimaryButtonClick += (sender, args) =>
            {
                Log.Debug("ErrorDialog closed by user. CorrelationId: {CorrelationId}", correlationId);
            };
        }
    }
}
