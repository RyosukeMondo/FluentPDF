/**
 * PdfViewerControl React Component
 *
 * Reverse-engineered from FluentPDF.App/Controls/PdfViewerControl.xaml
 * Main PDF viewer with toolbar, zoom controls, and page navigation.
 *
 * Visual structure only - stub interaction handlers for prototyping.
 */

import { useState } from 'react';
import styles from './PdfViewerControl.module.css';
import { SampleDocuments } from '../data/dummyPdfDocument';

export interface PdfViewerControlProps {
  /** Document to display */
  document?: typeof SampleDocuments.medium;
  /** Current page number (1-indexed) */
  currentPage?: number;
  /** Current zoom level (1.0 = 100%) */
  zoomLevel?: number;
  /** Whether search panel is visible */
  showSearchPanel?: boolean;
  /** Whether loading indicator is shown */
  isLoading?: boolean;
}

/**
 * Main PDF viewer control component.
 * Displays toolbar, zoom controls, page navigation, and PDF page image.
 */
export function PdfViewerControl({
  document = SampleDocuments.medium,
  currentPage = 1,
  zoomLevel = 1.0,
  showSearchPanel = false,
  isLoading = false,
}: PdfViewerControlProps) {
  const [localPage, setLocalPage] = useState(currentPage);
  const [localZoom, setLocalZoom] = useState(zoomLevel);
  const [searchVisible, setSearchVisible] = useState(showSearchPanel);
  const [searchQuery, setSearchQuery] = useState('');

  // Navigation handlers (stub)
  const handlePreviousPage = () => {
    if (localPage > 1) {
      setLocalPage(localPage - 1);
      console.log('Navigate to previous page:', localPage - 1);
    }
  };

  const handleNextPage = () => {
    if (localPage < document.pageCount) {
      setLocalPage(localPage + 1);
      console.log('Navigate to next page:', localPage + 1);
    }
  };

  // Zoom handlers (stub)
  const handleZoomIn = () => {
    const newZoom = Math.min(localZoom + 0.25, 3.0);
    setLocalZoom(newZoom);
    console.log('Zoom in to:', newZoom);
  };

  const handleZoomOut = () => {
    const newZoom = Math.max(localZoom - 0.25, 0.25);
    setLocalZoom(newZoom);
    console.log('Zoom out to:', newZoom);
  };

  const handleResetZoom = () => {
    setLocalZoom(1.0);
    console.log('Reset zoom to 100%');
  };

  // Toolbar action handlers (stub)
  const handleOpenFile = () => console.log('Open file clicked');
  const handleToggleSearch = () => {
    setSearchVisible(!searchVisible);
    console.log('Toggle search panel');
  };

  return (
    <div className={styles.viewerContainer}>
      {/* Main Toolbar */}
      <div className={styles.toolbar}>
        {/* Open File */}
        <button className={styles.toolbarButton} onClick={handleOpenFile} title="Open">
          <span className={styles.icon}>📂</span>
          <span>Open</span>
        </button>

        <div className={styles.separator} />

        {/* Navigation Controls */}
        <button
          className={styles.toolbarButton}
          onClick={handlePreviousPage}
          disabled={localPage === 1}
          title="Previous Page (Left Arrow)"
        >
          <span className={styles.icon}>◀</span>
          <span>Previous</span>
        </button>

        <div className={styles.pageIndicator}>
          <span>Page </span>
          <strong>{localPage}</strong>
          <span> of </span>
          <strong>{document.pageCount}</strong>
        </div>

        <button
          className={styles.toolbarButton}
          onClick={handleNextPage}
          disabled={localPage === document.pageCount}
          title="Next Page (Right Arrow)"
        >
          <span className={styles.icon}>▶</span>
          <span>Next</span>
        </button>

        <div className={styles.separator} />

        {/* Toggle Search */}
        <button
          className={styles.toolbarButton}
          onClick={handleToggleSearch}
          title="Search (Ctrl+F)"
        >
          <span className={styles.icon}>🔍</span>
          <span>Search</span>
        </button>

        <div className={styles.separator} />

        {/* Zoom Controls */}
        <button
          className={styles.toolbarButton}
          onClick={handleZoomOut}
          disabled={localZoom <= 0.25}
          title="Zoom Out (Ctrl+-)"
        >
          <span className={styles.icon}>🔍−</span>
          <span>Zoom Out</span>
        </button>

        <div className={styles.zoomIndicator}>
          <strong>{Math.round(localZoom * 100)}%</strong>
        </div>

        <button
          className={styles.toolbarButton}
          onClick={handleZoomIn}
          disabled={localZoom >= 3.0}
          title="Zoom In (Ctrl++)"
        >
          <span className={styles.icon}>🔍+</span>
          <span>Zoom In</span>
        </button>

        <button className={styles.toolbarButton} onClick={handleResetZoom} title="Reset Zoom (Ctrl+0)">
          <span className={styles.icon}>⟲</span>
          <span>Reset</span>
        </button>
      </div>

      {/* Search Panel (toggleable) */}
      {searchVisible && (
        <div className={styles.searchPanel}>
          <input
            type="text"
            className={styles.searchInput}
            placeholder="Search..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
          />
          <div className={styles.searchCounter}>0 of 0</div>
          <button className={styles.searchButton}>Previous</button>
          <button className={styles.searchButton}>Next</button>
          <label className={styles.searchCheckbox}>
            <input type="checkbox" />
            <span>Case sensitive</span>
          </label>
          <button className={styles.closeButton} onClick={handleToggleSearch} title="Close (Escape)">
            ✕
          </button>
        </div>
      )}

      {/* Main Viewer Area */}
      <div className={styles.viewerContent}>
        {isLoading ? (
          <div className={styles.loadingIndicator}>
            <div className={styles.spinner} />
            <p>Loading...</p>
          </div>
        ) : (
          <div className={styles.scrollContainer}>
            {/* PDF Page Placeholder (gradient rectangle simulating page content) */}
            <div
              className={styles.pdfPage}
              style={{
                transform: `scale(${localZoom})`,
                background: `linear-gradient(135deg,
                  #ffffff 0%,
                  #f5f5f5 25%,
                  #ffffff 50%,
                  #f0f0f0 75%,
                  #ffffff 100%)`,
              }}
            >
              <div className={styles.pageContent}>
                <div className={styles.dummyText}>
                  <h1>Page {localPage}</h1>
                  <p>Document: {document.fileName}</p>
                  <p className={styles.metaText}>
                    Size: {Math.round(document.fileSizeBytes / 1024)} KB
                  </p>
                  <div className={styles.dummyParagraphs}>
                    <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit.</p>
                    <p>Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.</p>
                    <p>Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris.</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Empty State */}
        {!isLoading && !document && (
          <div className={styles.emptyState}>
            <p>No document loaded</p>
            <button onClick={handleOpenFile}>Open a PDF file</button>
          </div>
        )}
      </div>
    </div>
  );
}

export default PdfViewerControl;
