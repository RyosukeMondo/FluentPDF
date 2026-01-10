using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentPDF.App.Services;
using FluentPDF.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace FluentPDF.App.Tests.Services;

/// <summary>
/// Tests for JumpListService demonstrating Windows Jump List integration.
/// Note: These tests verify error handling and basic logic.
/// Full Jump List integration requires running on Windows with taskbar.
/// </summary>
public sealed class JumpListServiceTests
{
    private readonly Mock<ILogger<JumpListService>> _loggerMock;

    public JumpListServiceTests()
    {
        _loggerMock = new Mock<ILogger<JumpListService>>();
    }

    [Fact]
    public void Constructor_ShouldThrowException_WhenLoggerIsNull()
    {
        // Act
        Action act = () => new JumpListService(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task UpdateJumpListAsync_ShouldThrowException_WhenRecentFilesIsNull()
    {
        // Arrange
        var service = new JumpListService(_loggerMock.Object);

        // Act
        Func<Task> act = async () => await service.UpdateJumpListAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("recentFiles");
    }

    [Fact]
    public async Task UpdateJumpListAsync_ShouldNotThrow_WhenRecentFilesIsEmpty()
    {
        // Arrange
        var service = new JumpListService(_loggerMock.Object);
        var emptyList = new List<RecentFileEntry>();

        // Act
        Func<Task> act = async () => await service.UpdateJumpListAsync(emptyList);

        // Assert
        await act.Should().NotThrowAsync("service should handle empty list gracefully");
    }

    [Fact]
    public async Task UpdateJumpListAsync_ShouldNotThrow_WhenCalledWithValidEntries()
    {
        // Arrange
        var service = new JumpListService(_loggerMock.Object);
        var recentFiles = new List<RecentFileEntry>
        {
            new RecentFileEntry
            {
                FilePath = @"C:\Test\file1.pdf",
                LastAccessed = DateTime.UtcNow
            },
            new RecentFileEntry
            {
                FilePath = @"C:\Test\file2.pdf",
                LastAccessed = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        // Act
        Func<Task> act = async () => await service.UpdateJumpListAsync(recentFiles);

        // Assert
        await act.Should().NotThrowAsync("service should handle valid entries");
    }

    [Fact]
    public async Task UpdateJumpListAsync_ShouldHandleMaxItems()
    {
        // Arrange
        var service = new JumpListService(_loggerMock.Object);
        var recentFiles = Enumerable.Range(1, 15)
            .Select(i => new RecentFileEntry
            {
                FilePath = $@"C:\Test\file{i}.pdf",
                LastAccessed = DateTime.UtcNow.AddMinutes(-i)
            })
            .ToList();

        // Act
        Func<Task> act = async () => await service.UpdateJumpListAsync(recentFiles);

        // Assert
        await act.Should().NotThrowAsync("service should handle more than max items");
    }

    [Fact]
    public async Task UpdateJumpListAsync_ShouldLogInformation()
    {
        // Arrange
        var service = new JumpListService(_loggerMock.Object);
        var recentFiles = new List<RecentFileEntry>
        {
            new RecentFileEntry
            {
                FilePath = @"C:\Test\file.pdf",
                LastAccessed = DateTime.UtcNow
            }
        };

        // Act
        await service.UpdateJumpListAsync(recentFiles);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Updating Jump List")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ClearJumpListAsync_ShouldNotThrow()
    {
        // Arrange
        var service = new JumpListService(_loggerMock.Object);

        // Act
        Func<Task> act = async () => await service.ClearJumpListAsync();

        // Assert
        await act.Should().NotThrowAsync("service should handle clear operation");
    }

    [Fact]
    public async Task ClearJumpListAsync_ShouldLogInformation()
    {
        // Arrange
        var service = new JumpListService(_loggerMock.Object);

        // Act
        await service.ClearJumpListAsync();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Clearing Jump List")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task UpdateJumpListAsync_ShouldHandleSpecialCharactersInFileName()
    {
        // Arrange
        var service = new JumpListService(_loggerMock.Object);
        var recentFiles = new List<RecentFileEntry>
        {
            new RecentFileEntry
            {
                FilePath = @"C:\Test\file with spaces & special-chars.pdf",
                LastAccessed = DateTime.UtcNow
            }
        };

        // Act
        Func<Task> act = async () => await service.UpdateJumpListAsync(recentFiles);

        // Assert
        await act.Should().NotThrowAsync("service should handle special characters");
    }

    [Fact]
    public async Task UpdateJumpListAsync_ShouldHandleLongFilePaths()
    {
        // Arrange
        var service = new JumpListService(_loggerMock.Object);
        var longPath = @"C:\Test\" + new string('a', 200) + ".pdf";
        var recentFiles = new List<RecentFileEntry>
        {
            new RecentFileEntry
            {
                FilePath = longPath,
                LastAccessed = DateTime.UtcNow
            }
        };

        // Act
        Func<Task> act = async () => await service.UpdateJumpListAsync(recentFiles);

        // Assert
        await act.Should().NotThrowAsync("service should handle long paths");
    }

    [Fact]
    public async Task UpdateJumpListAsync_ThenClear_ShouldNotThrow()
    {
        // Arrange
        var service = new JumpListService(_loggerMock.Object);
        var recentFiles = new List<RecentFileEntry>
        {
            new RecentFileEntry
            {
                FilePath = @"C:\Test\file.pdf",
                LastAccessed = DateTime.UtcNow
            }
        };

        // Act
        await service.UpdateJumpListAsync(recentFiles);
        Func<Task> act = async () => await service.ClearJumpListAsync();

        // Assert
        await act.Should().NotThrowAsync("service should handle update then clear");
    }

    [Fact]
    public async Task ClearJumpListAsync_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var service = new JumpListService(_loggerMock.Object);

        // Act
        await service.ClearJumpListAsync();
        Func<Task> act = async () => await service.ClearJumpListAsync();

        // Assert
        await act.Should().NotThrowAsync("service should handle multiple clear calls");
    }

    [Fact]
    public async Task UpdateJumpListAsync_WhenCalledMultipleTimes_ShouldNotThrow()
    {
        // Arrange
        var service = new JumpListService(_loggerMock.Object);
        var recentFiles1 = new List<RecentFileEntry>
        {
            new RecentFileEntry
            {
                FilePath = @"C:\Test\file1.pdf",
                LastAccessed = DateTime.UtcNow
            }
        };
        var recentFiles2 = new List<RecentFileEntry>
        {
            new RecentFileEntry
            {
                FilePath = @"C:\Test\file2.pdf",
                LastAccessed = DateTime.UtcNow
            }
        };

        // Act
        await service.UpdateJumpListAsync(recentFiles1);
        Func<Task> act = async () => await service.UpdateJumpListAsync(recentFiles2);

        // Assert
        await act.Should().NotThrowAsync("service should handle multiple update calls");
    }

    [Fact]
    public async Task UpdateJumpListAsync_ShouldUseDisplayNameFromEntry()
    {
        // Arrange
        var service = new JumpListService(_loggerMock.Object);
        var filePath = @"C:\Test\Folder\document.pdf";
        var recentFiles = new List<RecentFileEntry>
        {
            new RecentFileEntry
            {
                FilePath = filePath,
                LastAccessed = DateTime.UtcNow
            }
        };

        // Act
        Func<Task> act = async () => await service.UpdateJumpListAsync(recentFiles);

        // Assert
        await act.Should().NotThrowAsync();
        recentFiles[0].DisplayName.Should().Be("document.pdf", "DisplayName should be filename only");
    }
}
