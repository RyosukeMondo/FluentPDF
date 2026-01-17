import { useState } from 'react';
import './App.css';
import ThumbnailsSidebar from './components/ThumbnailsSidebar';
import { ThumbnailStates } from './data/dummyThumbnails';

function App() {
  // Initialize with 20 thumbnails, first 10 loaded (partially loaded state)
  const [thumbnails] = useState(() => ThumbnailStates.partiallyLoaded(20));
  const [selectedPage, setSelectedPage] = useState(1);

  const handleThumbnailClick = (pageNumber: number) => {
    console.log(`App: Page ${pageNumber} selected`);
    setSelectedPage(pageNumber);
  };

  const handleThumbnailContextMenu = (pageNumber: number, event: React.MouseEvent) => {
    console.log(`App: Context menu for page ${pageNumber}`, event);
  };

  return (
    <div className="app">
      <header className="app-header">
        <h1>FluentPDF Prototype</h1>
        <p>React prototype for rapid UI iteration</p>
      </header>
      <main className="app-main">
        <div style={{ display: 'flex', gap: '20px', height: '600px' }}>
          <div style={{ width: '200px', border: '1px solid #ccc', borderRadius: '8px', overflow: 'hidden' }}>
            <ThumbnailsSidebar
              thumbnails={thumbnails}
              onThumbnailClick={handleThumbnailClick}
              onThumbnailContextMenu={handleThumbnailContextMenu}
            />
          </div>
          <div style={{ flex: 1, padding: '20px', backgroundColor: '#f9f9f9', borderRadius: '8px' }}>
            <h2>Viewer Area</h2>
            <p>Selected page: {selectedPage}</p>
            <p>The ThumbnailsSidebar component is displayed on the left.</p>
            <ul>
              <li>Click thumbnails to select pages</li>
              <li>Right-click for context menu (check console)</li>
              <li>First 10 thumbnails are loaded, others show loading spinner</li>
              <li>Uses design tokens from tokens.css</li>
            </ul>
          </div>
        </div>
      </main>
    </div>
  );
}

export default App;
