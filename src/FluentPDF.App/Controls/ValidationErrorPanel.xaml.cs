using System.Collections.Generic;
using System.Collections.Specialized;
using FluentPDF.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentPDF.App.Controls;

/// <summary>
/// User control for displaying form validation errors with "Go to field" functionality.
/// Shows errors in an InfoBar with individual error messages and navigation buttons.
/// </summary>
public sealed partial class ValidationErrorPanel : UserControl
{
    /// <summary>
    /// Dependency property for ValidationErrors collection.
    /// </summary>
    public static readonly DependencyProperty ValidationErrorsProperty =
        DependencyProperty.Register(
            nameof(ValidationErrors),
            typeof(IEnumerable<FieldValidationError>),
            typeof(ValidationErrorPanel),
            new PropertyMetadata(null, OnValidationErrorsChanged));

    /// <summary>
    /// Dependency property for SummaryMessage.
    /// </summary>
    public static readonly DependencyProperty SummaryMessageProperty =
        DependencyProperty.Register(
            nameof(SummaryMessage),
            typeof(string),
            typeof(ValidationErrorPanel),
            new PropertyMetadata(null, OnSummaryMessageChanged));

    /// <summary>
    /// Dependency property for IsOpen.
    /// </summary>
    public static readonly DependencyProperty IsOpenProperty =
        DependencyProperty.Register(
            nameof(IsOpen),
            typeof(bool),
            typeof(ValidationErrorPanel),
            new PropertyMetadata(false, OnIsOpenChanged));

    /// <summary>
    /// Event fired when the user clicks "Go to field" for a specific error.
    /// </summary>
    public event EventHandler<string>? GoToFieldRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationErrorPanel"/> class.
    /// </summary>
    public ValidationErrorPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the collection of validation errors to display.
    /// </summary>
    public IEnumerable<FieldValidationError>? ValidationErrors
    {
        get => (IEnumerable<FieldValidationError>?)GetValue(ValidationErrorsProperty);
        set => SetValue(ValidationErrorsProperty, value);
    }

    /// <summary>
    /// Gets or sets the summary message to display.
    /// </summary>
    public string? SummaryMessage
    {
        get => (string?)GetValue(SummaryMessageProperty);
        set => SetValue(SummaryMessageProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the InfoBar is open.
    /// </summary>
    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>
    /// Called when ValidationErrors property changes.
    /// </summary>
    private static void OnValidationErrorsChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is ValidationErrorPanel panel)
        {
            panel.UpdateErrorsDisplay();
        }
    }

    /// <summary>
    /// Called when SummaryMessage property changes.
    /// </summary>
    private static void OnSummaryMessageChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is ValidationErrorPanel panel)
        {
            panel.SummaryText.Text = e.NewValue as string ?? string.Empty;
        }
    }

    /// <summary>
    /// Called when IsOpen property changes.
    /// </summary>
    private static void OnIsOpenChanged(
        DependencyObject d,
        DependencyPropertyChangedEventArgs e)
    {
        if (d is ValidationErrorPanel panel)
        {
            panel.ErrorInfoBar.IsOpen = (bool)e.NewValue;
        }
    }

    /// <summary>
    /// Updates the errors display based on current ValidationErrors collection.
    /// </summary>
    private void UpdateErrorsDisplay()
    {
        if (ValidationErrors == null)
        {
            ErrorsItemsControl.ItemsSource = null;
            return;
        }

        ErrorsItemsControl.ItemsSource = ValidationErrors;
    }

    /// <summary>
    /// Handles "Go to field" button click.
    /// </summary>
    private void OnGoToFieldClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string fieldName)
        {
            GoToFieldRequested?.Invoke(this, fieldName);
        }
    }
}
