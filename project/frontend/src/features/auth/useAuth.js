import { useState, useEffect } from 'react';

const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

export function useAuth() {
  const [currentUser, setCurrentUser] = useState(null);
  const [token, setToken] = useState(null);
  const [deviceToken, setDeviceToken] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  // Загрузить пользователя и токен при первом рендере
  useEffect(() => {
    const initAuth = async () => {
      try {
        // Загружаем token 
        const storedToken = localStorage.getItem('token');
        const storedDeviceToken = localStorage.getItem('deviceToken');
        
        if (storedToken) {
          setToken(storedToken);
          setDeviceToken(storedDeviceToken);
          
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
    // loginResponse содержит { token, userId, message, deviceToken? }
    localStorage.setItem('token', loginResponse.token);
    setToken(loginResponse.token);

    if (loginResponse.deviceToken) {
      localStorage.setItem('deviceToken', loginResponse.deviceToken);
      setDeviceToken(loginResponse.deviceToken);
    } else {
      localStorage.removeItem('deviceToken');
      setDeviceToken(null);
    }
  };

  // Функция для обновления данных пользователя
  const fetchCurrentUser = async () => {
    if (!token) return;
    
    try {
      const headers = {
        'Authorization': `Bearer ${token}`
      };
      if (deviceToken) {
        headers['X-Device-Token'] = deviceToken;
      }
      const response = await fetch(`${API_URL}/api/auth/current`, {
        headers
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
      const headers = {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      };
      if (deviceToken) {
        headers['X-Device-Token'] = deviceToken;
      }
      const response = await fetch(`${API_URL}/api/auth/change`, {
        method: 'POST',
        headers,
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
      const headers = {
        'Authorization': `Bearer ${token}`
      };
      if (deviceToken) {
        headers['X-Device-Token'] = deviceToken;
      }
      await fetch(`${API_URL}/api/auth/logout`, {
        method: 'POST',
        headers
      });
    } catch (error) {
      console.error('Error logging out:', error);
    } finally {
      // Очищаем токен и device token
      localStorage.removeItem('token');
      localStorage.removeItem('deviceToken');
      setToken(null);
      setDeviceToken(null);
      setCurrentUser(null);
    }
  };

  return { 
    currentUser, 
    setCurrentUser, 
    token,
    deviceToken,
    isLoading, 
    login, 
    logout, 
    changeUser,
    fetchCurrentUser  // Экспортируем для явной загрузки данных
  };
}