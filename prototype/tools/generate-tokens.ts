#!/usr/bin/env node

/**
 * Design Token Generator
 *
 * Validates design tokens JSON against schema and generates:
 * - CSS custom properties for React/web (prototype/src/styles/tokens.css)
 * - XAML ResourceDictionary for WinUI 3 (design-tokens/Tokens.xaml)
 *
 * Usage: npm run generate-tokens
 */

import * as fs from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';
import Ajv, { type ValidateFunction } from 'ajv';

// ES module __dirname equivalent
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Types for design tokens
interface DesignTokens {
  colors: {
    primary: string;
    white: string;
    black: string;
    background: {
      card: string;
      layer: string;
    };
    text: {
      primary: string;
      secondary: string;
    };
  };
  spacing: {
    xs: number;
    sm: number;
    md: number;
    lg: number;
    buttonPadding: {
      horizontal: number;
      vertical: number;
    };
  };
  typography: {
    fontSize: {
      base: number;
      caption: number;
    };
    fontWeight: {
      normal: number;
      semibold: number;
    };
  };
  borders: {
    radius: {
      sm: number;
      md: number;
    };
    width: {
      thin: number;
      thick: number;
    };
  };
  sizes?: {
    thumbnail?: {
      width: number;
      height: number;
    };
    thumbnailContainer?: {
      width: number;
      height: number;
    };
  };
}

// File paths (relative to project root)
const PROJECT_ROOT = path.resolve(__dirname, '..', '..');
const TOKENS_JSON_PATH = path.join(PROJECT_ROOT, 'design-tokens', 'tokens.json');
const SCHEMA_JSON_PATH = path.join(PROJECT_ROOT, 'design-tokens', 'tokens.schema.json');
const OUTPUT_CSS_PATH = path.join(PROJECT_ROOT, 'prototype', 'src', 'styles', 'tokens.css');
const OUTPUT_XAML_PATH = path.join(PROJECT_ROOT, 'design-tokens', 'Tokens.xaml');

/**
 * Load and parse JSON file
 */
function loadJSON<T>(filePath: string): T {
  try {
    const content = fs.readFileSync(filePath, 'utf-8');
    return JSON.parse(content) as T;
  } catch (error) {
    if (error instanceof Error) {
      throw new Error(`Failed to load ${filePath}: ${error.message}`);
    }
    throw error;
  }
}

/**
 * Validate tokens against JSON schema
 */
function validateTokens(tokens: unknown, schema: object): tokens is DesignTokens {
  const ajv = new Ajv({ allErrors: true });
  const validate: ValidateFunction = ajv.compile(schema);
  const valid = validate(tokens);

  if (!valid) {
    const errors = validate.errors?.map(err => {
      return `  - ${err.instancePath || 'root'}: ${err.message}`;
    }).join('\n');
    throw new Error(`Token validation failed:\n${errors}`);
  }

  return true;
}

/**
 * Generate CSS custom properties from tokens
 */
