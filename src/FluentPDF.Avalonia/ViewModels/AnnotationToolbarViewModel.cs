using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentPDF.Avalonia.ViewModels;

public partial class AnnotationToolbarViewModel : ObservableObject
{
    // Text markup mode properties
    [ObservableProperty]
    private bool _highlightModeActive;

    [ObservableProperty]
    private bool _underlineModeActive;

    [ObservableProperty]
    private bool _strikethroughModeActive;

    // Comment mode
    [ObservableProperty]
    private bool _commentModeActive;

    // Shape tool modes
    [ObservableProperty]
    private bool _rectangleModeActive;

    [ObservableProperty]
    private bool _circleModeActive;

    [ObservableProperty]
    private bool _freehandModeActive;

    // Stamp tool mode
    [ObservableProperty]
    private bool _stampModeActive;

    // Annotation properties
    [ObservableProperty]
    private Color _annotationColor = Color.FromArgb(255, 255, 255, 0); // Yellow default

    public AnnotationToolbarViewModel()
    {
        // Ensure only one mode is active at a time
        PropertyChanged += (s, e) =>
        {
            if (e.PropertyName?.EndsWith("ModeActive") == true)
            {
                DeactivateOtherModes(e.PropertyName);
            }
        };
    }

    private void DeactivateOtherModes(string activeProperty)
    {
        if (activeProperty != nameof(HighlightModeActive) && HighlightModeActive)
            HighlightModeActive = false;
        if (activeProperty != nameof(UnderlineModeActive) && UnderlineModeActive)
            UnderlineModeActive = false;
        if (activeProperty != nameof(StrikethroughModeActive) && StrikethroughModeActive)
            StrikethroughModeActive = false;
        if (activeProperty != nameof(CommentModeActive) && CommentModeActive)
            CommentModeActive = false;
        if (activeProperty != nameof(RectangleModeActive) && RectangleModeActive)
            RectangleModeActive = false;
        if (activeProperty != nameof(CircleModeActive) && CircleModeActive)
            CircleModeActive = false;
        if (activeProperty != nameof(FreehandModeActive) && FreehandModeActive)
            FreehandModeActive = false;
        if (activeProperty != nameof(StampModeActive) && StampModeActive)
            StampModeActive = false;
    }
}
