using Avalonia.Controls;
using FluentPDF.Core.ViewModels;

namespace FluentPDF.Avalonia.Controls;

public partial class MetadataPanel : UserControl
{
    public MetadataPanel()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MetadataViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            UpdateEncryptedText(vm.IsEncrypted);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MetadataViewModel.IsEncrypted) && sender is MetadataViewModel vm)
        {
            UpdateEncryptedText(vm.IsEncrypted);
        }
    }

    private void UpdateEncryptedText(bool isEncrypted)
    {
        var textBlock = this.FindControl<TextBlock>("EncryptedText");
        if (textBlock != null)
        {
            textBlock.Text = isEncrypted ? "Yes" : "No";
        }
    }
}
