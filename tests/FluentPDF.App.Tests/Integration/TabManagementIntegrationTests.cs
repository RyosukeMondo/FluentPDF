using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentPDF.App.Services;
using FluentPDF.App.ViewModels;
using FluentPDF.Core.Models;
using FluentPDF.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Windows.Storage;
using Windows.UI.StartScreen;

namespace FluentPDF.App.Tests.Integration;

/// <summary>
/// Integration tests for tab management, recent files persistence, and Jump List integration.
/// These tests verify the complete workflow of opening files in tabs, managing recent files,
/// and updating the Windows Jump List.
/// </summary>
public sealed class TabManagementIntegrationTests : IDisposable
{
    private const string StorageKey = "RecentFiles";
    private readonly ApplicationDataContainer _settings;
    private readonly List<string> _testFiles;
    private readonly Mock<ILogger<MainViewModel>> _mainLoggerMock;
    private readonly Mock<ILogger<TabViewModel>> _tabLoggerMock;
    private readonly Mock<ILogger<PdfViewerViewModel>> _viewerLoggerMock;
    private readonly Mock<ILogger<RecentFilesService>> _recentFilesLoggerMock;
    private readonly Mock<ILogger<JumpListService>> _jumpListLoggerMock;

    public TabManagementIntegrationTests()
    {
        _settings = ApplicationData.Current.LocalSettings;
        _testFiles = new List<string>();
        _mainLoggerMock = new Mock<ILogger<MainViewModel>>();
        _tabLoggerMock = new Mock<ILogger<TabViewModel>>();
        _viewerLoggerMock = new Mock<ILogger<PdfViewerViewModel>>();
        _recentFilesLoggerMock = new Mock<ILogger<RecentFilesService>>();
        _jumpListLoggerMock = new Mock<ILogger<JumpListService>>();

        // Clean up storage before each test
        _settings.Values.Remove(StorageKey);

        // Clean up Jump List before each test
        CleanupJumpListAsync().Wait();
    }

    public void Dispose()
    {
        // Clean up storage after each test
        _settings.Values.Remove(StorageKey);

        // Clean up test files
        foreach (var filePath in _testFiles.Where(File.Exists))
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Clean up Jump List after each test
        CleanupJumpListAsync().Wait();
    }

    [Fact]
    public async Task OpenMultipleFiles_ShouldCreateMultipleTabs()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var recentFilesService = new RecentFilesService(_recentFilesLoggerMock.Object);
        var mainViewModel = new MainViewModel(
            _mainLoggerMock.Object,
            recentFilesService,
            serviceProvider);

        var file1 = CreateTestFile();
        var file2 = CreateTestFile();
        var file3 = CreateTestFile();

        // Act - Simulate opening files via reflection since OpenFileInTabAsync is private
        await OpenFileAsync(mainViewModel, file1);
        await OpenFileAsync(mainViewModel, file2);
        await OpenFileAsync(mainViewModel, file3);

        // Assert
        mainViewModel.Tabs.Should().HaveCount(3);
        mainViewModel.Tabs[0].FilePath.Should().Be(file1);
        mainViewModel.Tabs[1].FilePath.Should().Be(file2);
        mainViewModel.Tabs[2].FilePath.Should().Be(file3);
        mainViewModel.ActiveTab.Should().Be(mainViewModel.Tabs[2], "last opened file should be active");

