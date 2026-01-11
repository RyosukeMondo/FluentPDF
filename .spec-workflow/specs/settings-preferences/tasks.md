# Tasks Document

## Implementation Tasks

- [x] 1. Create AppSettings model with enums
  - Files: `src/FluentPDF.Core/Models/AppSettings.cs`
  - Requirements: 1.1-1.7, 2.1-2.7
  - Instructions: Define strongly-typed settings model with ZoomLevel, ScrollMode, AppTheme enums. Include default values.

- [x] 2. Create ISettingsService interface
  - Files: `src/FluentPDF.Core/Services/ISettingsService.cs`
  - Requirements: 1.1-1.7
  - Instructions: Define service contract with Load, Save, Reset methods. Include SettingsChanged event.

- [ ] 3. Implement SettingsService with JSON persistence
  - Files: `src/FluentPDF.App/Services/SettingsService.cs`, `tests/FluentPDF.App.Tests/Services/SettingsServiceTests.cs`
  - Requirements: 1.1-1.7, 5.1-5.6
  - Instructions: Implement service using System.Text.Json for persistence in LocalFolder. Add validation, debouncing, error handling. Test persistence, corrupt file recovery.

- [ ] 4. Create SettingsViewModel
  - Files: `src/FluentPDF.App/ViewModels/SettingsViewModel.cs`, `tests/FluentPDF.App.Tests/ViewModels/SettingsViewModelTests.cs`
  - Requirements: 2.1-2.7, 3.1-3.7, 4.1-4.7
  - Instructions: ViewModel with observable properties for all settings. Wire property changes to save. Add ResetToDefaultsCommand. Test property change saves.

- [ ] 5. Create SettingsPage UI
  - Files: `src/FluentPDF.App/Views/SettingsPage.xaml`, `src/FluentPDF.App/Views/SettingsPage.xaml.cs`
  - Requirements: 4.1-4.7
  - Instructions: Build settings page with sections (Viewing, Appearance, Privacy). Use ComboBox for zoom/scroll, RadioButtons for theme, ToggleSwitch for telemetry.

- [ ] 6. Integrate theme switching
  - Files: `src/FluentPDF.App/App.xaml.cs`
  - Requirements: 2.5-2.7
  - Instructions: Listen to SettingsChanged event, apply theme to Application.RequestedTheme. Handle UseSystem theme.

- [ ] 7. Apply default settings to new documents
  - Files: `src/FluentPDF.App/ViewModels/PdfViewerViewModel.cs` (modify)
  - Requirements: 2.2, 2.4
  - Instructions: When opening document, set zoom level and scroll mode from settings.

- [ ] 8. Add Settings menu item and navigation
  - Files: `src/FluentPDF.App/Views/MainWindow.xaml`
  - Requirements: 4.1
  - Instructions: Add "Settings" menu item, navigate to SettingsPage on click.

- [ ] 9. Register SettingsService in DI
  - Files: `src/FluentPDF.App/App.xaml.cs`
  - Instructions: Register ISettingsService as singleton, load settings on startup.

- [ ] 10. Integration testing and documentation
  - Files: `tests/FluentPDF.App.Tests/Integration/SettingsIntegrationTests.cs`, `README.md`
  - Requirements: All
  - Instructions: Test settings persistence across app restarts. Test theme switching. Update docs.

## Summary

Implements comprehensive settings system:
- Strongly-typed settings model with validation
- JSON persistence in LocalFolder
- Settings page with grouped controls
- Theme switching (Light, Dark, System)
- Telemetry opt-in/out controls
- Debounced saves for efficiency
