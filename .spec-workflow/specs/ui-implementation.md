# UI Implementation Specification

**Status**: Draft
**Priority**: Critical
**Phase**: Core Implementation
**Dependencies**: verification-infrastructure.md

## Overview

This specification defines the complete UI implementation based on the ASCII wireframes in `.spec-workflow/steering/product.md`. Every UI component must be implemented with proper AutomationIds for REST API verification.

## Implementation Principles

1. **AutomationId on Every Interactive Element**: Required for UI verification
2. **MVVM Separation**: ViewModels testable without UI
3. **Responsive Layouts**: Support 800px - 4K displays
4. **Theme-Aware**: Light/Dark/System theme support
5. **Accessibility**: Keyboard navigation, screen reader support
6. **Performance**: 60 FPS scrolling, smooth animations

## Main Window Layout

### File: `Views/MainWindow.xaml`

**Structure**:
```xml
<Window x:Class="FluentPDF.App.Views.MainWindow"
        AutomationProperties.AutomationId="MainWindow">
    <Grid AutomationProperties.AutomationId="MainGrid">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>      <!-- Title Bar -->
            <RowDefinition Height="Auto"/>      <!-- Toolbar -->
            <RowDefinition Height="Auto"/>      <!-- Annotation Toolbar -->
            <RowDefinition Height="Auto"/>      <!-- Search Panel -->
            <RowDefinition Height="*"/>         <!-- Content Area -->
        </Grid.RowDefinitions>

        <!-- Title Bar (Row 0) -->
        <Grid Grid.Row="0" AutomationProperties.AutomationId="TitleBar">
            <!-- Custom title bar with window controls -->
        </Grid>

        <!-- Main Toolbar (Row 1) -->
        <controls:MainToolbar Grid.Row="1"
                            AutomationProperties.AutomationId="MainToolbar"/>

        <!-- Annotation Toolbar (Row 2) -->
        <controls:AnnotationToolbar Grid.Row="2"
                                  AutomationProperties.AutomationId="AnnotationToolbar"
                                  Visibility="{Binding ShowAnnotationToolbar}"/>

        <!-- Search Panel (Row 3) -->
        <controls:SearchPanel Grid.Row="3"
                            AutomationProperties.AutomationId="SearchPanel"
                            Visibility="{Binding ShowSearchPanel}"/>

        <!-- Content Area (Row 4) -->
        <Grid Grid.Row="4" AutomationProperties.AutomationId="ContentGrid">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto"/>    <!-- Thumbnails Sidebar -->
                <ColumnDefinition Width="Auto"/>    <!-- Bookmarks Sidebar -->
                <ColumnDefinition Width="*"/>       <!-- PDF Content -->
            </Grid.ColumnDefinitions>

            <!-- Thumbnails Sidebar -->
            <controls:ThumbnailsSidebar Grid.Column="0"
                                      AutomationProperties.AutomationId="ThumbnailsSidebar"
                                      Visibility="{Binding ShowThumbnails}"/>

            <!-- Bookmarks Sidebar -->
            <controls:BookmarksSidebar Grid.Column="1"
                                     AutomationProperties.AutomationId="BookmarksSidebar"
                                     Visibility="{Binding ShowBookmarks}"/>

            <!-- PDF Content Area -->
            <controls:PdfContentArea Grid.Column="2"
                                   AutomationProperties.AutomationId="PdfContentArea"/>
        </Grid>
    </Grid>
</Window>
```

**ViewModel**: `ViewModels/MainWindowViewModel.cs`

**Required Properties**:
```csharp
public class MainWindowViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _showAnnotationToolbar;

    [ObservableProperty]
    private bool _showSearchPanel;

    [ObservableProperty]
    private bool _showThumbnails = true;

    [ObservableProperty]
    private bool _showBookmarks;

    [ObservableProperty]
    private string _documentTitle = "FluentPDF";

    [ObservableProperty]
    private ApplicationTheme _currentTheme = ApplicationTheme.System;
}
```

## Main Toolbar

### File: `Controls/MainToolbar.xaml`

