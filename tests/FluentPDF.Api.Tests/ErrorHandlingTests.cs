using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace FluentPDF.Api.Tests;

/// <summary>
/// Tests for API error handling and edge cases.
/// These tests expect a running instance of FluentPDF.App with --api-server flag.
/// </summary>
public class ErrorHandlingTests
{
    private readonly HttpClient _client;

    public ErrorHandlingTests()
    {
        var baseUrl = Environment.GetEnvironmentVariable("TEST_API_BASE_URL") ?? "http://localhost:5000";
        _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _client.Timeout = TimeSpan.FromSeconds(30);
    }

    #region Invalid Request Tests

    [Fact(Skip = "Requires running API server")]
    public async Task LoadDocument_EmptyPath_ReturnsBadRequest()
    {
        // Arrange
        var request = new { path = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/document/load", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "Requires running API server")]
    public async Task LoadDocument_NullPath_ReturnsBadRequest()
    {
        // Arrange
        var request = new { path = (string?)null };

        // Act
        var response = await _client.PostAsJsonAsync("/api/document/load", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "Requires running API server")]
    public async Task Render_InvalidDocumentId_ReturnsNotFound()
    {
        // Arrange
        var invalidSessionId = Guid.NewGuid().ToString();

        // Act
        var response = await _client.GetAsync($"/api/render/{invalidSessionId}/0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Skip = "Requires running API server")]
    public async Task Render_NegativePageIndex_ReturnsBadRequest()
    {
        // Arrange - would need a valid session, but negative page should fail early
        var sessionId = Guid.NewGuid().ToString();

        // Act
        var response = await _client.GetAsync($"/api/render/{sessionId}/-1");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    #endregion

    #region Malformed Request Tests

    [Fact(Skip = "Requires running API server")]
    public async Task PostEndpoint_InvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var invalidJson = new StringContent("{invalid json", System.Text.Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/document/load", invalidJson);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact(Skip = "Requires running API server")]
    public async Task PostEndpoint_MissingRequiredField_ReturnsBadRequest()
    {
        // Arrange
        var requestMissingPath = new { };

        // Act
        var response = await _client.PostAsJsonAsync("/api/document/load", requestMissingPath);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Correlation ID Tests

    [Fact(Skip = "Requires running API server")]
    public async Task AllRequests_IncludeCorrelationId()
    {
        // Arrange
        var endpoints = new[]
        {
            "/api/health",
            "/api/status"
        };

        foreach (var endpoint in endpoints)
        {
            // Act
            var response = await _client.GetAsync(endpoint);

            // Assert
            response.Headers.Should().ContainKey("X-Correlation-Id");
            var correlationId = response.Headers.GetValues("X-Correlation-Id").First();
            correlationId.Should().NotBeNullOrEmpty();
            Guid.TryParse(correlationId, out _).Should().BeTrue($"{endpoint} should return valid GUID correlation ID");
        }
    }

    [Fact(Skip = "Requires running API server")]
    public async Task CustomCorrelationId_IsPreserved()
    {
        // Arrange
        var customCorrelationId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Add("X-Correlation-Id", customCorrelationId);

        try
        {
            // Act
            var response = await _client.GetAsync("/api/health");

            // Assert
            response.Headers.Should().ContainKey("X-Correlation-Id");
            var returnedCorrelationId = response.Headers.GetValues("X-Correlation-Id").First();
            returnedCorrelationId.Should().Be(customCorrelationId);
        }
        finally
        {
            _client.DefaultRequestHeaders.Remove("X-Correlation-Id");
        }
    }

    #endregion

    #region Response Format Tests

    [Fact(Skip = "Requires running API server")]
    public async Task ErrorResponses_HaveConsistentFormat()
    {
        // Arrange
        var invalidRequest = new { path = "C:\\nonexistent\\file.pdf" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/document/load", invalidRequest);

        // Assert
        response.IsSuccessStatusCode.Should().BeFalse();

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Should().NotBeNullOrEmpty();
        // Message is optional but if present should not be empty
        if (error.Message != null)
        {
            error.Message.Should().NotBeEmpty();
        }
    }

    [Fact(Skip = "Requires running API server")]
    public async Task ContentType_IsApplicationJson()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.Content.Headers.ContentType.Should().NotBeNull();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }

    #endregion

    #region Timeout and Performance Tests

    [Fact(Skip = "Requires running API server")]
    public async Task HealthCheck_RespondsQuickly()
    {
        // Arrange
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await _client.GetAsync("/api/health");
        stopwatch.Stop();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "Health check should respond within 500ms");
    }

    #endregion

    #region DTO Models

    private record ErrorResponse(string Error, string? Message);

    #endregion
}
