// Copyright (c) 2025 FluentPDF. All rights reserved.

using Avalonia.Threading;
using FluentPDF.Core.ViewModels;
using FluentPDF.Avalonia.Views;
using FluentPDF.Rendering.Interop;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FluentPDF.Avalonia.Api.Endpoints;

public static class SearchEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/gui/search")
            .WithTags("Search");

        MapSearchEndpoint(group);
        MapSearchResultsEndpoint(group);
    }

    private static MainWindow? GetMainWindow()
    {
        var appInstance = (App)global::Avalonia.Application.Current!;
        return appInstance.MainWindow;
    }

    private static PdfViewerViewModel? GetActiveViewer()
    {
        return GetMainWindow()?.ViewModel.ActiveTab?.ViewerViewModel;
    }

    private static void MapSearchEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/", async (HttpContext ctx) =>
        {
            try
            {
                var body = await ctx.Request.ReadFromJsonAsync<SearchRequest>();
                if (string.IsNullOrWhiteSpace(body?.Query))
                    return Results.BadRequest(new { error = "query is required" });

                var tcs = new TaskCompletionSource<IResult>();
                var query = body.Query!;
                var caseSensitive = body.CaseSensitive;
                var wholeWord = body.WholeWord;

                // PDFium is NOT thread-safe — all PDFium calls must run on UI thread
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        tcs.SetResult(RunSearchDirect(query, caseSensitive, wholeWord));
                    }
                    catch (Exception ex)
                    {
                        tcs.SetResult(Results.Json(new { success = false, error = ex.ToString() }));
                    }
                });

                return await tcs.Task;
            }
            catch (Exception ex)
            {
                return Results.Json(new { success = false, error = ex.ToString() });
            }
        })
        .WithName("Search")
        .WithSummary("Search for text with bounding boxes and context snippets")
        .WithDescription(
            "Searches the active document for text matches. " +
            "Returns each match with bounding box coordinates and surrounding text context.");
    }

    /// <summary>
    /// Runs PDFium search directly on the calling thread (must be UI thread).
    /// Bypasses TextSearchService which uses Task.Run internally.
    /// Returns per-match bounding boxes and context snippets.
    /// </summary>
    private static IResult RunSearchDirect(string query, bool caseSensitive, bool wholeWord)
    {
        var viewer = GetActiveViewer();
        var document = viewer?.CurrentDocument;
        if (document == null)
            return Results.Json(new { success = false, error = "No active document" });

        var docHandle = (SafePdfDocumentHandle)document.Handle;
        var flags = PdfiumInterop.SearchFlags.None;
        if (caseSensitive) flags |= PdfiumInterop.SearchFlags.MatchCase;
        if (wholeWord) flags |= PdfiumInterop.SearchFlags.MatchWholeWord;

        var allMatches = new List<MatchResult>();

        for (int pageIdx = 0; pageIdx < document.PageCount; pageIdx++)
        {
            using var pageHandle = PdfiumInterop.LoadPage(docHandle, pageIdx);
            using var textPage = PdfiumInterop.LoadTextPage(pageHandle);
            var totalChars = PdfiumInterop.GetTextCharCount(textPage);

            var searchHandle = PdfiumInterop.StartTextSearch(textPage, query, flags);
            if (searchHandle == IntPtr.Zero)
                continue;

            try
            {
                while (PdfiumInterop.FindNext(searchHandle))
                {
                    var charIndex = PdfiumInterop.GetSearchResultIndex(searchHandle);
                    var matchLength = PdfiumInterop.GetSearchResultCount(searchHandle);
                    var text = PdfiumInterop.GetText(textPage, charIndex, matchLength);

                    var bbox = GetMatchBoundingBox(textPage, charIndex, matchLength);
                    var snippet = GetContextSnippet(textPage, charIndex, matchLength, totalChars);

                    allMatches.Add(new MatchResult(
                        pageIdx + 1, charIndex, matchLength, text, snippet, bbox));
                }
            }
            finally
            {
                PdfiumInterop.CloseSearch(searchHandle);
            }
        }

        var matches = allMatches.Select(m => new
        {
            m.PageNumber,
            m.Text,
            m.Snippet,
            boundingBox = new
            {
                m.BoundingBox.Left,
                m.BoundingBox.Top,
                m.BoundingBox.Right,
                m.BoundingBox.Bottom
            }
        }).ToArray();

        var pageGroups = allMatches
            .GroupBy(m => m.PageNumber)
            .Select(g => new
            {
                pageNumber = g.Key,
                matchCount = g.Count()
            })
            .OrderBy(p => p.pageNumber)
            .ToArray();

        return Results.Json(new
        {
            success = true,
            totalMatches = allMatches.Count,
            query,
            matches,
            pageGroups
        });
    }

    private static BoundingBox GetMatchBoundingBox(
        SafePdfTextPageHandle textPage, int charIndex, int matchLength)
    {
        // Use CountTextRects/GetTextRect for multi-character bounding box
        var rectCount = PdfiumInterop.CountTextRects(textPage.DangerousGetHandle(), charIndex, matchLength);

        if (rectCount > 0)
        {
            double unionLeft = double.MaxValue, unionTop = double.MinValue;
            double unionRight = double.MinValue, unionBottom = double.MaxValue;

            for (int r = 0; r < rectCount; r++)
            {
                if (PdfiumInterop.GetTextRect(textPage.DangerousGetHandle(), r,
                        out var rLeft, out var rTop, out var rRight, out var rBottom))
                {
                    unionLeft = Math.Min(unionLeft, rLeft);
                    unionTop = Math.Max(unionTop, rTop);
                    unionRight = Math.Max(unionRight, rRight);
                    unionBottom = Math.Min(unionBottom, rBottom);
                }
            }

            if (unionLeft < double.MaxValue)
                return new BoundingBox(unionLeft, unionTop, unionRight, unionBottom);
        }

        // Fallback: use first/last character boxes
        if (PdfiumInterop.GetCharBox(textPage, charIndex,
                out var left, out var top, out var right, out var bottom))
        {
            if (matchLength > 1 && PdfiumInterop.GetCharBox(textPage, charIndex + matchLength - 1,
                    out _, out var lastTop, out var lastRight, out var lastBottom))
            {
                return new BoundingBox(left, Math.Max(top, lastTop), lastRight, Math.Min(bottom, lastBottom));
            }
            return new BoundingBox(left, top, right, bottom);
        }

        return new BoundingBox(0, 0, 0, 0);
    }

    private static string GetContextSnippet(
        SafePdfTextPageHandle textPage, int charIndex, int matchLength, int totalChars)
    {
        const int contextChars = 30;
        var snippetStart = Math.Max(0, charIndex - contextChars);
        var snippetEnd = Math.Min(totalChars, charIndex + matchLength + contextChars);
        var snippetLength = snippetEnd - snippetStart;

        if (snippetLength <= 0)
            return string.Empty;

        var snippet = PdfiumInterop.GetText(textPage, snippetStart, snippetLength);
        var prefix = snippetStart > 0 ? "..." : "";
        var suffix = snippetEnd < totalChars ? "..." : "";
        return prefix + snippet.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ") + suffix;
    }

    private static void MapSearchResultsEndpoint(RouteGroupBuilder group)
    {
        group.MapGet("/results", async (HttpContext _) =>
        {
            var result = await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var viewer = GetActiveViewer();
                if (viewer == null)
                    return (object)new { success = false, error = "No active document" };

                var searchVm = App.GetService<SearchPanelViewModel>();

                var matches = searchVm.SearchMatches;
                var pageGroups = matches
                    .GroupBy(m => m.PageNumber)
                    .Select(g => new
                    {
                        pageNumber = g.Key + 1,
                        matchCount = g.Count(),
                        snippets = g.Select(m => m.Text).ToArray()
                    })
                    .OrderBy(p => p.pageNumber)
                    .ToArray();

                return (object)new
                {
                    success = true,
                    totalMatches = matches.Count,
                    currentMatchIndex = searchVm.CurrentMatchIndex,
                    searchQuery = searchVm.SearchQuery,
                    pages = pageGroups
                };
            });

            return Results.Json(result);
        })
        .WithName("GetSearchResults")
        .WithSummary("Get current search results")
        .WithDescription(
            "Returns the current search matches from the search panel.");
    }

    private record SearchRequest(
        string? Query,
        bool CaseSensitive = false,
        bool WholeWord = false);

    private record BoundingBox(double Left, double Top, double Right, double Bottom);

    private record MatchResult(
        int PageNumber,
        int CharIndex,
        int Length,
        string Text,
        string Snippet,
        BoundingBox BoundingBox);
}
