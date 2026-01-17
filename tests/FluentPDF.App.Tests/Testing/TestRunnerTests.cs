using FluentAssertions;
using FluentPDF.App.Testing;
using Moq;
using Serilog;

namespace FluentPDF.App.Tests.Testing;

/// <summary>
/// Unit tests for TestRunner component.
/// Verifies orchestration of test discovery, execution, and reporting.
/// </summary>
public sealed class TestRunnerTests
{
    private readonly Mock<TestDiscovery> _mockDiscovery;
    private readonly Mock<TestExecutor> _mockExecutor;
    private readonly Mock<ResultVerifier> _mockVerifier;
    private readonly Mock<ILogger> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;

    public TestRunnerTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger>();
        _mockDiscovery = new Mock<TestDiscovery>(_mockServiceProvider.Object, _mockLogger.Object);
        _mockExecutor = new Mock<TestExecutor>(_mockServiceProvider.Object, _mockLogger.Object);
        _mockVerifier = new Mock<ResultVerifier>(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullDiscovery_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TestRunner(null!, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("discovery");
    }

    [Fact]
    public void Constructor_WithNullExecutor_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TestRunner(_mockDiscovery.Object, null!, _mockVerifier.Object, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("executor");
    }

    [Fact]
    public void Constructor_WithNullVerifier_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("verifier");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void DiscoverTests_CallsDiscoveryComponent()
    {
        // Arrange
        var expectedTests = new List<ICliTest> { CreateMockTest("test-1").Object };
        _mockDiscovery.Setup(d => d.DiscoverAllTests()).Returns(expectedTests);
        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);

        // Act
        var tests = runner.DiscoverTests();

        // Assert
        tests.Should().BeSameAs(expectedTests);
        _mockDiscovery.Verify(d => d.DiscoverAllTests(), Times.Once);
    }

    [Fact]
    public void DiscoverTests_WhenDiscoveryThrows_PropagatesException()
    {
        // Arrange
        _mockDiscovery.Setup(d => d.DiscoverAllTests())
            .Throws(new InvalidOperationException("Discovery failed"));
        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);

