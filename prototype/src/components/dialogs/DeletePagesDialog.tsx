import React from 'react';
import styles from './dialogs.module.css';

interface DeletePagesDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onDelete: () => void;
  pageCount?: number;
  selectedPages?: number[];
}

export const DeletePagesDialog: React.FC<DeletePagesDialogProps> = ({
  isOpen,
  onClose,
  onDelete,
  pageCount = 1,
  selectedPages = []
}) => {
  if (!isOpen) return null;

  const getMessage = () => {
    if (selectedPages.length > 1) {
      return `Are you sure you want to delete ${selectedPages.length} selected pages?`;
    } else if (selectedPages.length === 1) {
      return `Are you sure you want to delete page ${selectedPages[0]}?`;
    } else {
      return `Are you sure you want to delete ${pageCount} page(s)?`;
    }
  };

  const handleDelete = () => {
    console.log('Deleting pages:', selectedPages.length > 0 ? selectedPages : `${pageCount} pages`);
    onDelete();
  };

  return (
    <div className={styles.dialogOverlay} onClick={onClose}>
      <div className={styles.dialogContainer} onClick={(e) => e.stopPropagation()}>
        <div className={styles.dialogHeader}>
          <h2 className={styles.dialogTitle}>Delete Pages</h2>
        </div>

        <div className={styles.dialogContent}>
          <div className={styles.warningContent}>
            <div className={styles.warningIcon}>⚠️</div>
            <p className={styles.warningMessage}>{getMessage()}</p>
            <p className={styles.warningSubtext}>This action cannot be undone.</p>
          </div>
        </div>

        <div className={styles.dialogFooter}>
          <button className={styles.buttonSecondary} onClick={onClose}>
            Cancel
          </button>
          <button className={styles.buttonDanger} onClick={handleDelete}>
            Delete
          </button>
        </div>
      </div>
    </div>
  );
};