function generateCSS(tokens: DesignTokens): string {
  const lines: string[] = [];

  lines.push('/**');
  lines.push(' * Design Tokens - CSS Custom Properties');
  lines.push(' * ');
  lines.push(' * ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY');
  lines.push(' * ');
  lines.push(' * This file is generated from design-tokens/tokens.json');
  lines.push(' * To modify tokens, edit tokens.json and run: npm run generate-tokens');
  lines.push(' * ');
  lines.push(' * Generated: ' + new Date().toISOString());
  lines.push(' */');
  lines.push('');
  lines.push(':root {');
  lines.push('  /* Colors */');
  lines.push(`  --color-primary: ${tokens.colors.primary};`);
  lines.push(`  --color-white: ${tokens.colors.white};`);
  lines.push(`  --color-black: ${tokens.colors.black};`);
  lines.push(`  --color-background-card: ${tokens.colors.background.card};`);
  lines.push(`  --color-background-layer: ${tokens.colors.background.layer};`);
  lines.push(`  --color-text-primary: ${tokens.colors.text.primary};`);
  lines.push(`  --color-text-secondary: ${tokens.colors.text.secondary};`);
  lines.push('');
  lines.push('  /* Spacing */');
  lines.push(`  --spacing-xs: ${tokens.spacing.xs}px;`);
  lines.push(`  --spacing-sm: ${tokens.spacing.sm}px;`);
  lines.push(`  --spacing-md: ${tokens.spacing.md}px;`);
  lines.push(`  --spacing-lg: ${tokens.spacing.lg}px;`);
  lines.push(`  --spacing-button-padding-horizontal: ${tokens.spacing.buttonPadding.horizontal}px;`);
  lines.push(`  --spacing-button-padding-vertical: ${tokens.spacing.buttonPadding.vertical}px;`);
  lines.push('');
  lines.push('  /* Typography */');
  lines.push(`  --font-size-base: ${tokens.typography.fontSize.base}px;`);
  lines.push(`  --font-size-caption: ${tokens.typography.fontSize.caption}px;`);
  lines.push(`  --font-weight-normal: ${tokens.typography.fontWeight.normal};`);
  lines.push(`  --font-weight-semibold: ${tokens.typography.fontWeight.semibold};`);
  lines.push('');
  lines.push('  /* Borders */');
  lines.push(`  --border-radius-sm: ${tokens.borders.radius.sm}px;`);
  lines.push(`  --border-radius-md: ${tokens.borders.radius.md}px;`);
  lines.push(`  --border-width-thin: ${tokens.borders.width.thin}px;`);
  lines.push(`  --border-width-thick: ${tokens.borders.width.thick}px;`);

  if (tokens.sizes) {
    lines.push('');
    lines.push('  /* Sizes */');
    if (tokens.sizes.thumbnail) {
      lines.push(`  --size-thumbnail-width: ${tokens.sizes.thumbnail.width}px;`);
      lines.push(`  --size-thumbnail-height: ${tokens.sizes.thumbnail.height}px;`);
    }
    if (tokens.sizes.thumbnailContainer) {
      lines.push(`  --size-thumbnail-container-width: ${tokens.sizes.thumbnailContainer.width}px;`);
      lines.push(`  --size-thumbnail-container-height: ${tokens.sizes.thumbnailContainer.height}px;`);
    }
  }

  lines.push('}');
  lines.push('');

  return lines.join('\n');
}

/**
 * Generate XAML ResourceDictionary from tokens
 */
function generateXAML(tokens: DesignTokens): string {
  const lines: string[] = [];

  lines.push('<?xml version="1.0" encoding="utf-8"?>');
  lines.push('<!--');
  lines.push('  Design Tokens - XAML ResourceDictionary');
  lines.push('  ');
  lines.push('  ⚠️  AUTO-GENERATED FILE - DO NOT EDIT MANUALLY');
  lines.push('  ');
  lines.push('  This file is generated from design-tokens/tokens.json');
  lines.push('  To modify tokens, edit tokens.json and run: npm run generate-tokens');
  lines.push('  ');
  lines.push('  Generated: ' + new Date().toISOString());
  lines.push('-->');
  lines.push('<ResourceDictionary');
  lines.push('    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"');
  lines.push('    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">');
  lines.push('');
  lines.push('    <!-- Colors -->');
  lines.push(`    <Color x:Key="Primary">${tokens.colors.primary}</Color>`);
  lines.push(`    <Color x:Key="White">${tokens.colors.white}</Color>`);
  lines.push(`    <Color x:Key="Black">${tokens.colors.black}</Color>`);
  lines.push(`    <Color x:Key="BackgroundCard">${tokens.colors.background.card}</Color>`);
  lines.push(`    <Color x:Key="BackgroundLayer">${tokens.colors.background.layer}</Color>`);
  lines.push(`    <Color x:Key="TextPrimary">${tokens.colors.text.primary}</Color>`);
  lines.push(`    <Color x:Key="TextSecondary">${tokens.colors.text.secondary}</Color>`);
  lines.push('');
  lines.push('    <!-- Color Brushes -->');
  lines.push('    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource Primary}" />');
  lines.push('    <SolidColorBrush x:Key="WhiteBrush" Color="{StaticResource White}" />');
  lines.push('    <SolidColorBrush x:Key="BlackBrush" Color="{StaticResource Black}" />');
  lines.push('    <SolidColorBrush x:Key="BackgroundCardBrush" Color="{StaticResource BackgroundCard}" />');
  lines.push('    <SolidColorBrush x:Key="BackgroundLayerBrush" Color="{StaticResource BackgroundLayer}" />');
  lines.push('    <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimary}" />');
  lines.push('    <SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondary}" />');
  lines.push('');
  lines.push('    <!-- Spacing (Double values for Thickness) -->');
  lines.push(`    <x:Double x:Key="SpacingXs">${tokens.spacing.xs}</x:Double>`);
  lines.push(`    <x:Double x:Key="SpacingSm">${tokens.spacing.sm}</x:Double>`);
  lines.push(`    <x:Double x:Key="SpacingMd">${tokens.spacing.md}</x:Double>`);
  lines.push(`    <x:Double x:Key="SpacingLg">${tokens.spacing.lg}</x:Double>`);
  lines.push(`    <Thickness x:Key="ButtonPadding">${tokens.spacing.buttonPadding.horizontal},${tokens.spacing.buttonPadding.vertical}</Thickness>`);
  lines.push('');
  lines.push('    <!-- Typography -->');
  lines.push(`    <x:Double x:Key="FontSizeBase">${tokens.typography.fontSize.base}</x:Double>`);
  lines.push(`    <x:Double x:Key="FontSizeCaption">${tokens.typography.fontSize.caption}</x:Double>`);
  lines.push('');
  lines.push('    <!-- Borders -->');
  lines.push(`    <CornerRadius x:Key="BorderRadiusSm">${tokens.borders.radius.sm}</CornerRadius>`);
  lines.push(`    <CornerRadius x:Key="BorderRadiusMd">${tokens.borders.radius.md}</CornerRadius>`);
  lines.push(`    <x:Double x:Key="BorderWidthThin">${tokens.borders.width.thin}</x:Double>`);
  lines.push(`    <x:Double x:Key="BorderWidthThick">${tokens.borders.width.thick}</x:Double>`);

  if (tokens.sizes) {
    lines.push('');
    lines.push('    <!-- Sizes -->');
    if (tokens.sizes.thumbnail) {
      lines.push(`    <x:Double x:Key="ThumbnailWidth">${tokens.sizes.thumbnail.width}</x:Double>`);
      lines.push(`    <x:Double x:Key="ThumbnailHeight">${tokens.sizes.thumbnail.height}</x:Double>`);
    }
    if (tokens.sizes.thumbnailContainer) {
      lines.push(`    <x:Double x:Key="ThumbnailContainerWidth">${tokens.sizes.thumbnailContainer.width}</x:Double>`);
      lines.push(`    <x:Double x:Key="ThumbnailContainerHeight">${tokens.sizes.thumbnailContainer.height}</x:Double>`);
    }
  }

  lines.push('</ResourceDictionary>');
  lines.push('');

  return lines.join('\n');
}

