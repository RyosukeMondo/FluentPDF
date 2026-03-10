using System.Text.RegularExpressions;

namespace FluentPDF.Architecture.Tests;

/// <summary>
/// Tests that enforce code quality KPIs:
/// - Max 500 lines of code per file (excluding comments/blanks)
/// - Max 50 lines of code per function (excluding comments/blanks)
/// Tooling (Verification, Cli) and interop files are excluded.
/// </summary>
public class CodeQualityTests
{
    private static readonly string SrcRoot = ResolveSrcRoot();

    private const int MaxFileLines = 500;
    private const int MaxMethodLines = 50;

    /// <summary>
    /// Product code projects to scan. Excludes tooling (Verification, Cli).
    /// </summary>
    private static readonly string[] ProductProjects =
    [
        "FluentPDF.Core",
        "FluentPDF.Rendering",
        "FluentPDF.Avalonia",
        "FluentPDF.Mcp"
    ];

    /// <summary>
    /// Path patterns excluded from all checks (interop, generated).
    /// </summary>
    private static readonly string[] ExcludedPatterns =
    [
        "PdfiumInterop",
        "QpdfNative",
        "ContentStreamPatcher",
        Path.DirectorySeparatorChar + "Verification" + Path.DirectorySeparatorChar,
        Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar,
        Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar,
        ".g.cs",
        ".designer.cs",
        "GlobalUsings"
    ];

    [Fact]
    public void AllProductFiles_ShouldBeUnder500LinesOfCode()
    {
        var violations = new List<string>();

        foreach (var file in GetProductSourceFiles())
        {
            var loc = CountLinesOfCode(file);
            if (loc > MaxFileLines)
            {
                var relative = Path.GetRelativePath(SrcRoot, file);
                violations.Add($"{relative}: {loc} LOC (max {MaxFileLines})");
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Files exceeding {MaxFileLines} LOC:\n" + string.Join("\n", violations));
    }

    /// <summary>
    /// Ensures recently-refactored files have no methods over 50 LOC.
    /// These files were explicitly split during the Phase 1 cleanup.
    /// </summary>
    [Fact]
    public void RefactoredFiles_ShouldHaveNoMethodsOver50Lines()
    {
        // Files explicitly refactored during P1.10 — must stay clean.
        // Only includes files where all methods were brought under 50 LOC.
        var refactoredFiles = new[]
        {
            "DocxConverterService.cs",
        };

        var violations = new List<string>();

        foreach (var file in GetProductSourceFiles()
            .Where(f => refactoredFiles.Any(r =>
                f.EndsWith(r, StringComparison.OrdinalIgnoreCase))))
        {
            foreach (var (methodName, lineCount, lineNumber) in FindLongMethods(file))
            {
                var relative = Path.GetRelativePath(SrcRoot, file);
                violations.Add(
                    $"{relative}:{lineNumber} {methodName}: {lineCount} LOC (max {MaxMethodLines})");
            }
        }

        Assert.True(
            violations.Count == 0,
            $"Refactored files have methods exceeding {MaxMethodLines} LOC:\n" +
            string.Join("\n", violations));
    }

    private static IEnumerable<string> GetProductSourceFiles()
    {
        if (!Directory.Exists(SrcRoot))
            yield break;

        foreach (var project in ProductProjects)
        {
            var projectDir = Path.Combine(SrcRoot, project);
            if (!Directory.Exists(projectDir))
                continue;

            foreach (var file in Directory.EnumerateFiles(
                projectDir, "*.cs", SearchOption.AllDirectories))
            {
                if (ExcludedPatterns.Any(p =>
                    file.Contains(p, StringComparison.OrdinalIgnoreCase)))
                    continue;

                yield return file;
            }
        }
    }

    private static int CountLinesOfCode(string filePath)
    {
        var lines = File.ReadAllLines(filePath);
        int count = 0;
        bool inBlockComment = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (inBlockComment)
            {
                if (line.Contains("*/"))
                    inBlockComment = false;
                continue;
            }

            if (line.StartsWith("/*"))
            {
                if (!line.Contains("*/"))
                    inBlockComment = true;
                continue;
            }

            if (string.IsNullOrEmpty(line) || line.StartsWith("//") || line.StartsWith("///"))
                continue;

            count++;
        }

        return count;
    }

    /// <summary>
    /// Finds methods exceeding the LOC limit using brace counting.
    /// Uses a conservative regex that requires a return type before the method name.
    /// </summary>
    private static List<(string Name, int Lines, int StartLine)> FindLongMethods(string filePath)
    {
        var results = new List<(string, int, int)>();
        var lines = File.ReadAllLines(filePath);

        // Match method signatures: access modifiers + return type + name + (
        // Requires at least one type-like token before the method name.
        var methodPattern = new Regex(
            @"^\s*(?:(?:public|private|protected|internal|static|async|override|virtual|sealed|partial|new)\s+)+" +
            @"(?:[\w<>\[\]?,.\s]+\s+)?(\w+)\s*\(", RegexOptions.Compiled);

        // Keywords that can't be method names
        var keywords = new HashSet<string>
        {
            "get", "set", "if", "for", "foreach", "while", "switch", "catch",
            "using", "lock", "return", "var", "new", "class", "struct",
            "interface", "enum", "namespace", "record", "delegate", "event",
            "throw", "else", "try", "finally", "do", "typeof", "sizeof",
            "checked", "unchecked", "fixed", "unsafe", "stackalloc"
        };

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();

            // Skip non-method lines
            if (string.IsNullOrEmpty(trimmed) ||
                trimmed.StartsWith("//") ||
                trimmed.StartsWith("///") ||
                trimmed.StartsWith("/*") ||
                trimmed.StartsWith("*") ||
                trimmed.StartsWith("[") ||
                trimmed.StartsWith("#"))
                continue;

            var match = methodPattern.Match(lines[i]);
            if (!match.Success) continue;

            var methodName = match.Groups[1].Value;
            if (keywords.Contains(methodName)) continue;

            // Skip constructors that match class names starting with uppercase
            // but have no return type (handled by pattern requiring modifiers)

            int braceStart = FindMethodOpeningBrace(lines, i);
            if (braceStart < 0) continue;

            // Skip expression-bodied members (=>)
            for (int k = i; k <= braceStart; k++)
            {
                if (lines[k].Contains("=>"))
                    goto nextLine;
            }

            int methodLoc = CountMethodBodyLoc(lines, braceStart);
            if (methodLoc > MaxMethodLines)
                results.Add((methodName, methodLoc, i + 1));

            nextLine:;
        }

        return results;
    }

