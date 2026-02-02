using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace FluentPDF.Avalonia.Controls;

/// <summary>
/// Search panel control with slide animations.
/// </summary>
/// <remarks>
/// SIMPLIFIED: Removed complex GlassPanel, now uses simple Border with:
/// - Slide-in/out animations (250ms cubic-ease-out)
/// - Preserves existing search functionality
/// - Maintains ViewModel separation
/// </remarks>
public partial class SearchPanel : UserControl
{
    #region Dependency Properties

    /// <summary>
    /// Defines the IsVisible property.
    /// Controls slide-in/out animation state.
    /// </summary>
    public static readonly StyledProperty<bool> IsPanelVisibleProperty =
        AvaloniaProperty.Register<SearchPanel, bool>(
            nameof(IsPanelVisible),
            defaultValue: false);

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets whether the search panel is visible.
    /// Triggers slide-in animation when true, slide-out when false.
    /// </summary>
    public bool IsPanelVisible
    {
        get => GetValue(IsPanelVisibleProperty);
        set => SetValue(IsPanelVisibleProperty, value);
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchPanel"/> class.
    /// </summary>
    public SearchPanel()
    {
        InitializeComponent();

        // Wire up property change handler for visibility animations
        IsPanelVisibleProperty.Changed.AddClassHandler<SearchPanel>(
            (panel, e) => panel.OnIsPanelVisibleChanged(e));

        // Initialize in hidden state (slid up)
        UpdatePanelTransform(animated: false);
    }

    #endregion

    #region Protected Methods

    /// <summary>
    /// Called when the control is attached to the visual tree.
    /// </summary>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Ensure initial state is correct
        UpdatePanelTransform(animated: false);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Handles changes to the IsPanelVisible property.
    /// Triggers slide-in or slide-out animation.
    /// </summary>
    private void OnIsPanelVisibleChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is bool isVisible)
        {
            UpdatePanelTransform(animated: true);
        }
    }

    /// <summary>
    /// Updates the panel's render transform for slide animation.
    /// </summary>
    /// <param name="animated">Whether to animate the transition (250ms) or apply instantly.</param>
    private void UpdatePanelTransform(bool animated)
    {
        if (this.FindControl<Border>("SearchPanelContainer") is not { } container)
        {
            return;
        }

        // Calculate slide distance (panel height + some offset for complete hiding)
        var slideDistance = IsPanelVisible ? 0.0 : -60.0; // Slide up 60px when hidden

        // Create transform (slide vertically)
        var transform = new TranslateTransform
        {
            X = 0,
            Y = slideDistance
        };

        // Apply transform and opacity
        container.RenderTransform = transform;
        container.Opacity = IsPanelVisible ? 1.0 : 0.0;

        // If not animated, disable transitions temporarily
        if (!animated)
        {
            var transitions = container.Transitions;
            container.Transitions = null;

            // Re-enable transitions on next layout pass
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (container != null)
                {
                    container.Transitions = transitions;
                }
            }, global::Avalonia.Threading.DispatcherPriority.Loaded);
        }
    }

    #endregion
}
