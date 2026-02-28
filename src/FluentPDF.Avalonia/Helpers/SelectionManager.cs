using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using FluentPDF.Core.Services;

namespace FluentPDF.Avalonia.Helpers;

/// <summary>
/// Selection containment mode for marquee/lasso selection.
/// </summary>
public enum SelectionContainment
{
    /// <summary>Select objects that intersect with the selection area.</summary>
    Intersect,
    /// <summary>Select only objects fully contained within the selection area.</summary>
    FullyContained
}

/// <summary>
/// Selection modifier mode derived from keyboard modifiers.
/// Follows Illustrator conventions: Shift=add, Alt=subtract.
/// </summary>
public enum SelectionModifier
{
    Replace,
    Add,
    Subtract
}

/// <summary>
/// Manages marquee rectangle selection, lasso selection, and multi-object move.
/// Extracted from PdfViewerPage to keep complexity manageable.
/// </summary>
public sealed class SelectionManager
{
    private readonly Canvas _canvas;
    private readonly Func<Point, (float pdfX, float pdfY)?> _screenToPdf;
    private readonly Func<float, float, Point?> _pdfToScreen;

    // Marquee state
    private bool _isMarqueeActive;
    private Point _marqueeStart;
    private Rectangle? _marqueeRect;
    private SelectionModifier _marqueeModifier;

    // Lasso state
    private bool _isLassoActive;
    private readonly List<Point> _lassoPoints = new();
    private Polyline? _lassoPolyline;
    private SelectionModifier _lassoModifier;

    // Multi-move state
    private bool _isMultiMoving;
    private Point _moveStartScreen;
    private readonly List<(Rectangle rect, double origLeft, double origTop)> _moveHighlights = new();

    public SelectionContainment ContainmentMode { get; set; } = SelectionContainment.Intersect;

    public bool IsMarqueeActive => _isMarqueeActive;
    public bool IsLassoActive => _isLassoActive;
    public bool IsMultiMoving => _isMultiMoving;

    public SelectionManager(
        Canvas canvas,
        Func<Point, (float pdfX, float pdfY)?> screenToPdf,
        Func<float, float, Point?> pdfToScreen)
    {
        _canvas = canvas;
        _screenToPdf = screenToPdf;
        _pdfToScreen = pdfToScreen;
    }

    public static SelectionModifier GetModifier(bool shiftHeld, bool altHeld)
    {
        if (shiftHeld) return SelectionModifier.Add;
        if (altHeld) return SelectionModifier.Subtract;
        return SelectionModifier.Replace;
    }

    #region Marquee Selection

    public void StartMarquee(Point screenPoint, SelectionModifier modifier)
    {
        _isMarqueeActive = true;
        _marqueeStart = screenPoint;
        _marqueeModifier = modifier;

        _marqueeRect = new Rectangle
        {
            Stroke = new SolidColorBrush(Color.FromArgb(200, 0, 120, 215)),
            StrokeThickness = 1,
            StrokeDashArray = new global::Avalonia.Collections.AvaloniaList<double> { 4, 2 },
            Fill = new SolidColorBrush(Color.FromArgb(30, 0, 120, 215)),
            Width = 0,
            Height = 0,
            IsHitTestVisible = false
        };
        Canvas.SetLeft(_marqueeRect, screenPoint.X);
        Canvas.SetTop(_marqueeRect, screenPoint.Y);
        _canvas.Children.Add(_marqueeRect);
    }

    public void UpdateMarquee(Point currentPoint)
    {
        if (!_isMarqueeActive || _marqueeRect == null) return;

        var x = Math.Min(_marqueeStart.X, currentPoint.X);
        var y = Math.Min(_marqueeStart.Y, currentPoint.Y);
        var w = Math.Abs(currentPoint.X - _marqueeStart.X);
        var h = Math.Abs(currentPoint.Y - _marqueeStart.Y);

        _marqueeRect.Width = w;
        _marqueeRect.Height = h;
        Canvas.SetLeft(_marqueeRect, x);
        Canvas.SetTop(_marqueeRect, y);
    }

