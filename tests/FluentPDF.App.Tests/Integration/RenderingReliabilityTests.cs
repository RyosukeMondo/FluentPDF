using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentPDF.App.Interfaces;
using FluentPDF.App.Services;
using FluentPDF.App.Services.RenderingStrategies;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentPDF.Rendering.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Moq;

namespace FluentPDF.App.Tests.Integration;

/// <summary>
/// Integration tests for PDF rendering reliability features.
/// Tests rendering coordinator fallback logic, memory monitoring, UI binding verification,
/// and individual rendering strategies with real dependencies.
/// </summary>
[Trait("Category", "Integration")]
public sealed class RenderingReliabilityTests : IDisposable
{
    private const string SimplePdfPath = "../../../Fixtures/simple-text.pdf";
    private const string MultiPagePdfPath = "../../../Fixtures/multi-page.pdf";

    private readonly Mock<ILogger<RenderingObservabilityService>> _observabilityLoggerMock;
    private readonly Mock<ILogger<PdfRenderingService>> _pdfRenderingLoggerMock;
    private readonly List<string> _tempFiles;
    private bool _disposed;

    public RenderingReliabilityTests()
    {
        _observabilityLoggerMock = new Mock<ILogger<RenderingObservabilityService>>();
        _pdfRenderingLoggerMock = new Mock<ILogger<PdfRenderingService>>();
        _tempFiles = new List<string>();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Clean up temp files created by file-based strategy
        foreach (var file in _tempFiles.Where(File.Exists))
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        _disposed = true;
    }

