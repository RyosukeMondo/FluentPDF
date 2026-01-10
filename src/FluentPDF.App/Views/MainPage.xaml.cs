using FluentPDF.App.ViewModels;

namespace FluentPDF.App.Views
{
    /// <summary>
    /// Main page demonstrating MVVM pattern with data binding.
    /// </summary>
    public partial class MainPage : Page
    {
        /// <summary>
        /// Gets the view model for this page.
        /// </summary>
        public MainViewModel ViewModel { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();

            // Resolve ViewModel from DI container
            var app = (App)Application.Current;
            ViewModel = app.GetService<MainViewModel>();

            // Set DataContext for runtime binding (x:Bind doesn't need this, but good practice)
            this.DataContext = ViewModel;
        }
    }
}