    /// <summary>
    /// Finishes marquee selection and returns objects that should be selected.
    /// </summary>
    public MarqueeResult FinishMarquee(
        Point endPoint,
        List<PageObjectInfo> allObjects,
        List<PageObjectInfo> currentSelection)
    {
        if (!_isMarqueeActive) return new MarqueeResult(new List<PageObjectInfo>());

        // Clean up visual
        if (_marqueeRect != null)
        {
            _canvas.Children.Remove(_marqueeRect);
            _marqueeRect = null;
        }
        _isMarqueeActive = false;

        // Get marquee bounds in PDF coordinates
        var pdfTL = _screenToPdf(new Point(
            Math.Min(_marqueeStart.X, endPoint.X),
            Math.Min(_marqueeStart.Y, endPoint.Y)));
        var pdfBR = _screenToPdf(new Point(
            Math.Max(_marqueeStart.X, endPoint.X),
            Math.Max(_marqueeStart.Y, endPoint.Y)));

        if (pdfTL == null || pdfBR == null)
            return new MarqueeResult(new List<PageObjectInfo>());

        var marqueePdfLeft = Math.Min(pdfTL.Value.pdfX, pdfBR.Value.pdfX);
        var marqueePdfRight = Math.Max(pdfTL.Value.pdfX, pdfBR.Value.pdfX);
        var marqueePdfBottom = Math.Min(pdfTL.Value.pdfY, pdfBR.Value.pdfY);
        var marqueePdfTop = Math.Max(pdfTL.Value.pdfY, pdfBR.Value.pdfY);

        var hits = FindObjectsInRect(
            allObjects, marqueePdfLeft, marqueePdfBottom, marqueePdfRight, marqueePdfTop);

        return ApplyModifier(hits, currentSelection, _marqueeModifier);
    }

    #endregion

    #region Lasso Selection

    public void StartLasso(Point screenPoint, SelectionModifier modifier)
    {
        _isLassoActive = true;
        _lassoModifier = modifier;
        _lassoPoints.Clear();
        _lassoPoints.Add(screenPoint);

        _lassoPolyline = new Polyline
        {
            Stroke = new SolidColorBrush(Color.FromArgb(200, 0, 120, 215)),
            StrokeThickness = 1,
            StrokeDashArray = new global::Avalonia.Collections.AvaloniaList<double> { 4, 2 },
            Fill = new SolidColorBrush(Color.FromArgb(20, 0, 120, 215)),
            Points = new global::Avalonia.Collections.AvaloniaList<Point> { screenPoint },
            IsHitTestVisible = false
        };
        _canvas.Children.Add(_lassoPolyline);
    }

    public void UpdateLasso(Point currentPoint)
    {
        if (!_isLassoActive || _lassoPolyline == null) return;

        _lassoPoints.Add(currentPoint);
        ((global::Avalonia.Collections.AvaloniaList<Point>)_lassoPolyline.Points).Add(currentPoint);
    }

    public MarqueeResult FinishLasso(
        List<PageObjectInfo> allObjects,
        List<PageObjectInfo> currentSelection)
    {
        if (!_isLassoActive) return new MarqueeResult(new List<PageObjectInfo>());

        if (_lassoPolyline != null)
        {
            _canvas.Children.Remove(_lassoPolyline);
            _lassoPolyline = null;
        }
        _isLassoActive = false;

        if (_lassoPoints.Count < 3)
            return new MarqueeResult(new List<PageObjectInfo>());

        // Convert lasso points to PDF coordinates
        var pdfPolygon = new List<(float x, float y)>();
        foreach (var sp in _lassoPoints)
        {
            var pdf = _screenToPdf(sp);
            if (pdf != null) pdfPolygon.Add(pdf.Value);
        }

        if (pdfPolygon.Count < 3)
            return new MarqueeResult(new List<PageObjectInfo>());

        var hits = FindObjectsInPolygon(allObjects, pdfPolygon);
        return ApplyModifier(hits, currentSelection, _lassoModifier);
    }

    #endregion

    #region Multi-Move

    public void StartMultiMove(
        Point screenPoint, List<PageObjectInfo> selectedObjects)
    {
        _isMultiMoving = true;
        _moveStartScreen = screenPoint;
        _moveHighlights.Clear();
    }

    /// <summary>
    /// Tracks highlight rectangles that are being moved so we can
    /// update their positions during drag.
    /// </summary>
    public void RegisterMoveHighlight(Rectangle rect)
    {
        _moveHighlights.Add((rect, Canvas.GetLeft(rect), Canvas.GetTop(rect)));
    }

    public void UpdateMultiMove(Point currentPoint)
    {
        if (!_isMultiMoving) return;

        var dx = currentPoint.X - _moveStartScreen.X;
        var dy = currentPoint.Y - _moveStartScreen.Y;

        foreach (var (rect, origLeft, origTop) in _moveHighlights)
        {
            Canvas.SetLeft(rect, origLeft + dx);
            Canvas.SetTop(rect, origTop + dy);
        }
    }