**Layout**:
```xml
<UserControl x:Class="FluentPDF.App.Controls.MainToolbar">
    <StackPanel Orientation="Horizontal" Spacing="8" Padding="8">
        <!-- File Operations -->
        <Button AutomationProperties.AutomationId="OpenFileButton"
                Command="{Binding OpenFileCommand}"
                ToolTipService.ToolTip="Open (Ctrl+O)">
            <SymbolIcon Symbol="OpenFile"/>
        </Button>

        <AppBarSeparator/>

        <!-- Navigation -->
        <Button AutomationProperties.AutomationId="PreviousPageButton"
                Command="{Binding PreviousPageCommand}"
                IsEnabled="{Binding CanGoPreviousPage}">
            <SymbolIcon Symbol="Back"/>
        </Button>

        <TextBlock Text="Page" VerticalAlignment="Center"/>

        <NumberBox AutomationProperties.AutomationId="PageNumberBox"
                   Value="{Binding CurrentPage, Mode=TwoWay}"
                   Minimum="1"
                   Maximum="{Binding TotalPages}"
                   Width="60"/>

        <TextBlock Text="of" VerticalAlignment="Center"/>
        <TextBlock Text="{Binding TotalPages}" VerticalAlignment="Center"/>

        <Button AutomationProperties.AutomationId="NextPageButton"
                Command="{Binding NextPageCommand}"
                IsEnabled="{Binding CanGoNextPage}">
            <SymbolIcon Symbol="Forward"/>
        </Button>

        <AppBarSeparator/>

        <!-- View Controls -->
        <ToggleButton AutomationProperties.AutomationId="ToggleThumbnailsButton"
                      IsChecked="{Binding ShowThumbnails, Mode=TwoWay}"
                      ToolTipService.ToolTip="Thumbnails (Ctrl+T)">
            <FontIcon Glyph="&#xE8FD;"/>
        </ToggleButton>

        <ToggleButton AutomationProperties.AutomationId="ToggleBookmarksButton"
                      IsChecked="{Binding ShowBookmarks, Mode=TwoWay}"
                      ToolTipService.ToolTip="Bookmarks (Ctrl+B)">
            <FontIcon Glyph="&#xE8A4;"/>
        </ToggleButton>

        <ToggleButton AutomationProperties.AutomationId="ToggleSearchButton"
                      IsChecked="{Binding ShowSearch, Mode=TwoWay}"
                      ToolTipService.ToolTip="Search (Ctrl+F)">
            <SymbolIcon Symbol="Find"/>
        </ToggleButton>

        <AppBarSeparator/>

        <!-- Zoom Controls -->
        <Button AutomationProperties.AutomationId="ZoomOutButton"
                Command="{Binding ZoomOutCommand}">
            <SymbolIcon Symbol="ZoomOut"/>
        </Button>

        <ComboBox AutomationProperties.AutomationId="ZoomLevelComboBox"
                  SelectedItem="{Binding ZoomLevel, Mode=TwoWay}"
                  Width="100">
            <ComboBoxItem Content="50%"/>
            <ComboBoxItem Content="75%"/>
            <ComboBoxItem Content="100%"/>
            <ComboBoxItem Content="125%"/>
            <ComboBoxItem Content="150%"/>
            <ComboBoxItem Content="200%"/>
            <ComboBoxItem Content="300%"/>
            <ComboBoxItem Content="400%"/>
            <ComboBoxItem Content="Fit Width"/>
            <ComboBoxItem Content="Fit Page"/>
        </ComboBox>

        <Button AutomationProperties.AutomationId="ZoomInButton"
                Command="{Binding ZoomInCommand}">
            <SymbolIcon Symbol="ZoomIn"/>
        </Button>

        <Button AutomationProperties.AutomationId="ResetZoomButton"
                Command="{Binding ResetZoomCommand}"
                ToolTipService.ToolTip="Reset Zoom (Ctrl+0)">
            <FontIcon Glyph="&#xE7A8;"/>
        </Button>

        <ComboBox AutomationProperties.AutomationId="ViewModeComboBox"
                  SelectedItem="{Binding ViewMode, Mode=TwoWay}"
                  Width="120">
            <ComboBoxItem Content="Single Page"/>
            <ComboBoxItem Content="Continuous Scroll"/>
            <ComboBoxItem Content="Two Pages"/>
        </ComboBox>

        <AppBarSeparator/>

        <!-- Document Operations -->
        <Button AutomationProperties.AutomationId="MergeButton"
                Command="{Binding ShowMergeDialogCommand}"
                ToolTipService.ToolTip="Merge PDFs">
            <StackPanel Orientation="Horizontal" Spacing="4">
                <SymbolIcon Symbol="Merge"/>
                <TextBlock Text="Merge"/>
            </StackPanel>
        </Button>

        <Button AutomationProperties.AutomationId="SplitButton"
                Command="{Binding ShowSplitDialogCommand}"
                ToolTipService.ToolTip="Split PDF">
            <StackPanel Orientation="Horizontal" Spacing="4">
                <FontIcon Glyph="&#xE8E9;"/>
                <TextBlock Text="Split"/>
            </StackPanel>
        </Button>

        <Button AutomationProperties.AutomationId="OptimizeButton"
                Command="{Binding ShowOptimizeDialogCommand}"
                ToolTipService.ToolTip="Optimize PDF">
            <StackPanel Orientation="Horizontal" Spacing="4">
                <SymbolIcon Symbol="Repair"/>
                <TextBlock Text="Optimize"/>
            </StackPanel>
        </Button>

        <AppBarSeparator/>

        <!-- Image & Media -->
        <Button AutomationProperties.AutomationId="InsertImageButton"
                Command="{Binding InsertImageCommand}"
                ToolTipService.ToolTip="Insert Image">
            <StackPanel Orientation="Horizontal" Spacing="4">
                <SymbolIcon Symbol="Pictures"/>
                <TextBlock Text="Image"/>
            </StackPanel>
        </Button>

        <Button AutomationProperties.AutomationId="WatermarkButton"
                Command="{Binding ShowWatermarkDialogCommand}"
                ToolTipService.ToolTip="Add Watermark">
            <StackPanel Orientation="Horizontal" Spacing="4">
                <FontIcon Glyph="&#xF0B2;"/>
                <TextBlock Text="Watermark"/>
            </StackPanel>
        </Button>

        <AppBarSeparator/>

        <!-- Conversion -->
        <Button AutomationProperties.AutomationId="ConvertDocxButton"
                Command="{Binding ShowConversionPageCommand}"
                ToolTipService.ToolTip="Convert DOCX to PDF">
            <StackPanel Orientation="Horizontal" Spacing="4">
                <FontIcon Glyph="&#xE8A5;"/>
                <TextBlock Text="DOCX"/>
            </StackPanel>
        </Button>
    </StackPanel>
</UserControl>
```

