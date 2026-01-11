using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using FluentPDF.Core.Observability;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentPDF.App.Controls;

/// <summary>
/// Custom WinUI control for structured log viewer with comprehensive filtering.
/// </summary>
public sealed partial class LogViewerControl : UserControl
{
    public LogViewerControl()
    {
        InitializeComponent();
    }

    #region Dependency Properties

    /// <summary>
    /// Gets or sets the collection of log entries to display.
    /// </summary>
    public ObservableCollection<LogEntry> LogEntries
    {
        get => (ObservableCollection<LogEntry>)GetValue(LogEntriesProperty);
        set => SetValue(LogEntriesProperty, value);
    }

    public static readonly DependencyProperty LogEntriesProperty =
        DependencyProperty.Register(
            nameof(LogEntries),
            typeof(ObservableCollection<LogEntry>),
            typeof(LogViewerControl),
            new PropertyMetadata(new ObservableCollection<LogEntry>()));

    /// <summary>
    /// Gets or sets the currently selected log entry.
    /// </summary>
    public LogEntry? SelectedLogEntry
    {
        get => (LogEntry?)GetValue(SelectedLogEntryProperty);
        set => SetValue(SelectedLogEntryProperty, value);
    }

    public static readonly DependencyProperty SelectedLogEntryProperty =
        DependencyProperty.Register(
            nameof(SelectedLogEntry),
            typeof(LogEntry),
            typeof(LogViewerControl),
            new PropertyMetadata(null, OnSelectedLogEntryChanged));

