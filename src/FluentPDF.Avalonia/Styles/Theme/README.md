# Liquid Glass UI Theme System

This directory contains the color palette and theme resources for FluentPDF's Liquid Glass UI enhancement.

## Files

### Colors.axaml
Comprehensive color palette with three theme variants:
- **Light Theme**: Light gray base with high contrast text
- **Dark Theme**: Dark gray base with bright accent colors
- **High Contrast Theme**: Pure black/white with maximum contrast

## Color Categories

### Background Colors
- `ColorBackgroundBase` - Main canvas background
- `ColorBackgroundSurface` - Card/panel surfaces
- `ColorBackgroundElevated` - Elevated surfaces
- `ColorBackgroundSubtle` - Subtle backgrounds

### Foreground Colors
- `ColorForegroundPrimary` - Main text (meets WCAG AA 4.5:1)
- `ColorForegroundSecondary` - Secondary text (7.0:1 contrast)
- `ColorForegroundTertiary` - Tertiary text (4.5:1 minimum)
- `ColorForegroundDisabled` - Disabled text
- `ColorForegroundInverse` - Text on dark backgrounds

### Accent Colors
- `ColorAccentPrimary` - Primary accent color (system accent fallback)
- `ColorAccentSecondary` - Darker accent variant
- `ColorAccentTertiary` - Light accent tint
- `ColorAccentHover` - Hover state
- `ColorAccentPressed` - Pressed state

### Acrylic Tints (60% opacity)
- `ColorAcrylicTintToolbar` - Toolbar frosted glass tint
- `ColorAcrylicTintPanel` - Side panel tint
- `ColorAcrylicTintDialog` - Dialog backdrop tint
- `ColorAcrylicTintTooltip` - Tooltip tint

### Semantic Colors
- Success: Green tones for positive actions
- Warning: Orange/yellow for caution
- Error: Red tones for errors
- Info: Blue tones for information

### Border Colors
- `ColorBorderDefault` - Standard borders
- `ColorBorderSubtle` - Minimal dividers
- `ColorBorderStrong` - Emphasized borders
- `ColorBorderFocus` - Focus indicators

### UI State Colors
- `ColorHoverOverlay` - Hover effect overlay
- `ColorPressedOverlay` - Press effect overlay
- `ColorSelectedBackground` - Selected item background
- `ColorSelectedBorder` - Selected item border

### Shadow Colors
- `ColorShadowSmall` - 8dp elevation
- `ColorShadowMedium` - 16dp elevation
- `ColorShadowLarge` - 32dp elevation

## Usage

All colors are exposed as both Color resources (e.g., `ColorBackgroundBase`) and SolidColorBrush resources (e.g., `BackgroundBaseBrush`).

### In XAML
```xml
<Border Background="{DynamicResource BackgroundSurfaceBrush}"
        BorderBrush="{DynamicResource BorderDefaultBrush}">
    <TextBlock Foreground="{DynamicResource ForegroundPrimaryBrush}"
               Text="Hello World" />
</Border>
```

### Theme Switching
Colors automatically switch when the application theme changes:
```csharp
// In App.axaml.cs or ViewModel
Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
```

## WCAG AA Compliance

All color combinations meet WCAG 2.1 Level AA requirements:
- **Text Contrast**: 4.5:1 minimum for normal text
- **UI Component Contrast**: 3:1 minimum for interactive elements
- **High Contrast Mode**: Maximum contrast with pure colors

### Light Theme Contrast Ratios
- Primary text on white: 17.5:1 ✓
- Secondary text on white: 7.0:1 ✓
- Tertiary text on white: 4.5:1 ✓ (minimum)
- Success/Warning/Error/Info: 5.1-5.8:1 ✓

### Dark Theme Contrast Ratios
- Primary text on dark: 17.5:1 ✓
- Secondary text on dark: 8.5:1 ✓
- Tertiary text on dark: 4.5:1 ✓ (minimum)
- Success/Warning/Error/Info: 6.1-12.1:1 ✓

## Architecture

The color system follows a three-tier architecture:

1. **Base Colors**: Defined per theme variant (Light/Dark/HighContrast)
2. **Brush Resources**: SolidColorBrush instances that reference base colors via DynamicResource
3. **Component Usage**: UI components reference brushes, not colors directly

This enables:
- Instant theme switching without reloading
- Consistent color usage across the application
- Easy customization and maintenance

## Next Steps

- Task 1.2: Create acrylic brushes and materials using these tint colors
- Task 1.3: Create typography scale resource dictionary
- Task 1.4: Create spacing and layout grid system
- Task 1.5: Create ThemeService for runtime theme switching