## Annotation Toolbar

### File: `Controls/AnnotationToolbar.xaml`

**Layout**:
```xml
<UserControl x:Class="FluentPDF.App.Controls.AnnotationToolbar">
    <StackPanel Orientation="Horizontal" Spacing="8" Padding="8"
                Background="{ThemeResource LayerFillColorDefaultBrush}">

        <!-- Text Markup Tools -->
        <ToggleButton AutomationProperties.AutomationId="HighlightButton"
                      IsChecked="{Binding HighlightModeActive, Mode=TwoWay}"
                      ToolTipService.ToolTip="Highlight Text">
            <StackPanel Orientation="Horizontal" Spacing="4">
                <FontIcon Glyph="&#xE7ED;"/>
                <TextBlock Text="Highlight"/>
            </StackPanel>
        </ToggleButton>

        <ToggleButton AutomationProperties.AutomationId="UnderlineButton"
                      IsChecked="{Binding UnderlineModeActive, Mode=TwoWay}"
                      ToolTipService.ToolTip="Underline Text">
            <StackPanel Orientation="Horizontal" Spacing="4">
                <FontIcon Glyph="&#xE8DB;"/>
                <TextBlock Text="Underline"/>
            </StackPanel>
        </ToggleButton>

        <ToggleButton AutomationProperties.AutomationId="StrikethroughButton"
                      IsChecked="{Binding StrikethroughModeActive, Mode=TwoWay}"
                      ToolTipService.ToolTip="Strikethrough Text">
            <StackPanel Orientation="Horizontal" Spacing="4">
                <FontIcon Glyph="&#xEDE0;"/>
                <TextBlock Text="Strikethrough"/>
            </StackPanel>
        </ToggleButton>

        <AppBarSeparator/>

        <!-- Comment Tools -->
        <ToggleButton AutomationProperties.AutomationId="CommentButton"
                      IsChecked="{Binding CommentModeActive, Mode=TwoWay}"
                      ToolTipService.ToolTip="Add Comment">
            <StackPanel Orientation="Horizontal" Spacing="4">
                <SymbolIcon Symbol="Comment"/>
                <TextBlock Text="Comment"/>
            </StackPanel>
        </ToggleButton>

        <AppBarSeparator/>

        <!-- Shape Tools -->
        <ToggleButton AutomationProperties.AutomationId="RectangleButton"
                      IsChecked="{Binding RectangleModeActive, Mode=TwoWay}"
                      ToolTipService.ToolTip="Draw Rectangle">
            <StackPanel Orientation="Horizontal" Spacing="4">
                <FontIcon Glyph="&#xE735;"/>
                <TextBlock Text="Rectangle"/>
            </StackPanel>
        </ToggleButton>

        <ToggleButton AutomationProperties.AutomationId="CircleButton"
                      IsChecked="{Binding CircleModeActive, Mode=TwoWay}"
                      ToolTipService.ToolTip="Draw Circle">
            <StackPanel Orientation="Horizontal" Spacing="4">
                <FontIcon Glyph="&#xEA3F;"/>
                <TextBlock Text="Circle"/>
            </StackPanel>
        </ToggleButton>

        <ToggleButton AutomationProperties.AutomationId="FreehandButton"
                      IsChecked="{Binding FreehandModeActive, Mode=TwoWay}"
                      ToolTipService.ToolTip="Freehand Drawing">
            <StackPanel Orientation="Horizontal" Spacing="4">
                <SymbolIcon Symbol="Edit"/>
                <TextBlock Text="Freehand"/>
            </StackPanel>
        </ToggleButton>

        <AppBarSeparator/>

        <!-- Color Picker -->
        <TextBlock Text="Color:" VerticalAlignment="Center"/>
        <ColorPicker AutomationProperties.AutomationId="AnnotationColorPicker"
                     Color="{Binding AnnotationColor, Mode=TwoWay}"
                     IsAlphaEnabled="True"
                     ColorSpectrumShape="Ring"/>
    </StackPanel>
</UserControl>
```

