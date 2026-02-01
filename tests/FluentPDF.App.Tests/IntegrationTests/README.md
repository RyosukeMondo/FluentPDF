# Integration Tests

This directory contains FlaUI-based integration tests for FluentPDF's UI automation scenarios.

## Theme Switching Tests

### Overview

`ThemeSwitchingTests.cs` provides comprehensive integration testing for the application's theme switching functionality. Tests verify that themes can be changed at runtime and persist across application restarts.

### Test Coverage

1. **ThemeCycle_ShouldTransitionThroughAllThemes**
   - Tests complete theme cycle: Light → Dark → System
   - Verifies each transition using AutomationPeer properties
   - Implements retry logic for flaky UI elements (max 3 retries, 1s delay)

2. **ThemeSwitch_ShouldApplyImmediatelyWithoutRestart**
   - Verifies theme changes apply instantly without requiring restart
   - Checks UI background color changes

3. **ThemeSelection_ShouldPersistAcrossRestarts**
   - Tests theme persistence across app restarts
   - Ensures settings are saved and reloaded correctly

### Architecture

#### Page Object Pattern

The tests use the **Page Object Pattern** to encapsulate UI element interaction:

```csharp
var settingsPage = new SettingsPageObject(mainWindow, Automation);
settingsPage.NavigateToSettings();
settingsPage.SelectTheme("Dark");
```

**Benefits:**
- Decouples test logic from UI structure
- Makes tests more maintainable when UI changes
- Provides reusable component abstractions

#### Retry Strategy

All UI interactions use a retry mechanism to handle flaky elements:

```csharp
ExecuteWithRetry(() => {
    settingsPage.SelectTheme("Dark");
    // Assertions...
}, "Theme transition");
```

**Configuration:**
- Max retries: 3
- Retry delay: 1000ms
- Handles transient UI automation failures

### AutomationPeer Verification

Tests verify theme changes via AutomationPeer properties:

```csharp
var radioButton = FindThemeRadioButton("Dark");
bool isSelected = radioButton?.IsSelected ?? false;
```

This ensures:
- Theme state is correctly reflected in accessibility properties
- Screen readers and assistive technologies work correctly
- Theme changes are properly observable

### Running the Tests

#### Prerequisites

1. Build FluentPDF.App with x64 platform:
   ```bash
   dotnet build src/FluentPDF.App -p:Platform=x64 -c Debug
   ```

2. Ensure the executable exists at:
   ```
   src/FluentPDF.App/bin/x64/Debug/net8.0-windows10.0.19041.0/win-x64/FluentPDF.App.exe
   ```

#### Run Tests

```bash
# Run all integration tests
dotnet test tests/FluentPDF.App.Tests -p:Platform=x64 --filter "Category=IntegrationTests"

# Run specific theme switching tests
dotnet test tests/FluentPDF.App.Tests -p:Platform=x64 --filter "FullyQualifiedName~ThemeSwitchingTests"
```

#### CI/CD Integration

Add to your CI pipeline:

```yaml
- name: Run Integration Tests
  run: |
    dotnet build src/FluentPDF.App -p:Platform=x64 -c Release
    dotnet test tests/FluentPDF.App.Tests -p:Platform=x64 --filter "Category=IntegrationTests"
```

### Test Output

Tests produce:
- **Console logs**: Detailed test execution trace
- **Screenshots**: Captured on test failure for debugging
- **Location**: `tests/FluentPDF.App.Tests/Screenshots/{date}/{testname}_FAILED.png`

### Troubleshooting

#### Test Failures

1. **Element Not Found**
   - Check AutomationId assignments in XAML
   - Verify navigation timing (increase wait delays)
   - Review screenshot output for UI state

2. **Timeout Exceptions**
   - Increase `waitTimeoutMs` in `LaunchApp()`
   - Check if app is building correctly
   - Verify PDFium native dependencies exist

3. **Flaky Tests**
   - Retry logic should handle most transients
   - Increase `RetryDelayMs` if system is slow
   - Check for race conditions in UI updates

#### Debugging Tips

```csharp
// Add breakpoint before assertion
_output.WriteLine($"Current theme: {settingsPage.IsDarkThemeActive()}");
Thread.Sleep(5000); // Pause to inspect UI manually
```

### Code Metrics

- **File**: ThemeSwitchingTests.cs
- **Lines**: 325 (within 500 line limit)
- **Methods**: 9 (all under 50 lines)
- **Test Methods**: 3
- **Helper Methods**: 6
- **Page Objects**: 1 (SettingsPageObject)

### Dependencies

- **FlaUI.Core**: 4.0.0
- **FlaUI.UIA3**: 4.0.0
- **FluentAssertions**: 6.x
- **xUnit**: 2.9.x

### Future Enhancements

1. Add tests for High Contrast mode
2. Test theme switching with open documents
3. Verify theme affects all pages (not just Settings)
4. Test system theme change detection
5. Add performance benchmarks for theme transitions

### Related Documentation

- [FlaUI GitHub](https://github.com/FlaUI/FlaUI)
- [Page Object Pattern](https://www.selenium.dev/documentation/test_practices/encouraged/page_object_models/)
- [UI Automation](https://docs.microsoft.com/en-us/windows/win32/winauto/entry-uiauto-win32)
