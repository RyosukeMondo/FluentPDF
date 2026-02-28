using System.Globalization;
using System.Text;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Generates raw PDF content stream operators for shapes.
/// Used to bypass FPDFPage_GenerateContent which corrupts CIDFont text.
/// </summary>
public static class PdfOperatorWriter
{
    public static byte[] Rectangle(
        float x, float y, float width, float height,
        uint fillR, uint fillG, uint fillB, uint fillA,
        uint strokeR, uint strokeG, uint strokeB, uint strokeA,
        float strokeWidth)
    {
        var sb = new StringBuilder();
        sb.Append("q ");
        sb.AppendFormat(CultureInfo.InvariantCulture,
            "{0:G6} {1:G6} {2:G6} RG ", strokeR / 255f, strokeG / 255f, strokeB / 255f);
        sb.AppendFormat(CultureInfo.InvariantCulture,
            "{0:G6} {1:G6} {2:G6} rg ", fillR / 255f, fillG / 255f, fillB / 255f);
        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G6} w ", strokeWidth);
        sb.AppendFormat(CultureInfo.InvariantCulture,
            "{0:G6} {1:G6} {2:G6} {3:G6} re B Q", x, y, width, height);
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    public static byte[] Line(
        float x1, float y1, float x2, float y2,
        uint strokeR, uint strokeG, uint strokeB, uint strokeA,
        float strokeWidth)
    {
        var sb = new StringBuilder();
        sb.Append("q ");
        sb.AppendFormat(CultureInfo.InvariantCulture,
            "{0:G6} {1:G6} {2:G6} RG ", strokeR / 255f, strokeG / 255f, strokeB / 255f);
        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G6} w ", strokeWidth);
        sb.AppendFormat(CultureInfo.InvariantCulture,
            "{0:G6} {1:G6} m {2:G6} {3:G6} l S Q", x1, y1, x2, y2);
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    public static byte[] Circle(
        float cx, float cy, float r,
        uint fillR, uint fillG, uint fillB, uint fillA,
        uint strokeR, uint strokeG, uint strokeB, uint strokeA,
        float strokeWidth)
    {
        const float k = 0.5523f; // Bezier circle approximation
        var sb = new StringBuilder();
        sb.Append("q ");
        sb.AppendFormat(CultureInfo.InvariantCulture,
            "{0:G6} {1:G6} {2:G6} RG ", strokeR / 255f, strokeG / 255f, strokeB / 255f);
        sb.AppendFormat(CultureInfo.InvariantCulture,
            "{0:G6} {1:G6} {2:G6} rg ", fillR / 255f, fillG / 255f, fillB / 255f);
        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G6} w ", strokeWidth);

        // Move to top of circle
        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G6} {1:G6} m ", cx, cy + r);
        // Top-right quadrant
        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G6} {1:G6} {2:G6} {3:G6} {4:G6} {5:G6} c ",
            cx + r * k, cy + r, cx + r, cy + r * k, cx + r, cy);
        // Bottom-right quadrant
        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G6} {1:G6} {2:G6} {3:G6} {4:G6} {5:G6} c ",
            cx + r, cy - r * k, cx + r * k, cy - r, cx, cy - r);
        // Bottom-left quadrant
        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G6} {1:G6} {2:G6} {3:G6} {4:G6} {5:G6} c ",
            cx - r * k, cy - r, cx - r, cy - r * k, cx - r, cy);
        // Top-left quadrant
        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G6} {1:G6} {2:G6} {3:G6} {4:G6} {5:G6} c ",
            cx - r, cy + r * k, cx - r * k, cy + r, cx, cy + r);
        sb.Append("h B Q");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    public static byte[] FreehandPath(
        double[] points,
        uint strokeR, uint strokeG, uint strokeB, uint strokeA,
        float strokeWidth)
    {
        if (points.Length < 4) return Array.Empty<byte>();
        var sb = new StringBuilder();
        sb.Append("q ");
        sb.AppendFormat(CultureInfo.InvariantCulture,
            "{0:G6} {1:G6} {2:G6} RG ", strokeR / 255f, strokeG / 255f, strokeB / 255f);
        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G6} w ", strokeWidth);
        sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G6} {1:G6} m ", points[0], points[1]);
        for (int i = 2; i < points.Length; i += 2)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "{0:G6} {1:G6} l ", points[i], points[i + 1]);
        }
        sb.Append("S Q");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    /// <summary>
    /// Text operator. Note: font resource /Helv must exist in page /Resources.
    /// For standard fonts, PDFium would have added it during InsertPageObject.
    /// We rely on that font resource already being available.
    /// </summary>
    public static byte[] Text(
        float x, float y, string text, float fontSize, string fontResourceName,
        uint fillR, uint fillG, uint fillB, uint fillA)
    {
        var sb = new StringBuilder();
        sb.Append("BT ");
        sb.AppendFormat(CultureInfo.InvariantCulture, "/{0} {1:G6} Tf ", fontResourceName, fontSize);
        sb.AppendFormat(CultureInfo.InvariantCulture,
            "{0:G6} {1:G6} {2:G6} rg ", fillR / 255f, fillG / 255f, fillB / 255f);
        sb.AppendFormat(CultureInfo.InvariantCulture,
            "1 0 0 1 {0:G6} {1:G6} Tm ", x, y);
        // Escape special characters in PDF string
        var escaped = text.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
        sb.AppendFormat("({0}) Tj ET", escaped);
        return Encoding.ASCII.GetBytes(sb.ToString());
    }
}
