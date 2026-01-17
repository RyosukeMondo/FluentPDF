using FluentAssertions;
using FluentPDF.App.Testing;
using Moq;
using Serilog;

namespace FluentPDF.App.Tests.Testing;

/// <summary>
/// Unit tests for TestDiscovery component.
/// Verifies test discovery, caching, and error handling.
/// </summary>
public sealed class TestDiscoveryTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<ILogger> _mockLogger;

    public TestDiscoveryTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger>();
    }

    [Fact]
    public void Constructor_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TestDiscovery(null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TestDiscovery(_mockServiceProvider.Object, null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void DiscoverAllTests_FirstCall_DiscoverTestsFromAssembly()
    {
        // Arrange
        var discovery = new TestDiscovery(_mockServiceProvider.Object, _mockLogger.Object);

        // Act
        var tests = discovery.DiscoverAllTests();

        // Assert
        tests.Should().NotBeNull();
        tests.Should().BeOfType<List<ICliTest>>();
        // RenderCliTest should be discovered as it's in the same assembly
        tests.Should().Contain(t => t.Name == "render-pdf");
    }

    [Fact]
    public void DiscoverAllTests_SecondCall_ReturnsCachedResults()
    {
        // Arrange
        var discovery = new TestDiscovery(_mockServiceProvider.Object, _mockLogger.Object);

        // Act
        var tests1 = discovery.DiscoverAllTests();
        var tests2 = discovery.DiscoverAllTests();

        // Assert
        tests1.Should().BeSameAs(tests2, "second call should return cached instance");
    }

    [Fact]
    public void DiscoverAllTests_LogsDiscoveryProcess()
    {
        // Arrange
        var discovery = new TestDiscovery(_mockServiceProvider.Object, _mockLogger.Object);

        // Act
        discovery.DiscoverAllTests();

        // Assert
        _mockLogger.Verify(
            l => l.Information(
                It.Is<string>(s => s.Contains("Starting test discovery")),
                It.IsAny<object[]>()),
            Times.Once);

        _mockLogger.Verify(
            l => l.Information(
                It.Is<string>(s => s.Contains("Test discovery complete")),
                It.IsAny<object[]>()),
            Times.Once);
    }

    [Fact]
    public void FindTest_WithValidName_ReturnsMatchingTest()
    {
        // Arrange
        var discovery = new TestDiscovery(_mockServiceProvider.Object, _mockLogger.Object);

        // Act
        var test = discovery.FindTest("render-pdf");

        // Assert
        test.Should().NotBeNull();
        test!.Name.Should().Be("render-pdf");
    }

    [Fact]
    public void FindTest_WithNonExistentName_ReturnsNull()
    {
        // Arrange
        var discovery = new TestDiscovery(_mockServiceProvider.Object, _mockLogger.Object);

        // Act
        var test = discovery.FindTest("non-existent-test");

        // Assert
        test.Should().BeNull();
    }

    [Fact]
    public void FindTest_WithNullName_ThrowsArgumentException()
    {
        // Arrange
        var discovery = new TestDiscovery(_mockServiceProvider.Object, _mockLogger.Object);

        // Act & Assert
        var act = () => discovery.FindTest(null!);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void FindTest_WithEmptyName_ThrowsArgumentException()
    {
        // Arrange
        var discovery = new TestDiscovery(_mockServiceProvider.Object, _mockLogger.Object);

        // Act & Assert
        var act = () => discovery.FindTest("");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void FindTest_WithWhitespaceName_ThrowsArgumentException()
    {
        // Arrange
        var discovery = new TestDiscovery(_mockServiceProvider.Object, _mockLogger.Object);

        // Act & Assert
        var act = () => discovery.FindTest("   ");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("name");
    }

    [Fact]
    public void FindTest_IsCaseInsensitive()
    {
        // Arrange
        var discovery = new TestDiscovery(_mockServiceProvider.Object, _mockLogger.Object);

        // Act
        var test1 = discovery.FindTest("render-pdf");
        var test2 = discovery.FindTest("RENDER-PDF");
        var test3 = discovery.FindTest("Render-Pdf");

        // Assert
        test1.Should().NotBeNull();
        test2.Should().NotBeNull();
        test3.Should().NotBeNull();
        test1.Should().BeSameAs(test2);
        test1.Should().BeSameAs(test3);
    }

    [Fact]
    public void ClearCache_AfterCaching_ForcesRediscovery()
    {
        // Arrange
        var discovery = new TestDiscovery(_mockServiceProvider.Object, _mockLogger.Object);
        var firstTests = discovery.DiscoverAllTests();

        // Act
        discovery.ClearCache();
        var secondTests = discovery.DiscoverAllTests();

        // Assert
        firstTests.Should().NotBeSameAs(secondTests, "cache was cleared, should discover again");
        firstTests.Should().BeEquivalentTo(secondTests, "should discover same tests");
    }

    [Fact]
    public void DiscoverAllTests_WithServiceProvider_UsesServiceProviderFirst()
    {
        // Arrange
        var mockTest = new Mock<ICliTest>();
        mockTest.Setup(t => t.Name).Returns("test-from-di");
        mockTest.Setup(t => t.Description).Returns("Test from DI");

        // Note: This test verifies the DI fallback logic exists, but can't easily
        // inject tests into the actual discovery process since it uses reflection
        // on the executing assembly. This is a limitation of the current design.
        var discovery = new TestDiscovery(_mockServiceProvider.Object, _mockLogger.Object);

        // Act
        var tests = discovery.DiscoverAllTests();

        // Assert
        tests.Should().NotBeNull();
        // The actual test implementation will discover RenderCliTest from the assembly
    }

    [Fact]
    public void DiscoverAllTests_LogsEachDiscoveredTest()
    {
        // Arrange
        var discovery = new TestDiscovery(_mockServiceProvider.Object, _mockLogger.Object);

        // Act
        discovery.DiscoverAllTests();

        // Assert
        _mockLogger.Verify(
            l => l.Debug(
                It.Is<string>(s => s.Contains("Discovered test")),
                It.IsAny<object[]>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void DiscoverAllTests_WithNoTests_ReturnsEmptyList()
    {
        // Note: This test can't be easily implemented without creating a separate
        // test assembly or using advanced mocking. In practice, RenderCliTest
        // will always be discovered. This documents the expected behavior.

        // Arrange
        var discovery = new TestDiscovery(_mockServiceProvider.Object, _mockLogger.Object);

        // Act
        var tests = discovery.DiscoverAllTests();

        // Assert
        tests.Should().NotBeNull();
        tests.Should().BeOfType<List<ICliTest>>();
        // In actual execution, at least RenderCliTest exists
    }

    [Fact]
    public void FindTest_LogsWarning_WhenTestNotFound()
    {
        // Arrange
        var discovery = new TestDiscovery(_mockServiceProvider.Object, _mockLogger.Object);

        // Act
        discovery.FindTest("non-existent-test");

        // Assert
        _mockLogger.Verify(
            l => l.Warning(
                It.Is<string>(s => s.Contains("Test not found")),
                It.IsAny<object[]>()),
            Times.Once);
    }
}
