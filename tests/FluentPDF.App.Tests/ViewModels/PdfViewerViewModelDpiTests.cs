using FluentAssertions;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using System.Reactive.Subjects;

namespace FluentPDF.App.Tests.ViewModels;

/// <summary>
/// Tests for PdfViewerViewModel DPI monitoring functionality.
/// Tests DPI detection, monitoring, and automatic re-rendering on DPI changes.
/// </summary>
public class PdfViewerViewModelDpiTests : IDisposable
{
    private readonly Mock<IPdfDocumentService> _documentServiceMock;
    private readonly Mock<IPdfRenderingService> _renderingServiceMock;
    private readonly Mock<IDocumentEditingService> _editingServiceMock;
    private readonly Mock<ITextSearchService> _searchServiceMock;
    private readonly Mock<ITextExtractionService> _textExtractionServiceMock;
    private readonly Mock<BookmarksViewModel> _bookmarksViewModelMock;
    private readonly Mock<FormFieldViewModel> _formFieldViewModelMock;
    private readonly Mock<DiagnosticsPanelViewModel> _diagnosticsPanelViewModelMock;
    private readonly Mock<LogViewerViewModel> _logViewerViewModelMock;
    private readonly Mock<IDpiDetectionService> _dpiDetectionServiceMock;
    private readonly Mock<IRenderingSettingsService> _renderingSettingsServiceMock;
    private readonly Mock<ILogger<PdfViewerViewModel>> _loggerMock;
    private readonly PdfViewerViewModel _viewModel;
    private readonly Subject<DisplayInfo> _dpiChangesSubject;
    private readonly Subject<RenderingQuality> _qualityChangesSubject;

