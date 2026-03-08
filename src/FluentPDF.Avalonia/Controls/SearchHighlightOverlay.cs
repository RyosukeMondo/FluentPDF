using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using FluentPDF.Core.Models;
using System.Collections.Generic;

namespace FluentPDF.Avalonia.Controls;

/// <summary>
/// Canvas-based overlay that renders semi-transparent highlight rectangles
/// over PDF search matches. Sits on top of the PDF Image in the same Grid cell.
/// </summary>
public class SearchHighlightOverlay : Control
{
    private static readonly IBrush MatchBrush = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0xFF, 0x00));
    private static readonly IBrush ActiveMatchBrush = new SolidColorBrush(Color.FromArgb(0x80, 0xFF, 0x8C, 0x00));
    private static readonly IPen ActiveMatchPen = new Pen(new SolidColorBrush(Color.FromArgb(0xCC, 0xFF, 0x8C, 0x00)), 1.5);

    public static readonly StyledProperty<IReadOnlyList<SearchMatch>?> SearchMatchesProperty =
        AvaloniaProperty.Register<SearchHighlightOverlay, IReadOnlyList<SearchMatch>?>(
            nameof(SearchMatches));

    public static readonly StyledProperty<int> CurrentMatchIndexProperty =
        AvaloniaProperty.Register<SearchHighlightOverlay, int>(
            nameof(CurrentMatchIndex), defaultValue: -1);

    public static readonly StyledProperty<int> CurrentPageIndexProperty =
        AvaloniaProperty.Register<SearchHighlightOverlay, int>(
            nameof(CurrentPageIndex), defaultValue: 0);

    public static readonly StyledProperty<double> ZoomLevelProperty =
        AvaloniaProperty.Register<SearchHighlightOverlay, double>(
            nameof(ZoomLevel), defaultValue: 1.0);

    public static readonly StyledProperty<double> PageWidthProperty =
        AvaloniaProperty.Register<SearchHighlightOverlay, double>(
            nameof(PageWidth), defaultValue: 612.0);

    public static readonly StyledProperty<double> PageHeightProperty =
        AvaloniaProperty.Register<SearchHighlightOverlay, double>(
            nameof(PageHeight), defaultValue: 792.0);

    public IReadOnlyList<SearchMatch>? SearchMatches
    {
        get => GetValue(SearchMatchesProperty);
        set => SetValue(SearchMatchesProperty, value);
    }

    public int CurrentMatchIndex
    {
        get => GetValue(CurrentMatchIndexProperty);
        set => SetValue(CurrentMatchIndexProperty, value);
    }

    public int CurrentPageIndex
    {
        get => GetValue(CurrentPageIndexProperty);
        set => SetValue(CurrentPageIndexProperty, value);
    }

    public double ZoomLevel
    {
        get => GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    public double PageWidth
    {
        get => GetValue(PageWidthProperty);
        set => SetValue(PageWidthProperty, value);
    }

    public double PageHeight
    {
        get => GetValue(PageHeightProperty);
        set => SetValue(PageHeightProperty, value);
    }

    static SearchHighlightOverlay()
    {
        AffectsRender<SearchHighlightOverlay>(
            SearchMatchesProperty,
            CurrentMatchIndexProperty,
            CurrentPageIndexProperty,
            ZoomLevelProperty,
            PageWidthProperty,
            PageHeightProperty);
    }

    public SearchHighlightOverlay()
    {
        IsHitTestVisible = false;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var matches = SearchMatches;
        if (matches == null || matches.Count == 0) return;

        var pageWidth = PageWidth;
        var pageHeight = PageHeight;
        if (pageWidth <= 0 || pageHeight <= 0) return;

        // The overlay shares the same size as the Image (Stretch=Uniform in the Grid).
        // The Image renders the PDF bitmap uniformly within its bounds.
        // We need to compute the actual rendered area within this control.
        var renderWidth = Bounds.Width;
        var renderHeight = Bounds.Height;
        if (renderWidth <= 0 || renderHeight <= 0) return;

        // Compute the uniform scale factor (same logic as Image with Stretch.Uniform)
        var scaleX = renderWidth / pageWidth;
        var scaleY = renderHeight / pageHeight;
        var scale = System.Math.Min(scaleX, scaleY);

        // Compute offset to center the image within the control bounds
        var renderedW = pageWidth * scale;
        var renderedH = pageHeight * scale;
        var offsetX = (renderWidth - renderedW) / 2.0;
        var offsetY = (renderHeight - renderedH) / 2.0;

        var currentPage = CurrentPageIndex;
        var currentIdx = CurrentMatchIndex;

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            if (match.PageNumber != currentPage) continue;

            var bbox = match.BoundingBox;

            // Convert PDF coords (origin bottom-left, Y up) to screen coords (origin top-left, Y down)
            // screenX = (pdfX / pageWidth) * renderedW + offsetX
            // screenY = ((pageHeight - pdfTop) / pageHeight) * renderedH + offsetY
            var screenLeft = (bbox.Left / pageWidth) * renderedW + offsetX;
            var screenTop = ((pageHeight - bbox.Top) / pageHeight) * renderedH + offsetY;
            var screenRight = (bbox.Right / pageWidth) * renderedW + offsetX;
            var screenBottom = ((pageHeight - bbox.Bottom) / pageHeight) * renderedH + offsetY;

            var rect = new Rect(screenLeft, screenTop, screenRight - screenLeft, screenBottom - screenTop);

            if (i == currentIdx)
            {
                context.FillRectangle(ActiveMatchBrush, rect);
                context.DrawRectangle(ActiveMatchPen, rect);
            }
            else
            {
                context.FillRectangle(MatchBrush, rect);
            }
        }
    }
}
