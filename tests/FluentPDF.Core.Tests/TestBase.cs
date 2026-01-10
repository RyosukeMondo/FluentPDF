using AutoFixture;
using Moq;

namespace FluentPDF.Core.Tests;

/// <summary>
/// Base class for unit tests providing common test utilities and helpers.
/// </summary>
public abstract class TestBase
{
    /// <summary>
    /// AutoFixture instance for generating test data with randomized values.
    /// Use this to create test objects without manually specifying all properties.
    /// </summary>
    protected IFixture Fixture { get; }

    /// <summary>
    /// Initializes a new instance of the TestBase class.
    /// Sets up AutoFixture with default configuration.
    /// </summary>
    protected TestBase()
    {
        Fixture = new Fixture();
    }

    /// <summary>
    /// Creates a mock of the specified type with strict behavior.
    /// Strict mocks require all method calls to be explicitly set up.
    /// </summary>
    /// <typeparam name="T">The type to mock (must be an interface or abstract class).</typeparam>
    /// <returns>A Mock instance configured with strict behavior.</returns>
    protected Mock<T> CreateStrictMock<T>() where T : class
    {
        return new Mock<T>(MockBehavior.Strict);
    }

    /// <summary>
    /// Creates a mock of the specified type with loose behavior.
    /// Loose mocks return default values for methods that are not set up.
    /// </summary>
    /// <typeparam name="T">The type to mock (must be an interface or abstract class).</typeparam>
    /// <returns>A Mock instance configured with loose behavior.</returns>
    protected Mock<T> CreateLooseMock<T>() where T : class
    {
        return new Mock<T>(MockBehavior.Loose);
    }
}
