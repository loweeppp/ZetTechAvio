import { useState, useEffect } from 'react';

export function useAuth() {
  const [currentUser, setCurrentUser] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  // Загрузить пользователя при первом рендере
  useEffect(() => {
    const userData = localStorage.getItem('user');
    if (userData) {
      setCurrentUser(JSON.parse(userData));
    }
    setIsLoading(false);
  }, []);

  // Функция для сохранения пользователя
  const login = (user) => {
    localStorage.setItem('user', JSON.stringify(user));
    setCurrentUser(user);
  };

  // Функция для очистки (выход)
  const logout = () => {
    localStorage.removeItem('user');
    setCurrentUser(null);
  };

  return { currentUser, setCurrentUser, isLoading, login, logout };
}