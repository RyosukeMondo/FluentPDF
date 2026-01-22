using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentPDF.Rendering.Interop.Verification.Reports;

/// <summary>
/// Exports validation reports to JSON format with indentation for readability.
/// </summary>
public class JsonReportExporter : IReportExporter<ValidationReport>
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Exports the validation report to a JSON file.
    /// </summary>
    /// <param name="report">The validation report to export.</param>
    /// <param name="outputPath">The file path where the JSON should be saved.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    public async Task ExportAsync(ValidationReport report, string outputPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = File.Create(outputPath);
        await JsonSerializer.SerializeAsync(fileStream, report, Options, cancellationToken);
    }
}
