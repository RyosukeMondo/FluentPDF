namespace FluentPDF.App.Testing.Verification;

/// <summary>
/// Verification rule that checks if expected output files exist.
/// Used to verify tests produced expected file outputs.
/// </summary>
public sealed class FileExistsRule : IVerificationRule
{
    private readonly List<string> _expectedFiles;
    private readonly List<string> _missingFiles = new();

    /// <summary>
    /// Gets the name of this verification rule.
    /// </summary>
    public string Name => "FileExists";

    /// <summary>
    /// Initializes a new instance of the FileExistsRule class.
    /// </summary>
    /// <param name="expectedFiles">List of file paths that must exist</param>
    public FileExistsRule(params string[] expectedFiles)
    {
        _expectedFiles = expectedFiles?.ToList() ?? throw new ArgumentNullException(nameof(expectedFiles));

        if (_expectedFiles.Count == 0)
        {
            throw new ArgumentException("At least one expected file must be specified", nameof(expectedFiles));
        }
    }

    /// <summary>
    /// Verifies that all expected files exist in the test result outputs.
    /// </summary>
    /// <param name="result">The test result to verify</param>
    /// <returns>True if all expected files exist, false otherwise</returns>
    public Task<bool> VerifyAsync(CliTestResult result)
    {
        if (result is null)
        {
            throw new ArgumentNullException(nameof(result));
        }

        _missingFiles.Clear();

        // Check if OutputFiles key exists in outputs
        if (!result.Outputs.TryGetValue("OutputFiles", out var outputFilesObj))
        {
            _missingFiles.AddRange(_expectedFiles);
            return Task.FromResult(false);
        }

        // Get list of actual output files
        var outputFiles = outputFilesObj as List<string> ?? new List<string>();

        // Check each expected file
        foreach (var expectedFile in _expectedFiles)
        {
            var exists = outputFiles.Any(f => Path.GetFileName(f).Equals(Path.GetFileName(expectedFile), StringComparison.OrdinalIgnoreCase))
                        || File.Exists(expectedFile);

            if (!exists)
            {
                _missingFiles.Add(expectedFile);
            }
        }

        return Task.FromResult(_missingFiles.Count == 0);
    }

    /// <summary>
    /// Gets a descriptive message explaining which files are missing.
    /// </summary>
    /// <returns>Human-readable failure message</returns>
    public string GetFailureMessage()
    {
        if (_missingFiles.Count == 0)
        {
            return "File existence check passed";
        }

        return $"Missing expected files: {string.Join(", ", _missingFiles)}";
    }
}