    /// <summary>
    /// Tests that RenderingCoordinator successfully falls back to FileBasedRenderingStrategy
    /// when WriteableBitmapRenderingStrategy fails.
    /// </summary>
    [Fact]
    public async Task RenderingCoordinator_FallsBackToFileBasedStrategy_WhenWriteableBitmapFailsAsync()
    {
        // Arrange
        var memoryMonitor = new MemoryMonitor();
        var observabilityService = new RenderingObservabilityService(_observabilityLoggerMock.Object, memoryMonitor);

        // Create a mock strategy that always fails (simulates WriteableBitmap failure)
        var failingStrategy = new Mock<IRenderingStrategy>();
        failingStrategy.Setup(s => s.StrategyName).Returns("FailingStrategy");
        failingStrategy.Setup(s => s.Priority).Returns(0);
        failingStrategy.Setup(s => s.TryRenderAsync(It.IsAny<Stream>(), It.IsAny<RenderContext>()))
            .ReturnsAsync((ImageSource?)null);

        // Create real FileBasedRenderingStrategy as fallback
        var fileBasedStrategy = new FileBasedRenderingStrategy();

        // Create factory with both strategies
        var strategies = new List<IRenderingStrategy> { failingStrategy.Object, fileBasedStrategy };
        var factory = new RenderingStrategyFactory(strategies);

        // Create real PDF rendering service
        var pdfDocumentService = CreatePdfDocumentService();
        var pdfRenderingService = new PdfRenderingService(_pdfRenderingLoggerMock.Object);

        var coordinator = new RenderingCoordinator(factory, observabilityService, pdfRenderingService);

        // Load test PDF
        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SimplePdfPath));
        var documentResult = await pdfDocumentService.LoadDocumentAsync(fullPdfPath);
        documentResult.IsSuccess.Should().BeTrue();
        var document = documentResult.Value!;

        var context = new RenderContext(
            DocumentPath: fullPdfPath,
            PageNumber: 1,
            TotalPages: document.PageCount,
            RenderDpi: 96.0,
            RequestSource: "Test",
            RequestTime: DateTime.UtcNow,
            OperationId: Guid.NewGuid()
        );

        try
        {
            // Act
            var result = await coordinator.RenderWithFallbackAsync(document, 1, 1.0, 96, context);

            // Assert
            result.Should().NotBeNull("FileBasedStrategy should succeed when WriteableBitmap fails");
            result.Should().BeOfType<BitmapImage>("FileBasedStrategy returns BitmapImage");

            // Verify failing strategy was attempted
            failingStrategy.Verify(s => s.TryRenderAsync(It.IsAny<Stream>(), It.IsAny<RenderContext>()), Times.Once);
        }
        finally
        {
            document.Dispose();
            fileBasedStrategy.Dispose();
        }
    }

    /// <summary>
    /// Tests that MemoryMonitor accurately captures memory metrics during rendering operations.
    /// </summary>
    [Fact]
    public void MemoryMonitor_CapturesSnapshotAccurately_WithMemoryAllocation()
    {
        // Arrange
        var memoryMonitor = new MemoryMonitor();

        // Act
        var beforeSnapshot = memoryMonitor.CaptureSnapshot("Before");

        // Allocate known amount of memory (10 MB)
        var allocatedMemory = new byte[10 * 1024 * 1024];
        for (int i = 0; i < allocatedMemory.Length; i++)
        {
            allocatedMemory[i] = (byte)(i % 256);
        }

        var afterSnapshot = memoryMonitor.CaptureSnapshot("After");
        var delta = memoryMonitor.CalculateDelta(beforeSnapshot, afterSnapshot);

        // Assert
        beforeSnapshot.Label.Should().Be("Before");
        afterSnapshot.Label.Should().Be("After");

        beforeSnapshot.WorkingSetBytes.Should().BeGreaterThan(0);
        beforeSnapshot.ManagedMemoryBytes.Should().BeGreaterThan(0);
        beforeSnapshot.HandleCount.Should().BeGreaterThan(0);

        // Delta should show increase in memory
        delta.ManagedMemoryDelta.Should().BeGreaterThan(0, "allocating 10MB should increase managed memory");
        delta.Before.Should().Be(beforeSnapshot);
        delta.After.Should().Be(afterSnapshot);

        // Small allocation should not be flagged as abnormal (threshold is 100MB)
        delta.IsAbnormal.Should().BeFalse("10MB is below the 100MB abnormal threshold");

        // Keep reference to prevent GC
        GC.KeepAlive(allocatedMemory);
    }

    /// <summary>
    /// Tests that MemoryMonitor correctly identifies abnormal memory growth.
    /// </summary>
    [Fact]
    public void MemoryMonitor_DetectsAbnormalMemoryGrowth_WhenThresholdExceeded()
    {
        // Arrange
        var memoryMonitor = new MemoryMonitor();

        // Create snapshots with simulated abnormal growth
        var beforeSnapshot = new MemorySnapshot(
            Label: "Before",
            WorkingSetBytes: 100_000_000,
            PrivateMemoryBytes: 100_000_000,
            ManagedMemoryBytes: 50_000_000,
            HandleCount: 500,
            Timestamp: DateTime.UtcNow
        );

        var afterSnapshot = new MemorySnapshot(
            Label: "After",
            WorkingSetBytes: 250_000_000, // 150MB increase - abnormal
            PrivateMemoryBytes: 250_000_000,
            ManagedMemoryBytes: 200_000_000,
            HandleCount: 550,
            Timestamp: DateTime.UtcNow.AddSeconds(1)
        );

        // Act
        var delta = memoryMonitor.CalculateDelta(beforeSnapshot, afterSnapshot);

        // Assert
        delta.IsAbnormal.Should().BeTrue("150MB growth exceeds 100MB threshold");
        delta.WorkingSetDelta.Should().Be(150_000_000);
        delta.ManagedMemoryDelta.Should().Be(150_000_000);
    }

    /// <summary>
    /// Tests that UIBindingVerifier detects PropertyChanged events correctly.
    /// </summary>
    [Fact]
    public async Task UIBindingVerifier_DetectsPropertyChange_WhenEventFiredAsync()
    {
        // Arrange
        var verifier = new UIBindingVerifier();
        var viewModel = new TestViewModel();

        // Start verification (will wait for PropertyChanged)
        var verificationTask = verifier.VerifyPropertyUpdateAsync(
            viewModel,
            nameof(TestViewModel.TestProperty),
            TimeSpan.FromSeconds(2)
        );

        // Act
        await Task.Delay(100); // Small delay to ensure subscription is active
        viewModel.TestProperty = "NewValue"; // This triggers PropertyChanged

        var result = await verificationTask;

        // Assert
        result.Should().BeTrue("PropertyChanged event should have fired");
    }

    /// <summary>
    /// Tests that UIBindingVerifier returns false when PropertyChanged event is not fired within timeout.
    /// </summary>
    [Fact]
    public async Task UIBindingVerifier_ReturnsFalse_WhenEventNotFiredWithinTimeoutAsync()
    {
        // Arrange
        var verifier = new UIBindingVerifier();
        var viewModel = new TestViewModel();

        // Act - wait for PropertyChanged that never fires
        var result = await verifier.VerifyPropertyUpdateAsync(
            viewModel,
            nameof(TestViewModel.TestProperty),
            TimeSpan.FromMilliseconds(200)
        );

        // Assert
        result.Should().BeFalse("PropertyChanged event was not fired");
    }

    /// <summary>
    /// Tests that WriteableBitmapRenderingStrategy successfully renders a PNG stream.
    /// </summary>
    [Fact]
    public async Task WriteableBitmapStrategy_RendersSuccessfully_WithValidPngStreamAsync()
    {
        // Arrange
        var strategy = new WriteableBitmapRenderingStrategy();
        var pdfDocumentService = CreatePdfDocumentService();
        var pdfRenderingService = new PdfRenderingService(_pdfRenderingLoggerMock.Object);

        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SimplePdfPath));
        var documentResult = await pdfDocumentService.LoadDocumentAsync(fullPdfPath);
        documentResult.IsSuccess.Should().BeTrue();
        var document = documentResult.Value!;

        var context = new RenderContext(
            DocumentPath: fullPdfPath,
            PageNumber: 1,
            TotalPages: document.PageCount,
            RenderDpi: 96.0,
            RequestSource: "Test",
            RequestTime: DateTime.UtcNow,
            OperationId: Guid.NewGuid()
        );

        try
        {
            // Get PNG stream from PDF rendering service
            var renderResult = await pdfRenderingService.RenderPageAsync(document, 1, 1.0, 96);
            renderResult.IsSuccess.Should().BeTrue();
            var pngStream = renderResult.Value;

            // Act
            var imageSource = await strategy.TryRenderAsync(pngStream, context);

            // Assert
            imageSource.Should().NotBeNull("WriteableBitmap rendering should succeed");
            imageSource.Should().BeOfType<WriteableBitmap>();

            pngStream.Dispose();
        }
        finally
        {
            document.Dispose();
        }
    }

    /// <summary>
    /// Tests that FileBasedRenderingStrategy creates a temp file and loads it as BitmapImage.
    /// </summary>
    [Fact]
    public async Task FileBasedStrategy_CreatesTempFile_AndLoadsBitmapImageAsync()
    {
        // Arrange
        var strategy = new FileBasedRenderingStrategy();
        var pdfDocumentService = CreatePdfDocumentService();
        var pdfRenderingService = new PdfRenderingService(_pdfRenderingLoggerMock.Object);

        var fullPdfPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, SimplePdfPath));
        var documentResult = await pdfDocumentService.LoadDocumentAsync(fullPdfPath);
        documentResult.IsSuccess.Should().BeTrue();
        var document = documentResult.Value!;

        var context = new RenderContext(
            DocumentPath: fullPdfPath,
            PageNumber: 1,
            TotalPages: document.PageCount,
            RenderDpi: 96.0,
            RequestSource: "Test",
            RequestTime: DateTime.UtcNow,
            OperationId: Guid.NewGuid()
        );

        try
        {
            // Get PNG stream from PDF rendering service
            var renderResult = await pdfRenderingService.RenderPageAsync(document, 1, 1.0, 96);
            renderResult.IsSuccess.Should().BeTrue();
            var pngStream = renderResult.Value;

            // Act
            var imageSource = await strategy.TryRenderAsync(pngStream, context);

            // Assert
            imageSource.Should().NotBeNull("FileBased rendering should succeed");
            imageSource.Should().BeOfType<BitmapImage>();

            var bitmapImage = (BitmapImage)imageSource;
            bitmapImage.UriSource.Should().NotBeNull("BitmapImage should have UriSource set");
            bitmapImage.UriSource.LocalPath.Should().EndWith(".png", "temp file should be PNG format");

            // Track temp file for cleanup
            if (bitmapImage.UriSource != null && File.Exists(bitmapImage.UriSource.LocalPath))
            {
                _tempFiles.Add(bitmapImage.UriSource.LocalPath);
            }

            pngStream.Dispose();
        }
        finally
        {
            document.Dispose();
            strategy.Dispose();
        }
    }

    /// <summary>
    /// Tests that RenderingStrategyFactory orders strategies by priority correctly.
    /// </summary>
    [Fact]
    public void RenderingStrategyFactory_OrdersStrategiesByPriority_Correctly()
    {
        // Arrange
        var strategy1 = new Mock<IRenderingStrategy>();
        strategy1.Setup(s => s.Priority).Returns(10);
        strategy1.Setup(s => s.StrategyName).Returns("LowPriority");

        var strategy2 = new Mock<IRenderingStrategy>();
        strategy2.Setup(s => s.Priority).Returns(0);
        strategy2.Setup(s => s.StrategyName).Returns("HighPriority");

        var strategy3 = new Mock<IRenderingStrategy>();
        strategy3.Setup(s => s.Priority).Returns(5);
        strategy3.Setup(s => s.StrategyName).Returns("MediumPriority");

        var strategies = new List<IRenderingStrategy> { strategy1.Object, strategy2.Object, strategy3.Object };

        // Act
        var factory = new RenderingStrategyFactory(strategies);
        var orderedStrategies = factory.GetStrategies().ToList();

        // Assert
        orderedStrategies.Should().HaveCount(3);
        orderedStrategies[0].StrategyName.Should().Be("HighPriority", "priority 0 should be first");
        orderedStrategies[1].StrategyName.Should().Be("MediumPriority", "priority 5 should be second");
        orderedStrategies[2].StrategyName.Should().Be("LowPriority", "priority 10 should be last");
    }

    /// <summary>
    /// Tests that RenderingObservabilityService logs render operations correctly.
    /// </summary>
    [Fact]
    public void RenderingObservabilityService_LogsRenderOperation_WithTiming()
    {
        // Arrange
        var memoryMonitor = new MemoryMonitor();
        var observabilityService = new RenderingObservabilityService(_observabilityLoggerMock.Object, memoryMonitor);

        var context = new RenderContext(
            DocumentPath: "test.pdf",
            PageNumber: 1,
            TotalPages: 10,
            RenderDpi: 96.0,
            RequestSource: "Test",
            RequestTime: DateTime.UtcNow,
            OperationId: Guid.NewGuid()
        );

        // Act
        using (var operation = observabilityService.BeginRenderOperation("TestRender", context))
        {
            // Simulate some work
            System.Threading.Thread.Sleep(10);
        }

        // Assert - verify that logging was called (check via mock)
        _observabilityLoggerMock.Verify(
            logger => logger.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "BeginRenderOperation should log start and end events"
        );
    }

    /// <summary>
    /// Tests that MemoryMonitor.DetectSafeHandleLeaksAsync returns diagnostic information.
    /// </summary>
    [Fact]
    public async Task MemoryMonitor_DetectSafeHandleLeaks_ReturnsDiagnosticInfoAsync()
    {
        // Arrange
        var memoryMonitor = new MemoryMonitor();

        // Act
        var leaks = await memoryMonitor.DetectSafeHandleLeaksAsync();

        // Assert
        leaks.Should().NotBeNull();
        // Should return empty list if handle count is normal (< 10000)
        // or a diagnostic entry if handle count is high
        leaks.Should().BeOfType<List<SafeHandleLeak>>();
    }

    /// <summary>
    /// Helper method to create PdfDocumentService for tests.
    /// </summary>
    private PdfDocumentService CreatePdfDocumentService()
    {
        var loggerMock = new Mock<ILogger<PdfDocumentService>>();
        return new PdfDocumentService(loggerMock.Object);
    }

    /// <summary>
    /// Test ViewModel that implements INotifyPropertyChanged for binding verification tests.
    /// </summary>
    private class TestViewModel : INotifyPropertyChanged
    {
        private string? _testProperty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string? TestProperty
        {
            get => _testProperty;
            set
            {
                if (_testProperty != value)
                {
                    _testProperty = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TestProperty)));
                }
            }
        }
    }
}
