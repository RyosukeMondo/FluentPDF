import React from 'react';
import styles from './dialogs.module.css';

interface ErrorDialogProps {
  isOpen: boolean;
  onClose: () => void;
  message?: string;
  correlationId?: string;
}

export const ErrorDialog: React.FC<ErrorDialogProps> = ({
  isOpen,
  onClose,
  message = 'An unexpected error occurred. Please try again.',
  correlationId = generateCorrelationId()
}) => {
  if (!isOpen) return null;

  const handleClose = () => {
    console.log('Error dialog closed. Correlation ID:', correlationId);
    onClose();
  };

  return (
    <div className={styles.dialogOverlay} onClick={handleClose}>
      <div className={styles.dialogContainer} onClick={(e) => e.stopPropagation()}>
        <div className={styles.dialogHeader}>
          <h2 className={styles.dialogTitle}>An Error Occurred</h2>
        </div>

        <div className={styles.dialogContent}>
          <div className={styles.errorContent}>
            <div className={styles.errorIcon}>❌</div>
            <p className={styles.errorMessage}>{message}</p>

            <div className={styles.correlationSection}>
              <h3 className={styles.correlationTitle}>Error Reference ID:</h3>
              <code className={styles.correlationId}>{correlationId}</code>
            </div>

            <p className={styles.helpText}>
              If you need support, please provide the Error Reference ID above.
            </p>
          </div>
        </div>

        <div className={styles.dialogFooter}>
          <button className={styles.buttonPrimary} onClick={handleClose}>
            Close
          </button>
        </div>
      </div>
    </div>
  );
};

function generateCorrelationId(): string {
  const timestamp = Date.now().toString(36);
  const random = Math.random().toString(36).substring(2, 10);
  return `${timestamp}-${random}`.toUpperCase();
}