/**
 * Ensure directory exists
 */
function ensureDirectoryExists(filePath: string): void {
  const dir = path.dirname(filePath);
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
}

/**
 * Main execution
 */
function main(): void {
  console.log('🎨 Generating design tokens...\n');

  try {
    // Load tokens and schema
    console.log('📖 Loading tokens.json...');
    const tokens = loadJSON<unknown>(TOKENS_JSON_PATH);

    console.log('📖 Loading tokens.schema.json...');
    const schema = loadJSON<object>(SCHEMA_JSON_PATH);

    // Validate
    console.log('✓ Validating tokens against schema...');
    if (!validateTokens(tokens, schema)) {
      throw new Error('Token validation failed');
    }
    console.log('✓ Validation successful\n');

    // Generate CSS
    console.log('🔧 Generating CSS custom properties...');
    const css = generateCSS(tokens);
    ensureDirectoryExists(OUTPUT_CSS_PATH);
    fs.writeFileSync(OUTPUT_CSS_PATH, css, 'utf-8');
    console.log(`✓ Generated: ${path.relative(PROJECT_ROOT, OUTPUT_CSS_PATH)}\n`);

    // Generate XAML
    console.log('🔧 Generating XAML ResourceDictionary...');
    const xaml = generateXAML(tokens);
    ensureDirectoryExists(OUTPUT_XAML_PATH);
    fs.writeFileSync(OUTPUT_XAML_PATH, xaml, 'utf-8');
    console.log(`✓ Generated: ${path.relative(PROJECT_ROOT, OUTPUT_XAML_PATH)}\n`);

    console.log('✅ Token generation complete!\n');
    console.log('Next steps:');
    console.log('  - React: tokens.css is auto-imported, changes will hot-reload');
    console.log('  - XAML: Copy Tokens.xaml to src/FluentPDF.App/ and rebuild');
  } catch (error) {
    console.error('❌ Token generation failed:\n');
    if (error instanceof Error) {
      console.error(error.message);
    } else {
      console.error(String(error));
    }
    process.exit(1);
  }
}

// Run if executed directly (ES module check)
const isMainModule = import.meta.url === `file://${process.argv[1].replace(/\\/g, '/')}`;
if (isMainModule || process.argv[1]?.endsWith('generate-tokens.ts')) {
  main();
}

export { generateCSS, generateXAML, validateTokens, type DesignTokens };