    /// <summary>
    /// Finishes multi-move and returns the PDF delta.
    /// </summary>
    public (float deltaPdfX, float deltaPdfY)? FinishMultiMove(Point endPoint)
    {
        if (!_isMultiMoving) return null;
        _isMultiMoving = false;

        var dx = endPoint.X - _moveStartScreen.X;
        var dy = endPoint.Y - _moveStartScreen.Y;

        // Convert screen delta to PDF delta
        var pdfOrigin = _screenToPdf(new Point(0, 0));
        var pdfDelta = _screenToPdf(new Point(dx, dy));
        if (pdfOrigin == null || pdfDelta == null) return null;

        return (pdfDelta.Value.pdfX - pdfOrigin.Value.pdfX,
                pdfDelta.Value.pdfY - pdfOrigin.Value.pdfY);
    }

    public void CancelMultiMove()
    {
        if (!_isMultiMoving) return;
        _isMultiMoving = false;

        // Restore original positions
        foreach (var (rect, origLeft, origTop) in _moveHighlights)
        {
            Canvas.SetLeft(rect, origLeft);
            Canvas.SetTop(rect, origTop);
        }
        _moveHighlights.Clear();
    }

    #endregion

    #region Geometry Helpers

    private List<PageObjectInfo> FindObjectsInRect(
        List<PageObjectInfo> objects,
        float left, float bottom, float right, float top)
    {
        var result = new List<PageObjectInfo>();
        foreach (var obj in objects)
        {
            if (ContainmentMode == SelectionContainment.FullyContained)
            {
                if (obj.Left >= left && obj.Right <= right &&
                    obj.Bottom >= bottom && obj.Top <= top)
                    result.Add(obj);
            }
            else
            {
                // Intersect: AABB overlap test
                if (obj.Right >= left && obj.Left <= right &&
                    obj.Top >= bottom && obj.Bottom <= top)
                    result.Add(obj);
            }
        }
        return result;
    }

    private List<PageObjectInfo> FindObjectsInPolygon(
        List<PageObjectInfo> objects,
        List<(float x, float y)> polygon)
    {
        var result = new List<PageObjectInfo>();
        foreach (var obj in objects)
        {
            var cx = (obj.Left + obj.Right) / 2f;
            var cy = (obj.Bottom + obj.Top) / 2f;

            if (ContainmentMode == SelectionContainment.FullyContained)
            {
                // All 4 corners must be inside polygon
                if (PointInPolygon(obj.Left, obj.Bottom, polygon) &&
                    PointInPolygon(obj.Right, obj.Bottom, polygon) &&
                    PointInPolygon(obj.Left, obj.Top, polygon) &&
                    PointInPolygon(obj.Right, obj.Top, polygon))
                    result.Add(obj);
            }
            else
            {
                // Center point inside polygon = intersect (simplified)
                if (PointInPolygon(cx, cy, polygon))
                    result.Add(obj);
            }
        }
        return result;
    }

    /// <summary>
    /// Ray-casting point-in-polygon test.
    /// </summary>
    private static bool PointInPolygon(
        float px, float py, List<(float x, float y)> polygon)
    {
        bool inside = false;
        int n = polygon.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            var (xi, yi) = polygon[i];
            var (xj, yj) = polygon[j];

            if (((yi > py) != (yj > py)) &&
                (px < (xj - xi) * (py - yi) / (yj - yi) + xi))
                inside = !inside;
        }
        return inside;
    }

    private MarqueeResult ApplyModifier(
        List<PageObjectInfo> hits,
        List<PageObjectInfo> currentSelection,
        SelectionModifier modifier)
    {
        var result = new List<PageObjectInfo>();

        switch (modifier)
        {
            case SelectionModifier.Replace:
                result.AddRange(hits);
                break;

            case SelectionModifier.Add:
                result.AddRange(currentSelection);
                foreach (var h in hits)
                {
                    if (!result.Any(r => r.Index == h.Index))
                        result.Add(h);
                }
                break;

            case SelectionModifier.Subtract:
                result.AddRange(currentSelection);
                result.RemoveAll(r => hits.Any(h => h.Index == r.Index));
                break;
        }

        return new MarqueeResult(result);
    }

    public void Cleanup()
    {
        if (_marqueeRect != null)
        {
            _canvas.Children.Remove(_marqueeRect);
            _marqueeRect = null;
        }
        if (_lassoPolyline != null)
        {
            _canvas.Children.Remove(_lassoPolyline);
            _lassoPolyline = null;
        }
        _isMarqueeActive = false;
        _isLassoActive = false;
        _isMultiMoving = false;
        _lassoPoints.Clear();
        _moveHighlights.Clear();
    }

    #endregion
}

public record MarqueeResult(List<PageObjectInfo> SelectedObjects);
