import React, { useState } from 'react';
import styles from './layouts.module.css';

interface Tab {
  id: string;
  displayName: string;
  content: React.ReactNode;
}

interface MainWindowProps {
  children?: React.ReactNode;
}

export const MainWindow: React.FC<MainWindowProps> = ({ children }) => {
  const [tabs, setTabs] = useState<Tab[]>([]);
  const [activeTabId, setActiveTabId] = useState<string | null>(null);
  const [showEmptyState, setShowEmptyState] = useState(true);

  const handleOpenFile = () => {
    console.log('Open file clicked');
    // Stub: would open file dialog
  };

  const handleSave = () => {
    console.log('Save clicked');
  };

  const handleSaveAs = () => {
    console.log('Save As clicked');
  };

  const handleClearRecentFiles = () => {
    console.log('Clear recent files clicked');
  };

  const handleExit = () => {
    console.log('Exit clicked');
  };

  const handleSettings = () => {
    console.log('Settings clicked');
  };

  const handleAddTab = () => {
    console.log('Add tab clicked');
    const newTab: Tab = {
      id: `tab-${Date.now()}`,
      displayName: `Document ${tabs.length + 1}`,
      content: children,
    };
    setTabs([...tabs, newTab]);
    setActiveTabId(newTab.id);
    setShowEmptyState(false);
  };

  const handleCloseTab = (tabId: string) => {
    const updatedTabs = tabs.filter(tab => tab.id !== tabId);
    setTabs(updatedTabs);

    if (activeTabId === tabId) {
      setActiveTabId(updatedTabs.length > 0 ? updatedTabs[0].id : null);
    }

    if (updatedTabs.length === 0) {
      setShowEmptyState(true);
    }
  };

  const activeTab = tabs.find(tab => tab.id === activeTabId);

  return (
    <div className={styles.mainWindow}>
      {/* Menu Bar */}
      <div className={styles.menuBar}>
        <div className={styles.menuBarItem}>
          <span className={styles.menuBarTitle}>File</span>
          <div className={styles.menuDropdown}>
            <button className={styles.menuItem} onClick={handleOpenFile}>
              Open... <span className={styles.shortcut}>Ctrl+O</span>
            </button>
            <div className={styles.menuSeparator} />
            <button className={styles.menuItem} onClick={handleSave}>
              Save <span className={styles.shortcut}>Ctrl+S</span>
            </button>
            <button className={styles.menuItem} onClick={handleSaveAs}>
              Save As... <span className={styles.shortcut}>Ctrl+Shift+S</span>
            </button>
            <div className={styles.menuSeparator} />
            <button className={styles.menuItem}>Recent Files</button>
            <button className={styles.menuItem} onClick={handleClearRecentFiles}>
              Clear Recent Files...
            </button>
            <div className={styles.menuSeparator} />
            <button className={styles.menuItem} onClick={handleExit}>
              Exit
            </button>
          </div>
        </div>
        <div className={styles.menuBarItem}>
          <span className={styles.menuBarTitle}>Tools</span>
          <div className={styles.menuDropdown}>
            <button className={styles.menuItem} onClick={handleSettings}>
              Settings...
            </button>
          </div>
        </div>
      </div>

      {/* Tab View */}
      <div className={styles.tabView}>
        {tabs.length > 0 && (
          <div className={styles.tabStrip}>
            <div className={styles.tabList}>
              {tabs.map(tab => (
                <div
                  key={tab.id}
                  className={`${styles.tab} ${tab.id === activeTabId ? styles.tabActive : ''}`}
                  onClick={() => setActiveTabId(tab.id)}
                >
                  <span className={styles.tabIcon}>📄</span>
                  <span className={styles.tabTitle}>{tab.displayName}</span>
                  <button
                    className={styles.tabCloseButton}
                    onClick={(e) => {
                      e.stopPropagation();
                      handleCloseTab(tab.id);
                    }}
                    aria-label="Close tab"
                  >
                    ✕
                  </button>
                </div>
              ))}
            </div>
            <button className={styles.addTabButton} onClick={handleAddTab} aria-label="Add tab">
              +
            </button>
          </div>
        )}

        {/* Tab Content */}
        <div className={styles.tabContent}>
          {showEmptyState ? (
            <div className={styles.emptyState}>
              <div className={styles.emptyStateIcon}>📄</div>
              <h2 className={styles.emptyStateTitle}>No PDFs open</h2>
              <button className={styles.emptyStateButton} onClick={handleOpenFile}>
                Open File
              </button>
            </div>
          ) : (
            activeTab?.content
          )}
        </div>
      </div>
    </div>
  );
};
