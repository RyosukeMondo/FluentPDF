using FluentPDF.App.Api.Models;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Drawing;

namespace FluentPDF.App.Api.Endpoints;

/// <summary>
/// Stamp verification endpoints.
/// </summary>
public static class StampEndpoints
{
    /// <summary>
    /// Maps stamp endpoints to the application.
    /// </summary>
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/verify/stamp", async (
            StampVerificationRequest request,
            IServiceProvider serviceProvider) =>
        {
            var errors = new List<string>();

            try
            {
                var dispatcherQueue = App.MainWindow.DispatcherQueue;
                var tcs = new TaskCompletionSource<StampVerificationResponse>();

                dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        var mainViewModel = App.GetService<MainViewModel>();
                        var viewerViewModel = mainViewModel?.ActiveTab?.ViewerViewModel;

                        if (viewerViewModel?.CurrentDocument is null)
                        {
                            errors.Add("No document loaded");
                            tcs.SetResult(new StampVerificationResponse(
                                Success: false,
                                StampCreated: false,
                                StampId: null,
                                PageIndex: -1,
                                StampType: null,
                                Errors: errors
                            ));
                            return;
                        }

                        var stampService = serviceProvider.GetRequiredService<IStampService>();
                        var annotationService = serviceProvider.GetRequiredService<IAnnotationService>();

                        var document = viewerViewModel.CurrentDocument;
                        var pageIndex = request.PageIndex ?? 0;

                        // Validate page index
                        if (pageIndex < 0 || pageIndex >= document.PageCount)
                        {
                            errors.Add($"Invalid page index: {pageIndex}");
                            tcs.SetResult(new StampVerificationResponse(
                                Success: false,
                                StampCreated: false,
                                StampId: null,
                                PageIndex: pageIndex,
                                StampType: request.StampType,
                                Errors: errors
                            ));
                            return;
                        }

                        // Parse stamp type
                        if (!Enum.TryParse<StampType>(request.StampType, ignoreCase: true, out var stampType))
                        {
                            errors.Add($"Unknown stamp type: {request.StampType}");
                            tcs.SetResult(new StampVerificationResponse(
                                Success: false,
                                StampCreated: false,
                                StampId: null,
                                PageIndex: pageIndex,
                                StampType: request.StampType,
                                Errors: errors
                            ));
                            return;
                        }

                        // Create stamp
                        var stamp = stampService.CreateStamp(stampType);
                        stamp.Author = request.Author ?? "API";
                        stamp.RotationAngle = request.Rotation ?? -45f;
                        stamp.Opacity = request.Opacity ?? 0.5;
                        stamp.Width = request.Width ?? 100f;
                        stamp.Height = request.Height ?? 50f;

                        // Apply dynamic replacements if requested
                        if (request.ApplyDynamicReplacements ?? false)
                        {
                            stampService.ApplyDynamicReplacements(stamp);
                        }

                        // Parse position
                        var positionX = request.PositionX ?? 100f;
                        var positionY = request.PositionY ?? 100f;

                        // Apply stamp
                        var stampResult = await stampService.ApplyStampAsync(
                            document,
                            stamp,
                            pageIndex,
                            new System.Drawing.PointF(positionX, positionY));

                        if (!stampResult.IsSuccess)
                        {
                            errors.AddRange(stampResult.Errors.Select(e => e.Message));
                            tcs.SetResult(new StampVerificationResponse(
                                Success: false,
                                StampCreated: false,
                                StampId: null,
                                PageIndex: pageIndex,
                                StampType: request.StampType,
                                Errors: errors
                            ));
                            return;
                        }

                        var annotation = stampResult.Value;

                        // Create annotation in document
                        var createResult = await annotationService.CreateAnnotationAsync(document, annotation);

                        if (!createResult.IsSuccess)
                        {
                            errors.AddRange(createResult.Errors.Select(e => e.Message));
                            tcs.SetResult(new StampVerificationResponse(
                                Success: false,
                                StampCreated: false,
                                StampId: null,
                                PageIndex: pageIndex,
                                StampType: request.StampType,
                                Errors: errors
                            ));
                            return;
                        }

                        tcs.SetResult(new StampVerificationResponse(
                            Success: true,
                            StampCreated: true,
                            StampId: annotation.Id,
                            PageIndex: pageIndex,
                            StampType: stampType.ToString(),
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
                return Results.BadRequest(new StampVerificationResponse(
                    Success: false,
                    StampCreated: false,
                    StampId: null,
                    PageIndex: -1,
                    StampType: request.StampType,
                    Errors: errors
                ));
            }
        });
    }
}

/// <summary>
/// Request model for stamp verification.
/// </summary>
public class StampVerificationRequest
{
    /// <summary>
    /// Gets or sets the stamp type (Approved, Rejected, Draft, Final, Confidential, ForReview, Copy).
    /// </summary>
    public string StampType { get; set; } = "Approved";

    /// <summary>
    /// Gets or sets the page index (0-based).
    /// </summary>
    public int? PageIndex { get; set; }

    /// <summary>
    /// Gets or sets the X position for the stamp.
    /// </summary>
    public float? PositionX { get; set; }

    /// <summary>
    /// Gets or sets the Y position for the stamp.
    /// </summary>
    public float? PositionY { get; set; }

    /// <summary>
    /// Gets or sets the rotation angle in degrees.
    /// </summary>
    public float? Rotation { get; set; }

    /// <summary>
    /// Gets or sets the opacity (0.0 to 1.0).
    /// </summary>
    public double? Opacity { get; set; }

    /// <summary>
    /// Gets or sets the width in PDF points.
    /// </summary>
    public float? Width { get; set; }

    /// <summary>
    /// Gets or sets the height in PDF points.
    /// </summary>
    public float? Height { get; set; }

    /// <summary>
    /// Gets or sets the author of the stamp.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Gets or sets whether to apply dynamic replacements ({{DATE}}, {{TIME}}, etc.).
    /// </summary>
    public bool? ApplyDynamicReplacements { get; set; }
}

/// <summary>
/// Response model for stamp verification.
/// </summary>
public record StampVerificationResponse(
    bool Success,
    bool StampCreated,
    string? StampId,
    int PageIndex,
    string? StampType,
    List<string> Errors);
