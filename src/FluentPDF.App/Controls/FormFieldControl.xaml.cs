using FluentPDF.Core.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace FluentPDF.App.Controls;

/// <summary>
/// Custom WinUI control for rendering PDF form field overlays.
/// Provides interactive input controls positioned over PDF content.
/// </summary>
public sealed partial class FormFieldControl : UserControl
{
    /// <summary>
    /// Dependency property for the form field data.
    /// </summary>
    public static readonly DependencyProperty FieldProperty =
        DependencyProperty.Register(
            nameof(Field),
            typeof(PdfFormField),
            typeof(FormFieldControl),
            new PropertyMetadata(null, OnFieldChanged));

    /// <summary>
    /// Dependency property for zoom level (affects positioning and sizing).
    /// </summary>
    public static readonly DependencyProperty ZoomLevelProperty =
        DependencyProperty.Register(
            nameof(ZoomLevel),
            typeof(double),
            typeof(FormFieldControl),
            new PropertyMetadata(1.0, OnZoomLevelChanged));

    /// <summary>
    /// Dependency property for error state (shows validation errors).
    /// </summary>
    public static readonly DependencyProperty IsInErrorStateProperty =
        DependencyProperty.Register(
            nameof(IsInErrorState),
            typeof(bool),
            typeof(FormFieldControl),
            new PropertyMetadata(false, OnErrorStateChanged));

    /// <summary>
    /// Event raised when the field value changes.
    /// </summary>
    public event TypedEventHandler<FormFieldControl, string?>? ValueChanged;

    /// <summary>
    /// Event raised when the field gains or loses focus.
    /// </summary>
    public event TypedEventHandler<FormFieldControl, bool>? FocusChanged;

    /// <summary>
    /// Gets or sets the form field data.
    /// </summary>
    public PdfFormField? Field
    {
        get => (PdfFormField?)GetValue(FieldProperty);
        set => SetValue(FieldProperty, value);
    }

    /// <summary>
    /// Gets or sets the zoom level for positioning and sizing.
    /// </summary>
    public double ZoomLevel
    {
        get => (double)GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the field is in an error state.
    /// </summary>
    public bool IsInErrorState
    {
        get => (bool)GetValue(IsInErrorStateProperty);
        set => SetValue(IsInErrorStateProperty, value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FormFieldControl"/> class.
    /// </summary>
    public FormFieldControl()
    {
        InitializeComponent();

        // Subscribe to pointer events for hover state
        PointerEntered += OnPointerEntered;
        PointerExited += OnPointerExited;
        GotFocus += OnGotFocus;
        LostFocus += OnLostFocus;
    }

    /// <summary>
    /// Handles Field property changes.
    /// Updates the control template and position based on field type and bounds.
    /// </summary>
    private static void OnFieldChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormFieldControl control)
        {
            control.UpdateTemplate();
            control.UpdatePosition();
            control.UpdateVisualState();
        }
    }

    /// <summary>
    /// Handles ZoomLevel property changes.
    /// Recalculates position and size based on new zoom level.
    /// </summary>
    private static void OnZoomLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormFieldControl control)
        {
            control.UpdatePosition();
        }
    }

    /// <summary>
    /// Handles IsInErrorState property changes.
    /// Updates visual state to show or hide error styling.
    /// </summary>
    private static void OnErrorStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FormFieldControl control)
        {
            control.UpdateVisualState();
        }
    }

    /// <summary>
    /// Updates the content template based on field type.
    /// </summary>
    private void UpdateTemplate()
    {
        if (Field == null || FieldPresenter == null)
            return;

        var templateKey = Field.Type switch
        {
            FormFieldType.Text => "TextFieldTemplate",
            FormFieldType.Checkbox => "CheckboxFieldTemplate",
            FormFieldType.RadioButton => "RadioButtonFieldTemplate",
            _ => "TextFieldTemplate" // Default to text field
        };

        if (Resources.TryGetValue(templateKey, out var template) && template is DataTemplate dataTemplate)
        {
            FieldPresenter.ContentTemplate = dataTemplate;
        }
    }

    /// <summary>
    /// Updates the control position and size based on field bounds and zoom level.
    /// </summary>
    private void UpdatePosition()
    {
        if (Field == null)
            return;

        var bounds = Field.Bounds;

        // Calculate position and size with zoom
        Width = (bounds.Right - bounds.Left) * ZoomLevel;
        Height = (bounds.Top - bounds.Bottom) * ZoomLevel;

        // Position is set by parent container using Canvas.Left and Canvas.Top
    }

    /// <summary>
    /// Updates the visual state based on field properties and error state.
    /// </summary>
    private void UpdateVisualState()
    {
        if (Field == null)
            return;

        string stateName;

        if (IsInErrorState)
        {
            stateName = "Error";
        }
        else if (Field.IsReadOnly)
        {
            stateName = "ReadOnly";
        }
        else
        {
            stateName = "Normal";
        }

        VisualStateManager.GoToState(this, stateName, useTransitions: true);
    }

    /// <summary>
    /// Handles pointer entered event for hover state.
    /// </summary>
    private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (Field?.IsReadOnly != true && !IsInErrorState)
        {
            VisualStateManager.GoToState(this, "PointerOver", useTransitions: true);
        }
    }

    /// <summary>
    /// Handles pointer exited event to return from hover state.
    /// </summary>
    private void OnPointerExited(object sender, PointerRoutedEventArgs e)
    {
        UpdateVisualState();
    }

    /// <summary>
    /// Handles got focus event.
    /// </summary>
    private void OnGotFocus(object sender, RoutedEventArgs e)
    {
        if (Field?.IsReadOnly != true && !IsInErrorState)
        {
            VisualStateManager.GoToState(this, "Focused", useTransitions: true);
        }
        FocusChanged?.Invoke(this, true);
    }

    /// <summary>
    /// Handles lost focus event.
    /// </summary>
    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        UpdateVisualState();
        FocusChanged?.Invoke(this, false);
    }

    /// <summary>
    /// Raises the ValueChanged event.
    /// </summary>
    internal void RaiseValueChanged(string? newValue)
    {
        ValueChanged?.Invoke(this, newValue);
    }
}
