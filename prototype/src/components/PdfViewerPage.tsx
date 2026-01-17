import React, { useState } from 'react';
import { ThumbnailsSidebar } from './ThumbnailsSidebar';
import { PdfViewerControl } from './PdfViewerControl';
import { BookmarksPanel } from './BookmarksPanel';
import { generateDummyDocument } from '../data/dummyPdfDocument';
import { generateDummyThumbnails } from '../data/dummyThumbnails';
import { PdfScenario } from '../data/scenarios';
import { BookmarkNode } from '../types/models';
import styles from './layouts.module.css';

interface PdfViewerPageProps {
  scenario?: PdfScenario | null;
  onOpenDocument?: () => void;
}

export const PdfViewerPage: React.FC<PdfViewerPageProps> = ({ scenario, onOpenDocument }) => {
  const [isThumbnailsVisible, setIsThumbnailsVisible] = useState(true);
  const [isBookmarksVisible, setIsBookmarksVisible] = useState(true);
  const [isSearchVisible, setIsSearchVisible] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);
  const [zoomLevel, setZoomLevel] = useState(1.0);
  const [searchQuery, setSearchQuery] = useState('');

  // Use scenario data if provided, otherwise show placeholder
  const pdfDocument = scenario
    ? generateDummyDocument(scenario.documentConfig)
    : null;

  const thumbnails = scenario
    ? generateDummyThumbnails({ pageCount: scenario.documentConfig.pageCount || 15, selectedPageNumber: currentPage })
    : [];

  const bookmarks: BookmarkNode[] = scenario?.bookmarks || [];

  const handleOpenDocument = () => {
    console.log('Open document clicked');
    onOpenDocument?.();
  };

  const handlePreviousPage = () => {
    if (currentPage > 1) {
      setCurrentPage(currentPage - 1);
    }
  };

  const handleNextPage = () => {
    if (pdfDocument && currentPage < pdfDocument.pageCount) {
      setCurrentPage(currentPage + 1);
    }
  };

  const handleToggleThumbnails = () => {
    setIsThumbnailsVisible(!isThumbnailsVisible);
  };

  const handleToggleBookmarks = () => {
    setIsBookmarksVisible(!isBookmarksVisible);
  };

  const handleToggleSearch = () => {
    setIsSearchVisible(!isSearchVisible);
  };

  const handleZoomIn = () => {
    setZoomLevel(Math.min(zoomLevel + 0.25, 3.0));
  };

  const handleZoomOut = () => {
    setZoomLevel(Math.max(zoomLevel - 0.25, 0.25));
  };

  const handleResetZoom = () => {
    setZoomLevel(1.0);
  };

  const handleWatermark = () => {
    console.log('Add watermark');
  };

  const handleMerge = () => {
    console.log('Merge documents');
  };

  const handleSplit = () => {
    console.log('Split document');
  };

  const handleOptimize = () => {
    console.log('Optimize document');
  };

  const handleConvertDocx = () => {
    console.log('Convert DOCX');
  };

  const handleThumbnailClick = (pageNumber: number) => {
    setCurrentPage(pageNumber);
  };

  const handleBookmarkClick = (pageNumber: number) => {
    setCurrentPage(pageNumber);
  };

  return (
    <div className={styles.pdfViewerPage}>
      {/* Main Toolbar - Full Width */}
      <div className={styles.toolbar}>
            <button className={styles.toolbarButton} onClick={handleOpenDocument} title="Open">
              📂 Open
            </button>

            <div className={styles.toolbarSeparator} />

            {/* Navigation Controls */}
            <button
              className={styles.toolbarButton}
              onClick={handlePreviousPage}
              disabled={currentPage === 1}
              title="Previous Page"
            >
              ◀ Previous
            </button>

            <span className={styles.pageIndicator}>
              {pdfDocument ? `Page ${currentPage} of ${pdfDocument.pageCount}` : 'No document loaded'}
            </span>

            <button
              className={styles.toolbarButton}
              onClick={handleNextPage}
              disabled={!pdfDocument || currentPage === pdfDocument.pageCount}
              title="Next Page"
            >
              Next ▶
            </button>

            <div className={styles.toolbarSeparator} />

            {/* Toggle Panels */}
            <button
              className={`${styles.toolbarButton} ${isThumbnailsVisible ? styles.toolbarButtonActive : ''}`}
              onClick={handleToggleThumbnails}
              title="Toggle Thumbnails (Ctrl+T)"
            >
              🖼 Thumbnails
            </button>

            <button
              className={`${styles.toolbarButton} ${isBookmarksVisible ? styles.toolbarButtonActive : ''}`}
              onClick={handleToggleBookmarks}
              title="Toggle Bookmarks (Ctrl+B)"
            >
              📑 Bookmarks
            </button>

            <button
              className={`${styles.toolbarButton} ${isSearchVisible ? styles.toolbarButtonActive : ''}`}
              onClick={handleToggleSearch}
              title="Search (Ctrl+F)"
            >
              🔍 Search
            </button>

            <div className={styles.toolbarSeparator} />

            {/* Zoom Controls */}
            <button className={styles.toolbarButton} onClick={handleZoomOut} title="Zoom Out (Ctrl+-)">
              🔍− Zoom Out
            </button>

            <span className={styles.zoomIndicator}>{Math.round(zoomLevel * 100)}%</span>

            <button className={styles.toolbarButton} onClick={handleZoomIn} title="Zoom In (Ctrl++)">
              🔍+ Zoom In
            </button>

            <button className={styles.toolbarButton} onClick={handleResetZoom} title="Reset Zoom (Ctrl+0)">
              ↻ Reset
            </button>

            <div className={styles.toolbarSeparator} />

            {/* Document Operations */}
            <button className={styles.toolbarButton} onClick={handleWatermark} title="Add Watermark">
              💧 Watermark
            </button>

            <button className={styles.toolbarButton} onClick={handleMerge} title="Merge PDFs">
              🔗 Merge
            </button>

            <button className={styles.toolbarButton} onClick={handleSplit} title="Split PDF">
              ✂ Split
            </button>

            <button className={styles.toolbarButton} onClick={handleOptimize} title="Optimize PDF">
              ⚡ Optimize
            </button>

            <div className={styles.toolbarSeparator} />

            <button className={styles.toolbarButton} onClick={handleConvertDocx} title="Convert DOCX (Ctrl+Shift+C)">
              📝 Convert DOCX
            </button>
          </div>

          {/* Annotation Toolbar */}
          <div className={styles.annotationToolbar}>
            <button className={styles.toolbarButton} title="Highlight">
              ✏ Highlight
            </button>
            <button className={styles.toolbarButton} title="Underline">
              U Underline
            </button>
            <button className={styles.toolbarButton} title="Strikethrough">
              S̶ Strikethrough
            </button>

            <div className={styles.toolbarSeparator} />

            <button className={styles.toolbarButton} title="Comment">
              💬 Comment
            </button>

            <div className={styles.toolbarSeparator} />

            <button className={styles.toolbarButton} title="Rectangle">
              ▭ Rectangle
            </button>
            <button className={styles.toolbarButton} title="Circle">
              ○ Circle
            </button>
            <button className={styles.toolbarButton} title="Freehand">
              ✎ Freehand
            </button>

            <div className={styles.toolbarSeparator} />

            <label className={styles.colorPickerLabel}>
              Color:
              <input type="color" className={styles.colorPicker} defaultValue="#ffff00" />
            </label>
          </div>

      {/* Search Panel */}
      {isSearchVisible && (
        <div className={styles.searchPanel}>
          <input
            type="text"
            className={styles.searchInput}
            placeholder="Search..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
          <span className={styles.searchCounter}>0 of 0</span>
          <button className={styles.searchButton}>Previous</button>
          <button className={styles.searchButton}>Next</button>
          <label className={styles.searchCheckbox}>
            <input type="checkbox" />
            Case sensitive
          </label>
          <button className={styles.searchCloseButton} onClick={handleToggleSearch}>
            ✕
          </button>
        </div>
      )}

      {/* Three Column Layout: Thumbnails | Bookmarks | PDF Viewer */}
      <div className={styles.pdfViewerLayout}>
        {/* Thumbnails Sidebar */}
        {isThumbnailsVisible && (
          <div className={styles.thumbnailsColumn}>
            <ThumbnailsSidebar
              thumbnails={thumbnails}
              onThumbnailClick={handleThumbnailClick}
            />
          </div>
        )}

        {/* Bookmarks Column */}
        {isBookmarksVisible && (
          <div className={styles.bookmarksColumn}>
            <BookmarksPanel bookmarks={bookmarks} onNavigateToPage={handleBookmarkClick} />
          </div>
        )}

        {/* PDF Viewer Column */}
        <div className={styles.viewerColumn}>
          <PdfViewerControl
            document={pdfDocument}
            currentPage={currentPage}
            zoomLevel={zoomLevel}
          />
        </div>
      </div>
    </div>
  );
};