        // Act & Assert
        var act = () => runner.DiscoverTests();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Discovery failed");
    }

    [Fact]
    public async Task RunTestAsync_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);

        // Act & Assert
        var act = async () => await runner.RunTestAsync(null!);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("testName");
    }

    [Fact]
    public async Task RunTestAsync_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);

        // Act & Assert
        var act = async () => await runner.RunTestAsync("");
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("testName");
    }

    [Fact]
    public async Task RunTestAsync_WithNonExistentTest_ReturnsFailureResult()
    {
        // Arrange
        _mockDiscovery.Setup(d => d.FindTest("non-existent")).Returns((ICliTest?)null);
        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);

        // Act
        var result = await runner.RunTestAsync("non-existent");

        // Assert
        result.Should().NotBeNull();
        result.TotalTests.Should().Be(0);
        result.PassedTests.Should().Be(0);
        result.FailedTests.Should().Be(0);
    }

    [Fact]
    public async Task RunTestAsync_WithValidTest_ExecutesAndReturnsResult()
    {
        // Arrange
        var mockTest = CreateMockTest("test-1");
        _mockDiscovery.Setup(d => d.FindTest("test-1")).Returns(mockTest.Object);

        var testResult = new CliTestResult
        {
            TestName = "test-1",
            Success = true,
            Duration = TimeSpan.FromMilliseconds(100)
        };
        _mockExecutor.Setup(e => e.ExecuteTestAsync(mockTest.Object, null))
            .ReturnsAsync(testResult);

        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);

        // Act
        var result = await runner.RunTestAsync("test-1");

        // Assert
        result.Should().NotBeNull();
        result.TotalTests.Should().Be(1);
        result.PassedTests.Should().Be(1);
        result.FailedTests.Should().Be(0);
        result.Results.Should().ContainSingle();
        result.Results[0].Should().BeSameAs(testResult);
    }

    [Fact]
    public async Task RunTestAsync_WithFailedTest_ReturnsFailureResult()
    {
        // Arrange
        var mockTest = CreateMockTest("test-fail");
        _mockDiscovery.Setup(d => d.FindTest("test-fail")).Returns(mockTest.Object);

        var testResult = new CliTestResult
        {
            TestName = "test-fail",
            Success = false,
            Duration = TimeSpan.FromMilliseconds(50),
            ErrorMessage = "Test failed"
        };
        _mockExecutor.Setup(e => e.ExecuteTestAsync(mockTest.Object, null))
            .ReturnsAsync(testResult);

        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);

        // Act
        var result = await runner.RunTestAsync("test-fail");

        // Assert
        result.Should().NotBeNull();
        result.TotalTests.Should().Be(1);
        result.PassedTests.Should().Be(0);
        result.FailedTests.Should().Be(1);
        result.Results[0].ErrorMessage.Should().Be("Test failed");
    }

    [Fact]
    public async Task RunTestAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var mockTest = CreateMockTest("test-cancel");
        _mockDiscovery.Setup(d => d.FindTest("test-cancel")).Returns(mockTest.Object);
        _mockExecutor.Setup(e => e.ExecuteTestAsync(mockTest.Object, null))
            .ThrowsAsync(new OperationCanceledException());

        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        var act = async () => await runner.RunTestAsync("test-cancel", cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RunTestAsync_WhenExecutorThrows_ReturnsFailureResult()
    {
        // Arrange
        var mockTest = CreateMockTest("test-error");
        _mockDiscovery.Setup(d => d.FindTest("test-error")).Returns(mockTest.Object);
        _mockExecutor.Setup(e => e.ExecuteTestAsync(mockTest.Object, null))
            .ThrowsAsync(new InvalidOperationException("Execution error"));

        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);

        // Act
        var result = await runner.RunTestAsync("test-error");

        // Assert
        result.Should().NotBeNull();
        result.TotalTests.Should().Be(1);
        result.PassedTests.Should().Be(0);
        result.FailedTests.Should().Be(1);
        result.Results[0].ErrorMessage.Should().Contain("Test runner failed");
        result.Results[0].ErrorMessage.Should().Contain("Execution error");
    }

    [Fact]
    public async Task RunAllTestsAsync_WithNoTests_ReturnsEmptyResult()
    {
        // Arrange
        _mockDiscovery.Setup(d => d.DiscoverAllTests()).Returns(new List<ICliTest>());
        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);

        // Act
        var result = await runner.RunAllTestsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalTests.Should().Be(0);
        result.PassedTests.Should().Be(0);
        result.FailedTests.Should().Be(0);
    }

    [Fact]
    public async Task RunAllTestsAsync_WithMultipleTests_ExecutesAllTests()
    {
        // Arrange
        var mockTest1 = CreateMockTest("test-1");
        var mockTest2 = CreateMockTest("test-2");
        var mockTest3 = CreateMockTest("test-3");

        var tests = new List<ICliTest> { mockTest1.Object, mockTest2.Object, mockTest3.Object };
        _mockDiscovery.Setup(d => d.DiscoverAllTests()).Returns(tests);

        _mockExecutor.Setup(e => e.ExecuteTestAsync(mockTest1.Object, null))
            .ReturnsAsync(new CliTestResult { TestName = "test-1", Success = true });
        _mockExecutor.Setup(e => e.ExecuteTestAsync(mockTest2.Object, null))
            .ReturnsAsync(new CliTestResult { TestName = "test-2", Success = true });
        _mockExecutor.Setup(e => e.ExecuteTestAsync(mockTest3.Object, null))
            .ReturnsAsync(new CliTestResult { TestName = "test-3", Success = false });

        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);

        // Act
        var result = await runner.RunAllTestsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalTests.Should().Be(3);
        result.PassedTests.Should().Be(2);
        result.FailedTests.Should().Be(1);
        result.Results.Should().HaveCount(3);
        _mockExecutor.Verify(e => e.ExecuteTestAsync(It.IsAny<ICliTest>(), null), Times.Exactly(3));
    }

    [Fact]
    public async Task RunAllTestsAsync_WhenOneTestThrows_ContinuesWithOtherTests()
    {
        // Arrange
        var mockTest1 = CreateMockTest("test-1");
        var mockTest2 = CreateMockTest("test-2");
        var mockTest3 = CreateMockTest("test-3");

        var tests = new List<ICliTest> { mockTest1.Object, mockTest2.Object, mockTest3.Object };
        _mockDiscovery.Setup(d => d.DiscoverAllTests()).Returns(tests);

        _mockExecutor.Setup(e => e.ExecuteTestAsync(mockTest1.Object, null))
            .ReturnsAsync(new CliTestResult { TestName = "test-1", Success = true });
        _mockExecutor.Setup(e => e.ExecuteTestAsync(mockTest2.Object, null))
            .ThrowsAsync(new InvalidOperationException("Test 2 error"));
        _mockExecutor.Setup(e => e.ExecuteTestAsync(mockTest3.Object, null))
            .ReturnsAsync(new CliTestResult { TestName = "test-3", Success = true });

        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);

        // Act
        var result = await runner.RunAllTestsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalTests.Should().Be(3);
        result.PassedTests.Should().Be(2);
        result.FailedTests.Should().Be(1);
        result.Results.Should().HaveCount(3);
        result.Results.Should().Contain(r => r.TestName == "test-2" && !r.Success);
    }

    [Fact]
    public async Task RunAllTestsAsync_WhenCancelled_StopsExecution()
    {
        // Arrange
        var mockTest1 = CreateMockTest("test-1");
        var mockTest2 = CreateMockTest("test-2");

        var tests = new List<ICliTest> { mockTest1.Object, mockTest2.Object };
        _mockDiscovery.Setup(d => d.DiscoverAllTests()).Returns(tests);

        _mockExecutor.Setup(e => e.ExecuteTestAsync(mockTest1.Object, null))
            .ReturnsAsync(new CliTestResult { TestName = "test-1", Success = true });

        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act
        var result = await runner.RunAllTestsAsync(cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.TotalTests.Should().Be(0, "no tests should complete when cancelled immediately");
    }

    [Fact]
    public async Task RunAllTestsAsync_WhenDiscoveryThrows_ReturnsFailureResult()
    {
        // Arrange
        _mockDiscovery.Setup(d => d.DiscoverAllTests())
            .Throws(new InvalidOperationException("Discovery failed"));
        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);

        // Act
        var result = await runner.RunAllTestsAsync();

        // Assert
        result.Should().NotBeNull();
        result.TotalTests.Should().Be(0);
        result.PassedTests.Should().Be(0);
        result.FailedTests.Should().Be(0);
    }

    [Fact]
    public async Task RunAllTestsAsync_AllTestsPass_SetsAllTestsPassedTrue()
    {
        // Arrange
        var mockTest1 = CreateMockTest("test-1");
        var mockTest2 = CreateMockTest("test-2");

        var tests = new List<ICliTest> { mockTest1.Object, mockTest2.Object };
        _mockDiscovery.Setup(d => d.DiscoverAllTests()).Returns(tests);

        _mockExecutor.Setup(e => e.ExecuteTestAsync(It.IsAny<ICliTest>(), null))
            .ReturnsAsync((ICliTest t, TimeSpan? _) =>
                new CliTestResult { TestName = t.Name, Success = true });

        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);

        // Act
        var result = await runner.RunAllTestsAsync();

        // Assert
        result.AllTestsPassed.Should().BeTrue();
    }

    [Fact]
    public async Task RunAllTestsAsync_AnyTestFails_SetsAllTestsPassedFalse()
    {
        // Arrange
        var mockTest1 = CreateMockTest("test-1");
        var mockTest2 = CreateMockTest("test-2");

        var tests = new List<ICliTest> { mockTest1.Object, mockTest2.Object };
        _mockDiscovery.Setup(d => d.DiscoverAllTests()).Returns(tests);

        _mockExecutor.Setup(e => e.ExecuteTestAsync(mockTest1.Object, null))
            .ReturnsAsync(new CliTestResult { TestName = "test-1", Success = true });
        _mockExecutor.Setup(e => e.ExecuteTestAsync(mockTest2.Object, null))
            .ReturnsAsync(new CliTestResult { TestName = "test-2", Success = false });

        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);

        // Act
        var result = await runner.RunAllTestsAsync();

        // Assert
        result.AllTestsPassed.Should().BeFalse();
    }

    [Fact]
    public async Task RunAllTestsAsync_LogsProgress()
    {
        // Arrange
        var mockTest1 = CreateMockTest("test-1");
        var mockTest2 = CreateMockTest("test-2");

        var tests = new List<ICliTest> { mockTest1.Object, mockTest2.Object };
        _mockDiscovery.Setup(d => d.DiscoverAllTests()).Returns(tests);

        _mockExecutor.Setup(e => e.ExecuteTestAsync(It.IsAny<ICliTest>(), null))
            .ReturnsAsync((ICliTest t, TimeSpan? _) =>
                new CliTestResult { TestName = t.Name, Success = true });

        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);

        // Act
        await runner.RunAllTestsAsync();

        // Assert
        _mockLogger.Verify(
            l => l.Information(
                It.Is<string>(s => s.Contains("Running all CLI tests")),
                It.IsAny<object[]>()),
            Times.Once);

        _mockLogger.Verify(
            l => l.Information(
                It.Is<string>(s => s.Contains("All tests completed")),
                It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public async Task RunTestAsync_LogsTestExecution()
    {
        // Arrange
        var mockTest = CreateMockTest("test-1");
        _mockDiscovery.Setup(d => d.FindTest("test-1")).Returns(mockTest.Object);

        var testResult = new CliTestResult
        {
            TestName = "test-1",
            Success = true,
            Duration = TimeSpan.FromMilliseconds(100)
        };
        _mockExecutor.Setup(e => e.ExecuteTestAsync(mockTest.Object, null))
            .ReturnsAsync(testResult);

        var runner = new TestRunner(_mockDiscovery.Object, _mockExecutor.Object, _mockVerifier.Object, _mockLogger.Object);

        // Act
        await runner.RunTestAsync("test-1");

        // Assert
        _mockLogger.Verify(
            l => l.Information(
                It.Is<string>(s => s.Contains("Running test")),
                It.IsAny<object[]>()),
            Times.Once);

        _mockLogger.Verify(
            l => l.Information(
                It.Is<string>(s => s.Contains("completed")),
                It.IsAny<object[]>()),
            Times.Once);
    }

    /// <summary>
    /// Helper method to create a mock test.
    /// </summary>
    private Mock<ICliTest> CreateMockTest(string name)
    {
        var mockTest = new Mock<ICliTest>();
        mockTest.Setup(t => t.Name).Returns(name);
        mockTest.Setup(t => t.Description).Returns($"Mock test: {name}");
        return mockTest;
    }
}
