import React, { useState } from 'react';
import styles from './dialogs.module.css';

interface SettingsPageProps {
  isOpen: boolean;
  onClose: () => void;
}

type QualityLevel = 'auto' | 'low' | 'medium' | 'high' | 'ultra';
type ZoomLevel = 'fitWidth' | 'fitPage' | '50%' | '75%' | '100%' | '125%' | '150%' | '200%';
type ScrollMode = 'vertical' | 'horizontal' | 'wrapped';
type Theme = 'light' | 'dark' | 'system';

export const SettingsPage: React.FC<SettingsPageProps> = ({ isOpen, onClose }) => {
  const [qualityLevel, setQualityLevel] = useState<QualityLevel>('auto');
  const [defaultZoom, setDefaultZoom] = useState<ZoomLevel>('fitWidth');
  const [scrollMode, setScrollMode] = useState<ScrollMode>('vertical');
  const [theme, setTheme] = useState<Theme>('system');
  const [telemetryEnabled, setTelemetryEnabled] = useState(true);
  const [crashReportingEnabled, setCrashReportingEnabled] = useState(true);

  if (!isOpen) return null;

  const qualityOptions = [
    { value: 'auto', label: 'Auto', description: 'Automatically adjusts based on display DPI and zoom level' },
    { value: 'low', label: 'Low (72 DPI)', description: 'Fast rendering, lower quality' },
    { value: 'medium', label: 'Medium (96 DPI)', description: 'Balanced performance and quality' },
    { value: 'high', label: 'High (144 DPI)', description: 'High quality for standard displays' },
    { value: 'ultra', label: 'Ultra (288 DPI)', description: 'Maximum quality, high memory usage' }
  ];

  const zoomLevels: ZoomLevel[] = ['fitWidth', 'fitPage', '50%', '75%', '100%', '125%', '150%', '200%'];
  const scrollModes = [
    { value: 'vertical', label: 'Vertical' },
    { value: 'horizontal', label: 'Horizontal' },
    { value: 'wrapped', label: 'Wrapped' }
  ];

  const handleApplyQuality = () => {
    console.log('Applied quality level:', qualityLevel);
  };

  const handleReset = () => {
    console.log('Resetting all settings to defaults');
    setQualityLevel('auto');
    setDefaultZoom('fitWidth');
    setScrollMode('vertical');
    setTheme('system');
    setTelemetryEnabled(true);
    setCrashReportingEnabled(true);
  };

  return (
    <div className={styles.dialogOverlay} onClick={onClose}>
      <div className={styles.dialogContainer} onClick={(e) => e.stopPropagation()}>
        <div className={styles.dialogHeader}>
          <h2 className={styles.dialogTitle}>Settings</h2>
        </div>

        <div className={styles.dialogContent}>
          <div className={styles.settingsContent}>
            {/* Rendering Quality Section */}
            <div className={styles.settingsSection}>
              <h3 className={styles.settingsHeading}>Rendering Quality</h3>
              <p className={styles.settingsDescription}>
                Adjust the quality of PDF rendering to balance performance and visual clarity.
              </p>

              <div className={styles.formField}>
                <label className={styles.label}>Quality Level</label>
                <select
                  className={styles.select}
                  value={qualityLevel}
                  onChange={(e) => setQualityLevel(e.target.value as QualityLevel)}
                >
                  {qualityOptions.map(opt => (
                    <option key={opt.value} value={opt.value}>
                      {opt.label}
                    </option>
                  ))}
                </select>
                <p className={styles.optionDescription}>
                  {qualityOptions.find(opt => opt.value === qualityLevel)?.description}
                </p>
              </div>

              {qualityLevel === 'ultra' && (
                <div className={styles.warningBanner}>
                  <strong>Performance Warning:</strong> Ultra quality requires significant memory and may cause
                  slowdowns or crashes on large documents or low-end devices.
                </div>
              )}

              <button className={styles.buttonPrimary} onClick={handleApplyQuality}>
                Apply
              </button>

              <div className={styles.infoSection}>
                <h4 className={styles.infoTitle}>About Quality Settings</h4>
                <ul className={styles.infoList}>
                  <li>
                    <strong>Auto</strong> - The application automatically adjusts quality based on your display
                    DPI and zoom level. This is recommended for most users.
                  </li>
                  <li>
                    <strong>Manual Selection</strong> - Choose a specific quality level to override automatic
                    detection. Higher quality levels render at higher DPI but use more memory and processing power.
                  </li>
                  <li>
                    <strong>Real-Time Adjustment</strong> - When you move the application to a different display
                    or change scaling settings, the rendering will automatically adjust if Auto mode is enabled.
                  </li>
                </ul>
              </div>
            </div>

            {/* Viewing Section */}
            <div className={styles.settingsSection}>
              <h3 className={styles.settingsHeading}>Viewing</h3>
              <p className={styles.settingsDescription}>
                Configure default viewing preferences for newly opened documents.
              </p>

              <div className={styles.formField}>
                <label className={styles.label}>Default Zoom Level</label>
                <select
                  className={styles.select}
                  value={defaultZoom}
                  onChange={(e) => setDefaultZoom(e.target.value as ZoomLevel)}
                >
                  {zoomLevels.map(zoom => (
                    <option key={zoom} value={zoom}>
                      {zoom === 'fitWidth' ? 'Fit Width' : zoom === 'fitPage' ? 'Fit Page' : zoom}
                    </option>
                  ))}
                </select>
              </div>

              <div className={styles.formField}>
                <label className={styles.label}>Scroll Mode</label>
                <select
                  className={styles.select}
                  value={scrollMode}
                  onChange={(e) => setScrollMode(e.target.value as ScrollMode)}
                >
                  {scrollModes.map(mode => (
                    <option key={mode.value} value={mode.value}>{mode.label}</option>
                  ))}
                </select>
              </div>
            </div>

            {/* Appearance Section */}
            <div className={styles.settingsSection}>
              <h3 className={styles.settingsHeading}>Appearance</h3>
              <p className={styles.settingsDescription}>
                Customize the application's visual theme.
              </p>

              <div className={styles.formField}>
                <label className={styles.label}>Theme</label>
                <div className={styles.radioGroup}>
                  <label className={styles.radioLabel}>
                    <input
                      type="radio"
                      name="theme"
                      checked={theme === 'light'}
                      onChange={() => setTheme('light')}
                    />
                    <span>Light</span>
                  </label>
                  <label className={styles.radioLabel}>
                    <input
                      type="radio"
                      name="theme"
                      checked={theme === 'dark'}
                      onChange={() => setTheme('dark')}
                    />
                    <span>Dark</span>
                  </label>
                  <label className={styles.radioLabel}>
                    <input
                      type="radio"
                      name="theme"
                      checked={theme === 'system'}
                      onChange={() => setTheme('system')}
                    />
                    <span>Use System</span>
                  </label>
                </div>
              </div>
            </div>

            {/* Privacy Section */}
            <div className={styles.settingsSection}>
              <h3 className={styles.settingsHeading}>Privacy</h3>
              <p className={styles.settingsDescription}>
                Control data collection and reporting. All data is anonymous.
              </p>

              <label className={styles.toggleLabel}>
                <input
                  type="checkbox"
                  checked={telemetryEnabled}
                  onChange={(e) => setTelemetryEnabled(e.target.checked)}
                />
                <span className={styles.toggleText}>
                  <strong>Telemetry</strong>
                  <span className={styles.toggleDescription}>
                    Helps improve FluentPDF by sending anonymous usage data.
                  </span>
                </span>
              </label>

              <label className={styles.toggleLabel}>
                <input
                  type="checkbox"
                  checked={crashReportingEnabled}
                  onChange={(e) => setCrashReportingEnabled(e.target.checked)}
                />
                <span className={styles.toggleText}>
                  <strong>Crash Reporting</strong>
                  <span className={styles.toggleDescription}>
                    Sends anonymous crash reports to help diagnose issues.
                  </span>
                </span>
              </label>
            </div>

            {/* Reset Section */}
            <div className={styles.settingsSection}>
              <button className={styles.button} onClick={handleReset}>
                Reset All Settings to Defaults
              </button>
              <p className={styles.settingsDescription}>
                Restores all settings to their original default values.
              </p>
            </div>
          </div>
        </div>

        <div className={styles.dialogFooter}>
          <button className={styles.buttonSecondary} onClick={onClose}>
            Close
          </button>
        </div>
      </div>
    </div>
  );
};
