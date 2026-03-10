using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Windows.Input;

namespace FluentPDF.Avalonia.Controls;

/// <summary>
/// Overlay control for long-running operations (merge, export, watermark).
/// Shows a determinate/indeterminate progress bar, operation description,
/// and a cancel button.
/// </summary>
public partial class ProgressOverlay : UserControl
{
    /// <summary>
    /// Defines the OperationDescription property.
    /// </summary>
    public static readonly StyledProperty<string> OperationDescriptionProperty =
        AvaloniaProperty.Register<ProgressOverlay, string>(
            nameof(OperationDescription), "Processing...");

    /// <summary>
    /// Defines the Progress property (0-100). When negative, the bar is indeterminate.
    /// </summary>
    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<ProgressOverlay, double>(
            nameof(Progress), -1.0);

    /// <summary>
    /// Defines the CancelCommand property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> CancelCommandProperty =
        AvaloniaProperty.Register<ProgressOverlay, ICommand?>(nameof(CancelCommand));

    /// <summary>
    /// Gets or sets the description of the current operation.
    /// </summary>
    public string OperationDescription
    {
        get => GetValue(OperationDescriptionProperty);
        set => SetValue(OperationDescriptionProperty, value);
    }

    /// <summary>
    /// Gets or sets the progress value (0-100).
    /// A negative value makes the progress bar indeterminate.
    /// </summary>
    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    /// <summary>
    /// Gets or sets the command invoked when Cancel is clicked.
    /// </summary>
    public ICommand? CancelCommand
    {
        get => GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public ProgressOverlay()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        SyncProgressState();
        SyncDescription();
        WireCancelButton();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ProgressProperty)
        {
            SyncProgressState();
        }
        else if (change.Property == OperationDescriptionProperty)
        {
            SyncDescription();
        }
        else if (change.Property == CancelCommandProperty)
        {
            WireCancelButton();
        }
    }

    private void SyncProgressState()
    {
        var bar = this.FindControl<ProgressBar>("OperationProgressBar");
        var pctText = this.FindControl<TextBlock>("PercentageText");
        if (bar == null) return;

        if (Progress < 0)
        {
            bar.IsIndeterminate = true;
            if (pctText != null) pctText.Text = string.Empty;
        }
        else
        {
            bar.IsIndeterminate = false;
            bar.Value = Math.Clamp(Progress, 0, 100);
            if (pctText != null)
                pctText.Text = $"{(int)Math.Clamp(Progress, 0, 100)}%";
        }
    }

    private void SyncDescription()
    {
        var desc = this.FindControl<TextBlock>("DescriptionText");
        if (desc != null)
            desc.Text = OperationDescription;
    }

    private void WireCancelButton()
    {
        var btn = this.FindControl<Button>("CancelButton");
        if (btn != null)
            btn.Command = CancelCommand;
    }
}
