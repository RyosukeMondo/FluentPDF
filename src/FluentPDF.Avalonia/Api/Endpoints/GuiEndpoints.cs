// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.Avalonia.Views;
using FluentPDF.Core.ViewModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// Coordinator for GUI automation endpoints. Delegates to focused sub-modules:
/// <see cref="GuiStateEndpoints"/> — UI state queries, panel visibility, screenshots.
/// <see cref="GuiNavigationEndpoints"/> — page navigation, zoom, refresh, page operations.
/// <see cref="GuiActionEndpoints"/> — open, draw, annotate, watermark, shapes, save.
/// All GUI access is marshalled to the Avalonia UI thread.
/// </summary>
public static class GuiEndpoints
{
    /// <summary>
    /// Maps all GUI automation endpoints under /api/gui.
    /// </summary>
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/gui")
            .WithTags("GUI Automation");

        GuiStateEndpoints.MapGuiStateEndpoints(group);
        GuiNavigationEndpoints.MapGuiNavigationEndpoints(group);
        GuiActionEndpoints.MapGuiActionEndpoints(group);
    }

    /// <summary>
    /// Gets the main application window. Shared by all GUI endpoint modules.
    /// Must be called on the UI thread or within a Dispatcher.UIThread context.
    /// </summary>
    internal static MainWindow? GetMainWindow()
    {
        var appInstance = (App)global::Avalonia.Application.Current!;
        return appInstance.MainWindow;
    }

    /// <summary>
    /// Gets the active PDF viewer ViewModel. Shared by all GUI endpoint modules.
    /// Must be called on the UI thread or within a Dispatcher.UIThread context.
    /// </summary>
    internal static PdfViewerViewModel? GetActiveViewer()
    {
        return GetMainWindow()?.ViewModel.ActiveTab?.ViewerViewModel;
    }
}
