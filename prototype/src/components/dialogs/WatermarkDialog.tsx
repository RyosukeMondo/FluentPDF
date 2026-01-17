import React, { useState } from 'react';
import styles from './dialogs.module.css';

interface WatermarkDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onApply: () => void;
}

type WatermarkType = 'text' | 'image';
type Position = 'center' | 'topLeft' | 'topRight' | 'bottomLeft' | 'bottomRight' | 'custom';

export const WatermarkDialog: React.FC<WatermarkDialogProps> = ({ isOpen, onClose, onApply }) => {
  const [watermarkType, setWatermarkType] = useState<WatermarkType>('text');
  const [text, setText] = useState('');
  const [fontFamily, setFontFamily] = useState('Arial');
  const [fontSize, setFontSize] = useState(48);
  const [textColor, setTextColor] = useState('#808080');
  const [imagePath, setImagePath] = useState('');
  const [imageScale, setImageScale] = useState(100);
  const [position, setPosition] = useState<Position>('center');
  const [customX, setCustomX] = useState(50);
  const [customY, setCustomY] = useState(50);
  const [opacity, setOpacity] = useState(50);
  const [rotation, setRotation] = useState(0);
  const [behindContent, setBehindContent] = useState(false);
  const [pageRange, setPageRange] = useState('all');
  const [customPageRange, setCustomPageRange] = useState('');

  if (!isOpen) return null;

  const handlePresetClick = (preset: string) => {
    setText(preset);
    console.log('Applied preset:', preset);
  };

  const handleSelectImage = () => {
    console.log('Select image clicked');
    setImagePath('/path/to/selected/image.png');
  };

  const handleApply = () => {
    console.log('Watermark configuration:', {
      type: watermarkType,
      text,
      fontFamily,
      fontSize,
      textColor,
      imagePath,
      imageScale,
      position,
      customX,
      customY,
      opacity,
      rotation,
      behindContent,
      pageRange,
      customPageRange
    });
    onApply();
  };

  const fontFamilies = ['Arial', 'Times New Roman', 'Courier New', 'Georgia', 'Verdana'];
  const positions = [
    { value: 'center', label: 'Center' },
    { value: 'topLeft', label: 'Top Left' },
    { value: 'topRight', label: 'Top Right' },
    { value: 'bottomLeft', label: 'Bottom Left' },
    { value: 'bottomRight', label: 'Bottom Right' },
    { value: 'custom', label: 'Custom' }
  ];
  const pageRangeOptions = [
    { value: 'all', label: 'All Pages' },
    { value: 'current', label: 'Current Page' },
    { value: 'custom', label: 'Custom Range' }
  ];

  return (
    <div className={styles.dialogOverlay} onClick={onClose}>
      <div className={styles.dialogContainer} onClick={(e) => e.stopPropagation()}>
        <div className={styles.dialogHeader}>
          <h2 className={styles.dialogTitle}>Add Watermark</h2>
        </div>

        <div className={styles.dialogContent}>
          {/* Quick Presets */}
          <div className={styles.section}>
            <h3 className={styles.sectionTitle}>Quick Presets</h3>
            <div className={styles.presetButtons}>
              <button className={styles.presetButton} onClick={() => handlePresetClick('CONFIDENTIAL')}>
                CONFIDENTIAL
              </button>
              <button className={styles.presetButton} onClick={() => handlePresetClick('DRAFT')}>
                DRAFT
              </button>
              <button className={styles.presetButton} onClick={() => handlePresetClick('COPY')}>
                COPY
              </button>
              <button className={styles.presetButton} onClick={() => handlePresetClick('APPROVED')}>
                APPROVED
              </button>
            </div>
          </div>

          <div className={styles.watermarkLayout}>
            {/* Configuration Panel */}
            <div className={styles.configPanel}>
              {/* Watermark Type */}
              <div className={styles.section}>
                <h3 className={styles.sectionTitle}>Watermark Type</h3>
                <div className={styles.radioGroup}>
                  <label className={styles.radioLabel}>
                    <input
                      type="radio"
                      name="watermarkType"
                      checked={watermarkType === 'text'}
                      onChange={() => setWatermarkType('text')}
                    />
                    <span>Text</span>
                  </label>
                  <label className={styles.radioLabel}>
                    <input
                      type="radio"
                      name="watermarkType"
                      checked={watermarkType === 'image'}
                      onChange={() => setWatermarkType('image')}
                    />
                    <span>Image</span>
                  </label>
                </div>
              </div>

              {/* Text Configuration */}
              {watermarkType === 'text' && (
                <div className={styles.section}>
                  <h3 className={styles.sectionTitle}>Text Settings</h3>

                  <div className={styles.formField}>
                    <label className={styles.label}>Watermark Text</label>
                    <input
                      type="text"
                      className={styles.textInput}
                      placeholder="Enter watermark text"
                      value={text}
                      onChange={(e) => setText(e.target.value)}
                      maxLength={100}
                    />
                  </div>

                  <div className={styles.formField}>
                    <label className={styles.label}>Font Family</label>
                    <select
                      className={styles.select}
                      value={fontFamily}
                      onChange={(e) => setFontFamily(e.target.value)}
                    >
                      {fontFamilies.map(font => (
                        <option key={font} value={font}>{font}</option>
                      ))}
                    </select>
                  </div>

                  <div className={styles.formField}>
                    <label className={styles.label}>Font Size: {fontSize}pt</label>
                    <input
                      type="range"
                      className={styles.slider}
                      min="12"
                      max="144"
                      value={fontSize}
                      onChange={(e) => setFontSize(Number(e.target.value))}
                    />
                  </div>

                  <div className={styles.formField}>
                    <label className={styles.label}>Text Color</label>
                    <input
                      type="color"
                      className={styles.colorInput}
                      value={textColor}
                      onChange={(e) => setTextColor(e.target.value)}
                    />
                  </div>
                </div>
              )}

              {/* Image Configuration */}
              {watermarkType === 'image' && (
                <div className={styles.section}>
                  <h3 className={styles.sectionTitle}>Image Settings</h3>

                  <button className={styles.button} onClick={handleSelectImage}>
                    Select Image
                  </button>

                  {imagePath && (
                    <>
                      <p className={styles.imagePath}>{imagePath}</p>
                      <div className={styles.formField}>
                        <label className={styles.label}>Scale: {imageScale}%</label>
                        <input
                          type="range"
                          className={styles.slider}
                          min="10"
                          max="200"
                          step="5"
                          value={imageScale}
                          onChange={(e) => setImageScale(Number(e.target.value))}
                        />
                      </div>
                    </>
                  )}
                </div>
              )}

              {/* Position & Appearance */}
              <div className={styles.section}>
                <h3 className={styles.sectionTitle}>Position &amp; Appearance</h3>

                <div className={styles.formField}>
                  <label className={styles.label}>Position</label>
                  <select
                    className={styles.select}
                    value={position}
                    onChange={(e) => setPosition(e.target.value as Position)}
                  >
                    {positions.map(pos => (
                      <option key={pos.value} value={pos.value}>{pos.label}</option>
                    ))}
                  </select>
                </div>

                {position === 'custom' && (
                  <div className={styles.customPosition}>
                    <div className={styles.formField}>
                      <label className={styles.label}>X Position (%)</label>
                      <input
                        type="number"
                        className={styles.numberInput}
                        min="0"
                        max="100"
                        value={customX}
                        onChange={(e) => setCustomX(Number(e.target.value))}
                      />
                    </div>
                    <div className={styles.formField}>
                      <label className={styles.label}>Y Position (%)</label>
                      <input
                        type="number"
                        className={styles.numberInput}
                        min="0"
                        max="100"
                        value={customY}
                        onChange={(e) => setCustomY(Number(e.target.value))}
                      />
                    </div>
                  </div>
                )}

                <div className={styles.formField}>
                  <label className={styles.label}>Opacity: {opacity}%</label>
                  <input
                    type="range"
                    className={styles.slider}
                    min="0"
                    max="100"
                    step="5"
                    value={opacity}
                    onChange={(e) => setOpacity(Number(e.target.value))}
                  />
                </div>

                <div className={styles.formField}>
                  <div className={styles.rotationHeader}>
                    <label className={styles.label}>Rotation: {rotation}°</label>
                    <button
                      className={styles.diagonalButton}
                      onClick={() => setRotation(45)}
                    >
                      45° Diagonal
                    </button>
                  </div>
                  <input
                    type="range"
                    className={styles.slider}
                    min="-180"
                    max="180"
                    step="5"
                    value={rotation}
                    onChange={(e) => setRotation(Number(e.target.value))}
                  />
                </div>

                <label className={styles.checkboxLabel}>
                  <input
                    type="checkbox"
                    checked={behindContent}
                    onChange={(e) => setBehindContent(e.target.checked)}
                  />
                  <span>Place behind content</span>
                </label>
              </div>

              {/* Page Range */}
              <div className={styles.section}>
                <h3 className={styles.sectionTitle}>Apply To</h3>

                <div className={styles.formField}>
                  <label className={styles.label}>Page Range</label>
                  <select
                    className={styles.select}
                    value={pageRange}
                    onChange={(e) => setPageRange(e.target.value)}
                  >
                    {pageRangeOptions.map(opt => (
                      <option key={opt.value} value={opt.value}>{opt.label}</option>
                    ))}
                  </select>
                </div>

                {pageRange === 'custom' && (
                  <div className={styles.formField}>
                    <label className={styles.label}>Page Range (e.g., 1-5, 10, 15-20)</label>
                    <input
                      type="text"
                      className={styles.textInput}
                      placeholder="Enter page range"
                      value={customPageRange}
                      onChange={(e) => setCustomPageRange(e.target.value)}
                    />
                  </div>
                )}
              </div>
            </div>

            {/* Preview Panel */}
            <div className={styles.previewPanel}>
              <div className={styles.previewHeader}>
                <h3 className={styles.sectionTitle}>Preview</h3>
              </div>
              <div className={styles.previewContent}>
                <div className={styles.previewPlaceholder}>
                  <div className={styles.previewIcon}>📄</div>
                  <p className={styles.previewText}>Preview will appear here</p>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div className={styles.dialogFooter}>
          <button className={styles.buttonSecondary} onClick={onClose}>
            Cancel
          </button>
          <button className={styles.buttonPrimary} onClick={handleApply}>
            Apply
          </button>
        </div>
      </div>
    </div>
  );
};
