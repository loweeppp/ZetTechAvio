import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import translations from './translations';

const TranslationContext = createContext(null);

function getNestedValue(object, path) {
  return String(path)
    .split('.')
    .reduce((value, key) => (value && typeof value === 'object' ? value[key] : undefined), object);
}

export function TranslationProvider({ children }) {
  const [language, setLanguage] = useState(() => {
    const saved = window.localStorage.getItem('siteLanguage');
    return saved === 'en' ? 'en' : 'ru';
  });

  useEffect(() => {
    window.localStorage.setItem('siteLanguage', language);
    document.documentElement.lang = language === 'ru' ? 'ru' : 'en';
  }, [language]);

  const t = useCallback(
    (key) => {
      const value = getNestedValue(translations[language] || translations.ru, key);
      return value !== undefined ? value : key;
    },
    [language],
  );

  const toggleLanguage = useCallback(() => {
    setLanguage((current) => (current === 'ru' ? 'en' : 'ru'));
  }, []);

  const value = useMemo(
    () => ({ language, setLanguage, toggleLanguage, t }),
    [language, toggleLanguage, t],
  );

  return <TranslationContext.Provider value={value}>{children}</TranslationContext.Provider>;
}

export function useTranslation() {
  const context = useContext(TranslationContext);
  if (!context) {
    throw new Error('useTranslation must be used within TranslationProvider');
  }
  return context;
}
