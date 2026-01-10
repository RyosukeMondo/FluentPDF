# Design Document

## Overview

Settings & Preferences implements application configuration using a strongly-typed settings model, JSON persistence, and WinUI 3 settings page. The design follows MVVM with SettingsService for persistence, SettingsViewModel for presentation logic, and SettingsPage for UI.

## Steering Document Alignment

### Technical Standards (tech.md)
- **WinUI 3 + MVVM**: SettingsPage with SettingsViewModel
- **FluentResults**: Settings operations return Result<T>
- **Dependency Injection**: ISettingsService registered in container

### Project Structure (structure.md)
- `src/FluentPDF.Core/Services/ISettingsService.cs`
- `src/FluentPDF.Core/Models/AppSettings.cs`
- `src/FluentPDF.App/Services/SettingsService.cs`
- `src/FluentPDF.App/ViewModels/SettingsViewModel.cs`
- `src/FluentPDF.App/Views/SettingsPage.xaml`

## Components

### AppSettings Model

```csharp
public class AppSettings
{
    public ZoomLevel DefaultZoom { get; set; } = ZoomLevel.OneHundredPercent;
    public ScrollMode ScrollMode { get; set; } = ScrollMode.Vertical;
    public AppTheme Theme { get; set; } = AppTheme.UseSystem;
    public bool TelemetryEnabled { get; set; } = false;
    public bool CrashReportingEnabled { get; set; } = false;
}

public enum ZoomLevel
{
    FiftyPercent = 50,
    SeventyFivePercent = 75,
    OneHundredPercent = 100,
    OneTwentyFivePercent = 125,
    OneFiftyPercent = 150,
    OneSeventyFivePercent = 175,
    TwoHundredPercent = 200,
    FitWidth = 1000,
    FitPage = 1001
}

public enum ScrollMode { Vertical, Horizontal, FitPage }
public enum AppTheme { Light, Dark, UseSystem }
```

### ISettingsService

```csharp
public interface ISettingsService
{
    AppSettings Settings { get; }
    event EventHandler<AppSettings> SettingsChanged;
    Task LoadSettingsAsync();
    Task SaveSettingsAsync();
    Task ResetToDefaultsAsync();
}
```

### SettingsService

- **Storage**: ApplicationData.LocalFolder/settings.json
- **Debouncing**: Use SemaphoreSlim + Task.Delay for batching saves
- **Validation**: Validate enum values, clamp numeric values
- **Error Handling**: Corrupt file resets to defaults with log

### SettingsViewModel

```csharp
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty] private ZoomLevel _defaultZoom;
    [ObservableProperty] private ScrollMode _scrollMode;
    [ObservableProperty] private AppTheme _theme;
    [ObservableProperty] private bool _telemetryEnabled;
    [ObservableProperty] private bool _crashReportingEnabled;

    partial void OnDefaultZoomChanged(ZoomLevel value)
    {
        _settingsService.Settings.DefaultZoom = value;
        await _settingsService.SaveSettingsAsync();
    }

    // Similar for other properties...

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        var dialog = new ContentDialog
        {
            Title = "Reset Settings",
            Content = "This will reset all settings to defaults. Continue?",
            PrimaryButtonText = "Reset",
            CloseButtonText = "Cancel"
        };
        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await _settingsService.ResetToDefaultsAsync();
            LoadSettings();
        }
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Settings;
        DefaultZoom = settings.DefaultZoom;
        ScrollMode = settings.ScrollMode;
        Theme = settings.Theme;
        TelemetryEnabled = settings.TelemetryEnabled;
        CrashReportingEnabled = settings.CrashReportingEnabled;
    }
}
```

### SettingsPage XAML

```xml
<Page>
    <ScrollViewer>
        <StackPanel Spacing="24" Padding="24">
            <!-- Viewing section -->
            <StackPanel>
                <TextBlock Text="Viewing" Style="{StaticResource SubtitleTextBlockStyle}"/>
                <ComboBox Header="Default Zoom Level" ItemsSource="{x:Bind ViewModel.ZoomLevels}" SelectedItem="{x:Bind ViewModel.DefaultZoom, Mode=TwoWay}"/>
                <ComboBox Header="Scroll Mode" ItemsSource="{x:Bind ViewModel.ScrollModes}" SelectedItem="{x:Bind ViewModel.ScrollMode, Mode=TwoWay}"/>
            </StackPanel>

            <!-- Appearance section -->
            <StackPanel>
                <TextBlock Text="Appearance" Style="{StaticResource SubtitleTextBlockStyle}"/>
                <RadioButtons Header="Theme" SelectedItem="{x:Bind ViewModel.Theme, Mode=TwoWay}">
                    <RadioButton Content="Light" Tag="Light"/>
                    <RadioButton Content="Dark" Tag="Dark"/>
                    <RadioButton Content="Use System" Tag="UseSystem"/>
                </RadioButtons>
            </StackPanel>

            <!-- Privacy section -->
            <StackPanel>
                <TextBlock Text="Privacy" Style="{StaticResource SubtitleTextBlockStyle}"/>
                <ToggleSwitch Header="Telemetry" IsOn="{x:Bind ViewModel.TelemetryEnabled, Mode=TwoWay}" OnContent="Enabled" OffContent="Disabled"/>
                <TextBlock Text="Helps improve FluentPDF by sending anonymous usage data." Style="{StaticResource CaptionTextBlockStyle}" Margin="0,0,0,12"/>
                <ToggleSwitch Header="Crash Reporting" IsOn="{x:Bind ViewModel.CrashReportingEnabled, Mode=TwoWay}"/>
            </StackPanel>

            <!-- Reset button -->
            <Button Content="Reset to Defaults" Command="{x:Bind ViewModel.ResetToDefaultsCommand}"/>
        </StackPanel>
    </ScrollViewer>
</Page>
```

## Testing Strategy

- **SettingsServiceTests**: Persistence, validation, defaults
- **SettingsViewModelTests**: Property changes trigger saves
- **Integration**: Settings apply to new documents

## Future Enhancements

- Export/import settings
- Per-document overrides
- Advanced rendering options
