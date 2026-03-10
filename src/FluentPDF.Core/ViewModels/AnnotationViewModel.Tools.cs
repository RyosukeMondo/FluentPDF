using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentPDF.Core.Models;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// Tool selection, drawing mode, and keyboard shortcuts for AnnotationViewModel.
/// </summary>
public partial class AnnotationViewModel
{
    /// <summary>
    /// Gets or sets the currently active annotation tool.
    /// </summary>
    [ObservableProperty]
    private AnnotationTool _activeTool = AnnotationTool.None;

    /// <summary>
    /// Gets or sets a value indicating whether the tool should stay active after creating an annotation.
    /// When true, the tool remains selected; when false, it returns to None after annotation creation.
    /// </summary>
    [ObservableProperty]
    private bool _toolStaysActive = true;

    /// <summary>
    /// Gets or sets the status message for the current tool or operation.
    /// </summary>
    [ObservableProperty]
    private string _statusMessage = string.Empty;

    /// <summary>
    /// Selects an annotation tool for creating new annotations.
    /// </summary>
    /// <param name="tool">The tool to activate (can be AnnotationTool enum or string).</param>
    [RelayCommand]
    private void SelectTool(object tool)
    {
        AnnotationTool toolEnum;

        if (tool is AnnotationTool enumValue)
        {
            toolEnum = enumValue;
        }
        else if (tool is string toolString && Enum.TryParse<AnnotationTool>(toolString, out var parsedTool))
        {
            toolEnum = parsedTool;
        }
        else
        {
            _logger.LogWarning("Invalid annotation tool parameter: {Tool}", tool);
            return;
        }

        _logger.LogInformation("Selected annotation tool: {Tool}", toolEnum);

        // Toggle tool if clicking the same tool
        if (ActiveTool == toolEnum && toolEnum != AnnotationTool.None)
        {
            ActiveTool = AnnotationTool.None;
            StatusMessage = string.Empty;
        }
        else
        {
            ActiveTool = toolEnum;
            StatusMessage = GetToolStatusMessage(toolEnum);
        }

        // Deselect current annotation when switching tools
        if (SelectedAnnotation != null)
        {
            SelectedAnnotation.IsSelected = false;
            SelectedAnnotation = null;
        }
    }

    /// <summary>
    /// Gets the status message for the specified annotation tool.
    /// </summary>
    private static string GetToolStatusMessage(AnnotationTool tool)
    {
        return tool switch
        {
            AnnotationTool.Highlight => "Select text to highlight",
            AnnotationTool.Underline => "Select text to underline",
            AnnotationTool.Strikethrough => "Select text to strikethrough",
            AnnotationTool.Comment => "Click to add a comment",
            AnnotationTool.Rectangle => "Click and drag to draw a rectangle",
            AnnotationTool.Circle => "Click and drag to draw a circle",
            AnnotationTool.Freehand => "Click and drag to draw",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Toggles the visibility of the annotation toolbar.
    /// </summary>
    [RelayCommand]
    private void ToggleToolbar()
    {
        _logger.LogInformation("Toggling annotation toolbar. Current={Current}", IsToolbarVisible);
        IsToolbarVisible = !IsToolbarVisible;

        // Clear tool selection when hiding toolbar
        if (!IsToolbarVisible)
        {
            ActiveTool = AnnotationTool.None;
        }
    }

    /// <summary>
    /// Handles keyboard shortcuts for annotation tools.
    /// H = Highlight, U = Underline, S = Strikethrough, Esc = Clear tool
    /// </summary>
    /// <param name="key">The key that was pressed.</param>
    [RelayCommand]
    private void HandleKeyboardShortcut(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _logger.LogInformation("Keyboard shortcut pressed: {Key}", key);

        var tool = key.ToUpperInvariant() switch
        {
            "H" => AnnotationTool.Highlight,
            "U" => AnnotationTool.Underline,
            "S" => AnnotationTool.Strikethrough,
            "ESC" or "ESCAPE" => AnnotationTool.None,
            _ => ActiveTool
        };

        if (tool != ActiveTool)
        {
            SelectTool(tool);
        }
    }

    /// <summary>
    /// Selects an annotation for editing or deletion.
    /// </summary>
    /// <param name="annotation">The annotation to select.</param>
    [RelayCommand]
    private void SelectAnnotation(Annotation? annotation)
    {
        _logger.LogInformation(
            "Selecting annotation. Id={Id}, Type={Type}",
            annotation?.Id, annotation?.Type);

        // Deselect previous annotation
        if (SelectedAnnotation != null)
        {
            SelectedAnnotation.IsSelected = false;
        }

        SelectedAnnotation = annotation;

        // Mark new annotation as selected
        if (SelectedAnnotation != null)
        {
            SelectedAnnotation.IsSelected = true;
        }

        // Clear active tool when selecting an annotation
        if (SelectedAnnotation != null)
        {
            ActiveTool = AnnotationTool.None;
        }
    }
}
