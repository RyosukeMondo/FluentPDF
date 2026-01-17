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
import { PDF_SCENARIOS, PdfScenario } from './data/scenarios';

type DialogType = 'watermark' | 'deletePages' | 'error' | 'settings' | 'openPdf' | null;

function App() {
  const [openDialog, setOpenDialog] = useState<DialogType>(null);
  const [currentScenario, setCurrentScenario] = useState<PdfScenario | null>(null);

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

  const handleOpenPdfDialog = () => {
    setOpenDialog('openPdf');
  };

  const handleSelectScenario = (scenario: PdfScenario) => {
    setCurrentScenario(scenario);
    setOpenDialog(null);
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
        <PdfViewerPage scenario={currentScenario} onOpenDocument={handleOpenPdfDialog} />
      </MainWindow>

      {/* Dialog Test Buttons Overlay */}
      <div className={styles.dialogTestButtons}>
        <button
          onClick={handleOpenPdfDialog}
          className={`${styles.testButton} ${styles.primaryButton}`}
        >
          📂 Open PDF Scenario
        </button>
        <button
          onClick={handleOpenWatermarkDialog}
          className={styles.testButton}
        >
          💧 Watermark
        </button>
        <button
          onClick={handleOpenDeletePagesDialog}
          className={styles.testButton}
        >
          🗑️ Delete Pages
        </button>
        <button
          onClick={handleOpenErrorDialog}
          className={styles.testButton}
        >
          ⚠️ Error Dialog
        </button>
        <button
          onClick={handleOpenSettingsDialog}
          className={styles.testButton}
        >
          ⚙️ Settings
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

      {/* Open PDF Scenario Dialog */}
      {openDialog === 'openPdf' && (
        <div className={styles.modalBackdrop} onClick={handleCloseDialog}>
          <div className={styles.scenarioDialog} onClick={(e) => e.stopPropagation()}>
            <div className={styles.scenarioDialogHeader}>
              <h2>Open PDF Scenario</h2>
              <button className={styles.closeButton} onClick={handleCloseDialog}>
                ✕
              </button>
            </div>
            <div className={styles.scenarioDialogContent}>
              <p className={styles.scenarioDialogDescription}>
                Select a mock PDF scenario to test different UI states:
              </p>
              <div className={styles.scenarioGrid}>
                {PDF_SCENARIOS.map((scenario) => (
                  <button
                    key={scenario.id}
                    className={`${styles.scenarioCard} ${
                      currentScenario?.id === scenario.id ? styles.scenarioCardActive : ''
                    }`}
                    onClick={() => handleSelectScenario(scenario)}
                  >
                    <div className={styles.scenarioEmoji}>{scenario.emoji}</div>
                    <div className={styles.scenarioName}>{scenario.name}</div>
                    <div className={styles.scenarioDescription}>{scenario.description}</div>
                    <div className={styles.scenarioMeta}>
                      {scenario.documentConfig.pageCount} pages •{' '}
                      {Math.round((scenario.documentConfig.fileSizeKB || 0) / 1024)}MB
                    </div>
                  </button>
                ))}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default App;
