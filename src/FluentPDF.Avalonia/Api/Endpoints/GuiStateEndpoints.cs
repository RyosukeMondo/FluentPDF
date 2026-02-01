// Copyright (c) 2025 FluentPDF. All rights reserved.

using Avalonia.Media.Imaging;
using FluentPDF.Avalonia.Api.Models;
using FluentPDF.Avalonia.ViewModels;
using FluentPDF.Avalonia.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// GUI state inspection endpoints for verification and debugging.
/// </summary>
public static class GuiStateEndpoints
{
    /// <summary>
    /// Maps GUI state endpoints to the application.
    /// </summary>
    public static void Map(WebApplication app, MainViewModel mainViewModel, ILogBufferService? logBuffer = null)
    {
        // Get overall GUI state
        app.MapGet("/api/gui/state", () =>
        {
            var state = new
            {
                TabCount = mainViewModel.Tabs.Count,
                HasActiveTabs = mainViewModel.Tabs.Any(),
                ActiveTab = mainViewModel.ActiveTab != null ? new
                {
                    FilePath = mainViewModel.ActiveTab.FilePath,
                    FileName = mainViewModel.ActiveTab.FileName,
                    HasUnsavedChanges = mainViewModel.ActiveTab.HasUnsavedChanges
                } : null,
                Tabs = mainViewModel.Tabs.Select(t => new
                {
                    FilePath = t.FilePath,
                    FileName = t.FileName,
                    HasUnsavedChanges = t.HasUnsavedChanges
                }).ToList()
            };

            return Results.Ok(state);
        })
        .WithName("GetGuiState")
        .WithTags("GUI")
        .Produces<object>();

        // Get active viewer state
        app.MapGet("/api/gui/viewer/state", () =>
        {
            var activeTab = mainViewModel.ActiveTab;
            if (activeTab?.ViewerViewModel == null)
            {
                return Results.NotFound(new ErrorResponse(
                    "NO_ACTIVE_VIEWER",
                    "No active PDF viewer"));
            }

            var viewer = activeTab.ViewerViewModel;
            var bitmap = viewer.CurrentPageImage as Bitmap;
            var state = new
            {
                HasDocument = viewer.TotalPages > 0,
                IsLoading = viewer.IsLoading,
                CurrentPage = viewer.CurrentPageNumber,
                TotalPages = viewer.TotalPages,
                ZoomLevel = viewer.ZoomLevel,
                StatusMessage = viewer.StatusMessage,
                FilePath = activeTab.FilePath,
                HasRenderedImage = viewer.CurrentPageImage != null,
                ImageWidth = bitmap?.PixelSize.Width ?? 0,
                ImageHeight = bitmap?.PixelSize.Height ?? 0,
                ViewMode = viewer.ViewMode.ToString()
            };

            return Results.Ok(state);
        })
        .WithName("GetViewerState")
        .WithTags("GUI")
        .Produces<object>()
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        // Verify document is loaded in GUI
        app.MapGet("/api/gui/verify/document-loaded", (string? expectedPath = null) =>
        {
            var activeTab = mainViewModel.ActiveTab;
            if (activeTab?.ViewerViewModel == null)
            {
                return Results.Ok(new
                {
                    IsLoaded = false,
                    Reason = "No active tab or viewer",
                    TabCount = mainViewModel.Tabs.Count
                });
            }

            var viewer = activeTab.ViewerViewModel;
            var hasDocument = viewer.TotalPages > 0;
            var isLoaded = hasDocument && !viewer.IsLoading;

            var pathMatches = expectedPath == null ||
                string.Equals(activeTab.FilePath, expectedPath, StringComparison.OrdinalIgnoreCase);

            return Results.Ok(new
            {
                IsLoaded = isLoaded && pathMatches,
                HasDocument = hasDocument,
                IsLoading = viewer.IsLoading,
                CurrentPath = activeTab.FilePath,
                ExpectedPath = expectedPath,
                PathMatches = pathMatches,
                CurrentPage = viewer.CurrentPageNumber,
                TotalPages = viewer.TotalPages,
                HasRenderedImage = viewer.CurrentPageImage != null
            });
        })
        .WithName("VerifyDocumentLoaded")
        .WithTags("GUI")
        .Produces<object>();

        // Verify page is rendered
        app.MapGet("/api/gui/verify/page-rendered", (int? pageNumber = null) =>
        {
            var activeTab = mainViewModel.ActiveTab;
            if (activeTab?.ViewerViewModel == null)
            {
                return Results.Ok(new
                {
                    IsRendered = false,
                    Reason = "No active viewer"
                });
            }

            var viewer = activeTab.ViewerViewModel;
            var expectedPage = pageNumber ?? viewer.CurrentPageNumber;
            var isRendered = viewer.CurrentPageImage != null &&
                viewer.CurrentPageNumber == expectedPage;
            var bitmap = viewer.CurrentPageImage as Bitmap;

            return Results.Ok(new
            {
                IsRendered = isRendered,
                HasImage = viewer.CurrentPageImage != null,
                CurrentPage = viewer.CurrentPageNumber,
                ExpectedPage = expectedPage,
                PageMatches = viewer.CurrentPageNumber == expectedPage,
                ImageWidth = bitmap?.PixelSize.Width ?? 0,
                ImageHeight = bitmap?.PixelSize.Height ?? 0,
                IsLoading = viewer.IsLoading,
                StatusMessage = viewer.StatusMessage
            });
        })
        .WithName("VerifyPageRendered")
        .WithTags("GUI")
        .Produces<object>();

        // Trigger GUI action: Open file
        app.MapPost("/api/gui/action/open-file", async (
            OpenFileRequest request) =>
        {
            logBuffer?.AddLog("Info", $"[REST API] Received open-file request: {request.FilePath}", "GuiStateEndpoints");

            if (string.IsNullOrWhiteSpace(request.FilePath))
            {
                return Results.BadRequest(new ErrorResponse(
                    "INVALID_REQUEST",
                    "FilePath is required"));
            }

            if (!File.Exists(request.FilePath))
            {
                logBuffer?.AddLog("Error", $"[REST API] File not found: {request.FilePath}", "GuiStateEndpoints");
                return Results.NotFound(new ErrorResponse(
                    "FILE_NOT_FOUND",
                    $"File not found: {request.FilePath}"));
            }

            try
            {
                logBuffer?.AddLog("Info", "[REST API] Triggering file open via direct command execution...", "GuiStateEndpoints");

                // Execute command in background and poll for completion
                // Note: Static brushes are now initialized on UI thread during app startup
                bool commandExecuted = false;
                Exception? commandException = null;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        logBuffer?.AddLog("Info", "[REST API] Executing OpenRecentFileCommand...", "GuiStateEndpoints");
                        await mainViewModel.OpenRecentFileCommand.ExecuteAsync(request.FilePath);
                        logBuffer?.AddLog("Info", "[REST API] OpenRecentFileCommand completed successfully", "GuiStateEndpoints");
                        commandExecuted = true;
                    }
                    catch (Exception ex)
                    {
                        commandException = ex;
                        logBuffer?.AddLog("Error", $"[REST API] File open failed: {ex.Message}\n{ex.StackTrace}", "GuiStateEndpoints");
                    }
                });

                logBuffer?.AddLog("Info", "[REST API] Command execution started. Polling for completion (30s timeout)...", "GuiStateEndpoints");

                // Poll for completion with timeout
                var startTime = DateTime.UtcNow;
                var timeout = TimeSpan.FromSeconds(30);
                while ((DateTime.UtcNow - startTime) < timeout)
                {
                    await Task.Delay(500);

                    // Check if command executed or failed
                    if (commandExecuted)
                    {
                        logBuffer?.AddLog("Info", "[REST API] Command execution confirmed", "GuiStateEndpoints");
                        break;
                    }

                    if (commandException != null)
                    {
                        logBuffer?.AddLog("Error", $"[REST API] Command failed: {commandException.Message}", "GuiStateEndpoints");
                        break;
                    }

                    // Check if document loaded (alternative verification)
                    var docLoaded = (mainViewModel.ActiveTab?.ViewerViewModel?.TotalPages ?? 0) > 0;
                    if (docLoaded)
                    {
                        logBuffer?.AddLog("Info", "[REST API] Document detected via state polling", "GuiStateEndpoints");
                        commandExecuted = true;
                        break;
                    }
                }

                if (!commandExecuted && commandException == null && (DateTime.UtcNow - startTime) >= timeout)
                {
                    logBuffer?.AddLog("Error", "[REST API] Timeout waiting for file open to complete", "GuiStateEndpoints");
                }

                // Give rendering a moment
                await Task.Delay(1000);

                // Read final state directly (avoid Dispatcher.UIThread.InvokeAsync which hangs)
                var activeTab = mainViewModel.ActiveTab;
                var hasDocument = (activeTab?.ViewerViewModel?.TotalPages ?? 0) > 0;
                var success = activeTab != null && hasDocument;

                logBuffer?.AddLog("Info", $"[REST API] Final state - TabCreated: {activeTab != null}, DocumentLoaded: {hasDocument}, PageCount: {activeTab?.ViewerViewModel?.TotalPages ?? 0}", "GuiStateEndpoints");

                var result = new
                {
                    Success = success,
                    FilePath = request.FilePath,
                    TabCreated = activeTab != null,
                    DocumentLoaded = hasDocument,
                    PageCount = activeTab?.ViewerViewModel?.TotalPages ?? 0,
                    Message = success ? "Document loaded in GUI" : "Document load initiated but not confirmed"
                };

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                logBuffer?.AddLog("Error", $"[REST API] CRITICAL ERROR: {ex.Message}\n{ex.StackTrace}", "GuiStateEndpoints");
                return Results.Json(
                    new ErrorResponse("LOAD_FAILED", ex.Message),
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        })
        .WithName("OpenFileInGui")
        .WithTags("GUI")
        .Produces<object>()
        .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
        .Produces<ErrorResponse>(StatusCodes.Status404NotFound)
        .Produces<ErrorResponse>(StatusCodes.Status500InternalServerError);
    }
}

/// <summary>
/// Request to open a file in the GUI.
/// </summary>
/// <param name="FilePath">Path to the file to open.</param>
public record OpenFileRequest(string FilePath);
