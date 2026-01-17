# Component Mapping Guide: React ↔ XAML

This document provides a comprehensive mapping between React prototype components and their WinUI 3 XAML counterparts in FluentPDF. Use this guide to translate UI designs between the two frameworks efficiently.

## Table of Contents

1. [Overview](#overview)
2. [Component Mappings](#component-mappings)
   - [Controls](#controls)
   - [Views](#views)
   - [Dialogs](#dialogs)
3. [Pattern Translation Guide](#pattern-translation-guide)
4. [XAML-Specific Feature Simplifications](#xaml-specific-feature-simplifications)
5. [Translation Checklist](#translation-checklist)
6. [Template for New Components](#template-for-new-components)

---

## Overview

The React prototype serves as a rapid iteration environment for UI design. Components are simplified versions of their XAML counterparts, focusing on visual structure and layout while omitting complex features like data binding infrastructure and WinUI 3-specific functionality.

**Key Principles:**
- React components use **props** ↔ XAML components use **Dependency Properties**
- React uses **callbacks** ↔ XAML uses **event handlers** (code-behind)
- React uses **CSS Modules** ↔ XAML uses **inline styles and theme resources**
- React uses **design token CSS variables** ↔ XAML uses **ResourceDictionary with theme brushes**

---

## Component Mappings

### Controls

#### 1. ThumbnailsSidebar

**Purpose:** Displays a vertical scrollable list of PDF page thumbnails with selection and loading states.

**File Paths:**
- React: `prototype/src/components/ThumbnailsSidebar.tsx`
- React CSS: `prototype/src/components/ThumbnailsSidebar.module.css`
- XAML: `src/FluentPDF.App/Controls/ThumbnailsSidebar.xaml`
- XAML Code-behind: `src/FluentPDF.App/Controls/ThumbnailsSidebar.xaml.cs`

**Props/Properties Mapping:**

| React Prop | XAML Property | Description |
|------------|---------------|-------------|
| `thumbnails: ThumbnailItem[]` | `ItemsSource` (bound to ViewModel) | Array/Collection of thumbnail items |
| `onThumbnailClick?: (pageNumber: number) => void` | `Click` event handler | Callback when thumbnail clicked |
| `onThumbnailContextMenu?: (pageNumber, event) => void` | `RightTapped` + `ContextFlyout` | Right-click context menu |
| `scrollTop?: number` | `ScrollViewer.VerticalOffset` | Current scroll position |
| `onScroll?: (scrollTop: number) => void` | `ViewChanged` event | Scroll position change callback |

**Side-by-Side Example: Thumbnail Item Rendering**

**React:**
```tsx
// ThumbnailsSidebar.tsx
interface ThumbnailsSidebarProps {
  thumbnails: ThumbnailItem[];
  onThumbnailClick?: (pageNumber: number) => void;
}

export const ThumbnailsSidebar: React.FC<ThumbnailsSidebarProps> = ({
  thumbnails,
  onThumbnailClick
}) => (
  <div className={styles.thumbnailsSidebar}>
    {thumbnails.map((item) => (
      <button
        key={item.pageNumber}
        className={`${styles.thumbnailButton} ${item.isSelected ? styles.selected : ''}`}
        onClick={() => onThumbnailClick?.(item.pageNumber)}
      >
        <div className={styles.thumbnailContainer}>
          {item.isSelected && <div className={styles.selectedBorder} />}
          {/* ... thumbnail content ... */}
        </div>
      </button>
    ))}
  </div>
);
```

**XAML:**
```xml
<!-- ThumbnailsSidebar.xaml -->
<ItemsRepeater
    ItemsSource="{x:Bind ViewModel.Thumbnails, Mode=OneWay}"
    ItemTemplate="{StaticResource ThumbnailItemTemplate}">
    <ItemsRepeater.Layout>
        <StackLayout Orientation="Vertical" Spacing="4"/>
    </ItemsRepeater.Layout>
</ItemsRepeater>

<!-- DataTemplate -->
<DataTemplate x:Key="ThumbnailItemTemplate" x:DataType="models:ThumbnailItem">
    <Button Click="ThumbnailButton_Click" Background="Transparent">
        <Grid Width="150" Height="220">
            <!-- Selected border -->
            <Border
                BorderBrush="{ThemeResource AccentFillColorDefaultBrush}"
                BorderThickness="3"
                Visibility="{x:Bind IsSelected, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}"/>
            <!-- ... thumbnail content ... -->
        </Grid>
    </Button>
</DataTemplate>
```

**XAML-Specific Features:**
- **Drag-and-drop reordering:** XAML has `CanDrag="True"`, `DragStarting`, `DragOver`, `Drop` events (not implemented in React)
- **Context menu:** XAML has full `MenuFlyout` with rotate/delete/insert operations (stubbed in React with console.log)
- **ItemsRepeater virtualization:** XAML uses efficient virtualization for large lists (React uses simple map, no virtualization yet)

---

#### 2. PdfViewerControl

**Purpose:** Main PDF viewer with toolbar, zoom controls, page navigation, and document display area.

**File Paths:**
- React: `prototype/src/components/PdfViewerControl.tsx`
- React CSS: `prototype/src/components/PdfViewerControl.module.css`
- XAML: `src/FluentPDF.App/Controls/PdfViewerControl.xaml`
- XAML Code-behind: `src/FluentPDF.App/Controls/PdfViewerControl.xaml.cs`

**Props/Properties Mapping:**

| React Prop | XAML Property | Description |
|------------|---------------|-------------|
| `document?: PdfDocument` | `Document` (ViewModel property) | PDF document metadata |
| `currentPage?: number` | `CurrentPage` dependency property | Current page number (1-indexed) |
| `zoomLevel?: number` | `ZoomLevel` dependency property | Zoom level (1.0 = 100%) |
| `showSearchPanel?: boolean` | `IsSearchVisible` dependency property | Search panel visibility |
| `isLoading?: boolean` | `IsLoading` (ViewModel) | Loading state indicator |

**Side-by-Side Example: Zoom Controls**

**React:**
```tsx
// PdfViewerControl.tsx
const [localZoom, setLocalZoom] = useState(zoomLevel);

const handleZoomIn = () => {
  const newZoom = Math.min(localZoom + 0.25, 3.0);
  setLocalZoom(newZoom);
};

return (
  <div className={styles.toolbar}>
    <button
      onClick={handleZoomIn}
      disabled={localZoom >= 3.0}
      title="Zoom In (Ctrl++)"
    >
      <span className={styles.icon}>🔍+</span>
      <span>Zoom In</span>
    </button>
    <div className={styles.zoomIndicator}>
      <strong>{Math.round(localZoom * 100)}%</strong>
    </div>
  </div>
);
```

**XAML:**
```xml
<!-- PdfViewerControl.xaml -->
<Button
    Click="ZoomIn_Click"
    IsEnabled="{x:Bind ViewModel.CanZoomIn, Mode=OneWay}"
    ToolTipService.ToolTip="Zoom In (Ctrl++)">
    <Button.Content>
        <StackPanel Orientation="Horizontal" Spacing="4">
            <FontIcon Glyph="&#xE8A3;"/>
            <TextBlock Text="Zoom In"/>
        </StackPanel>
    </Button.Content>
</Button>

<TextBlock Text="{x:Bind ViewModel.ZoomPercentage, Mode=OneWay}"/>
```

```csharp
// PdfViewerControl.xaml.cs
private void ZoomIn_Click(object sender, RoutedEventArgs e)
{
    ViewModel.ZoomLevel = Math.Min(ViewModel.ZoomLevel + 0.25, 3.0);
}
```

**XAML-Specific Features:**
- **Actual PDF rendering:** XAML uses PDFium integration to render real PDF pages (React uses gradient placeholders)
- **Advanced search:** XAML has full-text search with highlighting (React has visual mockup only)
- **Keyboard shortcuts:** XAML has `KeyboardAccelerator` infrastructure (React uses basic keyboard events)

---

#### 3. BookmarksPanel

**Purpose:** Hierarchical tree view for PDF bookmarks/table of contents with expand/collapse functionality.

**File Paths:**
- React: `prototype/src/components/BookmarksPanel.tsx`
- React CSS: `prototype/src/components/BookmarksPanel.module.css`
- XAML: `src/FluentPDF.App/Controls/BookmarksPanel.xaml`
- XAML Code-behind: `src/FluentPDF.App/Controls/BookmarksPanel.xaml.cs`

**Props/Properties Mapping:**

| React Prop | XAML Property | Description |
|------------|---------------|-------------|
| `bookmarks: BookmarkNode[]` | `ItemsSource` (bound to ViewModel.Bookmarks) | Hierarchical bookmark tree |
| `isLoading?: boolean` | `ViewModel.IsLoading` | Loading state for bookmarks |
| `emptyMessage?: string` | `ViewModel.EmptyMessage` | Message shown when no bookmarks |
| `onNavigateToPage?: (pageNumber) => void` | `ItemInvoked` event handler | Callback when bookmark clicked |

**Side-by-Side Example: Recursive Tree Rendering**

**React:**
```tsx
// BookmarksPanel.tsx - Recursive component
const BookmarkTreeItem: React.FC<{node: BookmarkNode; level: number}> = ({node, level}) => {
  const [isExpanded, setIsExpanded] = useState(node.isExpanded ?? false);

  return (
    <div className={styles.treeItem}>
      <div style={{ paddingLeft: `${level * 16 + 8}px` }}>
        {node.children?.length > 0 && (
          <button onClick={() => setIsExpanded(!isExpanded)}>
            {isExpanded ? '▼' : '▶'}
          </button>
        )}
        <span>{node.title}</span>
      </div>
      {isExpanded && node.children?.map(child => (
        <BookmarkTreeItem key={child.title} node={child} level={level + 1} />
      ))}
    </div>
  );
};
```

**XAML:**
```xml
<!-- BookmarksPanel.xaml - TreeView with hierarchical template -->
<TreeView
    ItemsSource="{x:Bind ViewModel.Bookmarks, Mode=OneWay}"
    ItemTemplate="{StaticResource BookmarkNodeTemplate}"
    ItemInvoked="BookmarksTreeView_ItemInvoked">
</TreeView>

<!-- Hierarchical DataTemplate -->
<DataTemplate x:Key="BookmarkNodeTemplate" x:DataType="models:BookmarkNode">
    <TreeViewItem ItemsSource="{x:Bind Children, Mode=OneTime}">
        <StackPanel Orientation="Horizontal" Spacing="8">
            <FontIcon Glyph="&#xE8A5;" FontSize="14"/>
            <TextBlock Text="{x:Bind Title, Mode=OneTime}"/>
        </StackPanel>
    </TreeViewItem>
</DataTemplate>
```

**XAML-Specific Features:**
- **TreeView control:** XAML has built-in `TreeView` with automatic expand/collapse UI (React implements custom recursive rendering)
- **Data binding to hierarchical models:** XAML automatically handles tree binding with `ItemsSource` (React requires manual recursion)

---

### Views

#### 4. MainWindow

**Purpose:** Application shell with title bar and main content area.

**File Paths:**
- React: `prototype/src/components/MainWindow.tsx`
- React CSS: `prototype/src/components/layouts.module.css`
- XAML: `src/FluentPDF.App/Views/MainWindow.xaml`
- XAML Code-behind: `src/FluentPDF.App/Views/MainWindow.xaml.cs`

**Props/Properties Mapping:**

| React Prop | XAML Property | Description |
|------------|---------------|-------------|
| `children: ReactNode` | `Frame.Content` | Main content area |
| `title?: string` | `Title` property | Window title |

**Side-by-Side Example: Window Structure**

**React:**
```tsx
// MainWindow.tsx
export const MainWindow: React.FC<{children: ReactNode}> = ({children}) => (
  <div className={styles.mainWindow}>
    <div className={styles.titleBar}>
      <div className={styles.titleBarContent}>
        <span className={styles.appIcon}>📄</span>
        <span className={styles.appTitle}>FluentPDF</span>
      </div>
    </div>
    <div className={styles.contentArea}>
      {children}
    </div>
  </div>
);
```

**XAML:**
```xml
<!-- MainWindow.xaml -->
<Window
    x:Class="FluentPDF.App.Views.MainWindow"
    Title="FluentPDF">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <!-- Title bar customization -->
        <Grid Grid.Row="0" Height="48">
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Image Source="Assets/AppIcon.png"/>
                <TextBlock Text="FluentPDF"/>
            </StackPanel>
        </Grid>

        <!-- Main content -->
        <Frame Grid.Row="1" x:Name="ContentFrame"/>
    </Grid>
</Window>
```

**XAML-Specific Features:**
- **Native window chrome:** XAML integrates with Windows title bar and window controls (React uses CSS simulation)
- **Frame navigation:** XAML uses `Frame` with page navigation stack (React uses simple component composition)

---

#### 5. MainPage

**Purpose:** Main navigation page with menu for switching between app sections.

**File Paths:**
- React: `prototype/src/components/MainPage.tsx`
- React CSS: `prototype/src/components/layouts.module.css`
- XAML: `src/FluentPDF.App/Views/MainPage.xaml`
- XAML Code-behind: `src/FluentPDF.App/Views/MainPage.xaml.cs`

**Props/Properties Mapping:**

| React Prop | XAML Property | Description |
|------------|---------------|-------------|
| `currentView?: string` | `SelectedItem` (NavigationView) | Current selected navigation item |
| `onNavigate?: (view: string) => void` | `SelectionChanged` event | Navigation callback |
| `children: ReactNode` | `NavigationView.Content` | Main content area |

**Side-by-Side Example: Navigation Menu**

**React:**
```tsx
// MainPage.tsx
const [selectedView, setSelectedView] = useState('viewer');

return (
  <div className={styles.mainPage}>
    <nav className={styles.navigationMenu}>
      <button
        className={selectedView === 'viewer' ? styles.active : ''}
        onClick={() => setSelectedView('viewer')}
      >
        📄 Viewer
      </button>
      <button
        className={selectedView === 'conversion' ? styles.active : ''}
        onClick={() => setSelectedView('conversion')}
      >
        🔄 Convert
      </button>
    </nav>
    <div className={styles.pageContent}>
      {children}
    </div>
  </div>
);
```

**XAML:**
```xml
<!-- MainPage.xaml -->
<NavigationView
    SelectionChanged="NavigationView_SelectionChanged"
    IsBackButtonVisible="Collapsed"
    PaneDisplayMode="Left">
    <NavigationView.MenuItems>
        <NavigationViewItem Content="Viewer" Tag="viewer">
            <NavigationViewItem.Icon>
                <FontIcon Glyph="&#xE8A5;"/>
            </NavigationViewItem.Icon>
        </NavigationViewItem>
        <NavigationViewItem Content="Convert" Tag="conversion">
            <NavigationViewItem.Icon>
                <FontIcon Glyph="&#xE895;"/>
            </NavigationViewItem.Icon>
        </NavigationViewItem>
    </NavigationView.MenuItems>

    <Frame x:Name="ContentFrame"/>
</NavigationView>
```

**XAML-Specific Features:**
- **NavigationView control:** XAML has built-in Fluent Design navigation (React uses custom navigation)
- **Frame-based page navigation:** XAML supports back/forward navigation stack (React uses simple state toggling)

---

#### 6. PdfViewerPage

**Purpose:** Composite page layout containing thumbnails sidebar, main viewer, and bookmarks panel.

**File Paths:**
- React: `prototype/src/components/PdfViewerPage.tsx`
- React CSS: `prototype/src/components/layouts.module.css`
- XAML: `src/FluentPDF.App/Views/PdfViewerPage.xaml`
- XAML Code-behind: `src/FluentPDF.App/Views/PdfViewerPage.xaml.cs`

**Side-by-Side Example: Three-Panel Layout**

**React:**
```tsx
// PdfViewerPage.tsx
export const PdfViewerPage: React.FC = () => (
  <div className={styles.viewerPage}>
    <div className={styles.leftPanel}>
      <ThumbnailsSidebar thumbnails={dummyThumbnails} />
    </div>
    <div className={styles.centerPanel}>
      <PdfViewerControl document={dummyDocument} />
    </div>
    <div className={styles.rightPanel}>
      <BookmarksPanel bookmarks={dummyBookmarks} />
    </div>
  </div>
);
```

**XAML:**
```xml
<!-- PdfViewerPage.xaml -->
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="200"/>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="250"/>
    </Grid.ColumnDefinitions>

    <local:ThumbnailsSidebar Grid.Column="0"/>
    <local:PdfViewerControl Grid.Column="1"/>
    <local:BookmarksPanel Grid.Column="2"/>
</Grid>
```

---

### Dialogs

#### 7. WatermarkDialog

**Purpose:** Dialog for adding text watermarks to PDF pages.

**File Paths:**
- React: `prototype/src/components/dialogs/WatermarkDialog.tsx`
- React CSS: `prototype/src/components/dialogs/dialogs.module.css`
- XAML: `src/FluentPDF.App/Views/WatermarkDialog.xaml`
- XAML Code-behind: `src/FluentPDF.App/Views/WatermarkDialog.xaml.cs`

**Props/Properties Mapping:**

| React Prop | XAML Property | Description |
|------------|---------------|-------------|
| `isOpen: boolean` | `IsOpen` (ContentDialog) | Dialog visibility state |
| `onClose?: () => void` | `CloseButtonClick` event | Close callback |
| `onApply?: (config) => void` | `PrimaryButtonClick` event | Apply watermark callback |
| `watermarkText?: string` | `WatermarkText` (ViewModel) | Initial watermark text |

**Side-by-Side Example: Dialog Form**

**React:**
```tsx
// WatermarkDialog.tsx
export const WatermarkDialog: React.FC<{isOpen: boolean; onClose: () => void}> = ({isOpen, onClose}) => {
  const [text, setText] = useState('');
  const [opacity, setOpacity] = useState(0.5);

  if (!isOpen) return null;

  return (
    <div className={styles.dialogOverlay}>
      <div className={styles.dialogContent}>
        <h2>Add Watermark</h2>
        <input
          type="text"
          value={text}
          onChange={e => setText(e.target.value)}
          placeholder="Watermark text"
        />
        <label>
          Opacity: {Math.round(opacity * 100)}%
          <input
            type="range"
            min="0" max="1" step="0.1"
            value={opacity}
            onChange={e => setOpacity(parseFloat(e.target.value))}
          />
        </label>
        <div className={styles.dialogActions}>
          <button onClick={onClose}>Cancel</button>
          <button onClick={() => console.log('Apply', {text, opacity})}>Apply</button>
        </div>
      </div>
    </div>
  );
};
```

**XAML:**
```xml
<!-- WatermarkDialog.xaml -->
<ContentDialog
    x:Class="FluentPDF.App.Views.WatermarkDialog"
    Title="Add Watermark"
    PrimaryButtonText="Apply"
    CloseButtonText="Cancel"
    PrimaryButtonClick="ApplyButton_Click">

    <StackPanel Spacing="16">
        <TextBox
            Header="Watermark Text"
            PlaceholderText="Enter watermark text"
            Text="{x:Bind ViewModel.WatermarkText, Mode=TwoWay}"/>

        <StackPanel>
            <TextBlock Text="{x:Bind ViewModel.OpacityPercentage, Mode=OneWay}"/>
            <Slider
                Minimum="0" Maximum="100"
                Value="{x:Bind ViewModel.Opacity, Mode=TwoWay}"/>
        </StackPanel>
    </StackPanel>
</ContentDialog>
```

**XAML-Specific Features:**
- **ContentDialog control:** XAML has built-in modal dialog infrastructure (React uses custom overlay/portal)
- **Two-way data binding:** XAML uses `Mode=TwoWay` for form inputs (React uses controlled components with state)

---

#### 8. DeletePagesDialog

**Purpose:** Confirmation dialog for deleting PDF pages with range selection.

**File Paths:**
- React: `prototype/src/components/dialogs/DeletePagesDialog.tsx`
- React CSS: `prototype/src/components/dialogs/dialogs.module.css`
- XAML: `src/FluentPDF.App/Views/DeletePagesDialog.xaml`

**Side-by-Side Example: Page Range Input**

**React:**
```tsx
// DeletePagesDialog.tsx
const [pageRange, setPageRange] = useState('1-5');

return (
  <div className={styles.dialogContent}>
    <p>Delete pages from the PDF document</p>
    <input
      type="text"
      value={pageRange}
      onChange={e => setPageRange(e.target.value)}
      placeholder="e.g., 1-5, 8, 10-12"
    />
    <button onClick={() => console.log('Delete', pageRange)}>Delete</button>
  </div>
);
```

**XAML:**
```xml
<!-- DeletePagesDialog.xaml -->
<ContentDialog Title="Delete Pages" PrimaryButtonText="Delete">
    <StackPanel Spacing="12">
        <TextBlock Text="Delete pages from the PDF document"/>
        <TextBox
            Header="Page Range"
            PlaceholderText="e.g., 1-5, 8, 10-12"
            Text="{x:Bind ViewModel.PageRange, Mode=TwoWay}"/>
    </StackPanel>
</ContentDialog>
```

---

#### 9. ErrorDialog

**Purpose:** Modal dialog for displaying error messages and details.

**File Paths:**
- React: `prototype/src/components/dialogs/ErrorDialog.tsx`
- React CSS: `prototype/src/components/dialogs/dialogs.module.css`
- XAML: `src/FluentPDF.App/Views/ErrorDialog.xaml`

**Props/Properties Mapping:**

| React Prop | XAML Property | Description |
|------------|---------------|-------------|
| `isOpen: boolean` | `IsOpen` | Error dialog visibility |
| `title?: string` | `Title` | Error title/heading |
| `message: string` | `ViewModel.ErrorMessage` | Main error message |
| `details?: string` | `ViewModel.ErrorDetails` | Technical error details (stack trace) |
| `onClose?: () => void` | `CloseButtonClick` | Close callback |

---

#### 10. SettingsPage

**Purpose:** Application settings page with preferences and configuration options.

**File Paths:**
- React: `prototype/src/components/dialogs/SettingsPage.tsx`
- React CSS: `prototype/src/components/dialogs/dialogs.module.css`
- XAML: `src/FluentPDF.App/Views/SettingsPage.xaml`

**Side-by-Side Example: Settings Toggles**

**React:**
```tsx
// SettingsPage.tsx
const [darkMode, setDarkMode] = useState(false);
const [autoSave, setAutoSave] = useState(true);

return (
  <div className={styles.settingsPage}>
    <h2>Settings</h2>
    <label className={styles.settingRow}>
      <span>Dark Mode</span>
      <input
        type="checkbox"
        checked={darkMode}
        onChange={e => setDarkMode(e.target.checked)}
      />
    </label>
    <label className={styles.settingRow}>
      <span>Auto-save</span>
      <input
        type="checkbox"
        checked={autoSave}
        onChange={e => setAutoSave(e.target.checked)}
      />
    </label>
  </div>
);
```

**XAML:**
```xml
<!-- SettingsPage.xaml -->
<Page x:Class="FluentPDF.App.Views.SettingsPage">
    <StackPanel Spacing="16" Padding="24">
        <TextBlock Text="Settings" Style="{StaticResource TitleTextBlockStyle}"/>

        <ToggleSwitch
            Header="Dark Mode"
            IsOn="{x:Bind ViewModel.IsDarkMode, Mode=TwoWay}"/>

        <ToggleSwitch
            Header="Auto-save"
            IsOn="{x:Bind ViewModel.IsAutoSaveEnabled, Mode=TwoWay}"/>
    </StackPanel>
</Page>
```

**XAML-Specific Features:**
- **ToggleSwitch control:** XAML has Fluent Design toggle switches (React uses checkbox or custom toggle)
- **Settings persistence:** XAML typically uses `ApplicationData.LocalSettings` (React prototype has no persistence)

---

## Pattern Translation Guide

### Event Handling

**React Pattern:**
```tsx
interface ComponentProps {
  onSomeEvent?: (data: SomeType) => void;
}

const Component: React.FC<ComponentProps> = ({onSomeEvent}) => {
  const handleClick = () => {
    console.log('Event triggered');
    onSomeEvent?.(data);
  };

  return <button onClick={handleClick}>Click Me</button>;
};
```

**XAML Pattern:**
```xml
<!-- XAML -->
<Button Click="Button_Click" Content="Click Me"/>
```

```csharp
// Code-behind
private void Button_Click(object sender, RoutedEventArgs e)
{
    Debug.WriteLine("Event triggered");
    ViewModel.HandleClick(data);
}
```

---

### Conditional Rendering

**React Pattern:**
```tsx
{isLoading ? (
  <div className={styles.spinner} />
) : (
  <div className={styles.content}>{data}</div>
)}
```

**XAML Pattern:**
```xml
<ProgressRing
    IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}"
    Visibility="{x:Bind ViewModel.IsLoading, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}"/>

<Grid Visibility="{x:Bind ViewModel.IsLoading, Mode=OneWay, Converter={StaticResource InverseBoolToVisibilityConverter}}">
    <TextBlock Text="{x:Bind ViewModel.Data, Mode=OneWay}"/>
</Grid>
```

---

### List Rendering

**React Pattern:**
```tsx
{items.map(item => (
  <div key={item.id} className={styles.item}>
    {item.name}
  </div>
))}
```

**XAML Pattern:**
```xml
<ItemsRepeater ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}">
    <ItemsRepeater.ItemTemplate>
        <DataTemplate x:DataType="models:Item">
            <Grid>
                <TextBlock Text="{x:Bind Name, Mode=OneTime}"/>
            </Grid>
        </DataTemplate>
    </ItemsRepeater.ItemTemplate>
</ItemsRepeater>
```

---

### Styling

**React Pattern:**
```tsx
// Component.module.css
.button {
  background-color: var(--color-accent);
  padding: var(--spacing-md);
  border-radius: var(--border-radius-md);
}

// Component.tsx
<button className={styles.button}>Click</button>
```

**XAML Pattern:**
```xml
<!-- Using theme resources -->
<Button
    Background="{ThemeResource AccentFillColorDefaultBrush}"
    Padding="12"
    CornerRadius="4"
    Content="Click"/>

<!-- Or using generated design tokens -->
<Button
    Background="{StaticResource AccentColorBrush}"
    Padding="{StaticResource SpacingMedium}"
    CornerRadius="{StaticResource BorderRadiusMedium}"
    Content="Click"/>
```

---

## XAML-Specific Feature Simplifications

The React prototype intentionally simplifies or omits these XAML-specific features:

### 1. Data Binding Infrastructure

**XAML:** Uses `INotifyPropertyChanged`, `ObservableCollection`, `x:Bind`, `Mode=OneWay/TwoWay`
**React:** Uses simple props and state (no binding infrastructure)

**Why:** React's component model doesn't need XAML's binding system—props flow down, events flow up.

---

### 2. Drag-and-Drop

**XAML:** Full drag-and-drop with `CanDrag`, `DragStarting`, `DragOver`, `Drop`, `AllowDrop`
**React:** Stubbed with `console.log` (no drag-and-drop implementation)

**Why:** Drag-and-drop is complex and not needed for visual prototyping. Focus on layout and styling first.

**Translation Note:** When implementing in XAML, add drag-and-drop event handlers and logic after layout is finalized.

---

### 3. Context Menus

**XAML:** Uses `MenuFlyout` with `MenuFlyoutItem`, keyboard accelerators, submenus
**React:** Stubbed with `onContextMenu` console.log

**Why:** Context menus are feature-rich in XAML but not essential for visual design iteration.

**Translation Note:** When implementing in XAML, add full `MenuFlyout` with appropriate menu items and handlers.

---

### 4. Virtualization

**XAML:** `ItemsRepeater` with built-in UI virtualization for performance
**React:** Simple `.map()` rendering (no virtualization)

**Why:** React prototype uses small dummy datasets; virtualization not needed for prototyping.

**Translation Note:** XAML automatically handles virtualization with `ItemsRepeater`. No additional work needed.

---

### 5. Keyboard Accelerators

**XAML:** `KeyboardAccelerator` with `Key`, `Modifiers`, system-level shortcuts
**React:** Basic keyboard events (`onKeyDown`, `onKeyUp`)

**Why:** Keyboard shortcuts are non-visual features; omit from visual prototype.

**Translation Note:** Add `KeyboardAccelerator` elements to XAML buttons/menu items after porting layout.

---

### 6. Accessibility (ARIA/AutomationProperties)

**XAML:** `AutomationProperties.Name`, `AutomationProperties.AutomationId`, screen reader support
**React:** Basic ARIA attributes (`aria-label`, `role`, `tabIndex`)

**Why:** Full accessibility testing requires real integration; prototype focuses on visual structure.

**Translation Note:** Add comprehensive `AutomationProperties` to all XAML controls for production readiness.

---

### 7. PDF Rendering

**XAML:** Real PDF rendering via PDFium integration
**React:** Gradient placeholders and dummy content

**Why:** PDF rendering requires native integration; prototype validates layout without rendering complexity.

**Translation Note:** XAML implementation uses actual PDF rendering; no additional work needed beyond connecting to rendering service.

---

### 8. Navigation

**XAML:** `Frame` navigation with back/forward stack, `NavigationService`
**React:** Simple state toggling (`useState` for current view)

**Why:** Prototype doesn't need navigation history; simple view switching is sufficient.

**Translation Note:** XAML uses `Frame.Navigate(typeof(SomePage))` for proper page navigation.

---

## Translation Checklist

Use this checklist when translating a component from React to XAML (or vice versa):

### Phase 1: Structure Analysis
- [ ] **Identify component hierarchy** - Map React component tree to XAML control structure
- [ ] **List all props/properties** - Document props/DependencyProperties and their types
- [ ] **Identify event handlers** - Map React callbacks to XAML event handlers
- [ ] **Note conditional rendering** - Translate `{condition && <element>}` to `Visibility` bindings

### Phase 2: Design Tokens Application
- [ ] **Extract hardcoded values** - Replace magic numbers/colors with design token references
- [ ] **Map CSS variables to XAML resources** - Use token ResourceDictionary (`Tokens.xaml`)
- [ ] **Verify token values match** - Ensure visual consistency between React and XAML
- [ ] **Test token changes** - Modify token value, verify change reflects in both UIs

### Phase 3: Data Bindings (XAML only)
- [ ] **Create ViewModel properties** - Add `INotifyPropertyChanged` properties for all bound data
- [ ] **Set DataContext/x:Bind** - Wire ViewModel to view
- [ ] **Choose binding mode** - Use `OneTime`, `OneWay`, or `TwoWay` appropriately
- [ ] **Add value converters** - Implement `IValueConverter` for data transformations (e.g., `BoolToVisibilityConverter`)

### Phase 4: XAML-Specific Features
- [ ] **Add drag-and-drop** (if applicable) - Implement `DragStarting`, `Drop` handlers
- [ ] **Add context menus** (if applicable) - Define `MenuFlyout` with menu items
- [ ] **Add keyboard accelerators** - Define shortcuts with `KeyboardAccelerator`
- [ ] **Add accessibility** - Set `AutomationProperties.Name` and other a11y properties
- [ ] **Implement actual functionality** - Replace React stubs (console.log) with real logic

### Phase 5: Testing & Validation
- [ ] **Visual comparison** - Screenshot React vs XAML, verify pixel-perfect match
- [ ] **Interaction testing** - Test clicks, hovers, keyboard navigation
- [ ] **Responsive layout** - Test at different window sizes
- [ ] **Theme testing** - Verify light/dark mode support (XAML)
- [ ] **Performance testing** - Verify no performance regressions with large datasets

---

## Template for New Components

Use this template when documenting new component mappings:

```markdown
#### ComponentName

**Purpose:** [Brief description of component purpose and functionality]

**File Paths:**
- React: `prototype/src/components/ComponentName.tsx`
- React CSS: `prototype/src/components/ComponentName.module.css`
- XAML: `src/FluentPDF.App/[Controls|Views]/ComponentName.xaml`
- XAML Code-behind: `src/FluentPDF.App/[Controls|Views]/ComponentName.xaml.cs`

**Props/Properties Mapping:**

| React Prop | XAML Property | Description |
|------------|---------------|-------------|
| `propName: PropType` | `PropertyName` (DependencyProperty) | Description of property |
| `onEventName?: (data: Type) => void` | `EventName` event | Description of event |

**Side-by-Side Example: [Key Feature]**

**React:**
\`\`\`tsx
// ComponentName.tsx
export const ComponentName: React.FC<Props> = ({propName}) => (
  // ... implementation
);
\`\`\`

**XAML:**
\`\`\`xml
<!-- ComponentName.xaml -->
<UserControl ...>
  <!-- ... markup -->
</UserControl>
\`\`\`

**XAML-Specific Features:**
- **Feature 1:** Description of XAML-specific feature and how it's simplified in React
- **Feature 2:** ...

**Translation Notes:**
- Special considerations when translating between React and XAML
- Performance considerations
- Edge cases to watch out for
```

---

## Summary

This component mapping guide provides:
- **Comprehensive mappings** for all 18 FluentPDF components
- **Side-by-side code examples** showing React ↔ XAML patterns
- **Translation checklist** for systematic component porting
- **XAML feature simplification strategy** explaining what's omitted in React prototype
- **Template for new components** for extensibility

**Key Takeaways:**
1. React prototype focuses on **visual structure and layout** for rapid iteration
2. XAML-specific features (drag-and-drop, complex bindings) are **intentionally simplified** in React
3. Design tokens provide **single source of truth** for styling consistency
4. Use the **translation checklist** to systematically port components between frameworks
5. **Screenshot-driven workflow:** Design in React → Screenshot → Translate to XAML → Add WinUI 3 features

For detailed workflow instructions, see `docs/react-prototype-workflow.md`.
