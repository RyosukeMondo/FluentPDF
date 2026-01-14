using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Windows.Input;

namespace FluentPDF.App.Controls;

/// <summary>
/// Custom WinUI control for displaying real-time diagnostics overlay.
/// Shows FPS, memory usage, render times, and provides export/log viewing actions.
/// </summary>
public sealed partial class DiagnosticsPanelControl : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticsPanelControl"/> class.
    /// </summary>
    public DiagnosticsPanelControl()
    {
        InitializeComponent();
    }

    #region Dependency Properties

    /// <summary>
    /// Gets or sets the current FPS value.
    /// </summary>
    public double CurrentFPS
    {
        get => (double)GetValue(CurrentFPSProperty);
        set => SetValue(CurrentFPSProperty, value);
    }

    public static readonly DependencyProperty CurrentFPSProperty =
        DependencyProperty.Register(
            nameof(CurrentFPS),
            typeof(double),
            typeof(DiagnosticsPanelControl),
            new PropertyMetadata(0.0));

    /// <summary>
    /// Gets or sets the managed memory in MB.
    /// </summary>
    public long ManagedMemoryMB
    {
        get => (long)GetValue(ManagedMemoryMBProperty);
        set => SetValue(ManagedMemoryMBProperty, value);
    }

    public static readonly DependencyProperty ManagedMemoryMBProperty =
        DependencyProperty.Register(
            nameof(ManagedMemoryMB),
            typeof(long),
            typeof(DiagnosticsPanelControl),
            new PropertyMetadata(0L));

    /// <summary>
    /// Gets or sets the native memory in MB.
    /// </summary>
    public long NativeMemoryMB
    {
        get => (long)GetValue(NativeMemoryMBProperty);
        set => SetValue(NativeMemoryMBProperty, value);
    }

    public static readonly DependencyProperty NativeMemoryMBProperty =
        DependencyProperty.Register(
            nameof(NativeMemoryMB),
            typeof(long),
            typeof(DiagnosticsPanelControl),
            new PropertyMetadata(0L));

    /// <summary>
    /// Gets or sets the total memory in MB (managed + native).
    /// </summary>
    public long TotalMemoryMB
    {
        get => (long)GetValue(TotalMemoryMBProperty);
        set => SetValue(TotalMemoryMBProperty, value);
    }

    public static readonly DependencyProperty TotalMemoryMBProperty =
        DependencyProperty.Register(
            nameof(TotalMemoryMB),
            typeof(long),
            typeof(DiagnosticsPanelControl),
            new PropertyMetadata(0L));

    /// <summary>
    /// Gets or sets the last render time in milliseconds.
    /// </summary>
    public double LastRenderTimeMs
    {
        get => (double)GetValue(LastRenderTimeMsProperty);
        set => SetValue(LastRenderTimeMsProperty, value);
    }

    public static readonly DependencyProperty LastRenderTimeMsProperty =
        DependencyProperty.Register(
            nameof(LastRenderTimeMs),
            typeof(double),
            typeof(DiagnosticsPanelControl),
            new PropertyMetadata(0.0));

    /// <summary>
    /// Gets or sets the current page number.
    /// </summary>
    public int CurrentPageNumber
    {
        get => (int)GetValue(CurrentPageNumberProperty);
        set => SetValue(CurrentPageNumberProperty, value);
    }

    public static readonly DependencyProperty CurrentPageNumberProperty =
        DependencyProperty.Register(
            nameof(CurrentPageNumber),
            typeof(int),
            typeof(DiagnosticsPanelControl),
            new PropertyMetadata(0));

    /// <summary>
    /// Gets or sets the FPS color based on performance level.
    /// </summary>
    public SolidColorBrush FPSColor
    {
        get => (SolidColorBrush)GetValue(FPSColorProperty);
        set => SetValue(FPSColorProperty, value);
    }

    public static readonly DependencyProperty FPSColorProperty =
        DependencyProperty.Register(
            nameof(FPSColor),
            typeof(SolidColorBrush),
            typeof(DiagnosticsPanelControl),
            new PropertyMetadata(new SolidColorBrush(Colors.Green)));

    /// <summary>
    /// Gets or sets whether the diagnostics panel is visible.
    /// </summary>
    public bool IsVisible
    {
        get => (bool)GetValue(IsVisibleProperty);
        set => SetValue(IsVisibleProperty, value);
    }

    public static readonly DependencyProperty IsVisibleProperty =
        DependencyProperty.Register(
            nameof(IsVisible),
            typeof(bool),
            typeof(DiagnosticsPanelControl),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets or sets the command to export metrics.
    /// </summary>
    public ICommand? ExportMetricsCommand
    {
        get => (ICommand?)GetValue(ExportMetricsCommandProperty);
        set => SetValue(ExportMetricsCommandProperty, value);
    }

    public static readonly DependencyProperty ExportMetricsCommandProperty =
        DependencyProperty.Register(
            nameof(ExportMetricsCommand),
            typeof(ICommand),
            typeof(DiagnosticsPanelControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the command to open the log viewer.
    /// </summary>
    public ICommand? OpenLogViewerCommand
    {
        get => (ICommand?)GetValue(OpenLogViewerCommandProperty);
        set => SetValue(OpenLogViewerCommandProperty, value);
    }

    public static readonly DependencyProperty OpenLogViewerCommandProperty =
        DependencyProperty.Register(
            nameof(OpenLogViewerCommand),
            typeof(ICommand),
            typeof(DiagnosticsPanelControl),
            new PropertyMetadata(null));

    #endregion
}
