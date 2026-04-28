import { useState, useEffect } from 'react';

const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

export function useAuth() {
  const [currentUser, setCurrentUser] = useState(null);
  const [token, setToken] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  // Загрузить пользователя и токен при первом рендере
  useEffect(() => {
    const initAuth = async () => {
      try {
        // Загружаем token 
        const storedToken = localStorage.getItem('token');
        
        if (storedToken) {
          setToken(storedToken);
          
          const response = await fetch(`${API_URL}/api/auth/current`, {
            headers: {
              'Authorization': `Bearer ${storedToken}`
            }
          });
          
          if (response.ok) {
            const userData = await response.json();
            setCurrentUser(userData);
          } else {
            // Токен невалиден, очищаем
            localStorage.removeItem('token');
            setToken(null);
            setCurrentUser(null);
          }
        }
      } catch (error) {
        console.error('Error loading auth:', error);
      } finally {
        setIsLoading(false);
      }
    };

    initAuth();
  }, []);

  //  Функция для входа 
  const login = (loginResponse) => {
    // loginResponse содержит { token, userId, message }
    localStorage.setItem('token', loginResponse.token);
    setToken(loginResponse.token);
    
  };

  // Функция для обновления данных пользователя
  const fetchCurrentUser = async () => {
    if (!token) return;
    
    try {
      const response = await fetch(`${API_URL}/api/auth/current`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });
      
      if (response.ok) {
        const userData = await response.json();
        setCurrentUser(userData);
        return userData;
      } else {
        console.error('Failed to fetch current user');
        return null;
      }
    } catch (error) {
      console.error('Error fetching current user:', error);
      return null;
    }
  };

  // Функция для изменения данных пользователя
  const changeUser = async (userData) => {
    try {
      const response = await fetch(`${API_URL}/api/auth/change`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(userData)
      });
      
      if (response.ok) {
        const result = await response.json();
        // Обновляем token если вернулся новый
        if (result.token) {
          localStorage.setItem('token', result.token);
          setToken(result.token);
        }
        // Загружаем обновленные данные пользователя
        await fetchCurrentUser();
      }
    } catch (error) {
      console.error('Error changing user:', error);
    }
  };

  // Функция для выхода
  const logout = async () => {
    try {
      await fetch(`${API_URL}/api/auth/logout`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });
    } catch (error) {
      console.error('Error logging out:', error);
    } finally {
      // Очищаем ТОЛЬКО token 
      localStorage.removeItem('token');
      setToken(null);
      setCurrentUser(null);
    }
  };

  return { 
    currentUser, 
    setCurrentUser, 
    token,
    isLoading, 
    login, 
    logout, 
    changeUser,
    fetchCurrentUser  // Экспортируем для явной загрузки данных
  };
}