    private static void OnSelectedLogEntryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LogViewerControl control)
        {
            control.UpdateSelectedLogDetails();
        }
    }

    /// <summary>
    /// Gets or sets the available log levels for filtering.
    /// </summary>
    public ObservableCollection<LogLevel> LogLevels
    {
        get => (ObservableCollection<LogLevel>)GetValue(LogLevelsProperty);
        set => SetValue(LogLevelsProperty, value);
    }

    public static readonly DependencyProperty LogLevelsProperty =
        DependencyProperty.Register(
            nameof(LogLevels),
            typeof(ObservableCollection<LogLevel>),
            typeof(LogViewerControl),
            new PropertyMetadata(new ObservableCollection<LogLevel>(Enum.GetValues<LogLevel>())));

    /// <summary>
    /// Gets or sets the minimum log level filter.
    /// </summary>
    public LogLevel? MinimumLevel
    {
        get => (LogLevel?)GetValue(MinimumLevelProperty);
        set => SetValue(MinimumLevelProperty, value);
    }

    public static readonly DependencyProperty MinimumLevelProperty =
        DependencyProperty.Register(
            nameof(MinimumLevel),
            typeof(LogLevel?),
            typeof(LogViewerControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the correlation ID filter.
    /// </summary>
    public string CorrelationIdFilter
    {
        get => (string)GetValue(CorrelationIdFilterProperty);
        set => SetValue(CorrelationIdFilterProperty, value);
    }

    public static readonly DependencyProperty CorrelationIdFilterProperty =
        DependencyProperty.Register(
            nameof(CorrelationIdFilter),
            typeof(string),
            typeof(LogViewerControl),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the component filter.
    /// </summary>
    public string ComponentFilter
    {
        get => (string)GetValue(ComponentFilterProperty);
        set => SetValue(ComponentFilterProperty, value);
    }

    public static readonly DependencyProperty ComponentFilterProperty =
        DependencyProperty.Register(
            nameof(ComponentFilter),
            typeof(string),
            typeof(LogViewerControl),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the search text filter.
    /// </summary>
    public string SearchText
    {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    public static readonly DependencyProperty SearchTextProperty =
        DependencyProperty.Register(
            nameof(SearchText),
            typeof(string),
            typeof(LogViewerControl),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the start time filter.
    /// </summary>
    public DateTimeOffset? StartTime
    {
        get => (DateTimeOffset?)GetValue(StartTimeProperty);
        set => SetValue(StartTimeProperty, value);
    }

    public static readonly DependencyProperty StartTimeProperty =
        DependencyProperty.Register(
            nameof(StartTime),
            typeof(DateTimeOffset?),
            typeof(LogViewerControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the end time filter.
    /// </summary>
    public DateTimeOffset? EndTime
    {
        get => (DateTimeOffset?)GetValue(EndTimeProperty);
        set => SetValue(EndTimeProperty, value);
    }

    public static readonly DependencyProperty EndTimeProperty =
        DependencyProperty.Register(
            nameof(EndTime),
            typeof(DateTimeOffset?),
            typeof(LogViewerControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets whether the details panel is expanded.
    /// </summary>
    public bool IsDetailsExpanded
    {
        get => (bool)GetValue(IsDetailsExpandedProperty);
        set => SetValue(IsDetailsExpandedProperty, value);
    }

    public static readonly DependencyProperty IsDetailsExpandedProperty =
        DependencyProperty.Register(
            nameof(IsDetailsExpanded),
            typeof(bool),
            typeof(LogViewerControl),
            new PropertyMetadata(true));

    /// <summary>
    /// Gets whether a log is currently selected.
    /// </summary>
    public bool HasSelectedLog
    {
        get => (bool)GetValue(HasSelectedLogProperty);
        private set => SetValue(HasSelectedLogProperty, value);
    }

    public static readonly DependencyProperty HasSelectedLogProperty =
        DependencyProperty.Register(
            nameof(HasSelectedLog),
            typeof(bool),
            typeof(LogViewerControl),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets whether the selected log has an exception.
    /// </summary>
    public bool HasException
    {
        get => (bool)GetValue(HasExceptionProperty);
        private set => SetValue(HasExceptionProperty, value);
    }

    public static readonly DependencyProperty HasExceptionProperty =
        DependencyProperty.Register(
            nameof(HasException),
            typeof(bool),
            typeof(LogViewerControl),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets whether the selected log has a stack trace.
    /// </summary>
    public bool HasStackTrace
    {
        get => (bool)GetValue(HasStackTraceProperty);
        private set => SetValue(HasStackTraceProperty, value);
    }

    public static readonly DependencyProperty HasStackTraceProperty =
        DependencyProperty.Register(
            nameof(HasStackTrace),
            typeof(bool),
            typeof(LogViewerControl),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets whether the selected log has context data.
    /// </summary>
    public bool HasContext
    {
        get => (bool)GetValue(HasContextProperty);
        private set => SetValue(HasContextProperty, value);
    }

    public static readonly DependencyProperty HasContextProperty =
        DependencyProperty.Register(
            nameof(HasContext),
            typeof(bool),
            typeof(LogViewerControl),
            new PropertyMetadata(false));

    /// <summary>
    /// Gets the context data as formatted JSON string.
    /// </summary>
    public string ContextJson
    {
        get => (string)GetValue(ContextJsonProperty);
        private set => SetValue(ContextJsonProperty, value);
    }

    public static readonly DependencyProperty ContextJsonProperty =
        DependencyProperty.Register(
            nameof(ContextJson),
            typeof(string),
            typeof(LogViewerControl),
            new PropertyMetadata(string.Empty));

    /// <summary>
    /// Gets or sets the refresh command.
    /// </summary>
    public ICommand? RefreshCommand
    {
        get => (ICommand?)GetValue(RefreshCommandProperty);
        set => SetValue(RefreshCommandProperty, value);
    }

    public static readonly DependencyProperty RefreshCommandProperty =
        DependencyProperty.Register(
            nameof(RefreshCommand),
            typeof(ICommand),
            typeof(LogViewerControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the export command.
    /// </summary>
    public ICommand? ExportCommand
    {
        get => (ICommand?)GetValue(ExportCommandProperty);
        set => SetValue(ExportCommandProperty, value);
    }

    public static readonly DependencyProperty ExportCommandProperty =
        DependencyProperty.Register(
            nameof(ExportCommand),
            typeof(ICommand),
            typeof(LogViewerControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the clear filters command.
    /// </summary>
    public ICommand? ClearFiltersCommand
    {
        get => (ICommand?)GetValue(ClearFiltersCommandProperty);
        set => SetValue(ClearFiltersCommandProperty, value);
    }

    public static readonly DependencyProperty ClearFiltersCommandProperty =
        DependencyProperty.Register(
            nameof(ClearFiltersCommand),
            typeof(ICommand),
            typeof(LogViewerControl),
            new PropertyMetadata(null));

    /// <summary>
    /// Gets or sets the copy correlation ID command.
    /// </summary>
    public ICommand? CopyCorrelationIdCommand
    {
        get => (ICommand?)GetValue(CopyCorrelationIdCommandProperty);
        set => SetValue(CopyCorrelationIdCommandProperty, value);
    }

    public static readonly DependencyProperty CopyCorrelationIdCommandProperty =
        DependencyProperty.Register(
            nameof(CopyCorrelationIdCommand),
            typeof(ICommand),
            typeof(LogViewerControl),
            new PropertyMetadata(null));

    #endregion

    #region Private Methods

    /// <summary>
    /// Updates the details panel based on the selected log entry.
    /// </summary>
    private void UpdateSelectedLogDetails()
    {
        HasSelectedLog = SelectedLogEntry is not null;

        if (SelectedLogEntry is null)
        {
            HasException = false;
            HasStackTrace = false;
            HasContext = false;
            ContextJson = string.Empty;
            return;
        }

        HasException = !string.IsNullOrEmpty(SelectedLogEntry.Exception);
        HasStackTrace = !string.IsNullOrEmpty(SelectedLogEntry.StackTrace);
        HasContext = SelectedLogEntry.Context?.Count > 0;

        if (HasContext)
        {
            try
            {
                ContextJson = JsonSerializer.Serialize(
                    SelectedLogEntry.Context,
                    new JsonSerializerOptions { WriteIndented = true });
            }
            catch
            {
                ContextJson = "Unable to serialize context";
            }
        }
        else
        {
            ContextJson = string.Empty;
        }
    }

    #endregion
}
