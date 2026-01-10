using FluentAssertions;
using FluentPDF.Validation.Models;
using FluentPDF.Validation.Services;
using FluentPDF.Validation.Wrappers;
using FluentResults;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FluentPDF.Validation.Tests.Services;

public class PdfValidationServiceTests : IDisposable
{
    private readonly IQpdfWrapper _qpdfWrapper;
    private readonly IJhoveWrapper _jhoveWrapper;
    private readonly IVeraPdfWrapper _veraPdfWrapper;
    private readonly ILogger<PdfValidationService> _logger;
    private readonly PdfValidationService _service;
    private readonly string _testFilesPath;

    public PdfValidationServiceTests()
    {
        _qpdfWrapper = Substitute.For<IQpdfWrapper>();
        _jhoveWrapper = Substitute.For<IJhoveWrapper>();
        _veraPdfWrapper = Substitute.For<IVeraPdfWrapper>();
        _logger = Substitute.For<ILogger<PdfValidationService>>();

        _service = new PdfValidationService(
            _qpdfWrapper,
            _jhoveWrapper,
            _veraPdfWrapper,
            _logger);

        _testFilesPath = Path.Combine(Path.GetTempPath(), "validation-service-tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testFilesPath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFilesPath))
        {
            Directory.Delete(_testFilesPath, recursive: true);
        }
    }

    [Fact]
    public async Task ValidateAsync_WithNonExistentFile_ReturnsFailure()
    {
        // Arrange
        var filePath = Path.Combine(_testFilesPath, "nonexistent.pdf");

        // Act
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Quick);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("File not found");
    }

    [Fact]
    public async Task ValidateAsync_QuickProfile_ExecutesOnlyQpdf()
    {
        // Arrange
        var filePath = CreateTestFile("test.pdf");
        var qpdfResult = CreateSuccessQpdfResult();
        _qpdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(qpdfResult));

