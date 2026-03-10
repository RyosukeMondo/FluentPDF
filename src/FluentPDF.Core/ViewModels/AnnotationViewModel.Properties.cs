using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentPDF.Core.Models;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Core.ViewModels;

/// <summary>
/// Annotation properties (color, width, opacity) and text-markup creation methods for AnnotationViewModel.
/// </summary>
public partial class AnnotationViewModel
{
    /// <summary>
    /// Gets or sets the selected color for new annotations (ARGB format).
    /// </summary>
    [ObservableProperty]
    private Color _selectedColor = Color.Yellow;

    /// <summary>
    /// Gets or sets the stroke width for drawing annotations.
    /// </summary>
    [ObservableProperty]
    private double _strokeWidth = 2.0;

    /// <summary>
    /// Gets or sets the opacity for new annotations (0.0 to 1.0).
    /// </summary>
    [ObservableProperty]
    private double _opacity = 0.5;

    /// <summary>
    /// Creates a highlight annotation from a text selection.
    /// </summary>
    /// <param name="selection">The text selection with bounds and character information.</param>
    public async Task CreateHighlightFromSelectionAsync(TextSelection selection)
    {
        if (_currentDocument == null || selection == null || !selection.HasText)
        {
            _logger.LogWarning("Cannot create highlight: invalid parameters");
            return;
        }

        _logger.LogInformation(
            "Creating highlight annotation from selection. Page={Page}, TextLength={Length}",
            selection.PageNumber, selection.Text.Length);

        try
        {
            IsLoading = true;

            var annotation = new Annotation
            {
                Type = AnnotationType.Highlight,
                PageNumber = selection.PageNumber + 1, // Convert from 0-based to 1-based
                Bounds = new PdfRectangle
                {
                    Left = selection.SelectionBounds.Left,
                    Top = selection.SelectionBounds.Top,
                    Right = selection.SelectionBounds.Right,
                    Bottom = selection.SelectionBounds.Bottom
                },
                FillColor = Color.FromArgb(
                    SelectedColor.R,
                    SelectedColor.G,
                    SelectedColor.B),
                Opacity = Opacity,
                Contents = selection.Text,
                QuadPoints = selection.ToQuadPoints()
            };

            var result = await _annotationService.CreateAnnotationAsync(_currentDocument, annotation);

            if (result.IsSuccess)
            {
                Annotations.Add(result.Value);
                HasUnsavedChanges = true;
                _logger.LogInformation(
                    "Highlight annotation created successfully. Id={Id}",
                    result.Value.Id);

                // Deactivate tool if ToolStaysActive is false
                if (!ToolStaysActive)
                {
                    ActiveTool = AnnotationTool.None;
                    StatusMessage = string.Empty;
                }
            }
            else
            {
                _logger.LogError("Failed to create highlight annotation: {Errors}", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating highlight annotation");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Creates an underline annotation from a text selection.
    /// </summary>
    /// <param name="selection">The text selection with bounds and character information.</param>
    public async Task CreateUnderlineFromSelectionAsync(TextSelection selection)
    {
        if (_currentDocument == null || selection == null || !selection.HasText)
        {
            _logger.LogWarning("Cannot create underline: invalid parameters");
            return;
        }

        _logger.LogInformation(
            "Creating underline annotation from selection. Page={Page}, TextLength={Length}",
            selection.PageNumber, selection.Text.Length);

        try
        {
            IsLoading = true;

            var annotation = new Annotation
            {
                Type = AnnotationType.Underline,
                PageNumber = selection.PageNumber + 1,
                Bounds = new PdfRectangle
                {
                    Left = selection.SelectionBounds.Left,
                    Top = selection.SelectionBounds.Top,
                    Right = selection.SelectionBounds.Right,
                    Bottom = selection.SelectionBounds.Bottom
                },
                FillColor = Color.FromArgb(
                    SelectedColor.R,
                    SelectedColor.G,
                    SelectedColor.B),
                Opacity = Opacity,
                Contents = selection.Text,
                QuadPoints = selection.ToQuadPoints()
            };

            var result = await _annotationService.CreateAnnotationAsync(_currentDocument, annotation);

            if (result.IsSuccess)
            {
                Annotations.Add(result.Value);
                HasUnsavedChanges = true;
                _logger.LogInformation(
                    "Underline annotation created successfully. Id={Id}",
                    result.Value.Id);

                // Deactivate tool if ToolStaysActive is false
                if (!ToolStaysActive)
                {
                    ActiveTool = AnnotationTool.None;
                    StatusMessage = string.Empty;
                }
            }
            else
            {
                _logger.LogError("Failed to create underline annotation: {Errors}", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating underline annotation");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Creates a strikethrough annotation from a text selection.
    /// </summary>
    /// <param name="selection">The text selection with bounds and character information.</param>
    public async Task CreateStrikethroughFromSelectionAsync(TextSelection selection)
    {
        if (_currentDocument == null || selection == null || !selection.HasText)
        {
            _logger.LogWarning("Cannot create strikethrough: invalid parameters");
            return;
        }

        _logger.LogInformation(
            "Creating strikethrough annotation from selection. Page={Page}, TextLength={Length}",
            selection.PageNumber, selection.Text.Length);

        try
        {
            IsLoading = true;

            var annotation = new Annotation
            {
                Type = AnnotationType.StrikeOut,
                PageNumber = selection.PageNumber + 1,
                Bounds = new PdfRectangle
                {
                    Left = selection.SelectionBounds.Left,
                    Top = selection.SelectionBounds.Top,
                    Right = selection.SelectionBounds.Right,
                    Bottom = selection.SelectionBounds.Bottom
                },
                FillColor = Color.FromArgb(
                    SelectedColor.R,
                    SelectedColor.G,
                    SelectedColor.B),
                Opacity = Opacity,
                Contents = selection.Text,
                QuadPoints = selection.ToQuadPoints()
            };

            var result = await _annotationService.CreateAnnotationAsync(_currentDocument, annotation);

            if (result.IsSuccess)
            {
                Annotations.Add(result.Value);
                HasUnsavedChanges = true;
                _logger.LogInformation(
                    "Strikethrough annotation created successfully. Id={Id}",
                    result.Value.Id);

                // Deactivate tool if ToolStaysActive is false
                if (!ToolStaysActive)
                {
                    ActiveTool = AnnotationTool.None;
                    StatusMessage = string.Empty;
                }
            }
            else
            {
                _logger.LogError("Failed to create strikethrough annotation: {Errors}", result.Errors);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating strikethrough annotation");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
