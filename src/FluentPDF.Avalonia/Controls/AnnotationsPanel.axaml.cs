using Avalonia.Controls;
using FluentPDF.Core.ViewModels;

namespace FluentPDF.Avalonia.Controls;

public partial class AnnotationsPanel : UserControl
{
    public AnnotationsPanel()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        var combo = this.FindControl<ComboBox>("FilterComboBox");
        if (combo != null)
        {
            combo.SelectionChanged += OnFilterSelectionChanged;
        }
    }

    protected override void OnUnloaded(global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var combo = this.FindControl<ComboBox>("FilterComboBox");
        if (combo != null)
        {
            combo.SelectionChanged -= OnFilterSelectionChanged;
        }
        base.OnUnloaded(e);
    }

    private void OnFilterSelectionChanged(
        object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not AnnotationsListViewModel vm) return;
        if (sender is not ComboBox combo) return;

        var selected = combo.SelectedItem as ComboBoxItem;
        vm.FilterType = selected?.Tag as string;
    }
}
