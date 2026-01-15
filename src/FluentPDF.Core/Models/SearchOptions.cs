namespace FluentPDF.Core.Models;

/// <summary>
/// Configuration options for PDF text search operations.
/// Controls search behavior such as case sensitivity and whole word matching.
/// </summary>
public sealed class SearchOptions
{
    /// <summary>
    /// Gets or initializes a value indicating whether the search should be case-sensitive.
    /// Default is false (case-insensitive search).
    /// </summary>
    public bool CaseSensitive { get; init; } = false;

    /// <summary>
    /// Gets or initializes a value indicating whether to match whole words only.
    /// When true, matches must be bounded by word boundaries (spaces, punctuation, etc.).
    /// Default is false (partial word matches allowed).
    /// </summary>
    public bool WholeWord { get; init; } = false;

    /// <summary>
    /// Gets the default search options (case-insensitive, partial word matching).
    /// </summary>
    public static SearchOptions Default { get; } = new();

    /// <summary>
    /// Creates search options for case-sensitive search.
    /// </summary>
    /// <returns>SearchOptions with CaseSensitive set to true.</returns>
    public static SearchOptions CaseSensitiveSearch() => new() { CaseSensitive = true };

    /// <summary>
    /// Creates search options for whole word search.
    /// </summary>
    /// <returns>SearchOptions with WholeWord set to true.</returns>
    public static SearchOptions WholeWordSearch() => new() { WholeWord = true };

    /// <summary>
    /// Creates search options for case-sensitive whole word search.
    /// </summary>
    /// <returns>SearchOptions with both CaseSensitive and WholeWord set to true.</returns>
    public static SearchOptions CaseSensitiveWholeWordSearch() => new()
    {
        CaseSensitive = true,
        WholeWord = true
    };
}
