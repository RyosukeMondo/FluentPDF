using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using FluentPDF.Rendering.Interop;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Rendering.Services;

/// <summary>
/// Records a matrix modification: original matrix values → new matrix values.
/// Used to patch content stream Tm/cm operators without calling GenerateContent.
/// </summary>
public record MatrixPatch(
    float OrigA, float OrigB, float OrigC, float OrigD, float OrigE, float OrigF,
    float NewA, float NewB, float NewC, float NewD, float NewE, float NewF);

/// <summary>
/// Patches PDF content streams directly to avoid FPDFPage_GenerateContent,
/// which corrupts CIDFont Japanese text. Uses QPDF to read/write streams.
/// </summary>
public sealed class ContentStreamPatcher
{
    private readonly ILogger<ContentStreamPatcher> _logger;

    // Matrix patches per page: key = pageIndex (0-based)
    private readonly Dictionary<int, List<MatrixPatch>> _patches = new();

    // New object operators to append per page (raw PDF operator bytes)
    private readonly Dictionary<int, List<byte[]>> _newObjectOps = new();

    public ContentStreamPatcher(ILogger<ContentStreamPatcher> logger)
    {
        _logger = logger;
    }

    public void RecordMatrixPatch(int pageIndex, MatrixPatch patch)
    {
        if (!_patches.TryGetValue(pageIndex, out var list))
        {
            list = new List<MatrixPatch>();
            _patches[pageIndex] = list;
        }
        list.Add(patch);
    }

    public void RecordNewObjectOperators(int pageIndex, byte[] operators)
    {
        if (!_newObjectOps.TryGetValue(pageIndex, out var list))
        {
            list = new List<byte[]>();
            _newObjectOps[pageIndex] = list;
        }
        list.Add(operators);
    }

    public bool HasPendingChanges => _patches.Count > 0 || _newObjectOps.Count > 0;

    public IReadOnlySet<int> GetModifiedPages()
    {
        var pages = new HashSet<int>(_patches.Keys);
        foreach (var k in _newObjectOps.Keys) pages.Add(k);
        return pages;
    }

