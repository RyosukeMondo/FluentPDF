using Avalonia.Controls;
using Avalonia.Interactivity;
using FluentPDF.Avalonia.Helpers;

namespace FluentPDF.Avalonia.Dialogs;

public partial class WatermarkDialog : Window
{
    public WatermarkDialog()
    {
        InitializeComponent();
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e)
    {
        Close(new WatermarkDialogResult
        {
            Text = WatermarkText.Text ?? "WATERMARK",
            Opacity = (float)OpacitySlider.Value,
            FontSize = (float)(FontSizeInput.Value ?? 72),
            PositionIndex = PositionCombo.SelectedIndex
        });
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);
}
