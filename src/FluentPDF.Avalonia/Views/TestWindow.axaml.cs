using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;

namespace FluentPDF.Avalonia.Views;

public partial class TestWindow : Window
{
    public TestWindow()
    {
        try
        {
            Console.WriteLine("TestWindow constructor called");
            InitializeComponent();
            Console.WriteLine("TestWindow InitializeComponent complete");

            // Ensure window properties
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            CanResize = true;

            Console.WriteLine("TestWindow constructor finished");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TestWindow constructor failed: {ex.Message}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
            throw;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