## Search Panel

### File: `Controls/SearchPanel.xaml`

**Layout**:
```xml
<UserControl x:Class="FluentPDF.App.Controls.SearchPanel">
    <Grid Padding="8" Background="{ThemeResource LayerFillColorDefaultBrush}">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="Auto"/>
        </Grid.ColumnDefinitions>

        <StackPanel Grid.Column="0" Orientation="Horizontal" Spacing="8">
            <TextBox AutomationProperties.AutomationId="SearchTextBox"
                     Text="{Binding SearchText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                     PlaceholderText="Search..."
                     Width="300"/>

            <TextBlock AutomationProperties.AutomationId="SearchResultsText"
                       Text="{Binding SearchResultsText}"
                       VerticalAlignment="Center"
                       Foreground="{ThemeResource TextFillColorSecondaryBrush}"/>

            <Button AutomationProperties.AutomationId="PreviousMatchButton"
                    Command="{Binding PreviousMatchCommand}"
                    IsEnabled="{Binding HasMatches}"
                    ToolTipService.ToolTip="Previous Match (Shift+F3)">
                <SymbolIcon Symbol="Up"/>
            </Button>

            <Button AutomationProperties.AutomationId="NextMatchButton"
                    Command="{Binding NextMatchCommand}"
                    IsEnabled="{Binding HasMatches}"
                    ToolTipService.ToolTip="Next Match (F3)">
                <SymbolIcon Symbol="Down"/>
            </Button>

            <CheckBox AutomationProperties.AutomationId="CaseSensitiveCheckBox"
                      IsChecked="{Binding CaseSensitive, Mode=TwoWay}"
                      Content="Case Sensitive"/>
        </StackPanel>

        <Button Grid.Column="1"
                AutomationProperties.AutomationId="CloseSearchButton"
                Command="{Binding CloseSearchCommand}"
                ToolTipService.ToolTip="Close (Esc)">
            <SymbolIcon Symbol="Cancel"/>
        </Button>
    </Grid>
</UserControl>
```

## Thumbnails Sidebar

### File: `Controls/ThumbnailsSidebar.xaml`

