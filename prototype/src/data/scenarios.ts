/**
 * Pre-configured PDF document scenarios for testing different UI states.
 * Each scenario combines a document configuration with bookmark presets.
 */

import { DummyDocumentConfig } from './dummyPdfDocument';
import { BookmarkNode } from '../types/models';
import { BookmarkPresets } from './dummyBookmarks';

export interface PdfScenario {
  id: string;
  name: string;
  description: string;
  documentConfig: DummyDocumentConfig;
  bookmarks: BookmarkNode[];
  emoji: string;
}

/**
 * Pre-defined PDF scenarios for rapid UI testing.
 */
export const PDF_SCENARIOS: PdfScenario[] = [
  {
    id: 'simple',
    name: 'Simple Document',
    description: '5 pages, no bookmarks - minimal complexity',
    emoji: '📄',
    documentConfig: {
      pageCount: 5,
      fileName: 'Quick Guide.pdf',
      fileSizeKB: 450,
    },
    bookmarks: BookmarkPresets.empty(),
  },

  {
    id: 'quick-start',
    name: 'Quick Start Guide',
    description: '10 pages, flat bookmark list',
    emoji: '🚀',
    documentConfig: {
      pageCount: 10,
      fileName: 'Quick Start Guide.pdf',
      fileSizeKB: 850,
    },
    bookmarks: BookmarkPresets.flat,
  },

  {
    id: 'user-manual',
    name: 'User Manual',
    description: '25 pages, standard 3-level TOC (partially expanded)',
    emoji: '📖',
    documentConfig: {
      pageCount: 25,
      fileName: 'User Manual.pdf',
      fileSizeKB: 3200,
    },
    bookmarks: BookmarkPresets.standard,
  },

  {
    id: 'technical-manual',
    name: 'Technical Manual',
    description: '50 pages, complex structured TOC (collapsed)',
    emoji: '⚙️',
    documentConfig: {
      pageCount: 50,
      fileName: 'Technical Reference.pdf',
      fileSizeKB: 5800,
    },
    bookmarks: BookmarkPresets.technicalManual,
  },

  {
    id: 'reference-book',
    name: 'Complete Reference',
    description: '150 pages, large hierarchical TOC (4 levels)',
    emoji: '📚',
    documentConfig: {
      pageCount: 150,
      fileName: 'Complete Reference Guide.pdf',
      fileSizeKB: 12500,
    },
    bookmarks: BookmarkPresets.large,
  },

  {
    id: 'photo-album',
    name: 'Photo Album',
    description: '12 pages, no bookmarks, image-heavy',
    emoji: '🖼️',
    documentConfig: {
      pageCount: 12,
      fileName: 'Photo Album.pdf',
      fileSizeKB: 8500,
    },
    bookmarks: BookmarkPresets.empty(),
  },

  {
    id: 'tax-forms',
    name: 'Tax Forms 2024',
    description: '8 pages, flat section list',
    emoji: '📝',
    documentConfig: {
      pageCount: 8,
      fileName: 'Tax Form 2024.pdf',
      fileSizeKB: 1200,
    },
    bookmarks: [
      { title: 'Instructions', pageNumber: 1, children: [], isExpanded: false },
      { title: 'Form 1040', pageNumber: 3, children: [], isExpanded: false },
      { title: 'Schedule A', pageNumber: 5, children: [], isExpanded: false },
      { title: 'Schedule B', pageNumber: 6, children: [], isExpanded: false },
      { title: 'Worksheets', pageNumber: 7, children: [], isExpanded: false },
    ],
  },

  {
    id: 'collapsed-manual',
    name: 'Collapsed Manual',
    description: '30 pages, all bookmarks collapsed',
    emoji: '➕',
    documentConfig: {
      pageCount: 30,
      fileName: 'Installation Guide.pdf',
      fileSizeKB: 2800,
    },
    bookmarks: BookmarkPresets.collapsed,
  },

  {
    id: 'expanded-manual',
    name: 'Fully Expanded Manual',
    description: '30 pages, all bookmarks expanded',
    emoji: '➖',
    documentConfig: {
      pageCount: 30,
      fileName: 'API Documentation.pdf',
      fileSizeKB: 4200,
    },
    bookmarks: BookmarkPresets.expanded,
  },

  {
    id: 'deep-nesting',
    name: 'Deep Nesting Test',
    description: '20 pages, 10-level deep bookmark nesting',
    emoji: '🔗',
    documentConfig: {
      pageCount: 20,
      fileName: 'Deep Structure.pdf',
      fileSizeKB: 1500,
    },
    bookmarks: BookmarkPresets.deeplyNested,
  },
];

/**
 * Get scenario by ID, with fallback to default.
 */
export function getScenarioById(id: string): PdfScenario {
  return PDF_SCENARIOS.find((s) => s.id === id) || PDF_SCENARIOS[2]!; // Default to user-manual
}

/**
 * Get a random scenario for testing.
 */
export function getRandomScenario(): PdfScenario {
  return PDF_SCENARIOS[Math.floor(Math.random() * PDF_SCENARIOS.length)]!;
}