        // Act
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Quick);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Profile.Should().Be(ValidationProfile.Quick);
        result.Value.QpdfResult.Should().NotBeNull();
        result.Value.JhoveResult.Should().BeNull();
        result.Value.VeraPdfResult.Should().BeNull();

        await _qpdfWrapper.Received(1).ValidateAsync(
            filePath, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _jhoveWrapper.DidNotReceive().ValidateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _veraPdfWrapper.DidNotReceive().ValidateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_StandardProfile_ExecutesQpdfAndJhove()
    {
        // Arrange
        var filePath = CreateTestFile("test.pdf");
        var qpdfResult = CreateSuccessQpdfResult();
        var jhoveResult = CreateSuccessJhoveResult();

        _qpdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(qpdfResult));
        _jhoveWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(jhoveResult));

        // Act
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Standard);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Profile.Should().Be(ValidationProfile.Standard);
        result.Value.QpdfResult.Should().NotBeNull();
        result.Value.JhoveResult.Should().NotBeNull();
        result.Value.VeraPdfResult.Should().BeNull();

        await _qpdfWrapper.Received(1).ValidateAsync(
            filePath, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _jhoveWrapper.Received(1).ValidateAsync(
            filePath, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _veraPdfWrapper.DidNotReceive().ValidateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_FullProfile_ExecutesAllTools()
    {
        // Arrange
        var filePath = CreateTestFile("test.pdf");
        var qpdfResult = CreateSuccessQpdfResult();
        var jhoveResult = CreateSuccessJhoveResult();
        var veraPdfResult = CreateSuccessVeraPdfResult();

        _qpdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(qpdfResult));
        _jhoveWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(jhoveResult));
        _veraPdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(veraPdfResult));

        // Act
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Full);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Profile.Should().Be(ValidationProfile.Full);
        result.Value.QpdfResult.Should().NotBeNull();
        result.Value.JhoveResult.Should().NotBeNull();
        result.Value.VeraPdfResult.Should().NotBeNull();

        await _qpdfWrapper.Received(1).ValidateAsync(
            filePath, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _jhoveWrapper.Received(1).ValidateAsync(
            filePath, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _veraPdfWrapper.Received(1).ValidateAsync(
            filePath, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_FullProfile_ExecutesToolsInParallel()
    {
        // Arrange
        var filePath = CreateTestFile("test.pdf");
        var delay = TimeSpan.FromMilliseconds(100);

        _qpdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async x =>
            {
                await Task.Delay(delay);
                return Result.Ok(CreateSuccessQpdfResult());
            });

        _jhoveWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async x =>
            {
                await Task.Delay(delay);
                return Result.Ok(CreateSuccessJhoveResult());
            });

        _veraPdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async x =>
            {
                await Task.Delay(delay);
                return Result.Ok(CreateSuccessVeraPdfResult());
            });

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Full);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        result.IsSuccess.Should().BeTrue();
        // If executed in parallel, duration should be closer to delay (100ms) than 3x delay (300ms)
        duration.Should().BeLessThan(delay * 2, "tools should execute in parallel");
    }

    [Fact]
    public async Task ValidateAsync_AllToolsPass_ReturnsPassStatus()
    {
        // Arrange
        var filePath = CreateTestFile("test.pdf");
        _qpdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(CreateSuccessQpdfResult()));
        _jhoveWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(CreateSuccessJhoveResult()));
        _veraPdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(CreateSuccessVeraPdfResult()));

        // Act
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Full);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(ValidationStatus.Pass);
        result.Value.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_OneToolFails_ReturnsFailStatus()
    {
        // Arrange
        var filePath = CreateTestFile("test.pdf");
        var failedQpdfResult = new QpdfResult
        {
            Status = ValidationStatus.Fail,
            Errors = new[] { "Cross-reference table error" }
        };

        _qpdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(failedQpdfResult));
        _jhoveWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(CreateSuccessJhoveResult()));
        _veraPdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(CreateSuccessVeraPdfResult()));

        // Act
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Full);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(ValidationStatus.Fail);
        result.Value.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateAsync_OneToolWarns_ReturnsWarnStatus()
    {
        // Arrange
        var filePath = CreateTestFile("test.pdf");
        var warnQpdfResult = new QpdfResult
        {
            Status = ValidationStatus.Warn,
            Errors = Array.Empty<string>(),
            Warnings = new[] { "Minor issue detected" }
        };

        _qpdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(warnQpdfResult));
        _jhoveWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(CreateSuccessJhoveResult()));
        _veraPdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(CreateSuccessVeraPdfResult()));

        // Act
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Full);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OverallStatus.Should().Be(ValidationStatus.Warn);
    }

    [Fact]
    public async Task ValidateAsync_ToolThrowsException_ContinuesWithOtherTools()
    {
        // Arrange
        var filePath = CreateTestFile("test.pdf");
        _qpdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Fail<QpdfResult>("QPDF execution failed"));
        _jhoveWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(CreateSuccessJhoveResult()));
        _veraPdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(CreateSuccessVeraPdfResult()));

        // Act
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Full);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.QpdfResult.Should().BeNull();
        result.Value.JhoveResult.Should().NotBeNull();
        result.Value.VeraPdfResult.Should().NotBeNull();

        // Verify all tools were attempted
        await _qpdfWrapper.Received(1).ValidateAsync(
            filePath, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _jhoveWrapper.Received(1).ValidateAsync(
            filePath, Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _veraPdfWrapper.Received(1).ValidateAsync(
            filePath, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_CancellationRequested_ReturnsCancelledResult()
    {
        // Arrange
        var filePath = CreateTestFile("test.pdf");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _qpdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<OperationCanceledException>();

        // Act
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Quick, cancellationToken: cts.Token);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle()
            .Which.Message.Should().Contain("cancelled");
    }

    [Fact]
    public async Task ValidateAsync_SetsCorrectMetadata()
    {
        // Arrange
        var filePath = CreateTestFile("test.pdf");
        var correlationId = "test-correlation-id";
        _qpdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(CreateSuccessQpdfResult()));

        // Act
        var startTime = DateTime.UtcNow;
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Quick, correlationId);
        var endTime = DateTime.UtcNow;

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FilePath.Should().Be(filePath);
        result.Value.Profile.Should().Be(ValidationProfile.Quick);
        result.Value.ValidationDate.Should().BeCloseTo(startTime, TimeSpan.FromSeconds(1));
        result.Value.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.Value.Duration.Should().BeLessThan(endTime - startTime + TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ValidateAsync_GeneratesCorrelationIdIfNotProvided()
    {
        // Arrange
        var filePath = CreateTestFile("test.pdf");
        _qpdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(CreateSuccessQpdfResult()));

        // Act
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Quick);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _qpdfWrapper.Received(1).ValidateAsync(
            filePath, Arg.Is<string>(id => !string.IsNullOrEmpty(id)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ValidateAsync_UsesProvidedCorrelationId()
    {
        // Arrange
        var filePath = CreateTestFile("test.pdf");
        var correlationId = "my-correlation-id";
        _qpdfWrapper.ValidateAsync(filePath, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Ok(CreateSuccessQpdfResult()));

        // Act
        var result = await _service.ValidateAsync(filePath, ValidationProfile.Quick, correlationId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _qpdfWrapper.Received(1).ValidateAsync(filePath, correlationId, Arg.Any<CancellationToken>());
    }

    private string CreateTestFile(string fileName)
    {
        var filePath = Path.Combine(_testFilesPath, fileName);
        File.WriteAllText(filePath, "test content");
        return filePath;
    }

    private QpdfResult CreateSuccessQpdfResult() => new()
    {
        Status = ValidationStatus.Pass,
        Errors = Array.Empty<string>(),
        Warnings = Array.Empty<string>()
    };

    private JhoveResult CreateSuccessJhoveResult() => new()
    {
        Format = "1.7",
        Validity = "Valid",
        Status = ValidationStatus.Pass,
        Messages = Array.Empty<string>()
    };

    private VeraPdfResult CreateSuccessVeraPdfResult() => new()
    {
        IsCompliant = true,
        Flavour = PdfFlavour.PdfA1b,
        Status = ValidationStatus.Pass,
        Errors = Array.Empty<VeraPdfError>(),
        TotalChecks = 100,
        FailedChecks = 0
    };
}
