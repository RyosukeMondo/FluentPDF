namespace FluentPDF.Core.Models;

/// <summary>
/// Strongly-typed 0-based page index. Prevents accidental mixing with 1-based page numbers.
/// Use <see cref="FromPageNumber"/> to convert from user-facing 1-based numbers.
/// Use <see cref="ToPageNumber"/> to convert back for display.
/// </summary>
public readonly record struct PageIndex
{
    /// <summary>0-based page index value.</summary>
    public int Value { get; }

    public PageIndex(int zeroBasedIndex)
    {
        if (zeroBasedIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(zeroBasedIndex), "Page index must be >= 0");
        Value = zeroBasedIndex;
    }

    /// <summary>Converts from a 1-based page number (user-facing) to a 0-based PageIndex.</summary>
    public static PageIndex FromPageNumber(int oneBasedPageNumber)
    {
        if (oneBasedPageNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(oneBasedPageNumber), "Page number must be >= 1");
        return new PageIndex(oneBasedPageNumber - 1);
    }

    /// <summary>Returns the 1-based page number for user-facing display.</summary>
    public int ToPageNumber() => Value + 1;

    public override string ToString() => $"PageIndex({Value})";
}
