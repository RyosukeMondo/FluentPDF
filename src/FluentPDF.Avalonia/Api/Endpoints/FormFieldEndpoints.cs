// Copyright (c) 2025 FluentPDF. All rights reserved.

using FluentPDF.Avalonia.Api.Models;
using FluentPDF.Avalonia.Api.Services;
using FluentPDF.Core.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FluentPDF.Avalonia.Api.Endpoints;

/// <summary>
/// REST API endpoints for PDF form field operations.
/// </summary>
public static class FormFieldEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/api/forms")
            .WithTags("Form Fields");

        group.MapGet("/{documentId}/{pageNumber:int}", async (
            string documentId,
            int pageNumber,
            IPdfFormService formService,
            IDocumentSessionManager sessions) =>
        {
            var document = sessions.GetDocument(documentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {documentId}"));

            var result = await formService.GetFormFieldsAsync(document, pageNumber);

            if (!result.IsSuccess)
                return Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Failed to get form fields" },
                    statusCode: StatusCodes.Status500InternalServerError);

            var fields = result.Value.Select(f => new
            {
                name = f.Name,
                fieldType = f.Type.ToString(),
                value = f.Value,
                isReadOnly = f.IsReadOnly,
                isRequired = f.IsRequired,
                pageNumber = f.PageNumber,
                bounds = new { x = f.Bounds.Left, y = f.Bounds.Bottom, width = f.Bounds.Width, height = f.Bounds.Height },
                options = f.Options
            }).ToList();

            return Results.Json(new { success = true, pageNumber, count = fields.Count, fields });
        })
        .WithName("ListFormFields")
        .WithSummary("List form fields on a page");

        group.MapPost("/set-value", async (
            SetFormFieldValueRequest request,
            IPdfFormService formService,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            if (string.IsNullOrWhiteSpace(request.FieldName))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "fieldName is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            var fieldsResult = await formService.GetFormFieldsAsync(document, request.PageNumber);
            if (!fieldsResult.IsSuccess)
                return Results.Json(new { success = false, error = fieldsResult.Errors.FirstOrDefault()?.Message ?? "Failed to get form fields" },
                    statusCode: StatusCodes.Status500InternalServerError);

            var field = fieldsResult.Value.FirstOrDefault(f => f.Name == request.FieldName);
            if (field is null)
                return Results.NotFound(new ErrorResponse("FIELD_NOT_FOUND", $"No form field named '{request.FieldName}' on page {request.PageNumber}"));

            var result = await formService.SetFieldValueAsync(field, request.Value ?? string.Empty);

            return result.IsSuccess
                ? Results.Json(new { success = true, fieldName = request.FieldName, value = request.Value })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Set value failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("SetFormFieldValue")
        .WithSummary("Set the value of a form field");

        group.MapPost("/set-checkbox", async (
            SetCheckboxStateRequest request,
            IPdfFormService formService,
            IDocumentSessionManager sessions) =>
        {
            if (string.IsNullOrWhiteSpace(request.DocumentId))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "documentId is required"));

            if (string.IsNullOrWhiteSpace(request.FieldName))
                return Results.BadRequest(new ErrorResponse("INVALID_REQUEST", "fieldName is required"));

            var document = sessions.GetDocument(request.DocumentId);
            if (document is null)
                return Results.NotFound(new ErrorResponse("DOCUMENT_NOT_FOUND", $"No document with ID: {request.DocumentId}"));

            var fieldsResult = await formService.GetFormFieldsAsync(document, request.PageNumber);
            if (!fieldsResult.IsSuccess)
                return Results.Json(new { success = false, error = fieldsResult.Errors.FirstOrDefault()?.Message ?? "Failed to get form fields" },
                    statusCode: StatusCodes.Status500InternalServerError);

            var field = fieldsResult.Value.FirstOrDefault(f => f.Name == request.FieldName);
            if (field is null)
                return Results.NotFound(new ErrorResponse("FIELD_NOT_FOUND", $"No form field named '{request.FieldName}' on page {request.PageNumber}"));

            var result = await formService.SetCheckboxStateAsync(field, request.IsChecked);

            return result.IsSuccess
                ? Results.Json(new { success = true, fieldName = request.FieldName, isChecked = request.IsChecked })
                : Results.Json(new { success = false, error = result.Errors.FirstOrDefault()?.Message ?? "Set checkbox failed" },
                    statusCode: StatusCodes.Status500InternalServerError);
        })
        .WithName("SetCheckboxState")
        .WithSummary("Set the checked state of a checkbox form field");
    }

    internal record SetFormFieldValueRequest(string DocumentId, int PageNumber, string FieldName, string? Value);
    internal record SetCheckboxStateRequest(string DocumentId, int PageNumber, string FieldName, bool IsChecked);
}