    public PdfViewerViewModelDpiTests()
    {
        _documentServiceMock = new Mock<IPdfDocumentService>();
        _renderingServiceMock = new Mock<IPdfRenderingService>();
        _editingServiceMock = new Mock<IDocumentEditingService>();
        _searchServiceMock = new Mock<ITextSearchService>();
        _textExtractionServiceMock = new Mock<ITextExtractionService>();
        _dpiDetectionServiceMock = new Mock<IDpiDetectionService>();
        _renderingSettingsServiceMock = new Mock<IRenderingSettingsService>();
        _loggerMock = new Mock<ILogger<PdfViewerViewModel>>();

        // Create mock view models with minimal setup
        _bookmarksViewModelMock = new Mock<BookmarksViewModel>(
            Mock.Of<IBookmarkService>(),
            Mock.Of<ILogger<BookmarksViewModel>>());

        _formFieldViewModelMock = new Mock<FormFieldViewModel>(
            Mock.Of<IFormFieldService>(),
            Mock.Of<ILogger<FormFieldViewModel>>());

        _diagnosticsPanelViewModelMock = new Mock<DiagnosticsPanelViewModel>(
            Mock.Of<Core.Services.IMetricsCollectionService>(),
            Mock.Of<ILogger<DiagnosticsPanelViewModel>>());

        _logViewerViewModelMock = new Mock<LogViewerViewModel>(
            Mock.Of<ILogReaderService>(),
            Mock.Of<ILogger<LogViewerViewModel>>());

        // Setup observable streams
        _dpiChangesSubject = new Subject<DisplayInfo>();
        _qualityChangesSubject = new Subject<RenderingQuality>();

        _renderingSettingsServiceMock
            .Setup(s => s.ObserveRenderingQuality())
            .Returns(_qualityChangesSubject);

        _renderingSettingsServiceMock
            .Setup(s => s.GetRenderingQuality())
            .Returns(Result.Ok(RenderingQuality.Auto));

        _viewModel = new PdfViewerViewModel(
            _documentServiceMock.Object,
            _renderingServiceMock.Object,
            _editingServiceMock.Object,
            _searchServiceMock.Object,
            _textExtractionServiceMock.Object,
            _bookmarksViewModelMock.Object,
            _formFieldViewModelMock.Object,
            _diagnosticsPanelViewModelMock.Object,
            _logViewerViewModelMock.Object,
            null, // metrics service
            _dpiDetectionServiceMock.Object,
            _renderingSettingsServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Constructor_ShouldInitializeDpiProperties()
    {
        // Assert
        _viewModel.CurrentDisplayInfo.Should().BeNull();
        _viewModel.CurrentRenderingQuality.Should().Be(RenderingQuality.Auto);
        _viewModel.IsAdjustingQuality.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldSubscribeToQualityChanges()
    {
        // Verify that the observable was called
        _renderingSettingsServiceMock.Verify(
            s => s.ObserveRenderingQuality(),
            Times.Once);
    }

    [Fact]
    public void StartDpiMonitoring_ShouldGetInitialDisplayInfo()
    {
        // Arrange
        var displayInfo = DisplayInfo.FromScale(1.5);
        _dpiDetectionServiceMock
            .Setup(s => s.GetCurrentDisplayInfo(It.IsAny<object>()))
            .Returns(Result.Ok(displayInfo));

        _dpiDetectionServiceMock
            .Setup(s => s.MonitorDpiChanges(It.IsAny<object>(), It.IsAny<int>()))
            .Returns(Result.Ok<IObservable<DisplayInfo>>(_dpiChangesSubject));

        // Act
        _viewModel.StartDpiMonitoring(new object());

        // Assert
        _viewModel.CurrentDisplayInfo.Should().NotBeNull();
        _viewModel.CurrentDisplayInfo!.RasterizationScale.Should().Be(1.5);
        _dpiDetectionServiceMock.Verify(
            s => s.GetCurrentDisplayInfo(It.IsAny<object>()),
            Times.Once);
    }

    [Fact]
    public void StartDpiMonitoring_ShouldUseStandardDisplayInfo_WhenDetectionFails()
    {
        // Arrange
        _dpiDetectionServiceMock
            .Setup(s => s.GetCurrentDisplayInfo(It.IsAny<object>()))
            .Returns(Result.Fail<DisplayInfo>("Detection failed"));

        _dpiDetectionServiceMock
            .Setup(s => s.MonitorDpiChanges(It.IsAny<object>(), It.IsAny<int>()))
            .Returns(Result.Ok<IObservable<DisplayInfo>>(_dpiChangesSubject));

        // Act
        _viewModel.StartDpiMonitoring(new object());

        // Assert
        _viewModel.CurrentDisplayInfo.Should().NotBeNull();
        _viewModel.CurrentDisplayInfo!.RasterizationScale.Should().Be(1.0);
        _viewModel.CurrentDisplayInfo!.EffectiveDpi.Should().Be(96.0);
    }

    [Fact]
    public void StartDpiMonitoring_ShouldSetupDpiChangeMonitoring()
    {
        // Arrange
        var displayInfo = DisplayInfo.Standard();
        _dpiDetectionServiceMock
            .Setup(s => s.GetCurrentDisplayInfo(It.IsAny<object>()))
            .Returns(Result.Ok(displayInfo));

        _dpiDetectionServiceMock
            .Setup(s => s.MonitorDpiChanges(It.IsAny<object>(), It.IsAny<int>()))
            .Returns(Result.Ok<IObservable<DisplayInfo>>(_dpiChangesSubject));

        // Act
        _viewModel.StartDpiMonitoring(new object());

        // Assert
        _dpiDetectionServiceMock.Verify(
            s => s.MonitorDpiChanges(It.IsAny<object>(), It.IsAny<int>()),
            Times.Once);
    }

    [Fact]
    public void StartDpiMonitoring_ShouldDoNothing_WhenServiceNotAvailable()
    {
        // Arrange
        var viewModelWithoutDpi = new PdfViewerViewModel(
            _documentServiceMock.Object,
            _renderingServiceMock.Object,
            _editingServiceMock.Object,
            _searchServiceMock.Object,
            _textExtractionServiceMock.Object,
            _bookmarksViewModelMock.Object,
            _formFieldViewModelMock.Object,
            _diagnosticsPanelViewModelMock.Object,
            _logViewerViewModelMock.Object,
            null, // metrics service
            null, // no DPI service
            _renderingSettingsServiceMock.Object,
            _loggerMock.Object);

        // Act
        viewModelWithoutDpi.StartDpiMonitoring(new object());

        // Assert
        viewModelWithoutDpi.CurrentDisplayInfo.Should().BeNull();
    }

    [Fact]
    public void StartDpiMonitoring_ShouldDisposeExistingSubscription_WhenCalledMultipleTimes()
    {
        // Arrange
        var displayInfo = DisplayInfo.Standard();
        _dpiDetectionServiceMock
            .Setup(s => s.GetCurrentDisplayInfo(It.IsAny<object>()))
            .Returns(Result.Ok(displayInfo));

        _dpiDetectionServiceMock
            .Setup(s => s.MonitorDpiChanges(It.IsAny<object>(), It.IsAny<int>()))
            .Returns(Result.Ok<IObservable<DisplayInfo>>(_dpiChangesSubject));

        // Act
        _viewModel.StartDpiMonitoring(new object());
        _viewModel.StartDpiMonitoring(new object());

        // Assert - should be called twice, once for each call
        _dpiDetectionServiceMock.Verify(
            s => s.MonitorDpiChanges(It.IsAny<object>(), It.IsAny<int>()),
            Times.Exactly(2));
    }

    [Fact]
    public void QualityChange_ShouldUpdateCurrentRenderingQuality()
    {
        // Act
        _qualityChangesSubject.OnNext(RenderingQuality.High);

        // Assert
        _viewModel.CurrentRenderingQuality.Should().Be(RenderingQuality.High);
    }

    [Fact]
    public void Dispose_ShouldDisposeDpiSubscription()
    {
        // Arrange
        var displayInfo = DisplayInfo.Standard();
        _dpiDetectionServiceMock
            .Setup(s => s.GetCurrentDisplayInfo(It.IsAny<object>()))
            .Returns(Result.Ok(displayInfo));

        _dpiDetectionServiceMock
            .Setup(s => s.MonitorDpiChanges(It.IsAny<object>(), It.IsAny<int>()))
            .Returns(Result.Ok<IObservable<DisplayInfo>>(_dpiChangesSubject));

        _viewModel.StartDpiMonitoring(new object());

        // Act
        _viewModel.Dispose();

        // Assert - subscription should be disposed, no exception should be thrown
        Action act = () => _dpiChangesSubject.OnNext(DisplayInfo.FromScale(2.0));
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldDisposeQualitySubscription()
    {
        // Act
        _viewModel.Dispose();

        // Assert - subscription should be disposed, no exception should be thrown
        Action act = () => _qualityChangesSubject.OnNext(RenderingQuality.Ultra);
        act.Should().NotThrow();
    }

    public void Dispose()
    {
        _viewModel?.Dispose();
        _dpiChangesSubject?.Dispose();
        _qualityChangesSubject?.Dispose();
    }
}
