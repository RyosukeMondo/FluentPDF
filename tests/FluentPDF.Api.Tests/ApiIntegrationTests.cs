using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using FluentPDF.Core.Models;
using Xunit;

namespace FluentPDF.Api.Tests;

/// <summary>
/// Integration tests for the Verification API.
/// These tests expect a running instance of FluentPDF.App with --api-server flag.
///
/// To run tests:
/// 1. Start the API server: FluentPDF.App.exe --api-server --headless
/// 2. Run tests: dotnet test
///
/// Use the TEST_API_BASE_URL environment variable to override the default URL.
/// </summary>
public class ApiIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly string _testPdfPath;
    private string? _sessionId;

    public ApiIntegrationTests()
    {
        var baseUrl = Environment.GetEnvironmentVariable("TEST_API_BASE_URL") ?? "http://localhost:5000";
        _client = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _client.Timeout = TimeSpan.FromSeconds(30);

        // Use sample PDF from test fixtures
        _testPdfPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..", "..", "..", "..", "..", "tests", "Fixtures", "sample-with-text.pdf"
        );
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Clean up: close any open sessions
        if (_sessionId != null)
        {
            try
            {
                await _client.DeleteAsync($"/api/document/{_sessionId}");
            }
            catch
            {
                // Best effort cleanup
            }
        }
        _client.Dispose();
    }

    #region Health Endpoint Tests

    [Fact(Skip = "Requires running API server")]
    public async Task HealthCheck_ReturnsHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var healthResponse = await response.Content.ReadFromJsonAsync<HealthResponse>();
        healthResponse.Should().NotBeNull();
        healthResponse!.Status.Should().BeOneOf("healthy", "degraded");
        healthResponse.Version.Should().NotBeNullOrEmpty();
        healthResponse.PdfiumLoaded.Should().BeTrue();
    }

    [Fact(Skip = "Requires running API server")]
    public async Task HealthCheck_IncludesCorrelationId()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        response.Headers.Should().ContainKey("X-Correlation-Id");
        var correlationId = response.Headers.GetValues("X-Correlation-Id").First();
        correlationId.Should().NotBeNullOrEmpty();
    }

    [Fact(Skip = "Requires running API server")]
    public async Task Status_ReturnsApplicationState()
    {
        // Act
        var response = await _client.GetAsync("/api/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var statusResponse = await response.Content.ReadFromJsonAsync<StatusResponse>();
        statusResponse.Should().NotBeNull();
        statusResponse!.HasDocument.Should().BeFalse(); // No document loaded initially
    }

    #endregion

    #region Document Endpoint Tests

    [Fact(Skip = "Requires running API server")]
    public async Task LoadDocument_ValidPath_ReturnsSessionId()
    {
        // Arrange
        if (!File.Exists(_testPdfPath))
        {
            throw new FileNotFoundException($"Test PDF not found: {_testPdfPath}");
        }

        var request = new LoadDocumentRequest(_testPdfPath);

        // Act
        var response = await _client.PostAsJsonAsync("/api/document/load", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var loadResponse = await response.Content.ReadFromJsonAsync<LoadDocumentResponse>();
        loadResponse.Should().NotBeNull();
        loadResponse!.SessionId.Should().NotBeNullOrEmpty();
        loadResponse.PageCount.Should().BeGreaterThan(0);
        loadResponse.Success.Should().BeTrue();

        _sessionId = loadResponse.SessionId; // Save for cleanup
    }

    [Fact(Skip = "Requires running API server")]
    public async Task LoadDocument_InvalidPath_ReturnsNotFound()
    {
        // Arrange
        var request = new LoadDocumentRequest("C:\\nonexistent\\file.pdf");

        // Act
        var response = await _client.PostAsJsonAsync("/api/document/load", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Should().Contain("NOT_FOUND", "FILE_NOT_FOUND");
    }

    [Fact(Skip = "Requires running API server")]
    public async Task GetDocument_ValidSession_ReturnsDocumentInfo()
    {
        // Arrange
        var sessionId = await LoadTestDocument();

        // Act
        var response = await _client.GetAsync($"/api/document/{sessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var docInfo = await response.Content.ReadFromJsonAsync<DocumentInfoResponse>();
        docInfo.Should().NotBeNull();
        docInfo!.PageCount.Should().BeGreaterThan(0);
    }

    [Fact(Skip = "Requires running API server")]
    public async Task GetDocument_InvalidSession_ReturnsNotFound()
    {
        // Arrange
        var invalidSessionId = Guid.NewGuid().ToString();

        // Act
        var response = await _client.GetAsync($"/api/document/{invalidSessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact(Skip = "Requires running API server")]
    public async Task CloseDocument_ValidSession_ReturnsSuccess()
    {
        // Arrange
        var sessionId = await LoadTestDocument();

        // Act
        var response = await _client.DeleteAsync($"/api/document/{sessionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        _sessionId = null; // Clear since we closed it
    }

    #endregion

    #region Render Endpoint Tests

    [Fact(Skip = "Requires running API server")]
    public async Task Render_ValidPageIndex_ReturnsPng()
    {
        // Arrange
        var sessionId = await LoadTestDocument();

        // Act
        var response = await _client.GetAsync($"/api/render/{sessionId}/0");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/png");
        response.Headers.Should().ContainKey("X-Render-Time-Ms");

        var pngData = await response.Content.ReadAsByteArrayAsync();
        pngData.Should().NotBeEmpty();
        pngData.Should().StartWith(new byte[] { 0x89, 0x50, 0x4E, 0x47 }); // PNG signature
    }

    [Fact(Skip = "Requires running API server")]
    public async Task Render_InvalidPageIndex_ReturnsBadRequest()
    {
        // Arrange
        var sessionId = await LoadTestDocument();

        // Act
        var response = await _client.GetAsync($"/api/render/{sessionId}/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error.Should().NotBeNull();
        error!.Error.Should().Contain("PAGE", "RANGE", "OUT_OF_RANGE");
    }

    [Fact(Skip = "Requires running API server")]
    public async Task RenderPost_WithCustomDpi_ReturnsPng()
    {
        // Arrange
        var sessionId = await LoadTestDocument();
        var renderRequest = new RenderRequest
        {
            DocumentId = sessionId,
            PageIndex = 0,
            Dpi = 150,
            Zoom = 1.0f
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/render", renderRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("image/png");

        var pngData = await response.Content.ReadAsByteArrayAsync();
        pngData.Should().NotBeEmpty();
    }

    #endregion

    #region Verify Endpoint Tests

    [Fact(Skip = "Requires running API server")]
    public async Task VerifyRender_WithoutBaseline_ReturnsHashOnly()
    {
        // Arrange
        var sessionId = await LoadTestDocument();
        var verifyRequest = new VerifyRenderRequest
        {
            DocumentId = sessionId,
            PageIndex = 0
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/verify/render", verifyRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var verifyResponse = await response.Content.ReadFromJsonAsync<VerifyRenderResponse>();
        verifyResponse.Should().NotBeNull();
        verifyResponse!.CurrentHash.Should().NotBeNullOrEmpty();
        verifyResponse.CurrentHash.Should().HaveLength(64); // SHA256 hex string
        verifyResponse.Matched.Should().BeNull(); // No baseline provided
    }

    [Fact(Skip = "Requires running API server")]
    public async Task VerifyRender_WithMatchingBaseline_ReturnsMatched()
    {
        // Arrange
        var sessionId = await LoadTestDocument();

        // First, get the baseline hash
        var baselineRequest = new VerifyRenderRequest
        {
            DocumentId = sessionId,
            PageIndex = 0
        };
        var baselineResponse = await _client.PostAsJsonAsync("/api/verify/render", baselineRequest);
        var baseline = await baselineResponse.Content.ReadFromJsonAsync<VerifyRenderResponse>();

        // Now verify against the baseline
        var verifyRequest = new VerifyRenderRequest
        {
            DocumentId = sessionId,
            PageIndex = 0,
            BaselineHash = baseline!.CurrentHash
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/verify/render", verifyRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var verifyResponse = await response.Content.ReadFromJsonAsync<VerifyRenderResponse>();
        verifyResponse.Should().NotBeNull();
        verifyResponse!.Matched.Should().BeTrue();
        verifyResponse.CurrentHash.Should().Be(baseline.CurrentHash);
    }

    [Fact(Skip = "Requires running API server")]
    public async Task VerifyRender_WithNonMatchingBaseline_ReturnsNotMatched()
    {
        // Arrange
        var sessionId = await LoadTestDocument();
        var verifyRequest = new VerifyRenderRequest
        {
            DocumentId = sessionId,
            PageIndex = 0,
            BaselineHash = "0000000000000000000000000000000000000000000000000000000000000000"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/verify/render", verifyRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var verifyResponse = await response.Content.ReadFromJsonAsync<VerifyRenderResponse>();
        verifyResponse.Should().NotBeNull();
        verifyResponse!.Matched.Should().BeFalse();
        verifyResponse.CurrentHash.Should().NotBe(verifyRequest.BaselineHash);
    }

    [Fact(Skip = "Requires running API server")]
    public async Task BatchVerify_MultiplePages_ReturnsAllResults()
    {
        // Arrange
        var sessionId = await LoadTestDocument();
        var batchRequest = new BatchVerifyRequest
        {
            DocumentId = sessionId,
            PageIndices = new[] { 0, 1 }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/verify/batch", batchRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var batchResponse = await response.Content.ReadFromJsonAsync<BatchVerifyResponse>();
        batchResponse.Should().NotBeNull();
        batchResponse!.Results.Should().HaveCount(2);
        batchResponse.Results.Should().OnlyContain(r => r.Success);
        batchResponse.Results.Should().OnlyContain(r => !string.IsNullOrEmpty(r.Hash));
    }

    #endregion

    #region Helper Methods

    private async Task<string> LoadTestDocument()
    {
        if (!File.Exists(_testPdfPath))
        {
            throw new FileNotFoundException($"Test PDF not found: {_testPdfPath}");
        }

        var request = new LoadDocumentRequest(_testPdfPath);
        var response = await _client.PostAsJsonAsync("/api/document/load", request);
        response.EnsureSuccessStatusCode();

        var loadResponse = await response.Content.ReadFromJsonAsync<LoadDocumentResponse>();
        _sessionId = loadResponse!.SessionId;
        return _sessionId;
    }

    #endregion

    #region DTO Models (simplified versions for testing)

    private record HealthResponse(string Status, string Version, bool PdfiumLoaded);
    private record StatusResponse(bool HasDocument, int? CurrentPage, string? Theme);
    private record LoadDocumentRequest(string Path);
    private record LoadDocumentResponse(string SessionId, int PageCount, bool Success);
    private record DocumentInfoResponse(int PageCount);
    private record ErrorResponse(string Error, string? Message);

    private record RenderRequest
    {
        public string DocumentId { get; init; } = "";
        public int PageIndex { get; init; }
        public int Dpi { get; init; } = 96;
        public float Zoom { get; init; } = 1.0f;
    }

    private record VerifyRenderRequest
    {
        public string DocumentId { get; init; } = "";
        public int PageIndex { get; init; }
        public string? BaselineHash { get; init; }
    }

    private record VerifyRenderResponse
    {
        public string CurrentHash { get; init; } = "";
        public bool? Matched { get; init; }
    }

    private record BatchVerifyRequest
    {
        public string DocumentId { get; init; } = "";
        public int[] PageIndices { get; init; } = Array.Empty<int>();
    }

    private record BatchVerifyResponse
    {
        public List<PageVerifyResult> Results { get; init; } = new();
    }

    private record PageVerifyResult
    {
        public int PageIndex { get; init; }
        public string Hash { get; init; } = "";
        public bool Success { get; init; }
    }

    #endregion
}