**Layout**:
```xml
<UserControl x:Class="FluentPDF.App.Controls.ThumbnailsSidebar">
    <Grid AutomationProperties.AutomationId="ThumbnailsGrid"
          MinWidth="150"
          MaxWidth="600"
          Width="200">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Grid Grid.Row="0" Padding="8" Background="{ThemeResource LayerFillColorDefaultBrush}">
            <TextBlock Text="THUMBNAILS" FontWeight="SemiBold"/>
        </Grid>

        <!-- Thumbnail List -->
        <ScrollViewer Grid.Row="1"
                      AutomationProperties.AutomationId="ThumbnailsScrollViewer">
            <ItemsRepeater ItemsSource="{Binding Thumbnails}">
                <ItemsRepeater.ItemTemplate>
                    <DataTemplate>
                        <Button AutomationProperties.AutomationId="{Binding AutomationId}"
                                Command="{Binding NavigateToPageCommand}"
                                CommandParameter="{Binding PageIndex}"
                                Padding="8"
                                HorizontalAlignment="Stretch">
                            <Grid>
                                <Grid.RowDefinitions>
                                    <RowDefinition Height="*"/>
                                    <RowDefinition Height="Auto"/>
                                </Grid.RowDefinitions>

                                <Border Grid.Row="0"
                                        BorderBrush="{Binding IsCurrentPage, Converter={StaticResource BoolToHighlightConverter}}"
                                        BorderThickness="2">
                                    <Image Source="{Binding ThumbnailImage}"
                                           Width="160"
                                           Height="200"
                                           Stretch="Uniform"/>
                                </Border>

                                <TextBlock Grid.Row="1"
                                           Text="{Binding PageNumber}"
                                           HorizontalAlignment="Center"
                                           Margin="0,4,0,0"/>
                            </Grid>
                        </Button>
                    </DataTemplate>
                </ItemsRepeater.ItemTemplate>
            </ItemsRepeater>
        </ScrollViewer>

        <!-- Resize Gripper -->
        <GridSplitter Grid.Row="1"
                      AutomationProperties.AutomationId="ThumbnailsResizeGripper"
                      Width="4"
                      HorizontalAlignment="Right"
                      Background="Transparent"/>
    </Grid>
</UserControl>
```

## Bookmarks Sidebar

### File: `Controls/BookmarksSidebar.xaml`

**Layout**:
```xml
<UserControl x:Class="FluentPDF.App.Controls.BookmarksSidebar">
    <Grid AutomationProperties.AutomationId="BookmarksGrid"
          MinWidth="150"
          MaxWidth="600"
          Width="250">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Grid Grid.Row="0" Padding="8" Background="{ThemeResource LayerFillColorDefaultBrush}">
            <TextBlock Text="BOOKMARKS" FontWeight="SemiBold"/>
        </Grid>

        <!-- Bookmark Tree -->
        <TreeView Grid.Row="1"
                  AutomationProperties.AutomationId="BookmarksTreeView"
                  ItemsSource="{Binding Bookmarks}"
                  SelectionMode="Single">
            <TreeView.ItemTemplate>
                <DataTemplate>
                    <TreeViewItem AutomationProperties.AutomationId="{Binding AutomationId}"
                                  ItemsSource="{Binding Children}"
                                  Content="{Binding Title}"
                                  IsExpanded="{Binding IsExpanded, Mode=TwoWay}">
                        <TreeViewItem.ContextFlyout>
                            <MenuFlyout>
                                <MenuFlyoutItem Text="Go to Page"
                                              Command="{Binding NavigateCommand}"/>
                                <MenuFlyoutItem Text="Expand All Children"
                                              Command="{Binding ExpandAllCommand}"/>
                            </MenuFlyout>
                        </TreeViewItem.ContextFlyout>
                    </TreeViewItem>
                </DataTemplate>
            </TreeView.ItemTemplate>
        </TreeView>

        <!-- Resize Gripper -->
        <GridSplitter Grid.Row="1"
                      AutomationProperties.AutomationId="BookmarksResizeGripper"
                      Width="4"
                      HorizontalAlignment="Right"
                      Background="Transparent"/>
    </Grid>
</UserControl>
```

## PDF Content Area

### File: `Controls/PdfContentArea.xaml`

