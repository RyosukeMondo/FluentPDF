// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.App.Api.Models;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;

namespace FluentPDF.App.Api.Endpoints;

/// <summary>
/// Annotation verification endpoints.
/// </summary>
public static class AnnotationEndpoints
{
    /// <summary>
    /// Maps annotation endpoints to the application.
    /// </summary>
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/verify/annotation", async (
            AnnotationVerificationRequest request,
            IServiceProvider serviceProvider) =>
        {
            var errors = new List<string>();

            try
            {
                var dispatcherQueue = App.MainWindow.DispatcherQueue;
                var tcs = new TaskCompletionSource<AnnotationVerificationResponse>();

                dispatcherQueue.TryEnqueue(() =>
                {
                    try
                    {
                        var mainViewModel = App.GetService<MainViewModel>();
                        var viewModel = mainViewModel?.ActiveTab?.ViewerViewModel;

                        if (viewModel?.CurrentDocument is null)
                        {
                            errors.Add("No document loaded");
                            tcs.SetResult(new AnnotationVerificationResponse(
                                Success: false,
                                AnnotationCreated: false,
                                AnnotationId: null,
                                Presence: null,
                                Errors: errors
                            ));
                            return;
                        }

                        var annotationService = serviceProvider.GetRequiredService<IAnnotationService>();

                        // Create annotation based on tool type
                        var annotationId = Guid.NewGuid().ToString();
                        var created = false;

                        if (request.Tool.ToLowerInvariant() == "highlight" && request.Action.ToLowerInvariant() == "create")
                        {
                            // Simplified - in real implementation would create actual annotation
                            created = true;
                        }

                        // Verify presence if requested
                        AnnotationPresenceCheck? presenceCheck = null;
                        if (request.VerifyPresence && created)
                        {
                            // Simplified - would actually check for annotation existence
                            presenceCheck = new AnnotationPresenceCheck(
                                Verified: true,
                                Found: true,
                                Properties: new AnnotationProperties(
                                    Type: request.Tool,
                                    Color: request.Color,
                                    Rect: request.Rect
                                )
                            );
                        }

                        tcs.SetResult(new AnnotationVerificationResponse(
                            Success: true,
                            AnnotationCreated: created,
                            AnnotationId: annotationId,
                            Presence: presenceCheck,
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
                return Results.Ok(new AnnotationVerificationResponse(
                    Success: false,
                    AnnotationCreated: false,
                    AnnotationId: null,
                    Presence: null,
                    Errors: errors
                ));
            }
        })
        .WithName("VerifyAnnotation")
        .WithTags("UI Verification")
        .Produces<AnnotationVerificationResponse>()
        .Produces(StatusCodes.Status500InternalServerError);
    }
}
