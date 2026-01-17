# FluentPDF Design Tokens

This directory contains the design token system for FluentPDF - a **single source of truth** for styling shared between React (prototype) and XAML (WinUI 3 app).

## What are Design Tokens?

Design tokens are named design decisions (colors, spacing, typography, etc.) stored in a platform-agnostic format (JSON). They're transformed into platform-specific formats:
- **CSS custom properties** for React/web (`--color-primary: #512BD4`)
- **XAML ResourceDictionary** for WinUI 3 (`<Color x:Key="Primary">#512BD4</Color>`)

## File Structure

```
design-tokens/
├── tokens.json          # Source of truth (committed to Git)
├── tokens.schema.json   # JSON Schema for validation
└── README.md           # This file
```

Generated files (not committed):
- `prototype/src/styles/tokens.css` - CSS custom properties
- `design-tokens/Tokens.xaml` - WinUI 3 ResourceDictionary

## Token Categories

### Colors
- **primary**: Brand/accent color (`#512BD4`)
- **white**, **black**: Base colors
- **background.card**: Card background color
- **background.layer**: Loading placeholder background
- **text.primary**: Primary text color
- **text.secondary**: Secondary/muted text color

### Spacing
Scale based on 4px grid:
- **xs**: 4px - Small gaps, margins
- **sm**: 8px - Component spacing (thumbnail items, stack panels)
- **md**: 16px - Section spacing
- **lg**: 24px - Large section spacing
- **buttonPadding**: Horizontal (14px) and vertical (10px) button padding

### Typography
- **fontSize.base**: 14px - Default UI text
- **fontSize.caption**: 12px - Small labels (page numbers)
- **fontWeight.normal**: 400 - Regular text
- **fontWeight.semibold**: 600 - Emphasized text

### Borders
- **radius.sm**: 4px - Small components (borders, indicators)
- **radius.md**: 8px - Buttons, cards
- **width.thin**: 1px - Default borders
- **width.thick**: 3px - Selection indicator

### Sizes
- **thumbnail**: 150×190px - PDF page thumbnail dimensions
- **thumbnailContainer**: 150×220px - Thumbnail + label container

## Token Schema

Tokens are validated against `tokens.schema.json` using JSON Schema Draft-07. The schema enforces:
- **Type safety**: Colors must be hex format `#RRGGBB`, spacing/sizes must be integers
- **Required fields**: All core token categories must be present
- **Valid ranges**: Font weights limited to standard CSS values (100-900)
- **Extensibility**: New token types can be added following the schema structure

## Usage Workflow

### 1. Modifying Tokens

Edit `tokens.json` directly:

```json
{
  "colors": {
    "primary": "#0078D4"  // Change accent color
  }
}
```

### 2. Regenerating Output Files

Run the token generator from the prototype directory:

```bash
cd prototype
npm run generate-tokens
```

This validates `tokens.json` against the schema and generates:
- `prototype/src/styles/tokens.css`
- `design-tokens/Tokens.xaml`

### 3. Applying Changes

**React:**
- CSS custom properties auto-update via hot reload
- No build restart needed

**XAML:**
- Copy `Tokens.xaml` to `src/FluentPDF.App/`
- Add to `App.xaml` merged dictionaries (if not already present)
- Rebuild WinUI 3 project

## Adding New Tokens

1. **Update schema** (`tokens.schema.json`):
   ```json
   "shadows": {
     "type": "object",
     "properties": {
       "sm": { "type": "string", "pattern": "^\\d+px \\d+px \\d+px rgba\\(.*\\)$" }
     }
   }
   ```

2. **Add values** (`tokens.json`):
   ```json
   "shadows": {
     "sm": "0px 2px 4px rgba(0, 0, 0, 0.1)"
   }
   ```

3. **Update generator** (`prototype/tools/generate-tokens.ts`):
   - Add CSS generation logic for new token type
   - Add XAML generation logic (if applicable)

4. **Regenerate**: `npm run generate-tokens`

## Design Token Extraction

Current tokens were extracted from:
- **App.xaml** (lines 12-16): Primary color, white, black, font size
- **ThumbnailsSidebar.xaml** (lines 33, 88-116, 114, 165): Spacing (4px, 8px), thumbnail sizes (150×220px), border thickness (3px), corner radius (4px)
- **WinUI 3 theme resources**: `AccentFillColorDefaultBrush`, `CardBackgroundFillColorDefaultBrush`, `LayerFillColorDefaultBrush`, `TextFillColorSecondaryBrush`

## Validation

Tokens are validated on generation:
- JSON syntax must be valid
- Must conform to `tokens.schema.json`
- Hex colors must be 6-digit format (`#RRGGBB`)
- Spacing/size values must be non-negative integers

Generator script will fail with clear error messages if validation fails.

## Best Practices

1. **Never edit generated files** (`tokens.css`, `Tokens.xaml`) - they'll be overwritten
2. **Commit `tokens.json`** - it's the source of truth
3. **Don't commit generated files** - they're in `.gitignore`
4. **Use semantic names** - `spacing.sm` not `spacing.8px`
5. **Document new tokens** - add descriptions in schema
6. **Test in both platforms** - verify React and XAML visual parity

## Troubleshooting

**"Schema validation failed"**
- Check JSON syntax (trailing commas, quotes)
- Verify color format is `#RRGGBB` (not `#RGB` or `rgb()`)
- Ensure required fields are present

**"Generated CSS not working"**
- Restart Vite dev server
- Check browser DevTools for CSS variable values
- Verify `tokens.css` is imported in React app

**"XAML won't compile"**
- Check for invalid XAML characters in token names
- Verify resource keys are PascalCase
- Ensure `Tokens.xaml` namespace declarations are correct

## Further Reading

- [Design Tokens W3C Community Group](https://www.w3.org/community/design-tokens/)
- [WinUI 3 Theming Guide](https://learn.microsoft.com/en-us/windows/apps/design/style/xaml-theme-resources)
- [CSS Custom Properties (MDN)](https://developer.mozilla.org/en-US/docs/Web/CSS/Using_CSS_custom_properties)