        // Cleanup
        mainViewModel.Dispose();
    }

    [Fact]
    public async Task OpenSameFileTwice_ShouldActivateExistingTab()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var recentFilesService = new RecentFilesService(_recentFilesLoggerMock.Object);
        var mainViewModel = new MainViewModel(
            _mainLoggerMock.Object,
            recentFilesService,
            serviceProvider);

        var file = CreateTestFile();

        // Act - Open same file twice
        await OpenFileAsync(mainViewModel, file);
        var initialTabCount = mainViewModel.Tabs.Count;
        var firstTab = mainViewModel.ActiveTab;

        await OpenFileAsync(mainViewModel, file);

        // Assert
        mainViewModel.Tabs.Should().HaveCount(initialTabCount, "should not create duplicate tab");
        mainViewModel.ActiveTab.Should().Be(firstTab, "should activate existing tab");

        // Cleanup
        mainViewModel.Dispose();
    }

    [Fact]
    public async Task CloseActiveTab_ShouldActivateNextTab()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var recentFilesService = new RecentFilesService(_recentFilesLoggerMock.Object);
        var mainViewModel = new MainViewModel(
            _mainLoggerMock.Object,
            recentFilesService,
            serviceProvider);

        var file1 = CreateTestFile();
        var file2 = CreateTestFile();
        var file3 = CreateTestFile();

        await OpenFileAsync(mainViewModel, file1);
        await OpenFileAsync(mainViewModel, file2);
        await OpenFileAsync(mainViewModel, file3);

        var tab1 = mainViewModel.Tabs[0];
        var tab2 = mainViewModel.Tabs[1];
        var tab3 = mainViewModel.Tabs[2];

        // Activate tab2
        ActivateTab(mainViewModel, tab2);

        // Act - Close active tab (tab2)
        mainViewModel.CloseTabCommand.Execute(tab2);

        // Assert
        mainViewModel.Tabs.Should().HaveCount(2);
        mainViewModel.Tabs.Should().NotContain(tab2);
        mainViewModel.ActiveTab.Should().Be(tab3, "should activate next tab");

        // Cleanup
        mainViewModel.Dispose();
    }

    [Fact]
    public async Task CloseLastTab_ShouldLeaveNoActiveTab()
    {
        // Arrange
        var serviceProvider = CreateServiceProvider();
        var recentFilesService = new RecentFilesService(_recentFilesLoggerMock.Object);
        var mainViewModel = new MainViewModel(
            _mainLoggerMock.Object,
            recentFilesService,
            serviceProvider);

        var file = CreateTestFile();
        await OpenFileAsync(mainViewModel, file);

        var tab = mainViewModel.Tabs[0];

        // Act
        mainViewModel.CloseTabCommand.Execute(tab);

        // Assert
        mainViewModel.Tabs.Should().BeEmpty();
        mainViewModel.ActiveTab.Should().BeNull();

        // Cleanup
        mainViewModel.Dispose();
    }

    [Fact]
    public async Task OpeningFiles_ShouldPersistToRecentFiles()
    {
        // Arrange
        var file1 = CreateTestFile();
        var file2 = CreateTestFile();
        var file3 = CreateTestFile();

        // Act - Open files with first MainViewModel instance
        using (var serviceProvider = CreateServiceProvider())
        {
            var recentFilesService = new RecentFilesService(_recentFilesLoggerMock.Object);
            var mainViewModel = new MainViewModel(
                _mainLoggerMock.Object,
                recentFilesService,
                serviceProvider);

            await OpenFileAsync(mainViewModel, file1);
            await OpenFileAsync(mainViewModel, file2);
            await OpenFileAsync(mainViewModel, file3);

            mainViewModel.Dispose();
        }

        // Assert - Create new RecentFilesService instance and verify persistence
        using var newRecentFilesService = new RecentFilesService(_recentFilesLoggerMock.Object);
        var recentFiles = newRecentFilesService.GetRecentFiles();

        recentFiles.Should().HaveCount(3);
        recentFiles[0].FilePath.Should().Be(file3, "most recent file first");
        recentFiles[1].FilePath.Should().Be(file2);
        recentFiles[2].FilePath.Should().Be(file1);
    }

    [Fact]
    public async Task ReOpeningFile_ShouldUpdateRecentFilesOrder()
    {
        // Arrange
        var file1 = CreateTestFile();
        var file2 = CreateTestFile();
        var file3 = CreateTestFile();

        var serviceProvider = CreateServiceProvider();
        var recentFilesService = new RecentFilesService(_recentFilesLoggerMock.Object);
        var mainViewModel = new MainViewModel(
            _mainLoggerMock.Object,
            recentFilesService,
            serviceProvider);

        // Act - Open files, then re-open first file
        await OpenFileAsync(mainViewModel, file1);
        await OpenFileAsync(mainViewModel, file2);
        await OpenFileAsync(mainViewModel, file3);
        await Task.Delay(100); // Ensure different timestamp
        await OpenFileAsync(mainViewModel, file1); // Re-open file1

        // Assert
        var recentFiles = recentFilesService.GetRecentFiles();
        recentFiles.Should().HaveCount(3, "should not duplicate entries");
        recentFiles[0].FilePath.Should().Be(file1, "re-opened file should be most recent");
        recentFiles[1].FilePath.Should().Be(file3);
        recentFiles[2].FilePath.Should().Be(file2);

        // Cleanup
        mainViewModel.Dispose();
    }

    [Fact]
    public async Task JumpList_ShouldUpdateWithRecentFiles()
    {
        // Arrange
        var recentFilesService = new RecentFilesService(_recentFilesLoggerMock.Object);
        var jumpListService = new JumpListService(_jumpListLoggerMock.Object);

        var file1 = CreateTestFile();
        var file2 = CreateTestFile();
        var file3 = CreateTestFile();

        // Act - Add files to recent files and update Jump List
        recentFilesService.AddRecentFile(file1);
        recentFilesService.AddRecentFile(file2);
        recentFilesService.AddRecentFile(file3);

        var recentFiles = recentFilesService.GetRecentFiles();
        await jumpListService.UpdateJumpListAsync(recentFiles);

        // Assert - Verify Jump List was updated
        var jumpList = await JumpList.LoadCurrentAsync();
        jumpList.Items.Should().HaveCount(3);

        var jumpListPaths = jumpList.Items.Select(item => item.Arguments).ToList();
        jumpListPaths.Should().Contain(file1);
        jumpListPaths.Should().Contain(file2);
        jumpListPaths.Should().Contain(file3);

        jumpList.Items[0].Arguments.Should().Be(file3, "most recent file first");
        jumpList.Items[0].GroupName.Should().Be("Recent");
    }

    [Fact]
    public async Task JumpList_ShouldClearWhenRecentFilesCleared()
    {
        // Arrange
        var recentFilesService = new RecentFilesService(_recentFilesLoggerMock.Object);
        var jumpListService = new JumpListService(_jumpListLoggerMock.Object);

        var file1 = CreateTestFile();
        var file2 = CreateTestFile();

        recentFilesService.AddRecentFile(file1);
        recentFilesService.AddRecentFile(file2);
        await jumpListService.UpdateJumpListAsync(recentFilesService.GetRecentFiles());

        // Act - Clear recent files and update Jump List
        recentFilesService.ClearRecentFiles();
        await jumpListService.ClearJumpListAsync();

        // Assert
        var jumpList = await JumpList.LoadCurrentAsync();
        jumpList.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task JumpList_ShouldLimitToMaxItems()
    {
        // Arrange
        var recentFilesService = new RecentFilesService(_recentFilesLoggerMock.Object);
        var jumpListService = new JumpListService(_jumpListLoggerMock.Object);

        // Add 15 files (max is 10 for Jump List)
        var files = Enumerable.Range(1, 15).Select(_ => CreateTestFile()).ToList();
        foreach (var file in files)
        {
            recentFilesService.AddRecentFile(file);
        }

        // Act
        var recentFiles = recentFilesService.GetRecentFiles();
        await jumpListService.UpdateJumpListAsync(recentFiles);

        // Assert
        var jumpList = await JumpList.LoadCurrentAsync();
        jumpList.Items.Should().HaveCount(10, "Jump List should be limited to 10 items");

        // Verify most recent 10 are in Jump List
        for (int i = 0; i < 10; i++)
        {
            jumpList.Items[i].Arguments.Should().Be(files[14 - i], "most recent files should be in Jump List");
        }
    }

    [Fact]
    public async Task EndToEnd_OpenFilesCloseSomePersistAndRestore()
    {
        // Arrange
        var file1 = CreateTestFile();
        var file2 = CreateTestFile();
        var file3 = CreateTestFile();
        var file4 = CreateTestFile();

        // Act - Session 1: Open files, close some
        using (var serviceProvider = CreateServiceProvider())
        {
            var recentFilesService = new RecentFilesService(_recentFilesLoggerMock.Object);
            var jumpListService = new JumpListService(_jumpListLoggerMock.Object);
            var mainViewModel = new MainViewModel(
                _mainLoggerMock.Object,
                recentFilesService,
                serviceProvider);

            await OpenFileAsync(mainViewModel, file1);
            await OpenFileAsync(mainViewModel, file2);
            await OpenFileAsync(mainViewModel, file3);
            await OpenFileAsync(mainViewModel, file4);

            // Close file2
            var tab2 = mainViewModel.Tabs.First(t => t.FilePath == file2);
            mainViewModel.CloseTabCommand.Execute(tab2);

            mainViewModel.Tabs.Should().HaveCount(3);

            // Update Jump List
            await jumpListService.UpdateJumpListAsync(recentFilesService.GetRecentFiles());

            mainViewModel.Dispose();
        }

        // Assert - Session 2: Verify persistence
        using (var serviceProvider = CreateServiceProvider())
        {
            var newRecentFilesService = new RecentFilesService(_recentFilesLoggerMock.Object);
            var recentFiles = newRecentFilesService.GetRecentFiles();

            recentFiles.Should().HaveCount(4, "all opened files should be in recent files");
            recentFiles[0].FilePath.Should().Be(file4, "most recent opened file");

            // Verify Jump List persisted
            var jumpList = await JumpList.LoadCurrentAsync();
            jumpList.Items.Should().HaveCount(4);
        }
    }

    [Fact]
    public async Task OpenRecentFile_ShouldOpenInTab()
    {
        // Arrange
        var file = CreateTestFile();
        var serviceProvider = CreateServiceProvider();
        var recentFilesService = new RecentFilesService(_recentFilesLoggerMock.Object);
        var mainViewModel = new MainViewModel(
            _mainLoggerMock.Object,
            recentFilesService,
            serviceProvider);

        // Add file to recent files
        recentFilesService.AddRecentFile(file);

        // Act - Open recent file via command
        await mainViewModel.OpenRecentFileCommand.ExecuteAsync(file);

        // Assert
        mainViewModel.Tabs.Should().HaveCount(1);
        mainViewModel.Tabs[0].FilePath.Should().Be(file);
        mainViewModel.ActiveTab.Should().NotBeNull();

        // Cleanup
        mainViewModel.Dispose();
    }

    [Fact]
    public async Task OpenRecentFile_ShouldRemoveIfFileDeleted()
    {
        // Arrange
        var file = CreateTestFile();
        var serviceProvider = CreateServiceProvider();
        var recentFilesService = new RecentFilesService(_recentFilesLoggerMock.Object);
        var mainViewModel = new MainViewModel(
            _mainLoggerMock.Object,
            recentFilesService,
            serviceProvider);

        recentFilesService.AddRecentFile(file);

        // Delete the file
        File.Delete(file);

        // Act - Try to open deleted file
        await mainViewModel.OpenRecentFileCommand.ExecuteAsync(file);

        // Assert
        mainViewModel.Tabs.Should().BeEmpty("should not open deleted file");
        recentFilesService.GetRecentFiles().Should().NotContain(f => f.FilePath == file,
            "deleted file should be removed from recent files");

        // Cleanup
        mainViewModel.Dispose();
    }

    [Fact]
    public async Task ClearRecentFiles_ShouldClearAllFiles()
    {
        // Arrange
        var file1 = CreateTestFile();
        var file2 = CreateTestFile();
        var serviceProvider = CreateServiceProvider();
        var recentFilesService = new RecentFilesService(_recentFilesLoggerMock.Object);
        var mainViewModel = new MainViewModel(
            _mainLoggerMock.Object,
            recentFilesService,
            serviceProvider);

        await OpenFileAsync(mainViewModel, file1);
        await OpenFileAsync(mainViewModel, file2);

        recentFilesService.GetRecentFiles().Should().HaveCount(2);

        // Act
        mainViewModel.ClearRecentFilesCommand.Execute(null);

        // Assert
        recentFilesService.GetRecentFiles().Should().BeEmpty();

        // Cleanup
        mainViewModel.Dispose();
    }

    /// <summary>
    /// Creates a temporary test file and tracks it for cleanup.
    /// </summary>
    private string CreateTestFile()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"FluentPDF_IntegrationTest_{Guid.NewGuid()}.pdf");
        File.WriteAllText(tempPath, "%PDF-1.4\ntest content");
        _testFiles.Add(tempPath);
        return tempPath;
    }

    /// <summary>
    /// Creates a service provider with mocked dependencies for testing.
    /// </summary>
    private ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        // Register mocked PdfViewerViewModel dependencies
        services.AddTransient<IPdfDocumentService>(_ => Mock.Of<IPdfDocumentService>());
        services.AddTransient<IPdfRenderingService>(_ => Mock.Of<IPdfRenderingService>());
        services.AddTransient<IDocumentEditingService>(_ => Mock.Of<IDocumentEditingService>());
        services.AddTransient<ITextSearchService>(_ => Mock.Of<ITextSearchService>());
        services.AddTransient<ITextExtractionService>(_ => Mock.Of<ITextExtractionService>());
        services.AddTransient<ILogger<PdfViewerViewModel>>(_ => _viewerLoggerMock.Object);
        services.AddTransient<ILogger<TabViewModel>>(_ => _tabLoggerMock.Object);

        // Register BookmarksViewModel and FormFieldViewModel with mocks
        services.AddTransient(_ => new Mock<BookmarksViewModel>(
            Mock.Of<ILogger<BookmarksViewModel>>()).Object);
        services.AddTransient(_ => new Mock<FormFieldViewModel>(
            Mock.Of<ILogger<FormFieldViewModel>>()).Object);

        // Register PdfViewerViewModel
        services.AddTransient<PdfViewerViewModel>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Opens a file using reflection to access the private OpenFileInTabAsync method.
    /// </summary>
    private async Task OpenFileAsync(MainViewModel viewModel, string filePath)
    {
        var method = typeof(MainViewModel).GetMethod(
            "OpenFileInTabAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var task = (Task)method!.Invoke(viewModel, new object[] { filePath })!;
        await task;
    }

    /// <summary>
    /// Activates a tab using reflection to access the private ActivateTab method.
    /// </summary>
    private void ActivateTab(MainViewModel viewModel, TabViewModel tab)
    {
        var method = typeof(MainViewModel).GetMethod(
            "ActivateTab",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        method!.Invoke(viewModel, new object[] { tab });
    }

    /// <summary>
    /// Cleans up the Jump List before/after tests.
    /// </summary>
    private async Task CleanupJumpListAsync()
    {
        try
        {
            var jumpList = await JumpList.LoadCurrentAsync();
            jumpList.Items.Clear();
            await jumpList.SaveAsync();
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
