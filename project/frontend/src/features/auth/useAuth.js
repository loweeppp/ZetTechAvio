import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';

const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';
const STORAGE_KEY = 'token';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [currentUser, setCurrentUser] = useState(null);
  const [token, setToken] = useState(null);
  const [isLoading, setIsLoading] = useState(true);

  const clearAuth = useCallback(() => {
    localStorage.removeItem(STORAGE_KEY);
    setToken(null);
    setCurrentUser(null);
  }, []);

  const fetchCurrentUser = useCallback(async (currentToken) => {
    const authToken = currentToken || token;
    if (!authToken) {
      setCurrentUser(null);
      return null;
    }

    try {
      const response = await fetch(`${API_URL}/api/auth/current`, {
        headers: {
          Authorization: `Bearer ${authToken}`
        }
      });

      if (!response.ok) {
        clearAuth();
        return null;
      }

      const userData = await response.json();
      setCurrentUser(userData);
      return userData;
    } catch (error) {
      console.error('Error fetching current user:', error);
      clearAuth();
      return null;
    }
  }, [token, clearAuth]);

  useEffect(() => {
    const initAuth = async () => {
      const storedToken = localStorage.getItem(STORAGE_KEY);
      if (storedToken) {
        setToken(storedToken);
        await fetchCurrentUser(storedToken);
      }
      setIsLoading(false);
    };

    initAuth();

    const handleStorage = (event) => {
      if (event.key !== STORAGE_KEY) return;
      if (event.newValue) {
        setToken(event.newValue);
        fetchCurrentUser(event.newValue);
      } else {
        clearAuth();
      }
    };

    window.addEventListener('storage', handleStorage);
    return () => window.removeEventListener('storage', handleStorage);
  }, [fetchCurrentUser, clearAuth]);

  const login = useCallback(async (loginResponse) => {
    if (!loginResponse?.token) return;

    localStorage.setItem(STORAGE_KEY, loginResponse.token);
    setToken(loginResponse.token);
    await fetchCurrentUser(loginResponse.token);
  }, [fetchCurrentUser]);

  const logout = useCallback(async () => {
    try {
      if (token) {
        await fetch(`${API_URL}/api/auth/logout`, {
          method: 'POST',
          headers: {
            Authorization: `Bearer ${token}`
          }
        });
      }
    } catch (error) {
      console.error('Error logging out:', error);
    } finally {
      clearAuth();
    }
  }, [token, clearAuth]);

  const changeUser = useCallback(async (userData) => {
    if (userData?.token) {
      localStorage.setItem(STORAGE_KEY, userData.token);
      setToken(userData.token);
      await fetchCurrentUser(userData.token);
      return;
    }

    if (userData) {
      setCurrentUser(userData);
    }
  }, [fetchCurrentUser]);

  return (
    <AuthContext.Provider
      value={{
        currentUser,
        token,
        isLoading,
        login,
        logout,
        changeUser,
        fetchCurrentUser
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}