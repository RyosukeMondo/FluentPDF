/**
 * Dummy PDF document generator for UI prototyping.
 * Provides realistic mock data without actual PDF rendering logic.
 */

import { PdfDocument, PdfPage, PageContentType, RotationAngle } from '../types/models';

/**
 * Configuration for dummy document generation.
 */
export interface DummyDocumentConfig {
  pageCount?: number;
  fileName?: string;
  fileSizeKB?: number;
  contentTypes?: PageContentType[];
}

/**
 * Generates a dummy PDF document with configurable parameters.
 */
export function generateDummyDocument(config: DummyDocumentConfig = {}): PdfDocument {
  const {
    pageCount = 25,
    fileName = 'Sample Document.pdf',
    fileSizeKB = 2500,
  } = config;

  return {
    filePath: `C:\\Documents\\${fileName}`,
    fileName,
    pageCount,
    loadedAt: new Date(),
    fileSizeBytes: fileSizeKB * 1024,
  };
}

/**
 * Generates dummy page metadata for a specific page number.
 */
export function generateDummyPage(pageNumber: number, _contentType?: PageContentType): PdfPage {
  // Vary page sizes for visual diversity
  const sizes = [
    { width: 612, height: 792 }, // Letter
    { width: 612, height: 1008 }, // Legal
    { width: 595, height: 842 }, // A4
  ];

  const size = sizes[pageNumber % sizes.length]!;

  return {
    pageNumber,
    width: size.width,
    height: size.height,
    rotation: RotationAngle.None,
  };
}

/**
 * Generates an array of dummy pages for a document.
 */
export function generateDummyPages(pageCount: number): PdfPage[] {
  return Array.from({ length: pageCount }, (_, i) => generateDummyPage(i + 1));
}

/**
 * Determines content type for visual variety based on page number.
 * Uses a pattern to create diverse content types across pages.
 */
export function getPageContentType(pageNumber: number): PageContentType {
  const pattern = pageNumber % 10;

  if (pattern === 0 || pattern === 5) return PageContentType.Image;
  if (pattern === 3 || pattern === 7) return PageContentType.Mixed;
  if (pattern === 4) return PageContentType.Form;
  if (pattern === 9) return PageContentType.Blank;

  return PageContentType.Text;
}

/**
 * Pre-defined sample documents for different scenarios.
 */
export const SampleDocuments = {
  short: generateDummyDocument({
    pageCount: 5,
    fileName: 'Quick Start Guide.pdf',
    fileSizeKB: 850,
  }),

  medium: generateDummyDocument({
    pageCount: 25,
    fileName: 'User Manual.pdf',
    fileSizeKB: 3200,
  }),

  long: generateDummyDocument({
    pageCount: 150,
    fileName: 'Complete Reference.pdf',
    fileSizeKB: 12500,
  }),

  forms: generateDummyDocument({
    pageCount: 8,
    fileName: 'Tax Form 2024.pdf',
    fileSizeKB: 1200,
  }),

  images: generateDummyDocument({
    pageCount: 12,
    fileName: 'Photo Album.pdf',
    fileSizeKB: 8500,
  }),
};
