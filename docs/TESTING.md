# FluentPDF Testing Strategy

This document describes the comprehensive testing strategy for FluentPDF, including test types, coverage requirements, and how to write and run tests.

## Table of Contents

- [Testing Philosophy](#testing-philosophy)
- [Testing Pyramid](#testing-pyramid)
- [Test Project Structure](#test-project-structure)
- [Running Tests](#running-tests)
- [Writing Tests](#writing-tests)
- [Architecture Tests](#architecture-tests)
- [Unit Tests](#unit-tests)
- [UI Tests](#ui-tests)
- [Coverage Requirements](#coverage-requirements)
- [CI/CD Integration](#cicd-integration)

## Testing Philosophy

FluentPDF follows these core testing principles:

1. **Tests are First-Class Code**: Tests receive the same care and attention as production code
2. **Fast Feedback**: Unit tests should complete in seconds, not minutes
3. **Headless Where Possible**: Core logic tests run without UI runtime (Linux/CI compatible)
4. **Architecture as Code**: ArchUnitNET enforces architectural rules automatically
5. **Comprehensive Coverage**: 80% minimum, 90% for critical paths

## Testing Pyramid

```
                    /\
                   /  \
                  / UI \           FlaUI automation (slow, few)
                 /------\
                /        \
               / Integr.  \        Cross-layer tests (moderate, some)
              /------------\
             /              \
            /   Unit Tests   \    Isolated tests (fast, many)
           /------------------\
          /  Architecture Tests \ ArchUnitNET rules (instant, comprehensive)
         /______________________\
```

### Test Distribution

- **Architecture Tests**: ~10 tests, run on every build (< 1 second)
- **Unit Tests**: ~500+ tests, run on every build (< 10 seconds)
- **Integration Tests**: ~50 tests, run on PR (< 30 seconds)
- **UI Tests**: ~20 tests, run on release candidate (< 2 minutes)

## Test Project Structure

```
tests/
├── FluentPDF.Architecture.Tests/    # ArchUnitNET architecture validation
│   ├── LayerTests.cs                # Dependency rules
│   ├── NamingTests.cs               # Naming conventions
│   ├── InterfaceTests.cs            # Interface patterns
│   └── ArchitectureTestBase.cs     # Shared architecture setup
│
├── FluentPDF.Core.Tests/            # Unit tests (headless)
│   ├── ErrorHandling/
│   │   └── PdfErrorTests.cs         # Error type tests
│   ├── Logging/
│   │   └── SerilogConfigurationTests.cs
│   ├── ViewModels/
│   │   └── MainViewModelTests.cs   # ViewModel tests (no UI runtime)
│   └── TestBase.cs                  # Shared test utilities
│
└── FluentPDF.App.Tests/             # UI automation tests
    ├── Views/
    │   └── MainWindowTests.cs       # FlaUI automation
    └── UITestBase.cs                # FlaUI initialization helpers
```

## Running Tests

### Run All Tests

```bash
dotnet test FluentPDF.sln
```

### Run Specific Test Projects

```bash
# Architecture tests (fastest, no dependencies)
dotnet test tests/FluentPDF.Architecture.Tests

# Core unit tests (headless, Linux-compatible)
dotnet test tests/FluentPDF.Core.Tests

# UI tests (requires Windows runtime)
dotnet test tests/FluentPDF.App.Tests
```

### Run Tests with Coverage

```bash
# Install coverage tool (first time only)
dotnet tool install --global dotnet-coverage

# Run with coverage
dotnet coverage collect "dotnet test FluentPDF.sln" -f xml -o coverage.xml

# View coverage report
dotnet tool install --global dotnet-reportgenerator-globaltool
reportgenerator -reports:coverage.xml -targetdir:coverage-report
```

### Run Specific Tests

```bash
# By test name pattern
dotnet test --filter "FullyQualifiedName~PdfError"

# By test category (if using [Trait])
dotnet test --filter "Category=Unit"

# By class name
dotnet test --filter "ClassName~MainViewModelTests"
```

### Visual Studio Test Explorer

1. Open Test Explorer: **View** → **Test Explorer** (Ctrl+E, T)
2. Click **Run All** or right-click specific tests
3. View results, stack traces, and output
4. Debug tests by right-clicking → **Debug Selected Tests**

## Writing Tests

### General Test Structure

Follow the **Arrange-Act-Assert** pattern:

```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange: Set up test data and dependencies
    var sut = new SystemUnderTest();

    // Act: Execute the method being tested
    var result = sut.DoSomething();

    // Assert: Verify expected behavior
    result.Should().Be(expectedValue);
}
```

### Test Naming Conventions

Use the pattern: `MethodName_Scenario_ExpectedBehavior`

Examples:
- `LoadDocument_FileNotFound_ReturnsFailureResult`
- `SaveCommand_WhenLoading_CannotExecute`
- `PdfError_WithContext_IncludesContextInMetadata`

### Test Attributes

```csharp
[Fact]                                  // Single test case
public void SimpleTest() { }

[Theory]                                // Parameterized test
[InlineData(1, 2, 3)]
[InlineData(5, 5, 10)]
public void AddNumbers_ReturnsSum(int a, int b, int expected)
{
    var result = a + b;
    result.Should().Be(expected);
}

[Trait("Category", "Unit")]            // Test categorization
[Trait("Priority", "High")]
public void CriticalTest() { }
```

## Architecture Tests

### Purpose

ArchUnitNET tests enforce architectural rules at compile-time:
- Layer dependencies
- Naming conventions
- Interface patterns
- Code organization

### Example: Layer Dependency Rule

```csharp
[Fact]
public void CoreLayer_ShouldNot_DependOn_AppLayer()
{
    // Arrange
    var rule = Classes()
        .That().ResideInNamespace("FluentPDF.Core")
        .Should().NotDependOnAny(Classes()
            .That().ResideInNamespace("FluentPDF.App"))
        .Because("Core must be UI-agnostic for testability");

    // Act & Assert
    rule.Check(Architecture);
}
```

### Example: Naming Convention Rule

```csharp
[Fact]
public void ViewModels_Should_EndWith_ViewModel()
{
    var rule = Classes()
        .That().AreAssignableTo(typeof(ObservableObject))
        .And().DoNotHaveFullName("CommunityToolkit.Mvvm.ComponentModel.ObservableObject")
        .Should().HaveNameEndingWith("ViewModel")
        .Because("ViewModels must follow naming conventions");

    rule.Check(Architecture);
}
```

### Example: Interface Pattern Rule

```csharp
[Fact]
public void Services_Should_ImplementInterfaces()
{
    var rule = Classes()
        .That().HaveNameEndingWith("Service")
        .And().AreNotInterfaces()
        .Should().ImplementInterface(typeof(object))  // Placeholder - actual logic more complex
        .Because("Services must be abstracted for DI and testing");

    rule.Check(Architecture);
}
```

### Adding New Architecture Rules

1. Identify the architectural constraint
2. Create a new test in the appropriate file:
   - `LayerTests.cs`: Dependency rules
   - `NamingTests.cs`: Naming conventions
   - `InterfaceTests.cs`: Interface patterns
3. Write the rule using ArchUnitNET fluent API
4. Add `.Because()` clause explaining the rule
5. Run tests to verify rule enforcement

## Unit Tests

### Core Unit Test Example

```csharp
public class PdfErrorTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        // Arrange
        var errorCode = "PDF_ERROR_001";
        var category = ErrorCategory.IO;
        var severity = ErrorSeverity.Error;

        // Act
        var error = new PdfError(errorCode, category, severity);

        // Assert
        error.ErrorCode.Should().Be(errorCode);
        error.Category.Should().Be(category);
        error.Severity.Should().Be(severity);
        error.Metadata.Should().ContainKey("ErrorCode");
        error.Metadata.Should().ContainKey("Category");
        error.Metadata.Should().ContainKey("Severity");
    }

    [Fact]
    public void WithContext_AddsContextMetadata()
    {
        // Arrange
        var error = new PdfError("TEST", ErrorCategory.IO, ErrorSeverity.Error);

        // Act
        var result = error.WithContext("FilePath", "/path/to/file.pdf");

        // Assert
        result.Context.Should().ContainKey("FilePath");
        result.Context["FilePath"].Should().Be("/path/to/file.pdf");
    }
}
```

### ViewModel Unit Test Example

```csharp
public class MainViewModelTests
{
    [Fact]
    public void Title_InitializesToDefaultValue()
    {
        // Arrange
        var logger = Substitute.For<ILogger<MainViewModel>>();

        // Act
        var vm = new MainViewModel(logger);

        // Assert
        vm.Title.Should().Be("FluentPDF");
    }

    [Fact]
    public void LoadDocumentCommand_SetsIsLoadingDuringExecution()
    {
        // Arrange
        var logger = Substitute.For<ILogger<MainViewModel>>();
        var vm = new MainViewModel(logger);

        // Act & Assert
        vm.IsLoading.Should().BeFalse();

        // Note: Testing async command requires more complex setup
        // This is a simplified example
    }

    [Fact]
    public void SaveCommand_WhenLoading_CannotExecute()
    {
        // Arrange
        var logger = Substitute.For<ILogger<MainViewModel>>();
        var vm = new MainViewModel(logger);

        // Act
        vm.IsLoading = true;

        // Assert
        vm.SaveCommand.CanExecute(null).Should().BeFalse();
    }
}
```

### Using FluentAssertions

FluentAssertions provides readable test assertions:

```csharp
// Basic assertions
result.Should().Be(expected);
result.Should().NotBeNull();
result.Should().BeOfType<PdfDocument>();

// Collection assertions
list.Should().HaveCount(5);
list.Should().Contain(item);
list.Should().BeEmpty();

// String assertions
str.Should().StartWith("Pdf");
str.Should().Contain("Error");
str.Should().BeNullOrWhiteSpace();

// Exception assertions
Action act = () => method();
act.Should().Throw<InvalidOperationException>()
   .WithMessage("*not found*");

// Result pattern assertions
result.IsSuccess.Should().BeTrue();
result.IsFailed.Should().BeFalse();
result.Value.Should().NotBeNull();
```

### Using Mocks (NSubstitute)

```csharp
// Create mock
var mockLogger = Substitute.For<ILogger<MainViewModel>>();

// Setup behavior
mockLogger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);

// Verify calls
mockLogger.Received(1).Log(
    LogLevel.Information,
    Arg.Any<EventId>(),
    Arg.Any<object>(),
    Arg.Any<Exception>(),
    Arg.Any<Func<object, Exception, string>>());
```

### Using AutoFixture for Test Data

```csharp
// Create fixture
var fixture = new Fixture();

// Generate test data
var randomString = fixture.Create<string>();
var randomInt = fixture.Create<int>();
var documentList = fixture.CreateMany<PdfDocument>(10);

// Customize generation
fixture.Customize<PdfDocument>(c => c
    .With(d => d.PageCount, 100)
    .Without(d => d.InternalHandle));
```

## UI Tests

### FlaUI Automation Example

```csharp
public class MainWindowTests : UITestBase
{
    [Fact]
    public void MainWindow_OpensSuccessfully()
    {
        // Arrange & Act
        using var app = LaunchApp();
        using var automation = new UIA3Automation();
        var window = app.GetMainWindow(automation);

        // Assert
        window.Should().NotBeNull();
        window.Title.Should().Contain("FluentPDF");
    }

    [Fact]
    public void LoadButton_Click_OpensFileDialog()
    {
        // Arrange
        using var app = LaunchApp();
        using var automation = new UIA3Automation();
        var window = app.GetMainWindow(automation);

        // Act
        var loadButton = window.FindFirstDescendant(cf => cf.ByAutomationId("LoadButton"));
        loadButton.Click();

        // Assert
        var fileDialog = window.ModalWindows.FirstOrDefault();
        fileDialog.Should().NotBeNull();
    }
}
```

### Visual Regression Testing (Future)

Win2D headless rendering with SSIM comparison:

```csharp
[Fact]
public async Task PdfPage_RenderedImage_MatchesBaseline()
{
    // Arrange
    var page = await LoadTestPage();

    // Act
    var bitmap = await RenderPageHeadless(page);

    // Assert
    await Verifier.Verify(bitmap)
        .UseDirectory("Screenshots")
        .UseExtension("png");

    // Uses Verify.Xunit to compare against baseline
    // SSIM > 0.95 = pass
}
```

## Coverage Requirements

### Minimum Coverage Thresholds

- **Overall**: 80% line coverage minimum
- **Critical Paths**: 90% line coverage (error handling, rendering, file operations)
- **ViewModels**: 85% line coverage
- **Services**: 80% line coverage
- **Error Handling**: 95% line coverage

### Measuring Coverage

```bash
# Generate coverage report
dotnet coverage collect "dotnet test FluentPDF.sln" -f xml -o coverage.xml

# Generate HTML report
reportgenerator \
  -reports:coverage.xml \
  -targetdir:coverage-report \
  -reporttypes:Html

# Open report
start coverage-report/index.html  # Windows
```

### Coverage in CI/CD

GitHub Actions automatically:
1. Collects coverage during test run
2. Uploads coverage to Codecov (future)
3. Comments coverage % on PRs
4. Fails build if coverage drops below threshold

## CI/CD Integration

### Test Workflow (.github/workflows/test.yml)

```yaml
name: Test

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-2022
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x

      - name: Run Architecture Tests
        run: dotnet test tests/FluentPDF.Architecture.Tests --logger "trx;LogFileName=architecture.trx"

      - name: Run Unit Tests
        run: dotnet test tests/FluentPDF.Core.Tests --logger "trx;LogFileName=core.trx"

      - name: Upload Test Results
        uses: actions/upload-artifact@v4
        with:
          name: test-results
          path: "**/*.trx"

      - name: Publish Test Results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Test Results
          path: "**/*.trx"
          reporter: dotnet-trx
```

### Quality Analysis (Future)

AI-powered test failure analysis:
- Parse TRX files for failure patterns
- Correlate failures with recent code changes
- Generate natural language failure summaries
- Suggest fixes based on error messages

## Best Practices

### DO

✅ Write tests before or alongside code (TDD/BDD)
✅ Keep tests simple and focused (one assertion per test when possible)
✅ Use descriptive test names that explain the scenario
✅ Mock external dependencies (file system, network, etc.)
✅ Test both success and failure paths
✅ Use FluentAssertions for readable assertions
✅ Keep test setup minimal (use TestBase classes)
✅ Run tests frequently during development

### DON'T

❌ Test framework internals (e.g., WinUI framework behavior)
❌ Write tests that depend on external services (use mocks)
❌ Use Thread.Sleep or arbitrary delays (use async properly)
❌ Ignore flaky tests (fix them or remove them)
❌ Write overly complex test setups (extract to helper methods)
❌ Test private methods directly (test through public API)
❌ Commit code without running tests locally

## Troubleshooting Tests

### Tests Fail on CI but Pass Locally

**Cause**: Environment differences (paths, Windows version, timing)

**Solutions**:
- Use `Environment.GetFolderPath()` instead of hardcoded paths
- Avoid `Thread.Sleep()`, use proper async/await
- Check for Windows version-specific APIs
- Use `[Fact(Skip = "Requires Windows 11")]` for version-specific tests

### Flaky Tests

**Cause**: Race conditions, timing issues, shared state

**Solutions**:
- Use `await Task.Delay()` with reasonable timeouts
- Ensure test isolation (no shared static state)
- Use `Task.WaitAll()` instead of sequential awaits
- Add retry logic for integration tests (use Polly)

### Tests Run Slowly

**Cause**: Too many integration/UI tests, heavy setup

**Solutions**:
- Move tests down the pyramid (more unit, fewer UI)
- Use `[Collection]` to run slow tests in parallel
- Extract expensive setup to `IClassFixture<T>`
- Use in-memory databases/mocks instead of real services

### Coverage Too Low

**Cause**: Missing tests for edge cases, error paths

**Solutions**:
- Review coverage report to find untested code
- Add tests for error handling paths
- Test boundary conditions (null, empty, max values)
- Use mutation testing (Stryker.NET) to find weak tests

## References

- [Architecture Documentation](./ARCHITECTURE.md)
- [xUnit Documentation](https://xunit.net/)
- [FluentAssertions](https://fluentassertions.com/)
- [NSubstitute](https://nsubstitute.github.io/)
- [ArchUnitNET](https://archunitnet.readthedocs.io/)
- [FlaUI](https://github.com/FlaUI/FlaUI)
- [Verify.Xunit](https://github.com/VerifyTests/Verify)
