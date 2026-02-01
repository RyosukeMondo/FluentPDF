// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.App.Api.Models;
using FluentPDF.App.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace FluentPDF.App.Api.Endpoints;

/// <summary>
/// UI element verification endpoints.
/// </summary>
public static class ElementEndpoints
{
    /// <summary>
    /// Maps element verification endpoints to the application.
    /// </summary>
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/verify/element", async (
            ElementVerificationRequest request,
            IUiAutomationService uiService) =>
        {
            try
            {
                var response = await uiService.VerifyElementAsync(request);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("VerifyElement")
        .WithTags("UI Verification")
        .Produces<ElementVerificationResponse>()
        .Produces(StatusCodes.Status500InternalServerError);

        app.MapPost("/api/verify/layout", async (
            LayoutVerificationRequest request,
            IUiAutomationService uiService) =>
        {
            try
            {
                var response = await uiService.VerifyLayoutAsync(request);
                return Results.Ok(response);
            }
            catch (Exception ex)
            {
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }
        })
        .WithName("VerifyLayout")
        .WithTags("UI Verification")
        .Produces<LayoutVerificationResponse>()
        .Produces(StatusCodes.Status500InternalServerError);
    }
}
