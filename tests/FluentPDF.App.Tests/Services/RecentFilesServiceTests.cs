using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using FluentPDF.App.Services;
using FluentPDF.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Windows.Storage;

namespace FluentPDF.App.Tests.Services;

/// <summary>
/// Tests for RecentFilesService demonstrating persistence, validation, and MRU ordering.
/// Tests use real ApplicationData.LocalSettings for integration testing.
/// </summary>
public sealed class RecentFilesServiceTests : IDisposable
{
    private const string StorageKey = "RecentFiles";
    private readonly Mock<ILogger<RecentFilesService>> _loggerMock;
    private readonly ApplicationDataContainer _settings;
    private readonly List<string> _testFiles;

    public RecentFilesServiceTests()
    {
        _loggerMock = new Mock<ILogger<RecentFilesService>>();
        _settings = ApplicationData.Current.LocalSettings;
        _testFiles = new List<string>();

        // Clean up storage before each test
        _settings.Values.Remove(StorageKey);
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
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Act
        Action act = () => new RecentFilesService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldInitializeWithEmptyList_WhenNoStoredData()
    {
        // Act
        using var service = new RecentFilesService(_loggerMock.Object);

        // Assert
        service.GetRecentFiles().Should().BeEmpty();
    }

    [Fact]
    public void GetRecentFiles_ShouldReturnEmptyList_WhenNoFilesAdded()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);

        // Act
        var recentFiles = service.GetRecentFiles();

