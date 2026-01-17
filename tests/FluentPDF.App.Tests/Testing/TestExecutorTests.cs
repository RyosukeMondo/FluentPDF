using FluentAssertions;
using FluentPDF.App.Testing;
using Moq;
using Serilog;

namespace FluentPDF.App.Tests.Testing;

/// <summary>
/// Unit tests for TestExecutor component.
/// Verifies test execution, isolation, timeout handling, and error handling.
/// </summary>
public sealed class TestExecutorTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger> _mockLogger;

    public TestExecutorTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public void Constructor_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TestExecutor(null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TestExecutor(_mockServiceProvider.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task ExecuteTestAsync_WithNullTest_ThrowsArgumentNullException()
    {
        // Arrange
        var executor = new TestExecutor(_mockServiceProvider.Object, _mockLogger.Object);

        // Act & Assert
        var act = async () => await executor.ExecuteTestAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("test");
    }

    [Fact]
    public async Task ExecuteTestAsync_WithPassingTest_ReturnsSuccessResult()
    {
        // Arrange
        var executor = new TestExecutor(_mockServiceProvider.Object, _mockLogger.Object);
        var mockTest = CreateMockTest("test-success", success: true, verifyPasses: true);

        // Act
        var result = await executor.ExecuteTestAsync(mockTest.Object);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.TestName.Should().Be("test-success");
        result.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        result.ErrorMessage.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteTestAsync_WithFailingTest_ReturnsFailureResult()
    {
        // Arrange
        var executor = new TestExecutor(_mockServiceProvider.Object, _mockLogger.Object);
        var mockTest = CreateMockTest("test-failure", success: false, verifyPasses: false);

        // Act
        var result = await executor.ExecuteTestAsync(mockTest.Object);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.TestName.Should().Be("test-failure");
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ExecuteTestAsync_WhenTestThrowsException_CapturesException()
    {
        // Arrange
        var executor = new TestExecutor(_mockServiceProvider.Object, _mockLogger.Object);
        var mockTest = new Mock<ICliTest>();
        mockTest.Setup(t => t.Name).Returns("test-exception");
        mockTest.Setup(t => t.Description).Returns("Test that throws");
        mockTest.Setup(t => t.RunAsync(It.IsAny<CliTestContext>()))
            .ThrowsAsync(new InvalidOperationException("Test failed"));

        // Act
        var result = await executor.ExecuteTestAsync(mockTest.Object);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.TestName.Should().Be("test-exception");
        result.ErrorMessage.Should().Contain("Test execution failed");
        result.ErrorMessage.Should().Contain("Test failed");
    }

    [Fact]
    public async Task ExecuteTestAsync_WhenVerificationFails_ReturnsFailureResult()
    {
        // Arrange
        var executor = new TestExecutor(_mockServiceProvider.Object, _mockLogger.Object);
        var mockTest = CreateMockTest("test-verify-fail", success: true, verifyPasses: false);

        // Act
        var result = await executor.ExecuteTestAsync(mockTest.Object);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Verification failed");
    }

    [Fact]
    public async Task ExecuteTestAsync_WhenVerificationThrows_CapturesException()
    {
        // Arrange
        var executor = new TestExecutor(_mockServiceProvider.Object, _mockLogger.Object);
        var mockTest = new Mock<ICliTest>();
        mockTest.Setup(t => t.Name).Returns("test-verify-throw");
        mockTest.Setup(t => t.Description).Returns("Test with throwing verification");
        mockTest.Setup(t => t.RunAsync(It.IsAny<CliTestContext>()))
            .ReturnsAsync(new CliTestResult { TestName = "test-verify-throw", Success = true });
        mockTest.Setup(t => t.VerifyAsync(It.IsAny<CliTestResult>()))
            .ThrowsAsync(new InvalidOperationException("Verification error"));

        // Act
        var result = await executor.ExecuteTestAsync(mockTest.Object);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Verification threw exception");
        result.ErrorMessage.Should().Contain("Verification error");
    }

    [Fact]
    public async Task ExecuteTestAsync_WithTimeout_TimesOutLongRunningTest()
    {
        // Arrange
        var executor = new TestExecutor(_mockServiceProvider.Object, _mockLogger.Object);
        var mockTest = new Mock<ICliTest>();
        mockTest.Setup(t => t.Name).Returns("test-timeout");
        mockTest.Setup(t => t.Description).Returns("Test that times out");
        mockTest.Setup(t => t.RunAsync(It.IsAny<CliTestContext>()))
            .Returns(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5)); // Longer than timeout
                return new CliTestResult { TestName = "test-timeout", Success = true };
            });

        // Act
        var result = await executor.ExecuteTestAsync(mockTest.Object, timeout: TimeSpan.FromMilliseconds(100));

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("timed out");
        result.Duration.Should().BeCloseTo(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public async Task ExecuteTestAsync_WithDefaultTimeout_Uses30Seconds()
    {
        // Arrange
        var executor = new TestExecutor(_mockServiceProvider.Object, _mockLogger.Object);
        var mockTest = CreateMockTest("test-default-timeout", success: true, verifyPasses: true);

        // Act
        var result = await executor.ExecuteTestAsync(mockTest.Object);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        // Test should complete quickly, well under 30 seconds
        result.Duration.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ExecuteTestAsync_CreatesIsolatedContext()
    {
        // Arrange
        var executor = new TestExecutor(_mockServiceProvider.Object, _mockLogger.Object);
        CliTestContext? capturedContext = null;

        var mockTest = new Mock<ICliTest>();
        mockTest.Setup(t => t.Name).Returns("test-context");
        mockTest.Setup(t => t.Description).Returns("Test to capture context");
        mockTest.Setup(t => t.RunAsync(It.IsAny<CliTestContext>()))
            .Callback<CliTestContext>(ctx => capturedContext = ctx)
            .ReturnsAsync(new CliTestResult { TestName = "test-context", Success = true });
        mockTest.Setup(t => t.VerifyAsync(It.IsAny<CliTestResult>()))
            .ReturnsAsync(true);

        // Act
        var result = await executor.ExecuteTestAsync(mockTest.Object);

        // Assert
        capturedContext.Should().NotBeNull();
        capturedContext!.WorkingDirectory.Should().NotBeNullOrEmpty();
        capturedContext.Services.Should().BeSameAs(_mockServiceProvider.Object);
        capturedContext.Logger.Should().NotBeNull();
        // Working directory should have been cleaned up after execution
        Directory.Exists(capturedContext.WorkingDirectory).Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteTestAsync_CleanupContext_EvenOnException()
    {
        // Arrange
        var executor = new TestExecutor(_mockServiceProvider.Object, _mockLogger.Object);
        string? workingDirectory = null;

        var mockTest = new Mock<ICliTest>();
        mockTest.Setup(t => t.Name).Returns("test-cleanup");
        mockTest.Setup(t => t.Description).Returns("Test cleanup on exception");
        mockTest.Setup(t => t.RunAsync(It.IsAny<CliTestContext>()))
            .Callback<CliTestContext>(ctx => workingDirectory = ctx.WorkingDirectory)
            .ThrowsAsync(new InvalidOperationException("Test error"));

        // Act
        var result = await executor.ExecuteTestAsync(mockTest.Object);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        workingDirectory.Should().NotBeNullOrEmpty();
        Directory.Exists(workingDirectory!).Should().BeFalse("context should be cleaned up");
    }

    [Fact]
    public void CreateContext_WithValidName_CreatesUniqueDirectory()
    {
        // Arrange
        var executor = new TestExecutor(_mockServiceProvider.Object, _mockLogger.Object);

        // Act
        using var context1 = executor.CreateContext("test-1");
        using var context2 = executor.CreateContext("test-1");

        // Assert
        context1.WorkingDirectory.Should().NotBe(context2.WorkingDirectory);
        Directory.Exists(context1.WorkingDirectory).Should().BeTrue();
        Directory.Exists(context2.WorkingDirectory).Should().BeTrue();
    }

    [Fact]
    public void CreateContext_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var executor = new TestExecutor(_mockServiceProvider.Object, _mockLogger.Object);

        // Act & Assert
        var act = () => executor.CreateContext(null!);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("testName");
    }

    [Fact]
    public void CreateContext_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var executor = new TestExecutor(_mockServiceProvider.Object, _mockLogger.Object);

        // Act & Assert
        var act = () => executor.CreateContext("");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("testName");
    }

    [Fact]
    public void CreateContext_WithInvalidChars_SanitizesFileName()
    {
        // Arrange
        var executor = new TestExecutor(_mockServiceProvider.Object, _mockLogger.Object);

        // Act
        using var context = executor.CreateContext("test/with\\invalid:chars");

        // Assert
        context.WorkingDirectory.Should().NotContain("/");
        context.WorkingDirectory.Should().NotContain("\\\\"); // Not literal backslashes in name
        context.WorkingDirectory.Should().NotContain(":");
        Directory.Exists(context.WorkingDirectory).Should().BeTrue();
    }

    [Fact]
    public void CreateContext_SetsServicesAndLogger()
    {
        // Arrange
        var executor = new TestExecutor(_mockServiceProvider.Object, _mockLogger.Object);

        // Act
        using var context = executor.CreateContext("test-services");

        // Assert
        context.Services.Should().BeSameAs(_mockServiceProvider.Object);
        context.Logger.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecuteTestAsync_LogsExecutionDetails()
    {
        // Arrange
        var executor = new TestExecutor(_mockServiceProvider.Object, _mockLogger.Object);
        var mockTest = CreateMockTest("test-logging", success: true, verifyPasses: true);

        // Act
        await executor.ExecuteTestAsync(mockTest.Object);

        // Assert
        _mockLogger.Verify(
            l => l.Information(
                It.Is<string>(s => s.Contains("Executing test")),
                It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteTestAsync_MultipleConcurrentTests_IsolatesContexts()
    {
        // Arrange
        var executor = new TestExecutor(_mockServiceProvider.Object, _mockLogger.Object);
        var directories = new List<string>();

        var mockTest1 = new Mock<ICliTest>();
        mockTest1.Setup(t => t.Name).Returns("concurrent-1");
        mockTest1.Setup(t => t.Description).Returns("Concurrent test 1");
        mockTest1.Setup(t => t.RunAsync(It.IsAny<CliTestContext>()))
            .Callback<CliTestContext>(ctx => { lock (directories) directories.Add(ctx.WorkingDirectory); })
            .ReturnsAsync(new CliTestResult { TestName = "concurrent-1", Success = true });
        mockTest1.Setup(t => t.VerifyAsync(It.IsAny<CliTestResult>())).ReturnsAsync(true);

        var mockTest2 = new Mock<ICliTest>();
        mockTest2.Setup(t => t.Name).Returns("concurrent-2");
        mockTest2.Setup(t => t.Description).Returns("Concurrent test 2");
        mockTest2.Setup(t => t.RunAsync(It.IsAny<CliTestContext>()))
            .Callback<CliTestContext>(ctx => { lock (directories) directories.Add(ctx.WorkingDirectory); })
            .ReturnsAsync(new CliTestResult { TestName = "concurrent-2", Success = true });
        mockTest2.Setup(t => t.VerifyAsync(It.IsAny<CliTestResult>())).ReturnsAsync(true);

        // Act
        var task1 = executor.ExecuteTestAsync(mockTest1.Object);
        var task2 = executor.ExecuteTestAsync(mockTest2.Object);
        await Task.WhenAll(task1, task2);

        // Assert
        directories.Should().HaveCount(2);
        directories[0].Should().NotBe(directories[1], "tests should have isolated directories");
    }

    /// <summary>
    /// Helper method to create a mock test with common behavior.
    /// </summary>
    private Mock<ICliTest> CreateMockTest(string name, bool success, bool verifyPasses)
    {
        var mockTest = new Mock<ICliTest>();
        mockTest.Setup(t => t.Name).Returns(name);
        mockTest.Setup(t => t.Description).Returns($"Mock test: {name}");
        mockTest.Setup(t => t.RunAsync(It.IsAny<CliTestContext>()))
            .ReturnsAsync(new CliTestResult
            {
                TestName = name,
                Success = success,
                ErrorMessage = success ? null : "Test failed"
            });
        mockTest.Setup(t => t.VerifyAsync(It.IsAny<CliTestResult>()))
            .ReturnsAsync(verifyPasses);
        return mockTest;
    }
}
