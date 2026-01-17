/**
 * Dummy bookmark generator for UI prototyping.
 * Creates hierarchical bookmark structures with realistic nesting.
 */

import { BookmarkNode } from '../types/models';

/**
 * Configuration for dummy bookmark generation.
 */
export interface DummyBookmarkConfig {
  maxDepth?: number;
  entriesPerLevel?: number;
  expandAll?: boolean;
}

/**
 * Generates a hierarchical bookmark tree with configurable depth.
 */
export function generateDummyBookmarks(config: DummyBookmarkConfig = {}): BookmarkNode[] {
  const { maxDepth = 3, entriesPerLevel = 4, expandAll = false } = config;

  const chapters = [
    'Introduction',
    'Getting Started',
    'Core Concepts',
    'Advanced Features',
    'Troubleshooting',
    'API Reference',
    'Appendix',
  ];

  const sections = [
    'Overview',
    'Installation',
    'Configuration',
    'Usage',
    'Examples',
    'Best Practices',
  ];

  const subsections = [
    'Prerequisites',
    'Step-by-Step Guide',
    'Common Issues',
    'Tips and Tricks',
    'Performance Optimization',
  ];

  let currentPage = 1;

  const bookmarks: BookmarkNode[] = [];

  // Generate chapters (level 1)
  for (let i = 0; i < Math.min(chapters.length, entriesPerLevel); i++) {
    const chapterNode: BookmarkNode = {
      title: `${i + 1}. ${chapters[i]}`,
      pageNumber: currentPage,
      children: [],
      isExpanded: expandAll || i === 0, // Expand first chapter by default
    };

    currentPage += 5;

    // Generate sections (level 2)
    if (maxDepth >= 2) {
      for (let j = 0; j < Math.min(sections.length, entriesPerLevel - 1); j++) {
        const sectionNode: BookmarkNode = {
          title: `${i + 1}.${j + 1} ${sections[j]}`,
          pageNumber: currentPage,
          children: [],
          isExpanded: expandAll || (i === 0 && j === 0), // Expand first section of first chapter
        };

        currentPage += 3;

        // Generate subsections (level 3)
        if (maxDepth >= 3) {
          for (let k = 0; k < Math.min(subsections.length, entriesPerLevel - 2); k++) {
            const subsectionNode: BookmarkNode = {
              title: `${i + 1}.${j + 1}.${k + 1} ${subsections[k]}`,
              pageNumber: currentPage,
              children: [],
              isExpanded: expandAll,
            };

            currentPage += 2;

            // Optional level 4 (only for first subsection to demonstrate deep nesting)
            if (maxDepth >= 4 && k === 0 && j === 0 && i === 0) {
              for (let m = 0; m < 2; m++) {
                subsectionNode.children.push({
                  title: `${i + 1}.${j + 1}.${k + 1}.${m + 1} Additional Details`,
                  pageNumber: currentPage,
                  children: [],
                  isExpanded: expandAll,
                });
                currentPage += 1;
              }
            }

            sectionNode.children.push(subsectionNode);
          }
        }

        chapterNode.children.push(sectionNode);
      }
    }

    bookmarks.push(chapterNode);
  }

  return bookmarks;
}

/**
 * Generates a flat bookmark structure (no nesting).
 */
export function generateFlatBookmarks(count: number = 10): BookmarkNode[] {
  return Array.from({ length: count }, (_, i) => ({
    title: `Page ${i + 1}`,
    pageNumber: i + 1,
    children: [],
    isExpanded: false,
  }));
}

/**
 * Generates a deeply nested bookmark structure for testing scroll/collapse.
 */
export function generateDeeplyNestedBookmarks(): BookmarkNode[] {
  const root: BookmarkNode = {
    title: 'Chapter 1: Introduction',
    pageNumber: 1,
    children: [],
    isExpanded: true,
  };

  let current = root;
  let pageNumber = 2;

  // Create a chain of 10 nested levels
  for (let i = 0; i < 10; i++) {
    const child: BookmarkNode = {
      title: `Section 1.${'.1'.repeat(i + 1)} - Level ${i + 2}`,
      pageNumber: pageNumber++,
      children: [],
      isExpanded: true,
    };
    current.children.push(child);
    current = child;
  }

  return [root];
}

/**
 * Recursively counts total bookmark nodes in a tree.
 */
export function countBookmarkNodes(bookmarks: BookmarkNode[]): number {
  let count = 0;
  for (const bookmark of bookmarks) {
    count += 1;
    count += countBookmarkNodes(bookmark.children);
  }
  return count;
}

/**
 * Pre-configured bookmark sets for different scenarios.
 */
export const BookmarkPresets = {
  /**
   * Empty document with no bookmarks.
   */
  empty: (): BookmarkNode[] => [],

  /**
   * Simple flat list of bookmarks.
   */
  flat: generateFlatBookmarks(15),

  /**
   * Standard 3-level hierarchy (default).
   */
  standard: generateDummyBookmarks({
    maxDepth: 3,
    entriesPerLevel: 4,
    expandAll: false,
  }),

  /**
   * Collapsed hierarchy (all nodes collapsed).
   */
  collapsed: generateDummyBookmarks({
    maxDepth: 3,
    entriesPerLevel: 4,
    expandAll: false,
  }).map((node) => ({ ...node, isExpanded: false })),

  /**
   * Fully expanded hierarchy.
   */
  expanded: generateDummyBookmarks({
    maxDepth: 3,
    entriesPerLevel: 4,
    expandAll: true,
  }),

  /**
   * Deep nesting for stress testing.
   */
  deeplyNested: generateDeeplyNestedBookmarks(),

  /**
   * Large document with many chapters.
   */
  large: generateDummyBookmarks({
    maxDepth: 4,
    entriesPerLevel: 6,
    expandAll: false,
  }),

  /**
   * Technical manual structure.
   */
  technicalManual: [
    {
      title: 'Table of Contents',
      pageNumber: 2,
      children: [],
      isExpanded: false,
    },
    {
      title: 'I. Introduction',
      pageNumber: 5,
      children: [
        { title: 'About This Manual', pageNumber: 5, children: [], isExpanded: false },
        { title: 'Conventions Used', pageNumber: 7, children: [], isExpanded: false },
      ],
      isExpanded: true,
    },
    {
      title: 'II. Installation',
      pageNumber: 10,
      children: [
        { title: 'System Requirements', pageNumber: 10, children: [], isExpanded: false },
        {
          title: 'Installation Steps',
          pageNumber: 12,
          children: [
            { title: 'Windows', pageNumber: 12, children: [], isExpanded: false },
            { title: 'macOS', pageNumber: 15, children: [], isExpanded: false },
            { title: 'Linux', pageNumber: 18, children: [], isExpanded: false },
          ],
          isExpanded: true,
        },
        { title: 'Post-Installation', pageNumber: 21, children: [], isExpanded: false },
      ],
      isExpanded: true,
    },
    {
      title: 'III. User Guide',
      pageNumber: 25,
      children: [
        { title: 'Basic Operations', pageNumber: 25, children: [], isExpanded: false },
        { title: 'Advanced Features', pageNumber: 40, children: [], isExpanded: false },
      ],
      isExpanded: false,
    },
    {
      title: 'IV. Reference',
      pageNumber: 80,
      children: [
        { title: 'Glossary', pageNumber: 80, children: [], isExpanded: false },
        { title: 'Index', pageNumber: 95, children: [], isExpanded: false },
      ],
      isExpanded: false,
    },
  ] as BookmarkNode[],
};