**Layout**:
```xml
<UserControl x:Class="FluentPDF.App.Controls.PdfContentArea">
    <Grid AutomationProperties.AutomationId="ContentArea">
        <ScrollViewer AutomationProperties.AutomationId="PdfScrollViewer"
                      ZoomMode="Enabled"
                      HorizontalScrollBarVisibility="Auto"
                      VerticalScrollBarVisibility="Auto">
            <Canvas AutomationProperties.AutomationId="PdfCanvas"
                    Width="{Binding PageWidth}"
                    Height="{Binding PageHeight}">

                <!-- PDF Page Image -->
                <Image AutomationProperties.AutomationId="PdfPageImage"
                       Source="{Binding CurrentPageImage}"
                       Width="{Binding PageWidth}"
                       Height="{Binding PageHeight}"/>

                <!-- Annotation Layer -->
                <Canvas AutomationProperties.AutomationId="AnnotationLayer"
                        Width="{Binding PageWidth}"
                        Height="{Binding PageHeight}">
                    <ItemsControl ItemsSource="{Binding Annotations}">
                        <!-- Annotation rendering templates -->
                    </ItemsControl>
                </Canvas>

                <!-- Form Field Layer -->
                <Canvas AutomationProperties.AutomationId="FormFieldLayer"
                        Width="{Binding PageWidth}"
                        Height="{Binding PageHeight}">
                    <ItemsControl ItemsSource="{Binding FormFields}">
                        <!-- Form field rendering templates -->
                    </ItemsControl>
                </Canvas>

                <!-- Selection Layer -->
                <Canvas AutomationProperties.AutomationId="SelectionLayer"
                        Width="{Binding PageWidth}"
                        Height="{Binding PageHeight}">
                    <!-- Text selection highlighting -->
                </Canvas>
            </Canvas>
        </ScrollViewer>

        <!-- Validation Error Bar -->
        <Grid AutomationProperties.AutomationId="ValidationErrorBar"
              VerticalAlignment="Top"
              Background="{ThemeResource SystemFillColorCriticalBrush}"
              Padding="12,8"
              Visibility="{Binding HasValidationErrors, Converter={StaticResource BoolToVisibilityConverter}}">
            <StackPanel Orientation="Horizontal" Spacing="8">
                <FontIcon Glyph="&#xE7BA;" Foreground="{ThemeResource SystemFillColorCriticalBrush}"/>
                <TextBlock AutomationProperties.AutomationId="ValidationErrorText"
                           Text="{Binding ValidationErrorMessage}"
                           TextWrapping="Wrap"/>
            </StackPanel>
        </Grid>
    </Grid>
</UserControl>
```

## Dialogs

### Watermark Dialog

**File**: `Views/WatermarkDialog.xaml`

**AutomationIds**:
- `WatermarkDialog`
- `TextRadioButton`, `ImageRadioButton`
- `WatermarkTextBox`
- `FontComboBox`, `FontSizeBox`
- `WatermarkColorPicker`, `OpacitySlider`
- `PositionGrid` (with buttons: `TL`, `TC`, `TR`, `ML`, `MC`, `MR`, `BL`, `BC`, `BR`)
- `RotationSlider`
- `AllPagesRadioButton`, `PageRangeRadioButton`, `PageRangeTextBox`
- `PreviewButton`, `ApplyButton`, `CancelButton`

### Merge Dialog

**File**: `Views/MergeDialog.xaml`

**AutomationIds**:
- `MergeDialog`
- `MergeFilesListView`
- `AddFilesButton`, `AddFolderButton`
- `MoveUpButton`, `MoveDownButton`, `RemoveButton`
- `OutputFileNameTextBox`
- `TotalPagesText`, `TotalSizeText`
- `PreviewButton`, `MergeButton`, `CancelButton`

### Split Dialog

**File**: `Views/SplitDialog.xaml`

**AutomationIds**:
- `SplitDialog`
- `SplitMethodComboBox` (By page ranges, Every N pages, By bookmarks, By file size)
- `PageRangesList` (dynamic list of range inputs)
- `AddRangeButton`
- `OutputFolderTextBox`, `BrowseFolderButton`
- `FilenamePatternTextBox`
- `PreviewListView`
- `SplitButton`, `CancelButton`

### Settings Page

**File**: `Views/SettingsPage.xaml`

**AutomationIds**:
- `SettingsPage`
- `ThemeLightRadio`, `ThemeDarkRadio`, `ThemeSystemRadio`
- `AccentColorPicker`
- `DefaultZoomComboBox`
- `DefaultViewModeComboBox`
- `ShowThumbnailsCheckBox`, `ShowBookmarksCheckBox`
- `RenderQualityComboBox`
- `CacheSizeTextBox`
- `HardwareAccelerationCheckBox`
- `SendUsageDataCheckBox`
- `ClearRecentFilesButton`
- `CheckUpdatesButton`, `ViewLicensesButton`, `SendFeedbackButton`

## Responsive Layout Breakpoints

