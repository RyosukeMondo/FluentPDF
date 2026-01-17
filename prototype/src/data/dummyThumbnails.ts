/**
 * Dummy thumbnail generator for UI prototyping.
 * Creates thumbnail items with gradient placeholders indicating content types.
 */

import { ThumbnailItem, PageContentType } from '../types/models';
import { getPageContentType } from './dummyPdfDocument';

/**
 * Configuration for dummy thumbnail generation.
 */
export interface DummyThumbnailConfig {
  pageCount: number;
  selectedPageNumber?: number;
  loadedPages?: Set<number>;
  thumbnailWidth?: number;
  thumbnailHeight?: number;
}

/**
 * Generates a data URL for a thumbnail placeholder with gradient and page number.
 */
function generateThumbnailDataUrl(
  pageNumber: number,
  width: number,
  height: number,
  contentType: PageContentType
): string {
  // Extract colors from gradient for SVG (simplified)
  const colorMap = {
    [PageContentType.Text]: ['#667eea', '#764ba2'],
    [PageContentType.Image]: ['#f093fb', '#f5576c'],
    [PageContentType.Mixed]: ['#4facfe', '#00f2fe'],
    [PageContentType.Form]: ['#43e97b', '#38f9d7'],
    [PageContentType.Blank]: ['#e0e0e0', '#f5f5f5'],
  };

  const [color1, color2] = colorMap[contentType];

  const svg = `
    <svg width="${width}" height="${height}" xmlns="http://www.w3.org/2000/svg">
      <defs>
        <linearGradient id="grad${pageNumber}" x1="0%" y1="0%" x2="100%" y2="100%">
          <stop offset="0%" style="stop-color:${color1};stop-opacity:1" />
          <stop offset="100%" style="stop-color:${color2};stop-opacity:1" />
        </linearGradient>
      </defs>
      <rect width="${width}" height="${height}" fill="url(#grad${pageNumber})" />
      <rect x="10" y="10" width="${width - 20}" height="${height - 20}"
            fill="white" fill-opacity="0.9" rx="4" />
      <text x="50%" y="50%"
            font-family="Arial, sans-serif"
            font-size="48"
            font-weight="bold"
            fill="#333"
            text-anchor="middle"
            dominant-baseline="middle">
        ${pageNumber}
      </text>
      <text x="50%" y="70%"
            font-family="Arial, sans-serif"
            font-size="12"
            fill="#666"
            text-anchor="middle"
            dominant-baseline="middle">
        ${contentType}
      </text>
    </svg>
  `;

  return `data:image/svg+xml;base64,${btoa(svg)}`;
}

/**
 * Generates a single dummy thumbnail item.
 */
export function generateDummyThumbnail(
  pageNumber: number,
  config: Partial<DummyThumbnailConfig> = {}
): ThumbnailItem {
  const {
    selectedPageNumber = 1,
    loadedPages = new Set<number>(),
    thumbnailWidth = 150,
    thumbnailHeight = 200,
  } = config;

  const contentType = getPageContentType(pageNumber);
  const isLoaded = loadedPages.has(pageNumber);

  return {
    pageNumber,
    thumbnail: isLoaded ? generateThumbnailDataUrl(pageNumber, thumbnailWidth, thumbnailHeight, contentType) : null,
    isLoading: !isLoaded,
    isSelected: pageNumber === selectedPageNumber,
    width: thumbnailWidth,
    height: thumbnailHeight,
  };
}

/**
 * Generates an array of dummy thumbnails for a document.
 */
export function generateDummyThumbnails(config: DummyThumbnailConfig): ThumbnailItem[] {
  const { pageCount } = config;

  return Array.from({ length: pageCount }, (_, i) =>
    generateDummyThumbnail(i + 1, config)
  );
}

/**
 * Simulates loading a thumbnail (async operation).
 * Returns updated thumbnail item after delay.
 */
export async function loadThumbnailAsync(
  item: ThumbnailItem,
  delayMs: number = 300
): Promise<ThumbnailItem> {
  await new Promise((resolve) => setTimeout(resolve, delayMs));

  const contentType = getPageContentType(item.pageNumber);

  return {
    ...item,
    thumbnail: generateThumbnailDataUrl(item.pageNumber, item.width, item.height, contentType),
    isLoading: false,
  };
}

/**
 * Pre-configured thumbnail sets for different states.
 */
export const ThumbnailStates = {
  /**
   * All thumbnails loaded, page 1 selected.
   */
  allLoaded: (pageCount: number) =>
    generateDummyThumbnails({
      pageCount,
      selectedPageNumber: 1,
      loadedPages: new Set(Array.from({ length: pageCount }, (_, i) => i + 1)),
    }),

  /**
   * First 10 thumbnails loaded, others loading.
   */
  partiallyLoaded: (pageCount: number) =>
    generateDummyThumbnails({
      pageCount,
      selectedPageNumber: 1,
      loadedPages: new Set(Array.from({ length: 10 }, (_, i) => i + 1)),
    }),

  /**
   * No thumbnails loaded yet (initial state).
   */
  notLoaded: (pageCount: number) =>
    generateDummyThumbnails({
      pageCount,
      selectedPageNumber: 1,
      loadedPages: new Set(),
    }),

  /**
   * Mid-document navigation (page 15 selected, neighbors loaded).
   */
  midDocument: (pageCount: number) =>
    generateDummyThumbnails({
      pageCount,
      selectedPageNumber: 15,
      loadedPages: new Set(Array.from({ length: 10 }, (_, i) => i + 11)), // Pages 11-20
    }),
};
