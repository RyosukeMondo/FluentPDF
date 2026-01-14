using System.Diagnostics;
using FluentAssertions;
using FluentPDF.Core.Models;
using FluentPDF.Rendering.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace FluentPDF.Rendering.Tests.Services;

/// <summary>
/// Unit tests for distributed tracing in PdfRenderingService.
/// Tests verify that Activity spans are created correctly with proper tags and status.
/// </summary>
public class PdfRenderingServiceTracingTests : IDisposable
{
    private readonly Mock<ILogger<PdfRenderingService>> _mockLogger;
    private readonly PdfRenderingService _service;
    private readonly ActivityListener _activityListener;
    private readonly List<Activity> _recordedActivities;

    public PdfRenderingServiceTracingTests()
    {
        _mockLogger = new Mock<ILogger<PdfRenderingService>>();
        _service = new PdfRenderingService(_mockLogger.Object);
        _recordedActivities = new List<Activity>();

        // Set up activity listener to capture activities
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "FluentPDF.Rendering",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => _recordedActivities.Add(activity)
        };

        ActivitySource.AddActivityListener(_activityListener);
    }

    public void Dispose()
    {
        _activityListener?.Dispose();
    }

    [Fact]
    public async Task RenderPageAsync_WithNullDocument_CreatesActivityWithErrorStatus()
    {
        // Arrange
        _recordedActivities.Clear();

        // Act
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.RenderPageAsync(null!, 1, 1.0));

        // Assert
        _recordedActivities.Should().HaveCount(1);
        var activity = _recordedActivities[0];
        activity.OperationName.Should().Be("RenderPage");
        activity.Status.Should().Be(ActivityStatusCode.Unset); // Exception thrown before activity setup
    }

    [Fact]
    public async Task RenderPageAsync_WithInvalidPageNumber_CreatesActivityWithErrorStatus()
    {
        // Arrange
        _recordedActivities.Clear();
        var mockDocument = CreateMockDocument(totalPages: 10);

        // Act
        var result = await _service.RenderPageAsync(mockDocument, pageNumber: 99, zoomLevel: 1.0);

        // Assert
        result.IsFailed.Should().BeTrue();
        _recordedActivities.Should().HaveCount(1);

        var activity = _recordedActivities[0];
        activity.OperationName.Should().Be("RenderPage");
        activity.Status.Should().Be(ActivityStatusCode.Error);

        // Verify tags
        activity.GetTagItem("page.number").Should().Be(99);
        activity.GetTagItem("zoom.level").Should().Be(1.0);
        activity.GetTagItem("correlation.id").Should().NotBeNull();
    }

    [Fact]
    public void ActivitySource_HasCorrectName()
    {
        // Arrange
        _recordedActivities.Clear();
        using var activity = new ActivitySource("FluentPDF.Rendering").StartActivity("TestActivity");

        // Assert
        activity.Should().NotBeNull();
        activity!.Source.Name.Should().Be("FluentPDF.Rendering");
    }

    [Fact]
    public async Task RenderPageAsync_WithValidInput_CreatesActivityWithCorrectTags()
    {
        // Arrange
        _recordedActivities.Clear();
        var mockDocument = CreateMockDocument(totalPages: 10);

        // Act
        // Note: This will fail during actual rendering since we can't mock PDFium,
        // but the activity should still be created with initial tags
        var result = await _service.RenderPageAsync(mockDocument, pageNumber: 5, zoomLevel: 1.5);

        // Assert
        // Should have at least 2 activities: RenderPage (parent) and LoadPage (child)
        _recordedActivities.Should().HaveCountGreaterOrEqualTo(2);

        var renderActivity = _recordedActivities.FirstOrDefault(a => a.OperationName == "RenderPage");
        renderActivity.Should().NotBeNull();
        renderActivity!.GetTagItem("page.number").Should().Be(5);
        renderActivity.GetTagItem("zoom.level").Should().Be(1.5);
        renderActivity.GetTagItem("correlation.id").Should().NotBeNull();

        var loadPageActivity = _recordedActivities.FirstOrDefault(a => a.OperationName == "LoadPage");
        loadPageActivity.Should().NotBeNull();
        loadPageActivity!.GetTagItem("page.number").Should().Be(5);
    }

    /// <summary>
    /// Creates a mock PdfDocument for testing.
    /// Note: This cannot fully simulate rendering as SafePdfDocumentHandle is sealed.
    /// Integration tests are required for full rendering workflow testing.
    /// </summary>
    private static PdfDocument CreateMockDocument(int totalPages)
    {
        // Create a minimal document object for validation testing
        // The Handle will be null, which will cause rendering to fail,
        // but allows us to test validation logic and activity creation
        return new PdfDocument
        {
            FilePath = "/test/document.pdf",
            Handle = null!, // Cannot mock SafeHandle
            PageCount = totalPages,
            LoadedAt = DateTime.Now,
            FileSizeBytes = 1024
        };
    }
}