        // Assert
        recentFiles.Should().BeEmpty();
    }

    [Fact]
    public void AddRecentFile_ShouldThrowException_WhenFilePathIsNull()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);

        // Act
        Action act = () => service.AddRecentFile(null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("filePath")
            .WithMessage("File path cannot be null, empty, or whitespace.*");
    }

    [Fact]
    public void AddRecentFile_ShouldThrowException_WhenFilePathIsEmpty()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);

        // Act
        Action act = () => service.AddRecentFile(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("filePath");
    }

    [Fact]
    public void AddRecentFile_ShouldThrowException_WhenFilePathIsWhitespace()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);

        // Act
        Action act = () => service.AddRecentFile("   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("filePath");
    }

    [Fact]
    public void AddRecentFile_ShouldAddFileToList()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);
        var filePath = CreateTestFile();

        // Act
        service.AddRecentFile(filePath);

        // Assert
        var recentFiles = service.GetRecentFiles();
        recentFiles.Should().HaveCount(1);
        recentFiles[0].FilePath.Should().Be(filePath);
        recentFiles[0].LastAccessed.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AddRecentFile_ShouldMaintainMruOrdering()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);
        var file1 = CreateTestFile();
        var file2 = CreateTestFile();
        var file3 = CreateTestFile();

        // Act - Add files in order
        service.AddRecentFile(file1);
        System.Threading.Thread.Sleep(100); // Ensure different timestamps
        service.AddRecentFile(file2);
        System.Threading.Thread.Sleep(100);
        service.AddRecentFile(file3);

        // Assert - Most recent should be first
        var recentFiles = service.GetRecentFiles();
        recentFiles.Should().HaveCount(3);
        recentFiles[0].FilePath.Should().Be(file3, "most recently added should be first");
        recentFiles[1].FilePath.Should().Be(file2);
        recentFiles[2].FilePath.Should().Be(file1, "oldest should be last");
    }

    [Fact]
    public void AddRecentFile_ShouldUpdateExistingEntry_WhenFileAlreadyExists()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);
        var file1 = CreateTestFile();
        var file2 = CreateTestFile();

        service.AddRecentFile(file1);
        System.Threading.Thread.Sleep(100);
        service.AddRecentFile(file2);
        System.Threading.Thread.Sleep(100);

        // Act - Re-add file1, should move to top
        service.AddRecentFile(file1);

        // Assert
        var recentFiles = service.GetRecentFiles();
        recentFiles.Should().HaveCount(2, "should not duplicate entries");
        recentFiles[0].FilePath.Should().Be(file1, "re-added file should move to top");
        recentFiles[1].FilePath.Should().Be(file2);
    }

    [Fact]
    public void AddRecentFile_ShouldLimitToMaxItems()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);
        var files = Enumerable.Range(1, 15).Select(_ => CreateTestFile()).ToList();

        // Act - Add 15 files (max is 10)
        foreach (var file in files)
        {
            service.AddRecentFile(file);
        }

        // Assert
        var recentFiles = service.GetRecentFiles();
        recentFiles.Should().HaveCount(10, "should not exceed max items");

        // Verify most recent 10 are kept
        for (int i = 0; i < 10; i++)
        {
            recentFiles[i].FilePath.Should().Be(files[14 - i], "most recent files should be kept");
        }
    }

    [Fact]
    public void AddRecentFile_ShouldBeCaseInsensitive()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);
        var filePath = CreateTestFile();
        var upperPath = filePath.ToUpperInvariant();

        service.AddRecentFile(filePath);
        System.Threading.Thread.Sleep(100);

        // Act - Add same file with different casing
        service.AddRecentFile(upperPath);

        // Assert
        var recentFiles = service.GetRecentFiles();
        recentFiles.Should().HaveCount(1, "should treat paths as case-insensitive");
    }

    [Fact]
    public void RemoveRecentFile_ShouldRemoveFile()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);
        var file1 = CreateTestFile();
        var file2 = CreateTestFile();

        service.AddRecentFile(file1);
        service.AddRecentFile(file2);

        // Act
        service.RemoveRecentFile(file1);

        // Assert
        var recentFiles = service.GetRecentFiles();
        recentFiles.Should().HaveCount(1);
        recentFiles[0].FilePath.Should().Be(file2);
    }

    [Fact]
    public void RemoveRecentFile_ShouldBeCaseInsensitive()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);
        var filePath = CreateTestFile();

        service.AddRecentFile(filePath);

        // Act - Remove with different casing
        service.RemoveRecentFile(filePath.ToUpperInvariant());

        // Assert
        service.GetRecentFiles().Should().BeEmpty();
    }

    [Fact]
    public void RemoveRecentFile_ShouldNotThrow_WhenFileNotInList()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);
        var file = CreateTestFile();

        // Act
        Action act = () => service.RemoveRecentFile(file);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RemoveRecentFile_ShouldNotThrow_WhenFilePathIsNull()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);

        // Act
        Action act = () => service.RemoveRecentFile(null!);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void RemoveRecentFile_ShouldNotThrow_WhenFilePathIsEmpty()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);

        // Act
        Action act = () => service.RemoveRecentFile(string.Empty);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ClearRecentFiles_ShouldRemoveAllFiles()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);
        var files = Enumerable.Range(1, 5).Select(_ => CreateTestFile()).ToList();

        foreach (var file in files)
        {
            service.AddRecentFile(file);
        }

        // Act
        service.ClearRecentFiles();

        // Assert
        service.GetRecentFiles().Should().BeEmpty();
    }

    [Fact]
    public void ClearRecentFiles_ShouldNotThrow_WhenListIsEmpty()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);

        // Act
        Action act = () => service.ClearRecentFiles();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Service_ShouldPersistToStorage_WhenFilesAdded()
    {
        // Arrange
        var file1 = CreateTestFile();
        var file2 = CreateTestFile();

        // Act - Add files with first service instance
        using (var service = new RecentFilesService(_loggerMock.Object))
        {
            service.AddRecentFile(file1);
            service.AddRecentFile(file2);
        }

        // Assert - Create new service instance and verify persistence
        using (var service = new RecentFilesService(_loggerMock.Object))
        {
            var recentFiles = service.GetRecentFiles();
            recentFiles.Should().HaveCount(2);
            recentFiles[0].FilePath.Should().Be(file2);
            recentFiles[1].FilePath.Should().Be(file1);
        }
    }

    [Fact]
    public void Service_ShouldPersistToStorage_WhenFilesCleared()
    {
        // Arrange
        var file = CreateTestFile();

        using (var service = new RecentFilesService(_loggerMock.Object))
        {
            service.AddRecentFile(file);
        }

        // Act - Clear with new service instance
        using (var service = new RecentFilesService(_loggerMock.Object))
        {
            service.ClearRecentFiles();
        }

        // Assert - Verify persistence with third service instance
        using (var service = new RecentFilesService(_loggerMock.Object))
        {
            service.GetRecentFiles().Should().BeEmpty();
        }
    }

    [Fact]
    public void Service_ShouldRemoveNonExistentFiles_OnLoad()
    {
        // Arrange - Add files, then delete one physically
        var existingFile = CreateTestFile();
        var deletedFile = CreateTestFile();

        using (var service = new RecentFilesService(_loggerMock.Object))
        {
            service.AddRecentFile(existingFile);
            service.AddRecentFile(deletedFile);
        }

        // Delete one file from disk
        File.Delete(deletedFile);

        // Act - Create new service instance
        using var newService = new RecentFilesService(_loggerMock.Object);

        // Assert - Deleted file should be filtered out
        var recentFiles = newService.GetRecentFiles();
        recentFiles.Should().HaveCount(1);
        recentFiles[0].FilePath.Should().Be(existingFile);
    }

    [Fact]
    public void Service_ShouldHandleCorruptedStorage_Gracefully()
    {
        // Arrange - Manually corrupt storage
        _settings.Values[StorageKey] = "{ invalid json }";

        // Act
        Action act = () =>
        {
            using var service = new RecentFilesService(_loggerMock.Object);
            service.GetRecentFiles();
        };

        // Assert
        act.Should().NotThrow("should handle corrupted storage gracefully");
    }

    [Fact]
    public void GetRecentFiles_ShouldReturnReadOnlyList()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);
        var file = CreateTestFile();
        service.AddRecentFile(file);

        // Act
        var recentFiles = service.GetRecentFiles();

        // Assert
        recentFiles.Should().BeAssignableTo<IReadOnlyList<RecentFileEntry>>();
    }

    [Fact]
    public void DisplayName_ShouldReturnFileName()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);
        var filePath = CreateTestFile();
        var expectedName = Path.GetFileName(filePath);

        service.AddRecentFile(filePath);

        // Act
        var entry = service.GetRecentFiles()[0];

        // Assert
        entry.DisplayName.Should().Be(expectedName);
    }

    [Fact]
    public void Service_ShouldHandleMultipleOperations_InSequence()
    {
        // Arrange
        using var service = new RecentFilesService(_loggerMock.Object);
        var files = Enumerable.Range(1, 5).Select(_ => CreateTestFile()).ToList();

        // Act - Perform various operations
        service.AddRecentFile(files[0]);
        service.AddRecentFile(files[1]);
        service.AddRecentFile(files[2]);
        service.RemoveRecentFile(files[1]);
        service.AddRecentFile(files[3]);
        service.AddRecentFile(files[0]); // Re-add first file
        service.AddRecentFile(files[4]);

        // Assert
        var recentFiles = service.GetRecentFiles();
        recentFiles.Should().HaveCount(4, "after all operations");
        recentFiles[0].FilePath.Should().Be(files[4], "last added");
        recentFiles[1].FilePath.Should().Be(files[0], "re-added");
        recentFiles[2].FilePath.Should().Be(files[3]);
        recentFiles[3].FilePath.Should().Be(files[2]);
        recentFiles.Should().NotContain(entry => entry.FilePath == files[1], "removed file should not be present");
    }

    /// <summary>
    /// Creates a temporary test file and tracks it for cleanup.
    /// </summary>
    private string CreateTestFile()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"FluentPDF_Test_{Guid.NewGuid()}.pdf");
        File.WriteAllText(tempPath, "test content");
        _testFiles.Add(tempPath);
        return tempPath;
    }
}
