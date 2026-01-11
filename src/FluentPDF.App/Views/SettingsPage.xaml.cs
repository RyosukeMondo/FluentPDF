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
    }
}
