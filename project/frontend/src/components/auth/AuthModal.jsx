import { useState } from 'react';
import './AuthModal.css';

export default function AuthModal({ isOpen, onClose, onLoginSuccess }) {
  const [isLoginMode, setIsLoginMode] = useState(true);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  
  // Поля для входа
  const [loginEmail, setLoginEmail] = useState('');
  const [loginPassword, setLoginPassword] = useState('');
  
  // Поля для регистрации
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [agreeToPolicy, setAgreeToPolicy] = useState(false);

  const handleLogin = async (e) => {
    e.preventDefault();
    setError('');
    
    if (!loginEmail || !loginPassword) {
      setError('Email и пароль обязательны');
      return;
    }

    setLoading(true);
    try {
      const response = await fetch('http://localhost:5151/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          email: loginEmail,
          password: loginPassword
        })
      });

      if (response.ok) {
        const data = await response.json();
        onLoginSuccess(data.user);
        onClose();
      } else {
        setError('Неверный email или пароль');
      }
    } catch (err) {
      setError('Ошибка подключения');
    } finally {
      setLoading(false);
    }
  };

  const handleRegister = async (e) => {
    e.preventDefault();
    setError('');
    
    if (!fullName || !email || !password) {
      setError('Поля не могут быть пустыми');
      return;
    }
    
    if (!agreeToPolicy) {
      setError('Вы должны согласиться с политикой');
      return;
    }
    
    if (password.length < 6) {
      setError('Пароль должен быть не менее 6 символов');
      return;
    }

    setLoading(true);
    try {
      const response = await fetch('http://localhost:5151/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          fullName,
          email,
          password,
          phone
        })
      });

      if (response.ok) {
        const data = await response.json();
        onLoginSuccess(data.user);
        onClose();
      } else {
        setError('Ошибка при регистрации');
      }
    } catch (err) {
      setError('Ошибка подключения');
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-box" onClick={(e) => e.stopPropagation()}>
        {isLoginMode ? (
          // РЕЖИМ ВХОДА
          <form onSubmit={handleLogin}>
            <h3>Вход в аккаунт</h3>
            <input
              className="input"
              type="email"
              placeholder="Email"
              value={loginEmail}
              onChange={(e) => setLoginEmail(e.target.value)}
            />
            <input
              className="input mt-2"
              type="password"
              placeholder="Пароль"
              value={loginPassword}
              onChange={(e) => setLoginPassword(e.target.value)}
            />
            
            {error && <div className="text-danger mt-2">{error}</div>}
            
            <div className="mt-3">
              <button type="submit" className="btn btn-primary" disabled={loading}>
                {loading ? 'Загрузка...' : 'Войти'}
              </button>
              <button type="button" className="btn btn-link" onClick={onClose} disabled={loading}>
                Отмена
              </button>
            </div>
            
            <div className="mt-3 text-center">
              <span>Нет аккаунта? </span>
              <button type="button" className="btn btn-link" onClick={() => setIsLoginMode(false)}>
                Создать
              </button>
            </div>
          </form>
        ) : (
          // РЕЖИМ РЕГИСТРАЦИИ
          <form onSubmit={handleRegister}>
            <h3>Регистрация</h3>
            <input
              className="input"
              type="text"
              placeholder="Имя"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
            />
            <input
              className="input mt-2"
              type="email"
              placeholder="Email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
            />
            <input
              className="input mt-2"
              type="tel"
              placeholder="Телефон"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
            />
            <input
              className="input mt-2"
              type="password"
              placeholder="Пароль"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
            
            <label className="mt-2">
              <input
                type="checkbox"
                checked={agreeToPolicy}
                onChange={(e) => setAgreeToPolicy(e.target.checked)}
              />
              <span> Я согласен с политикой конфиденциальности</span>
            </label>
            
            {error && <div className="text-danger mt-2">{error}</div>}
            
            <div className="mt-3">
              <button type="submit" className="btn btn-primary" disabled={loading}>
                {loading ? 'Загрузка...' : 'Создать аккаунт'}
              </button>
              <button type="button" className="btn btn-link" onClick={onClose} disabled={loading}>
                Отмена
              </button>
            </div>
            
            <div className="mt-3 text-center">
              <button type="button" className="btn btn-link" onClick={() => setIsLoginMode(true)}>
                Вернуться к входу
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}