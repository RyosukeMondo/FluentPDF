// Copyright (c) 2025 FluentPDF. All rights reserved.

using Avalonia.Threading;
using FluentPDF.Core.Services;
using FluentPDF.Core.ViewModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// POST /api/gui/smoke-test — comprehensive feature exercise endpoint.
/// Opens a PDF, exercises navigation, zoom, panels, search, edit tools, bookmarks,
/// text extraction, and returns pass/fail for each step.
/// </summary>
public static class SmokeTestEndpoint
{
    private record SmokeTestRequest(string? FilePath);

    public static void MapSmokeTestEndpoint(RouteGroupBuilder group)
    {
        group.MapPost("/smoke-test", async (HttpContext ctx) =>
        {
            var body = await ctx.Request.ReadFromJsonAsync<SmokeTestRequest>();
            var pdfPath = body?.FilePath ?? "C:/Users/ryosu/Downloads/Auction_Invitation_Fixed_Final.pdf";

            if (!File.Exists(pdfPath))
                return Results.BadRequest(new { error = $"File not found: {pdfPath}" });

            var results = new List<object>();

            async Task<bool> Step(string name, Func<Task<bool>> action)
            {
                try
                {
                    var ok = await action();
                    results.Add(new { step = name, pass = ok });
                    return ok;
                }
                catch (Exception ex)
                {
                    results.Add(new { step = name, pass = false, error = ex.Message });
                    return false;
                }
            }

            await RunOpenStep(Step, pdfPath);
            await RunDocLoadedStep(Step);
            await RunPageSummaryStep(Step);
            await RunNavigateStep(Step);
            await RunNavigateBackStep(Step);
            await RunZoomStep(Step);
            await RunPanelSteps(Step);
            await RunSearchStep(Step);
            await RunTextExtractStep(Step);
            await RunEditModeStep(Step);
            await RunUserBookmarkStep(Step);
            await RunToastStep(Step);
            await RunCloseTabStep(Step);

            var passed = results.Count(r => ((dynamic)r).pass == true);
            var total = results.Count;

            return Results.Json(new
            {
                summary = $"{passed}/{total} passed",
                allPassed = passed == total,
                passed,
                total,
                steps = results
            });
        })
        .WithName("SmokeTest")
        .WithSummary("Run comprehensive feature smoke test")
        .WithDescription("Exercises: open, navigation, zoom, panels, search, text, edit mode, bookmarks, toast, close.");
    }

    private static async Task RunOpenStep(Func<string, Func<Task<bool>>, Task<bool>> step, string pdfPath)
    {
        await step("open", async () =>
        {
            var vm = await Dispatcher.UIThread.InvokeAsync(() => GuiEndpoints.GetMainWindow()?.ViewModel);
            if (vm == null) return false;
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(async () =>
            {
                try { await vm.OpenFileInTabAsync(pdfPath); await Task.Delay(500); tcs.SetResult(true); }
                catch { tcs.SetResult(false); }
            });
            return await tcs.Task;
        });
    }

