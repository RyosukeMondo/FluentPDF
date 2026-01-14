using System.Threading.Tasks;
using FluentPDF.App.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentPDF.App.Views
{
    /// <summary>
    /// Settings page for configuring application preferences.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        /// <summary>
        /// Gets the view model for this page.
        /// </summary>
        public SettingsViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsPage"/> class.
        /// </summary>
        public SettingsPage()
        {
            this.InitializeComponent();

            // Resolve ViewModel from DI container
            var app = (App)Microsoft.UI.Xaml.Application.Current;
            ViewModel = app.GetService<SettingsViewModel>();

            // Set DataContext for runtime binding
            this.DataContext = ViewModel;
        }

        /// <summary>
        /// Handles the reset button click with confirmation dialog.
        /// </summary>
        private async void ResetButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Reset Settings",
                Content = "This will reset all settings to their default values. Do you want to continue?",
                PrimaryButtonText = "Reset",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // User confirmed, proceed with reset
                await ViewModel.ResetToDefaultsCommand.ExecuteAsync(null);
            }
        }
    }
}