    /// <summary>
    /// Saves the document by patching content streams via QPDF.
    /// Reads from inputPath, applies patches, writes to outputPath.
    /// </summary>
    public bool SaveWithPatches(string inputPath, string outputPath)
    {
        if (!QpdfNative.Initialize())
        {
            _logger.LogError("Failed to initialize QPDF");
            return false;
        }

        using var job = QpdfNative.CreateJob();
        var readResult = QpdfNative.ReadDocument(job, inputPath);
        if (readResult != QpdfNative.ErrorCodes.Success)
        {
            _logger.LogError("QPDF failed to read {Path}: {Error}", inputPath, QpdfNative.TranslateErrorCode(readResult));
            return false;
        }

        var modifiedPages = GetModifiedPages();
        foreach (var pageIdx in modifiedPages)
        {
            try
            {
                PatchPage(job, pageIdx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to patch page {PageIndex}", pageIdx);
                return false;
            }
        }

        var writeResult = QpdfNative.WriteDocument(job, outputPath);
        if (writeResult != QpdfNative.ErrorCodes.Success)
        {
            _logger.LogError("QPDF failed to write {Path}: {Error}", outputPath, QpdfNative.TranslateErrorCode(writeResult));
            return false;
        }

        _logger.LogInformation("Saved with content stream patches to {Path} ({PageCount} pages patched)",
            outputPath, modifiedPages.Count);
        return true;
    }

    public void Clear()
    {
        _patches.Clear();
        _newObjectOps.Clear();
    }

    private void PatchPage(SafeQpdfJobHandle job, int pageIdx)
    {
        // QPDF uses 0-based page numbers in qpdf_get_page_n
        var pageOh = QpdfNative.GetPageHandle(job, pageIdx);
        if (pageOh == 0)
        {
            _logger.LogWarning("Could not get page handle for page {Index}", pageIdx);
            return;
        }

        var contentsOh = QpdfNative.GetObjectKey(job, pageOh, "/Contents");
        if (contentsOh == 0)
        {
            _logger.LogWarning("No /Contents for page {Index}", pageIdx);
            return;
        }

        // Get existing content stream bytes
        byte[] originalStreamBytes;
        uint streamOhToReplace;

        if (QpdfNative.IsStream(job, contentsOh))
        {
            originalStreamBytes = ReadStreamData(job, contentsOh);
            streamOhToReplace = contentsOh;
        }
        else if (QpdfNative.IsArray(job, contentsOh))
        {
            // Concatenate all streams in the array
            var sb = new List<byte>();
            int n = QpdfNative.GetArrayNItems(job, contentsOh);
            for (int i = 0; i < n; i++)
            {
                var itemOh = QpdfNative.GetArrayItem(job, contentsOh, i);
                if (QpdfNative.IsStream(job, itemOh))
                {
                    var data = ReadStreamData(job, itemOh);
                    sb.AddRange(data);
                    sb.Add((byte)'\n');
                }
            }
            originalStreamBytes = sb.ToArray();
            streamOhToReplace = contentsOh; // We'll replace with a single stream
        }
        else
        {
            _logger.LogWarning("Unexpected /Contents type for page {Index}", pageIdx);
            return;
        }

        // Apply matrix patches
        var patchedBytes = originalStreamBytes;
        if (_patches.TryGetValue(pageIdx, out var patches) && patches.Count > 0)
        {
            patchedBytes = PatchMatrices(patchedBytes, patches);
        }

        // Append new object operators
        if (_newObjectOps.TryGetValue(pageIdx, out var newOps) && newOps.Count > 0)
        {
            var combined = new List<byte>(patchedBytes);
            combined.Add((byte)'\n');
            foreach (var ops in newOps)
            {
                combined.AddRange(ops);
                combined.Add((byte)'\n');
            }
            patchedBytes = combined.ToArray();
        }

        // Replace content stream
        if (QpdfNative.IsStream(job, contentsOh))
        {
            var nullOh = QpdfNative.NewNull(job);
            QpdfNative.ReplaceStreamData(job, contentsOh, patchedBytes, nullOh, nullOh);
        }
        else if (QpdfNative.IsArray(job, contentsOh))
        {
            // Replace /Contents array with a single stream
            var newStreamOh = QpdfNative.NewStream(job, patchedBytes);
            QpdfNative.ReplaceKey(job, pageOh, "/Contents", newStreamOh);
        }

        _logger.LogInformation("Patched page {Index}: {OrigLen} → {NewLen} bytes, {PatchCount} matrix patches, {NewObjCount} new objects",
            pageIdx, originalStreamBytes.Length, patchedBytes.Length,
            patches?.Count ?? 0, newOps?.Count ?? 0);
    }

    private static byte[] ReadStreamData(SafeQpdfJobHandle job, uint streamOh)
    {
        var (bufp, length, _) = QpdfNative.GetStreamData(job, streamOh, 3); // decode_level=all
        if (bufp == IntPtr.Zero || length == 0)
            return Array.Empty<byte>();

        var data = new byte[length];
        Marshal.Copy(bufp, data, 0, length);
        return data;
    }

    /// <summary>
    /// Patches matrix operators (Tm/cm) in content stream bytes.
    /// Matches by original matrix values (within tolerance) and replaces with new values.
    /// </summary>
    private byte[] PatchMatrices(byte[] streamBytes, List<MatrixPatch> patches)
    {
        var text = Encoding.ASCII.GetString(streamBytes);
        var remaining = new List<MatrixPatch>(patches);

        // Tokenize and find matrix operators
        var tokens = Tokenize(text);
        var result = new StringBuilder(text.Length + 256);

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            // Check for "a b c d e f Tm" or "a b c d e f cm" pattern
            if ((token.Value == "Tm" || token.Value == "cm") && i >= 6)
            {
                // Try to parse the 6 preceding tokens as floats
                if (TryParseFloat(tokens[i - 6].Value, out var a) &&
                    TryParseFloat(tokens[i - 5].Value, out var b) &&
                    TryParseFloat(tokens[i - 4].Value, out var c) &&
                    TryParseFloat(tokens[i - 3].Value, out var d) &&
                    TryParseFloat(tokens[i - 2].Value, out var e) &&
                    TryParseFloat(tokens[i - 1].Value, out var f))
                {
                    // Find matching patch
                    var matchIdx = remaining.FindIndex(p =>
                        MatrixClose(p.OrigA, a) && MatrixClose(p.OrigB, b) &&
                        MatrixClose(p.OrigC, c) && MatrixClose(p.OrigD, d) &&
                        MatrixClose(p.OrigE, e) && MatrixClose(p.OrigF, f));

                    if (matchIdx >= 0)
                    {
                        var patch = remaining[matchIdx];
                        remaining.RemoveAt(matchIdx);

                        // Remove the 6 number tokens we already wrote and replace
                        // We need to go back in the result and replace the last 6 tokens
                        // Easier: rebuild from token positions

                        // Remove last 6 token outputs from result
                        var resultStr = result.ToString();
                        // Find where the 6th-back token starts
                        var cutPos = tokens[i - 6].StartInOutput;
                        result.Clear();
                        result.Append(resultStr, 0, cutPos);

                        // Write new matrix values
                        result.Append(Fmt(patch.NewA)).Append(' ');
                        result.Append(Fmt(patch.NewB)).Append(' ');
                        result.Append(Fmt(patch.NewC)).Append(' ');
                        result.Append(Fmt(patch.NewD)).Append(' ');
                        result.Append(Fmt(patch.NewE)).Append(' ');
                        result.Append(Fmt(patch.NewF)).Append(' ');
                        result.Append(token.Value);

                        // Update output positions for remaining tokens
                        for (int j = i + 1; j < tokens.Count; j++)
                            tokens[j] = tokens[j] with { StartInOutput = -1 };

                        continue;
                    }
                }
            }

            // Track output position before writing
            tokens[i] = tokens[i] with { StartInOutput = result.Length };

            // Write token with original whitespace
            if (i > 0 && token.StartPos > tokens[i - 1].EndPos)
            {
                result.Append(text, tokens[i - 1].EndPos, token.StartPos - tokens[i - 1].EndPos);
            }
            else if (i > 0)
            {
                result.Append(' ');
            }
            result.Append(token.Value);
        }

        if (remaining.Count > 0)
            _logger.LogWarning("{Count} matrix patches could not be matched in content stream", remaining.Count);

        return Encoding.ASCII.GetBytes(result.ToString());
    }

