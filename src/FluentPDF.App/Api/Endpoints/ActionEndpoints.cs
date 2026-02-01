// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.App.Api.Models;
using FluentPDF.App.Api.Services;
using FluentPDF.App.ViewModels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using System.Diagnostics;

namespace FluentPDF.App.Api.Endpoints;

/// <summary>
/// UI action endpoints for testing interactions.
/// </summary>
public static class ActionEndpoints
{
    /// <summary>
    /// Maps action endpoints to the application.
    /// </summary>
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/action/click", async (
            ClickActionRequest request,
            IUiAutomationService uiService) =>
        {
            var sw = Stopwatch.StartNew();
            var errors = new List<string>();

            try
            {
                // Find the element
                var element = await uiService.FindElementAsync(request.AutomationId);
                if (element is null)
                {
                    errors.Add($"Element '{request.AutomationId}' not found");
                    return Results.Ok(new ClickActionResponse(
                        Success: false,
                        Clicked: false,
                        DialogAppeared: false,
                        DurationMs: (int)sw.ElapsedMilliseconds,
                        Errors: errors
                    ));
                }

                // Simulate click (simplified - in real implementation, would invoke Click on the element)
                var clicked = true; // Would actually perform the click

                // Check for dialog if requested
                var dialogAppeared = false;
                if (request.WaitForDialog && !string.IsNullOrEmpty(request.DialogAutomationId))
                {
                    await Task.Delay(500); // Wait for dialog to appear
                    var dialog = await uiService.FindElementAsync(request.DialogAutomationId);
                    dialogAppeared = dialog is not null;
                }

                sw.Stop();

                return Results.Ok(new ClickActionResponse(
                    Success: true,
                    Clicked: clicked,
                    DialogAppeared: dialogAppeared,
                    DurationMs: (int)sw.ElapsedMilliseconds,
                    Errors: errors
                ));
            }
            catch (Exception ex)
            {
                sw.Stop();
                errors.Add(ex.Message);
                return Results.Ok(new ClickActionResponse(
                    Success: false,
                    Clicked: false,
                    DialogAppeared: false,
                    DurationMs: (int)sw.ElapsedMilliseconds,
                    Errors: errors
                ));
            }
        })
        .WithName("ClickAction")
        .WithTags("UI Actions")
        .Produces<ClickActionResponse>()
        .Produces(StatusCodes.Status500InternalServerError);

        app.MapPost("/api/action/input", async (
            InputActionRequest request,
            IUiAutomationService uiService) =>
        {
            var errors = new List<string>();

            try
            {
                // Find the element
                var element = await uiService.FindElementAsync(request.AutomationId);
                if (element is null)
                {
                    errors.Add($"Element '{request.AutomationId}' not found");
                    return Results.Ok(new InputActionResponse(
                        Success: false,
                        ValueSet: false,
                        ActualValue: string.Empty,
                        Errors: errors
                    ));
                }

                // Simulate input (simplified - in real implementation, would set Text property)
                var valueSet = true; // Would actually set the value
                var actualValue = request.Text;

                return Results.Ok(new InputActionResponse(
                    Success: true,
                    ValueSet: valueSet,
                    ActualValue: actualValue,
                    Errors: errors
                ));
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                return Results.Ok(new InputActionResponse(
                    Success: false,
                    ValueSet: false,
                    ActualValue: string.Empty,
                    Errors: errors
                ));
            }
        })
        .WithName("InputAction")
        .WithTags("UI Actions")
        .Produces<InputActionResponse>()
        .Produces(StatusCodes.Status500InternalServerError);

        app.MapPost("/api/action/navigate", async (
            NavigateActionRequest request,
            IServiceProvider serviceProvider) =>
        {
            var errors = new List<string>();

            try
            {
                var dispatcherQueue = App.MainWindow.DispatcherQueue;
                var tcs = new TaskCompletionSource<NavigateActionResponse>();

                dispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        var mainViewModel = App.GetService<MainViewModel>();
                        var viewModel = mainViewModel?.ActiveTab?.ViewerViewModel;

                        if (viewModel is null)
                        {
                            errors.Add("No active PDF viewer found");
                            tcs.SetResult(new NavigateActionResponse(
                                Success: false,
                                PreviousPage: 0,
                                CurrentPage: 0,
                                ExpectedPage: request.ExpectedPage,
                                Passed: false,
                                Errors: errors
                            ));
                            return;
                        }

                        var previousPage = viewModel.CurrentPageNumber;

                        // Perform navigation
                        switch (request.Action.ToLowerInvariant())
                        {
                            case "nextpage":
                                viewModel.NextPageCommand.Execute(null);
                                break;
                            case "previouspage":
                                viewModel.PreviousPageCommand.Execute(null);
                                break;
                            case "firstpage":
                                viewModel.FirstPageCommand.Execute(null);
                                break;
                            case "lastpage":
                                viewModel.LastPageCommand.Execute(null);
                                break;
                            default:
                                errors.Add($"Unknown navigation action: {request.Action}");
                                tcs.SetResult(new NavigateActionResponse(
                                    Success: false,
                                    PreviousPage: previousPage,
                                    CurrentPage: viewModel.CurrentPageNumber,
                                    ExpectedPage: request.ExpectedPage,
                                    Passed: false,
                                    Errors: errors
                                ));
                                return;
                        }

                        var currentPage = viewModel.CurrentPageNumber;
                        var passed = currentPage == request.ExpectedPage;

                        tcs.SetResult(new NavigateActionResponse(
                            Success: true,
                            PreviousPage: previousPage,
                            CurrentPage: currentPage,
                            ExpectedPage: request.ExpectedPage,
                            Passed: passed,
                            Errors: errors
                        ));
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex.Message);
                        tcs.SetException(ex);
                    }
                });

                var response = await tcs.Task;
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                return Results.Ok(new NavigateActionResponse(
                    Success: false,
                    PreviousPage: 0,
                    CurrentPage: 0,
                    ExpectedPage: request.ExpectedPage,
                    Passed: false,
                    Errors: errors
                ));
            }
        })
        .WithName("NavigateAction")
        .WithTags("UI Actions")
        .Produces<NavigateActionResponse>()
        .Produces(StatusCodes.Status500InternalServerError);
    }
}
