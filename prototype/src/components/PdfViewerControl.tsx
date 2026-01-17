/**
 * PdfViewerControl React Component
 *
 * Reverse-engineered from FluentPDF.App/Controls/PdfViewerControl.xaml
 * Main PDF viewer with toolbar, zoom controls, and page navigation.
 *
 * Visual structure only - stub interaction handlers for prototyping.
 */

import styles from './PdfViewerControl.module.css';
import { SampleDocuments } from '../data/dummyPdfDocument';

export interface PdfViewerControlProps {
  /** Document to display */
  document?: typeof SampleDocuments.medium;
  /** Current page number (1-indexed) */
  currentPage?: number;
  /** Current zoom level (1.0 = 100%) */
  zoomLevel?: number;
  /** Whether loading indicator is shown */
  isLoading?: boolean;
}

/**
 * Main PDF viewer control component.
 * Displays toolbar, zoom controls, page navigation, and PDF page image.
 */
export function PdfViewerControl({
  document,
  currentPage = 1,
  zoomLevel = 1.0,
  isLoading = false,
}: PdfViewerControlProps) {

  // If no document, show empty state
  if (!document) {
    return (
      <div className={styles.viewerContainer}>
        <div className={styles.viewerContent}>
          <div className={styles.emptyState}>
            <div className={styles.emptyIcon}>📄</div>
            <h2>No Document Open</h2>
            <p>Click "📂 Open PDF Scenario" to load a sample document</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.viewerContainer}>
      {/* PDF Viewer Area */}
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
                transform: `scale(${zoomLevel})`,
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
                  <h1>Page {currentPage}</h1>
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
