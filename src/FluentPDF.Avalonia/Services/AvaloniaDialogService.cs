using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using FluentPDF.Core.Services;
using Microsoft.Extensions.Logging;

namespace FluentPDF.Avalonia.Services;

/// <summary>
/// Avalonia implementation of the Core IDialogService.
/// Provides file pickers and message dialogs using Avalonia's APIs.
/// </summary>
public sealed class AvaloniaDialogService : IDialogService
{
    private readonly ILogger<AvaloniaDialogService>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaDialogService"/> class.
    /// </summary>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    public AvaloniaDialogService(ILogger<AvaloniaDialogService>? logger = null)
    {
        _logger = logger;
    }

    private Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }

        _logger?.LogWarning("Application lifetime is not classic desktop style");
        return null;
    }

    /// <inheritdoc/>
    public async Task<string?> ShowOpenFileDialogAsync(
        string? title = null,
        IEnumerable<string>? filterExtensions = null)
    {
        var window = GetMainWindow();
        if (window?.StorageProvider == null)
        {
            _logger?.LogError("StorageProvider not available");
            return null;
        }

        try
        {
            var fileTypes = new List<FilePickerFileType>();
            if (filterExtensions != null)
            {
                var extensions = filterExtensions.ToList();
                if (extensions.Any())
                {
                    fileTypes.Add(new FilePickerFileType("Supported Files")
                    {
                        Patterns = extensions.Select(ext => $"*{ext}").ToArray()
                    });
                }
            }
            else
            {
                // Default to PDF
                fileTypes.Add(new FilePickerFileType("PDF Documents")
                {
                    Patterns = new[] { "*.pdf" }
                });
            }

            var options = new FilePickerOpenOptions
            {
                Title = title ?? "Open File",
                AllowMultiple = false,
                FileTypeFilter = fileTypes
            };

            var result = await window.StorageProvider.OpenFilePickerAsync(options);

            if (result.Count > 0)
            {
                var selectedPath = result[0].Path.LocalPath;
                _logger?.LogInformation("File selected: {FilePath}", selectedPath);
                return selectedPath;
            }

            _logger?.LogInformation("File picker cancelled");
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open file picker");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> ShowOpenMultipleFilesDialogAsync(
        string? title = null,
        IEnumerable<string>? filterExtensions = null)
    {
        var window = GetMainWindow();
        if (window?.StorageProvider == null)
        {
            _logger?.LogError("StorageProvider not available");
            return Array.Empty<string>();
        }

        try
        {
            var fileTypes = new List<FilePickerFileType>();
            if (filterExtensions != null)
            {
                var extensions = filterExtensions.ToList();
                if (extensions.Any())
                {
                    fileTypes.Add(new FilePickerFileType("Supported Files")
                    {
                        Patterns = extensions.Select(ext => $"*{ext}").ToArray()
                    });
                }
            }

            var options = new FilePickerOpenOptions
            {
                Title = title ?? "Open Files",
                AllowMultiple = true,
                FileTypeFilter = fileTypes
            };

            var result = await window.StorageProvider.OpenFilePickerAsync(options);

            if (result.Count > 0)
            {
                var paths = result.Select(f => f.Path.LocalPath).ToList();
                _logger?.LogInformation("Files selected: {Count}", paths.Count);
                return paths;
            }

            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open files picker");
            return Array.Empty<string>();
        }
    }

    /// <inheritdoc/>
    public async Task<string?> ShowSaveFileDialogAsync(
        string? suggestedFileName = null,
        string? title = null,
        IEnumerable<string>? filterExtensions = null)
    {
        var window = GetMainWindow();
        if (window?.StorageProvider == null)
        {
            _logger?.LogError("StorageProvider not available");
            return null;
        }

        try
        {
            var fileTypes = new List<FilePickerFileType>();
            if (filterExtensions != null)
            {
                var extensions = filterExtensions.ToList();
                if (extensions.Any())
                {
                    fileTypes.Add(new FilePickerFileType("Supported Files")
                    {
                        Patterns = extensions.Select(ext => $"*{ext}").ToArray()
                    });
                }
            }
            else
            {
                fileTypes.Add(new FilePickerFileType("PDF Documents")
                {
                    Patterns = new[] { "*.pdf" }
                });
            }

            var options = new FilePickerSaveOptions
            {
                Title = title ?? "Save File",
                SuggestedFileName = suggestedFileName,
                FileTypeChoices = fileTypes,
                ShowOverwritePrompt = true
            };

            var result = await window.StorageProvider.SaveFilePickerAsync(options);

            if (result != null)
            {
                var savePath = result.Path.LocalPath;
                _logger?.LogInformation("Save location selected: {FilePath}", savePath);
                return savePath;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open save dialog");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> ShowFolderPickerAsync(string? title = null)
    {
        var window = GetMainWindow();
        if (window?.StorageProvider == null)
        {
            _logger?.LogError("StorageProvider not available");
            return null;
        }

        try
        {
            var options = new FolderPickerOpenOptions
            {
                Title = title ?? "Select Folder",
                AllowMultiple = false
            };

            var result = await window.StorageProvider.OpenFolderPickerAsync(options);

            if (result.Count > 0)
            {
                var folderPath = result[0].Path.LocalPath;
                _logger?.LogInformation("Folder selected: {FolderPath}", folderPath);
                return folderPath;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to open folder picker");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task ShowInfoAsync(string title, string message)
    {
        await ShowMessageBoxAsync(title, message, MessageBoxIcon.Information);
    }

    /// <inheritdoc/>
    public async Task ShowWarningAsync(string title, string message)
    {
        await ShowMessageBoxAsync(title, message, MessageBoxIcon.Warning);
    }

    /// <inheritdoc/>
    public async Task ShowErrorAsync(string title, string message)
    {
        await ShowMessageBoxAsync(title, message, MessageBoxIcon.Error);
    }

    /// <inheritdoc/>
    public async Task<bool> ShowConfirmationAsync(string title, string message)
    {
        return await ShowConfirmationAsync(title, message, "Yes", "No");
    }

    /// <inheritdoc/>
    public async Task<bool> ShowConfirmationAsync(string title, string message, string confirmText, string cancelText)
    {
        var window = GetMainWindow();
        if (window == null)
        {
            _logger?.LogError("Main window not available for dialog");
            return false;
        }

        try
        {
            // Create a simple confirmation dialog using Avalonia window
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInTaskbar = false
            };

            var result = false;

            var panel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 20
            };

            panel.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
            });

            var buttonPanel = new StackPanel
            {
                Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10
            };

            var confirmButton = new Button { Content = confirmText, IsDefault = true };
            confirmButton.Click += (s, e) => { result = true; dialog.Close(); };

            var cancelButton = new Button { Content = cancelText, IsCancel = true };
            cancelButton.Click += (s, e) => { result = false; dialog.Close(); };

            buttonPanel.Children.Add(confirmButton);
            buttonPanel.Children.Add(cancelButton);

            panel.Children.Add(buttonPanel);
            dialog.Content = panel;

            await dialog.ShowDialog(window);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to show confirmation dialog");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> ShowInputDialogAsync(string title, string message, string? defaultValue = null)
    {
        var window = GetMainWindow();
        if (window == null)
        {
            _logger?.LogError("Main window not available for dialog");
            return null;
        }

        try
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInTaskbar = false
            };

            string? result = null;

            var panel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 15
            };

            panel.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
            });

            var textBox = new TextBox
            {
                Text = defaultValue ?? string.Empty
            };
            panel.Children.Add(textBox);

            var buttonPanel = new StackPanel
            {
                Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10
            };

            var okButton = new Button { Content = "OK", IsDefault = true };
            okButton.Click += (s, e) => { result = textBox.Text; dialog.Close(); };

            var cancelButton = new Button { Content = "Cancel", IsCancel = true };
            cancelButton.Click += (s, e) => { result = null; dialog.Close(); };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            panel.Children.Add(buttonPanel);
            dialog.Content = panel;

            await dialog.ShowDialog(window);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to show input dialog");
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> ShowPasswordDialogAsync(string title, string message)
    {
        var window = GetMainWindow();
        if (window == null)
        {
            _logger?.LogError("Main window not available for dialog");
            return null;
        }

        try
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInTaskbar = false
            };

            string? result = null;

            var panel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 15
            };

            panel.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap
            });

            var passwordBox = new TextBox
            {
                PasswordChar = '*'
            };
            panel.Children.Add(passwordBox);

            var buttonPanel = new StackPanel
            {
                Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
                Spacing = 10
            };

            var okButton = new Button { Content = "OK", IsDefault = true };
            okButton.Click += (s, e) => { result = passwordBox.Text; dialog.Close(); };

            var cancelButton = new Button { Content = "Cancel", IsCancel = true };
            cancelButton.Click += (s, e) => { result = null; dialog.Close(); };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);

            panel.Children.Add(buttonPanel);
            dialog.Content = panel;

            await dialog.ShowDialog(window);

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to show password dialog");
            return null;
        }
    }

    private enum MessageBoxIcon
    {
        Information,
        Warning,
        Error
    }

    private async Task ShowMessageBoxAsync(string title, string message, MessageBoxIcon icon)
    {
        var window = GetMainWindow();
        if (window == null)
        {
            _logger?.LogError("Main window not available for dialog");
            return;
        }

        try
        {
            var dialog = new Window
            {
                Title = title,
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                ShowInTaskbar = false
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 20
            };

            var messagePanel = new StackPanel
            {
                Orientation = global::Avalonia.Layout.Orientation.Horizontal,
                Spacing = 15
            };

            // Add icon indicator
            var iconText = icon switch
            {
                MessageBoxIcon.Information => "i",
                MessageBoxIcon.Warning => "!",
                MessageBoxIcon.Error => "X",
                _ => "?"
            };

            messagePanel.Children.Add(new TextBlock
            {
                Text = iconText,
                FontSize = 24,
                FontWeight = global::Avalonia.Media.FontWeight.Bold,
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Top
            });

            messagePanel.Children.Add(new TextBlock
            {
                Text = message,
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
                MaxWidth = 300
            });

            panel.Children.Add(messagePanel);

            var buttonPanel = new StackPanel
            {
                HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right
            };

            var okButton = new Button { Content = "OK", IsDefault = true, MinWidth = 80 };
            okButton.Click += (s, e) => dialog.Close();
            buttonPanel.Children.Add(okButton);

            panel.Children.Add(buttonPanel);
            dialog.Content = panel;

            await dialog.ShowDialog(window);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to show message box");
        }
    }
}
