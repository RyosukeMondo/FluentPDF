# FluentPDF E2E Tests

End-to-end UI automation tests for FluentPDF using FlaUI.

## Requirements

- Windows 10/11 (WinUI 3 requirement)
- .NET 8.0 Windows SDK
- FlaUI 4.0 (UI automation library)
- xUnit test framework

## Building

```bash
# Windows only - requires x64 platform
dotnet build -p:Platform=x64
```

## Running Tests

```bash
dotnet test -p:Platform=x64
```

## Test Infrastructure

- **FlaUI.UIA3**: UI Automation provider for WinUI 3 applications
- **FlaUI.Core**: Core automation framework
- **FluentAssertions**: Assertion library for readable test code
- **xUnit**: Test framework

## Test Structure

Tests are organized by feature:
- `Tests/` - Test classes organized by feature
- `Fixtures/` - Shared test fixtures and utilities
- `TestData/` - Sample PDF files and test assets

## Notes

- Tests require the FluentPDF.App to be built in Release or Debug mode
- Tests will launch the app executable and automate UI interactions
- Some tests may require specific test PDF files in TestData folder
