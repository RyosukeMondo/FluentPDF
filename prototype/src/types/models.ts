/**
 * TypeScript type definitions for FluentPDF domain models.
 * Simplified for UI prototyping - mirrors core C# models from FluentPDF.Core.
 */

/**
 * Represents a loaded PDF document with metadata.
 */
export interface PdfDocument {
  filePath: string;
  pageCount: number;
  loadedAt: Date;
  fileSizeBytes: number;
  fileName: string;
}

/**
 * Represents a single page in a PDF document.
 */
export interface PdfPage {
  pageNumber: number; // 1-based
  width: number; // in points
  height: number; // in points
  rotation: RotationAngle;
}

/**
 * Rotation angle for PDF pages.
 */
export enum RotationAngle {
  None = 0,
  Rotate90 = 90,
  Rotate180 = 180,
  Rotate270 = 270,
}

/**
 * Represents a thumbnail item for the thumbnails sidebar.
 */
export interface ThumbnailItem {
  pageNumber: number; // 1-based
  thumbnail: string | null; // data URL or null if not loaded
  isLoading: boolean;
  isSelected: boolean;
  width: number;
  height: number;
}

/**
 * Represents a bookmark node in a hierarchical bookmark tree.
 */
export interface BookmarkNode {
  title: string;
  pageNumber?: number; // 1-based, optional destination
  x?: number;
  y?: number;
  children: BookmarkNode[];
  isExpanded?: boolean; // UI state for tree expansion
}

/**
 * Form field type enumeration.
 */
export enum FormFieldType {
  Text = 'text',
  Checkbox = 'checkbox',
  RadioButton = 'radio',
  ComboBox = 'combobox',
  ListBox = 'listbox',
  PushButton = 'button',
  Signature = 'signature',
}

/**
 * Represents a form field in a PDF document.
 */
export interface PdfFormField {
  name: string;
  type: FormFieldType;
  pageNumber: number; // 1-based
  bounds: PdfRectangle;
  tabOrder: number;
  value?: string;
  isChecked?: boolean;
  isRequired: boolean;
  isReadOnly: boolean;
  maxLength?: number;
  formatMask?: string;
  groupName?: string; // for radio buttons
}

/**
 * Represents a rectangle in PDF coordinates.
 */
export interface PdfRectangle {
  left: number;
  bottom: number;
  right: number;
  top: number;
}

/**
 * Search match result.
 */
export interface SearchMatch {
  pageNumber: number; // 0-based in C# model, but we'll use 1-based for UI consistency
  charIndex: number;
  matchLength: number;
  contextBefore: string;
  contextAfter: string;
  matchedText: string;
}

/**
 * Annotation types.
 */
export enum AnnotationType {
  Text = 'text',
  Highlight = 'highlight',
  Underline = 'underline',
  Strikeout = 'strikeout',
  Ink = 'ink',
  Square = 'square',
  Circle = 'circle',
}

/**
 * Represents an annotation in a PDF.
 */
export interface Annotation {
  id: string;
  type: AnnotationType;
  pageNumber: number; // 1-based
  bounds: PdfRectangle;
  contents: string;
  author: string;
  createdAt: Date;
  color: string; // hex color
}

/**
 * Page size presets for blank page insertion.
 */
export enum PageSize {
  Letter = 'letter',
  Legal = 'legal',
  A4 = 'a4',
  A5 = 'a5',
  Tabloid = 'tabloid',
}

/**
 * Content type for visual variety in dummy data.
 */
export enum PageContentType {
  Text = 'text',
  Image = 'image',
  Mixed = 'mixed',
  Form = 'form',
  Blank = 'blank',
}

/**
 * Loading state for async operations.
 */
export enum LoadingState {
  Idle = 'idle',
  Loading = 'loading',
  Success = 'success',
  Error = 'error',
}
