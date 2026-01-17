/**
 * ThumbnailsSidebar Component
 *
 * React implementation of FluentPDF.App.Controls.ThumbnailsSidebar
 * Displays a vertical list of PDF page thumbnails with selection and loading states.
 *
 * XAML Source: src/FluentPDF.App/Controls/ThumbnailsSidebar.xaml
 */

import React from 'react';
import { ThumbnailItem } from '../types/models';
import styles from './ThumbnailsSidebar.module.css';

export interface ThumbnailsSidebarProps {
  /**
   * Array of thumbnail items to display.
   */
  thumbnails: ThumbnailItem[];

  /**
   * Callback when a thumbnail is clicked.
   * @param pageNumber 1-based page number
   */
  onThumbnailClick?: (pageNumber: number) => void;

  /**
   * Callback when a thumbnail is right-clicked (context menu).
   * @param pageNumber 1-based page number
   */
  onThumbnailContextMenu?: (pageNumber: number, event: React.MouseEvent) => void;

  /**
   * Current scroll position (for virtualization in future).
   */
  scrollTop?: number;

  /**
   * Callback when scroll position changes.
   */
  onScroll?: (scrollTop: number) => void;
}

/**
 * Individual thumbnail item component.
 */
const ThumbnailItemComponent: React.FC<{
  item: ThumbnailItem;
  onClick?: (pageNumber: number) => void;
  onContextMenu?: (pageNumber: number, event: React.MouseEvent) => void;
}> = ({ item, onClick, onContextMenu }) => {
  const handleClick = () => {
    console.log(`Thumbnail clicked: Page ${item.pageNumber}`);
    onClick?.(item.pageNumber);
  };

  const handleContextMenu = (event: React.MouseEvent) => {
    event.preventDefault();
    console.log(`Thumbnail context menu: Page ${item.pageNumber}`);
    onContextMenu?.(item.pageNumber, event);
  };

  const handleKeyDown = (event: React.KeyboardEvent) => {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      handleClick();
    }
  };

  return (
    <button
      className={`${styles.thumbnailButton} ${item.isSelected ? styles.selected : ''}`}
      onClick={handleClick}
      onContextMenu={handleContextMenu}
      onKeyDown={handleKeyDown}
      aria-label={`Page ${item.pageNumber}`}
      tabIndex={0}
    >
      <div className={styles.thumbnailContainer}>
        {/* Selected border indicator */}
        {item.isSelected && <div className={styles.selectedBorder} />}

        <div className={styles.thumbnailContent}>
          {/* Thumbnail image area */}
          <div className={styles.thumbnailImageContainer}>
            {item.isLoading ? (
              // Loading spinner
              <div className={styles.loadingContainer}>
                <div className={styles.loadingPlaceholder} />
                <div className={styles.spinner} aria-label="Loading thumbnail" />
              </div>
            ) : item.thumbnail ? (
              // Thumbnail image
              <img
                src={item.thumbnail}
                alt={`Page ${item.pageNumber}`}
                className={styles.thumbnailImage}
              />
            ) : (
              // Empty placeholder
              <div className={styles.emptyPlaceholder} />
            )}
          </div>

          {/* Page number label */}
          <div className={styles.pageNumberLabel}>{item.pageNumber}</div>
        </div>
      </div>
    </button>
  );
};

/**
 * ThumbnailsSidebar component - displays scrollable list of page thumbnails.
 */
export const ThumbnailsSidebar: React.FC<ThumbnailsSidebarProps> = ({
  thumbnails,
  onThumbnailClick,
  onThumbnailContextMenu,
  onScroll,
}) => {
  const handleScroll = (event: React.UIEvent<HTMLDivElement>) => {
    const scrollTop = (event.target as HTMLDivElement).scrollTop;
    onScroll?.(scrollTop);
  };

  return (
    <div className={styles.thumbnailsSidebar}>
      <div className={styles.scrollContainer} onScroll={handleScroll}>
        <div className={styles.thumbnailsList}>
          {thumbnails.map((item) => (
            <ThumbnailItemComponent
              key={item.pageNumber}
              item={item}
              onClick={onThumbnailClick}
              onContextMenu={onThumbnailContextMenu}
            />
          ))}
        </div>
      </div>
    </div>
  );
};

export default ThumbnailsSidebar;
