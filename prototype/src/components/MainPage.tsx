import React from 'react';
import styles from './layouts.module.css';

export const MainPage: React.FC = () => {
  return (
    <div className={styles.mainPage}>
      <div className={styles.mainPageContent}>
        <h1 className={styles.mainPageTitle}>FluentPDF</h1>
        <p className={styles.mainPageSubtitle}>Welcome to FluentPDF</p>
      </div>
    </div>
  );
};
