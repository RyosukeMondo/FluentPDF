import { useState } from 'react';
import './App.css';
import './styles/tokens.css';
import styles from './App.module.css';
import { MainWindow } from './components/MainWindow';
import { PdfViewerPage } from './components/PdfViewerPage';
import { WatermarkDialog } from './components/dialogs/WatermarkDialog';
import { DeletePagesDialog } from './components/dialogs/DeletePagesDialog';
import { ErrorDialog } from './components/dialogs/ErrorDialog';
import { SettingsPage } from './components/dialogs/SettingsPage';

type DialogType = 'watermark' | 'deletePages' | 'error' | 'settings' | null;

function App() {
  const [openDialog, setOpenDialog] = useState<DialogType>(null);

  const handleOpenWatermarkDialog = () => {
    setOpenDialog('watermark');
  };

  const handleOpenDeletePagesDialog = () => {
    setOpenDialog('deletePages');
  };

  const handleOpenErrorDialog = () => {
    setOpenDialog('error');
  };

  const handleOpenSettingsDialog = () => {
    setOpenDialog('settings');
  };

  const handleCloseDialog = () => {
    setOpenDialog(null);
  };

  const handleApplyWatermark = () => {
    console.log('Watermark applied');
    setOpenDialog(null);
  };

  const handleDeletePages = () => {
    console.log('Pages deleted');
    setOpenDialog(null);
  };

  return (
    <div className={styles.app}>
      {/* Main Application Window */}
      <MainWindow>
        <PdfViewerPage />
      </MainWindow>

      {/* Dialog Test Buttons Overlay */}
      <div className={styles.dialogTestButtons}>
        <button
          onClick={handleOpenWatermarkDialog}
          className={styles.testButton}
        >
          Open Watermark Dialog
        </button>
        <button
          onClick={handleOpenDeletePagesDialog}
          className={styles.testButton}
        >
          Open Delete Pages Dialog
        </button>
        <button
          onClick={handleOpenErrorDialog}
          className={styles.testButton}
        >
          Open Error Dialog
        </button>
        <button
          onClick={handleOpenSettingsDialog}
          className={styles.testButton}
        >
          Open Settings
        </button>
      </div>

      {/* Dialogs */}
      <WatermarkDialog
        isOpen={openDialog === 'watermark'}
        onClose={handleCloseDialog}
        onApply={handleApplyWatermark}
      />
      <DeletePagesDialog
        isOpen={openDialog === 'deletePages'}
        onClose={handleCloseDialog}
        onDelete={handleDeletePages}
      />
      <ErrorDialog
        isOpen={openDialog === 'error'}
        onClose={handleCloseDialog}
        message="The PDF file could not be loaded. It may be corrupted or in an unsupported format."
      />
      <SettingsPage
        isOpen={openDialog === 'settings'}
        onClose={handleCloseDialog}
      />
    </div>
  );
}

export default App;