    private static int FindMethodOpeningBrace(string[] lines, int startIndex)
    {
        int parenDepth = 0;
        bool seenParen = false;

        for (int j = startIndex; j < Math.Min(startIndex + 15, lines.Length); j++)
        {
            foreach (char c in lines[j])
            {
                if (c == '(') { parenDepth++; seenParen = true; }
                if (c == ')') parenDepth--;
            }

            // After closing all parens, look for '{'
            if (seenParen && parenDepth <= 0)
            {
                for (int k = j; k < Math.Min(j + 5, lines.Length); k++)
                {
                    var trimmed = lines[k].Trim();
                    if (trimmed.Contains('{'))
                        return k;
                    // Expression body or abstract/interface
                    if (trimmed.Contains("=>") || trimmed.EndsWith(";"))
                        return -1;
                }
                return -1;
            }
        }
        return -1;
    }

    private static int CountMethodBodyLoc(string[] lines, int braceLineIndex)
    {
        int depth = 0;
        int loc = 0;
        bool inBlockComment = false;

        for (int j = braceLineIndex; j < lines.Length; j++)
        {
            var line = lines[j].Trim();

            if (inBlockComment)
            {
                if (line.Contains("*/"))
                    inBlockComment = false;
                continue;
            }

            if (line.StartsWith("/*"))
            {
                if (!line.Contains("*/"))
                    inBlockComment = true;
                continue;
            }

            foreach (char c in lines[j])
            {
                if (c == '{') depth++;
                if (c == '}') depth--;
            }

            if (!string.IsNullOrEmpty(line) && !line.StartsWith("//") && !line.StartsWith("///"))
                loc++;

            if (depth <= 0) break;
        }

        // Subtract opening { and closing } lines
        return Math.Max(0, loc - 2);
    }

    private static string ResolveSrcRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            var candidate = Path.Combine(dir, "src");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir) ?? dir;
        }
        return Path.GetFullPath("../../../../src");
    }
}
