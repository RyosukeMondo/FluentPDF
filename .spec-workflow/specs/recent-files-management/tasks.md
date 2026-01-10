# Tasks Document

## Implementation Tasks

- [x] 1. Create RecentFileEntry model and IRecentFilesService interface
  - Files: `src/FluentPDF.Core/Models/RecentFileEntry.cs`, `src/FluentPDF.Core/Services/IRecentFilesService.cs`
  - Requirements: 1.1-1.8
  - Instructions: Define simple model with FilePath, LastAccessed. Define service interface with Get, Add, Remove, Clear methods.

- [x] 2. Implement RecentFilesService with persistence
  - Files: `src/FluentPDF.App/Services/RecentFilesService.cs`, `tests/FluentPDF.App.Tests/Services/RecentFilesServiceTests.cs`
  - Requirements: 1.1-1.8
  - Instructions: Implement service using ApplicationData.LocalSettings for JSON storage. Max 10 items, MRU ordering. Test persistence, validation, edge cases.

- [x] 3. Implement JumpListService for Windows taskbar integration
  - Files: `src/FluentPDF.App/Services/JumpListService.cs`, `tests/FluentPDF.App.Tests/Services/JumpListServiceTests.cs`
  - Requirements: 3.1-3.7
  - Instructions: Use Windows.UI.StartScreen.JumpList API. Update jump list with recent files. Handle errors gracefully.

- [x] 4. Create TabViewModel for per-tab state management
  - Files: `src/FluentPDF.App/ViewModels/TabViewModel.cs`, `tests/FluentPDF.App.Tests/ViewModels/TabViewModelTests.cs`
  - Requirements: 4.1-4.10, 5.1-5.6
  - Instructions: ViewModel wrapping PdfViewerViewModel with tab-specific state (file path, name, active state). Test state preservation.

- [x] 5. Extend MainViewModel with tab management
  - Files: `src/FluentPDF.App/ViewModels/MainViewModel.cs`, `tests/FluentPDF.App.Tests/ViewModels/MainViewModelTests.cs`
  - Requirements: 4.1-4.10, 6.1-6.7
  - Instructions: Add ObservableCollection<TabViewModel>, commands for opening files in tabs, closing tabs, recent files integration. Test tab lifecycle.

- [x] 6. Update MainWindow with TabView UI
  - Files: `src/FluentPDF.App/Views/MainWindow.xaml`, `src/FluentPDF.App/Views/MainWindow.xaml.cs`
  - Requirements: 4.1-4.10, 6.1-6.7
  - Instructions: Replace content with TabView bound to MainViewModel.Tabs. Add keyboard shortcuts (Ctrl+Tab, Ctrl+W). Test tab switching, closing.

- [x] 7. Add Recent Files menu and Jump List integration
  - Files: `src/FluentPDF.App/Views/MainWindow.xaml` (modify menu)
  - Requirements: 2.1-2.8, 3.1-3.7
  - Instructions: Add File menu with Recent Files submenu (dynamic items). Add Clear Recent Files button. Integrate Jump List updates.

- [x] 8. Register services in DI container
  - Files: `src/FluentPDF.App/App.xaml.cs`
  - Requirements: DI integration
  - Instructions: Register IRecentFilesService, JumpListService, MainViewModel. Handle app launch arguments from Jump List.

- [ ] 9. Integration testing with multiple tabs and persistence
  - Files: `tests/FluentPDF.App.Tests/Integration/TabManagementIntegrationTests.cs`
  - Requirements: All
  - Instructions: Test opening multiple files in tabs, switching, closing. Test recent files persistence. Test Jump List integration.

- [ ] 10. Final testing and documentation
  - Files: `docs/ARCHITECTURE.md`, `README.md`
  - Requirements: All
  - Instructions: E2E testing of tab management and recent files. Update docs with multi-tab feature.

## Summary

Implements recent files and multi-tab support:
- MRU recent files tracking with persistence
- Windows Jump List integration
- TabView-based multi-document interface
- Per-tab state management
- Keyboard shortcuts for tab navigation