### Wide Layout (>1400px)
- Show Thumbnails (200px) + Bookmarks (250px) + Content
- Full toolbar with text labels
- All features visible

### Medium Layout (800-1400px)
- Show Thumbnails (200px) + Content
- Bookmarks collapsed (toggle button)
- Toolbar with icons + text on important items

### Narrow Layout (<800px)
- Content only
- Sidebars as overlays
- Compact toolbar (icons only)

## Theme Implementation

### Files
- `Styles/LightTheme.xaml`
- `Styles/DarkTheme.xaml`
- `Styles/HighContrastTheme.xaml`

### Theme Resources

**Background Colors**:
```xml
<!-- Light Theme -->
<Color x:Key="PageBackgroundColor">#FFFFFF</Color>
<Color x:Key="SidebarBackgroundColor">#F3F3F3</Color>
<Color x:Key="ToolbarBackgroundColor">#FAFAFA</Color>

<!-- Dark Theme -->
<Color x:Key="PageBackgroundColor">#1E1E1E</Color>
<Color x:Key="SidebarBackgroundColor">#252525</Color>
<Color x:Key="ToolbarBackgroundColor">#2D2D2D</Color>
```

**Text Colors**:
```xml
<!-- Light Theme -->
<Color x:Key="PrimaryTextColor">#000000</Color>
<Color x:Key="SecondaryTextColor">#666666</Color>

<!-- Dark Theme -->
<Color x:Key="PrimaryTextColor">#FFFFFF</Color>
<Color x:Key="SecondaryTextColor">#AAAAAA</Color>
```

## Keyboard Navigation

### Focus Management

**Tab Order**:
1. Main Toolbar buttons (left to right)
2. Annotation Toolbar buttons (left to right)
3. Search Panel controls (left to right)
4. Thumbnails Sidebar (if visible)
5. Bookmarks Sidebar (if visible)
6. PDF Content Area
7. Form fields (if present)

### Keyboard Shortcuts

Implemented via `KeyboardAccelerator`:

```xml
<Button.KeyboardAccelerators>
    <KeyboardAccelerator Key="O" Modifiers="Control"/>
</Button.KeyboardAccelerators>
```

**Shortcuts Table**: See product.md keyboard shortcuts reference

## Accessibility

### Screen Reader Support

- All interactive elements have `AutomationProperties.Name`
- Status changes announced via `AutomationProperties.LiveSetting`
- Form validation errors announced
- Page navigation announced

### High Contrast Mode

- Respect system high contrast settings
- Override colors with `SystemColor*` resources
- Sufficient contrast ratios (WCAG AA)

## Performance Requirements

### Rendering
- 60 FPS scrolling
- Smooth zoom transitions (<100ms)
- Thumbnail generation on background thread

### Memory
- <200MB for typical documents
- Virtualized thumbnail list (only render visible)
- Dispose resources when sidebars collapsed

## Testing Requirements

### Unit Tests
- ViewModel logic (commands, properties)
- Converters
- Validation logic

### UI Automation Tests (REST API)
- Element presence and layout
- Button click behavior
- Form field interaction
- Theme switching
- Sidebar resize
- Navigation

### Visual Regression Tests
- Snapshot tests for all dialogs
- Theme comparison tests
- Layout tests at different breakpoints

## Implementation Order

1. **Phase 1**: Main window structure, basic layout
2. **Phase 2**: Main toolbar, navigation
3. **Phase 3**: Sidebars (thumbnails, bookmarks)
4. **Phase 4**: Annotation toolbar
5. **Phase 5**: Search panel
6. **Phase 6**: Dialogs (watermark, merge, split, settings)
7. **Phase 7**: Theme implementation
8. **Phase 8**: Keyboard navigation and accessibility
9. **Phase 9**: Responsive layouts
10. **Phase 10**: Performance optimization

## AutomationId Naming Convention

**Pattern**: `{ComponentName}{ElementType}{Purpose}`

**Examples**:
- `OpenFileButton`
- `SearchTextBox`
- `ThumbnailsScrollViewer`
- `PageNumberBox`
- `AnnotationColorPicker`

**Consistency**:
- Buttons: `*Button`
- TextBoxes: `*TextBox`
- ComboBoxes: `*ComboBox`
- CheckBoxes: `*CheckBox`
- RadioButtons: `*RadioButton`
- Panels/Grids: `*Panel`, `*Grid`