    private record struct Token(string Value, int StartPos, int EndPos, int StartInOutput);

    private static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < text.Length)
        {
            // Skip whitespace
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            if (i >= text.Length) break;

            var start = i;
            char ch = text[i];

            if (ch == '%')
            {
                // Comment - skip to end of line
                while (i < text.Length && text[i] != '\n' && text[i] != '\r') i++;
                // Include comment as a token to preserve it
                tokens.Add(new Token(text[start..i], start, i, -1));
            }
            else if (ch == '(')
            {
                // String literal - handle nested parens and escapes
                int depth = 1;
                i++;
                while (i < text.Length && depth > 0)
                {
                    if (text[i] == '\\') { i += 2; continue; }
                    if (text[i] == '(') depth++;
                    else if (text[i] == ')') depth--;
                    i++;
                }
                tokens.Add(new Token(text[start..i], start, i, -1));
            }
            else if (ch == '<')
            {
                if (i + 1 < text.Length && text[i + 1] == '<')
                {
                    // Dictionary
                    tokens.Add(new Token("<<", start, i + 2, -1));
                    i += 2;
                }
                else
                {
                    // Hex string
                    i++;
                    while (i < text.Length && text[i] != '>') i++;
                    if (i < text.Length) i++;
                    tokens.Add(new Token(text[start..i], start, i, -1));
                }
            }
            else if (ch == '>' && i + 1 < text.Length && text[i + 1] == '>')
            {
                tokens.Add(new Token(">>", start, i + 2, -1));
                i += 2;
            }
            else if (ch == '[' || ch == ']')
            {
                tokens.Add(new Token(ch.ToString(), start, i + 1, -1));
                i++;
            }
            else if (ch == '/')
            {
                // Name
                i++;
                while (i < text.Length && !char.IsWhiteSpace(text[i]) &&
                       text[i] != '/' && text[i] != '(' && text[i] != '<' &&
                       text[i] != '[' && text[i] != ']' && text[i] != '>' &&
                       text[i] != ')') i++;
                tokens.Add(new Token(text[start..i], start, i, -1));
            }
            else
            {
                // Number or operator
                while (i < text.Length && !char.IsWhiteSpace(text[i]) &&
                       text[i] != '/' && text[i] != '(' && text[i] != '<' &&
                       text[i] != '[' && text[i] != ']' && text[i] != '>' &&
                       text[i] != ')' && text[i] != '%') i++;
                tokens.Add(new Token(text[start..i], start, i, -1));
            }
        }
        return tokens;
    }

    private static bool TryParseFloat(string s, out float value)
    {
        return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool MatrixClose(float a, float b)
    {
        return Math.Abs(a - b) < 0.01f;
    }

    private static string Fmt(float v)
    {
        return v.ToString("G6", CultureInfo.InvariantCulture);
    }
}
