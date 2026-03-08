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
        .WithSummary("Trigger a text search")
        .WithDescription(
            "Searches the active document for text matches.");
    }

    /// <summary>
    /// Runs PDFium search directly on the calling thread (must be UI thread).
    /// Bypasses TextSearchService which uses Task.Run internally.
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

        var allMatches = new List<object>();

        for (int pageIdx = 0; pageIdx < document.PageCount; pageIdx++)
        {
            using var pageHandle = PdfiumInterop.LoadPage(docHandle, pageIdx);
            using var textPage = PdfiumInterop.LoadTextPage(pageHandle);

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

                    allMatches.Add(new
                    {
                        pageNumber = pageIdx,
                        charIndex,
                        length = matchLength,
                        text
                    });
                }
            }
            finally
            {
                PdfiumInterop.CloseSearch(searchHandle);
            }
        }

        var pageGroups = allMatches
            .GroupBy(m => ((dynamic)m).pageNumber)
            .Select(g => new
            {
                pageNumber = (int)g.Key + 1,
                matchCount = g.Count(),
                snippets = g.Select(m => ((dynamic)m).text as string).ToArray()
            })
            .OrderBy(p => p.pageNumber)
            .ToArray();

        return Results.Json(new
        {
            success = true,
            totalMatches = allMatches.Count,
            pages = pageGroups
        });
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
}