    private static async Task RunDocLoadedStep(Func<string, Func<Task<bool>>, Task<bool>> step)
    {
        await step("doc_loaded", async () =>
        {
            return await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var v = GuiEndpoints.GetActiveViewer();
                return v?.CurrentDocument != null && v.TotalPages > 0;
            });
        });
    }

    private static async Task RunPageSummaryStep(Func<string, Func<Task<bool>>, Task<bool>> step)
    {
        await step("page_summary", async () =>
        {
            return await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var v = GuiEndpoints.GetActiveViewer();
                return !string.IsNullOrEmpty(v?.PageSummary);
            });
        });
    }

    private static async Task RunNavigateStep(Func<string, Func<Task<bool>>, Task<bool>> step)
    {
        await step("navigate", async () =>
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(async () =>
            {
                var v = GuiEndpoints.GetActiveViewer();
                if (v == null || v.TotalPages < 2) { tcs.SetResult(v?.TotalPages == 1); return; }
                await v.GoToPageCommand.ExecuteAsync(2);
                await Task.Delay(300);
                tcs.SetResult(v.CurrentPageNumber == 2);
            });
            return await tcs.Task;
        });
    }

    private static async Task RunNavigateBackStep(Func<string, Func<Task<bool>>, Task<bool>> step)
    {
        await step("navigate_back", async () =>
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(async () =>
            {
                var v = GuiEndpoints.GetActiveViewer();
                if (v == null) { tcs.SetResult(false); return; }
                await v.GoToPageCommand.ExecuteAsync(1);
                await Task.Delay(300);
                tcs.SetResult(v.CurrentPageNumber == 1);
            });
            return await tcs.Task;
        });
    }

    private static async Task RunZoomStep(Func<string, Func<Task<bool>>, Task<bool>> step)
    {
        await step("zoom", async () =>
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(async () =>
            {
                var v = GuiEndpoints.GetActiveViewer();
                if (v == null) { tcs.SetResult(false); return; }
                await v.SetZoomCommand.ExecuteAsync(1.5);
                await Task.Delay(300);
                var zoomed = Math.Abs(v.ZoomLevel - 1.5) < 0.01;
                await v.SetZoomCommand.ExecuteAsync(1.0);
                await Task.Delay(300);
                tcs.SetResult(zoomed);
            });
            return await tcs.Task;
        });
    }

    private static async Task RunPanelSteps(Func<string, Func<Task<bool>>, Task<bool>> step)
    {
        foreach (var panel in new[] { "thumbnails", "bookmarks", "search", "annotations", "metadata" })
        {
            await step($"panel_{panel}", async () =>
            {
                return await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var v = GuiEndpoints.GetActiveViewer();
                    if (v == null) return false;
                    var cmd = panel switch
                    {
                        "thumbnails" => v.ToggleThumbnailsCommand,
                        "bookmarks" => v.ToggleBookmarksCommand,
                        "search" => v.ToggleSearchPanelCommand,
                        "annotations" => v.ToggleAnnotationsCommand,
                        "metadata" => v.ToggleMetadataCommand,
                        _ => null
                    };
                    if (cmd == null) return false;
                    cmd.Execute(null); // toggle on
                    cmd.Execute(null); // toggle off (restore)
                    return true;
                });
            });
        }
    }

    private static async Task RunSearchStep(Func<string, Func<Task<bool>>, Task<bool>> step)
    {
        await step("search", async () =>
        {
            var searchService = App.GetService<ITextSearchService>();
            var doc = await Dispatcher.UIThread.InvokeAsync(() => GuiEndpoints.GetActiveViewer()?.CurrentDocument);
            if (searchService == null || doc == null) return false;
            var result = await searchService.SearchAsync(doc, "a");
            return result.IsSuccess && result.Value.Count > 0;
        });
    }

    private static async Task RunTextExtractStep(Func<string, Func<Task<bool>>, Task<bool>> step)
    {
        await step("text_extract", async () =>
        {
            var textService = App.GetService<ITextExtractionService>();
            var doc = await Dispatcher.UIThread.InvokeAsync(() => GuiEndpoints.GetActiveViewer()?.CurrentDocument);
            if (textService == null || doc == null) return false;
            var result = await textService.ExtractTextAsync(doc, 1);
            return result.IsSuccess && !string.IsNullOrWhiteSpace(result.Value);
        });
    }

    private static async Task RunEditModeStep(Func<string, Func<Task<bool>>, Task<bool>> step)
    {
        await step("edit_mode", async () =>
        {
            return await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var v = GuiEndpoints.GetActiveViewer();
                if (v == null) return false;
                v.IsDrawingToolbarVisible = true;
                v.ActiveDrawingTool = DrawingTool.Rectangle;
                var on = v.IsDrawingToolbarVisible && v.ActiveDrawingTool == DrawingTool.Rectangle;
                v.IsDrawingToolbarVisible = false;
                v.ActiveDrawingTool = DrawingTool.None;
                return on;
            });
        });
    }

    private static async Task RunUserBookmarkStep(Func<string, Func<Task<bool>>, Task<bool>> step)
    {
        await step("user_bookmark", async () =>
        {
            return await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var v = GuiEndpoints.GetActiveViewer();
                if (v == null) return false;
                var before = v.IsCurrentPageBookmarked;
                v.ToggleUserBookmarkCommand.Execute(null);
                var after = v.IsCurrentPageBookmarked;
                v.ToggleUserBookmarkCommand.Execute(null); // restore
                return before != after;
            });
        });
    }

    private static async Task RunToastStep(Func<string, Func<Task<bool>>, Task<bool>> step)
    {
        await step("toast", () =>
        {
            var svc = App.GetService<INotificationService>();
            if (svc == null) return Task.FromResult(false);
            Dispatcher.UIThread.Post(() => svc.ShowInfo("Smoke test passed!"));
            return Task.FromResult(true);
        });
    }

    private static async Task RunCloseTabStep(Func<string, Func<Task<bool>>, Task<bool>> step)
    {
        await step("close_tab", async () =>
        {
            return await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var vm = GuiEndpoints.GetMainWindow()?.ViewModel;
                if (vm?.ActiveTab == null) return false;
                vm.CloseTabCommand.Execute(vm.ActiveTab);
                return true;
            });
        });
    }
}
