// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.App.Api.Models;
using FluentPDF.App.Api.Services;
using FluentPDF.App.Services;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace FluentPDF.App.Api.Endpoints;

/// <summary>
/// Theme verification endpoints.
/// </summary>
public static class ThemeEndpoints
{
    /// <summary>
    /// Maps theme endpoints to the application.
    /// </summary>
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/verify/theme", async (
            ThemeVerificationRequest request,
            IUiAutomationService uiService,
            IServiceProvider serviceProvider) =>
        {
            var errors = new List<string>();
            var elements = new List<ThemeElementResult>();

            try
            {
                var dispatcherQueue = App.MainWindow.DispatcherQueue;
                var tcs = new TaskCompletionSource<bool>();

                // Set theme on UI thread
                dispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        var settingsService = serviceProvider.GetRequiredService<ISettingsService>();

                        // Parse and set theme
                        var theme = request.SetTheme.ToLowerInvariant() switch
                        {
                            "dark" => AppTheme.Dark,
                            "light" => AppTheme.Light,
                            "system" => AppTheme.UseSystem,
                            _ => AppTheme.UseSystem
                        };

                        var settings = settingsService.Settings;
                        settings.Theme = theme;
                        await settingsService.SaveAsync();

                        // Wait for theme to apply
                        await Task.Delay(500);

                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });

                await tcs.Task;

                // Verify theme on elements
                var allPassed = true;
                foreach (var themeElement in request.VerifyElements)
                {
                    var background = await uiService.GetElementBackgroundAsync(themeElement.AutomationId);

                    var passed = background == themeElement.ExpectedBackground;
                    if (!passed)
                    {
                        allPassed = false;
                    }

                    elements.Add(new ThemeElementResult(
                        AutomationId: themeElement.AutomationId,
                        Background: background ?? string.Empty,
                        ExpectedBackground: themeElement.ExpectedBackground,
                        Passed: passed
                    ));
                }

                return Results.Ok(new ThemeVerificationResponse(
                    ThemeSet: request.SetTheme,
                    Passed: allPassed,
                    Elements: elements,
                    Errors: errors
                ));
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                return Results.Ok(new ThemeVerificationResponse(
                    ThemeSet: request.SetTheme,
                    Passed: false,
                    Elements: elements,
                    Errors: errors
                ));
            }
        })
        .WithName("VerifyTheme")
        .WithTags("UI Verification")
        .Produces<ThemeVerificationResponse>()
        .Produces(StatusCodes.Status500InternalServerError);
    }
}
