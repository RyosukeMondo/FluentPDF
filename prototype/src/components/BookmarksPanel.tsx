/**
 * BookmarksPanel - Hierarchical tree view for PDF bookmarks/table of contents.
 * Reverse-engineered from src/FluentPDF.App/Controls/BookmarksPanel.xaml
 *
 * Features:
 * - Recursive tree rendering for arbitrary nesting depth
 * - Expand/collapse functionality for nodes with children
 * - Visual indentation based on nesting level
 * - Page number navigation (stubbed with console.log)
 */

import React, { useState } from 'react';
import { BookmarkNode } from '../types/models';
import styles from './BookmarksPanel.module.css';

interface BookmarksPanelProps {
  bookmarks: BookmarkNode[];
  isLoading?: boolean;
  emptyMessage?: string;
  onNavigateToPage?: (pageNumber: number) => void;
}

interface BookmarkTreeItemProps {
  node: BookmarkNode;
  level: number;
  onNavigateToPage?: (pageNumber: number) => void;
}

/**
 * Recursive component for rendering a single bookmark node and its children.
 */
const BookmarkTreeItem: React.FC<BookmarkTreeItemProps> = ({ node, level, onNavigateToPage }) => {
  const [isExpanded, setIsExpanded] = useState<boolean>(node.isExpanded ?? false);
  const hasChildren = node.children && node.children.length > 0;

  const handleToggleExpand = (e: React.MouseEvent) => {
    e.stopPropagation();
    if (hasChildren) {
      setIsExpanded(!isExpanded);
    }
  };

  const handleItemClick = () => {
    if (node.pageNumber !== undefined) {
      console.log(`[BookmarksPanel] Navigate to page ${node.pageNumber}: ${node.title}`);
      onNavigateToPage?.(node.pageNumber);
    }
  };

  return (
    <div className={styles.treeItem} data-level={level}>
      <div
        className={styles.itemContent}
        style={{ paddingLeft: `${level * 16 + 8}px` }}
        onClick={handleItemClick}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            handleItemClick();
          }
        }}
      >
        {/* Expand/collapse icon */}
        {hasChildren ? (
          <button
            className={styles.expandButton}
            onClick={handleToggleExpand}
            aria-label={isExpanded ? 'Collapse' : 'Expand'}
            aria-expanded={isExpanded}
          >
            <svg
              width="12"
              height="12"
              viewBox="0 0 12 12"
              fill="currentColor"
              className={isExpanded ? styles.expandIconExpanded : styles.expandIcon}
            >
              <path d="M4 2 L8 6 L4 10" stroke="currentColor" strokeWidth="1.5" fill="none" />
            </svg>
          </button>
        ) : (
          <span className={styles.expandPlaceholder}></span>
        )}

        {/* Bookmark icon */}
        <span className={styles.bookmarkIcon} aria-hidden="true">
          📄
        </span>

        {/* Title */}
        <span className={styles.title} title={node.title}>
          {node.title}
        </span>

        {/* Page number badge */}
        {node.pageNumber !== undefined && (
          <span className={styles.pageNumber}>p. {node.pageNumber}</span>
        )}
      </div>

      {/* Recursively render children */}
      {hasChildren && isExpanded && (
        <div className={styles.children}>
          {node.children.map((child, index) => (
            <BookmarkTreeItem
              key={`${child.title}-${child.pageNumber}-${index}`}
              node={child}
              level={level + 1}
              onNavigateToPage={onNavigateToPage}
            />
          ))}
        </div>
      )}
    </div>
  );
};

/**
 * Main BookmarksPanel component - displays hierarchical bookmark tree.
 */
export const BookmarksPanel: React.FC<BookmarksPanelProps> = ({
  bookmarks,
  isLoading = false,
  emptyMessage = 'No bookmarks available',
  onNavigateToPage,
}) => {
  // Loading state
  if (isLoading) {
    return (
      <div className={styles.container}>
        <div className={styles.loading}>
          <div className={styles.spinner} aria-label="Loading bookmarks"></div>
          <p>Loading bookmarks...</p>
        </div>
      </div>
    );
  }

  // Empty state
  if (!bookmarks || bookmarks.length === 0) {
    return (
      <div className={styles.container}>
        <div className={styles.empty}>
          <p>{emptyMessage}</p>
        </div>
      </div>
    );
  }

  // Bookmark tree
  return (
    <div className={styles.container}>
      <div className={styles.treeView} role="tree">
        {bookmarks.map((bookmark, index) => (
          <BookmarkTreeItem
            key={`${bookmark.title}-${bookmark.pageNumber}-${index}`}
            node={bookmark}
            level={0}
            onNavigateToPage={onNavigateToPage}
          />
        ))}
      </div>
    </div>
  );
};

export default BookmarksPanel;
