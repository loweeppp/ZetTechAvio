import { useEffect, useRef, useState } from 'react';
import './ProfileModal.css';

const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

function FloatingInput({
  id,
  label,
  type = 'text',
  value,
  onChange,
  disabled = false,
  placeholder,
}) {
  return (
    <div className="floating-input-wrapper">
      <input
        id={id}
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        disabled={disabled}
        placeholder=" "
        className="floating-input"
      />
      <label htmlFor={id} className="floating-label">
        {label}
      </label>
    </div>
  );
}

export default function ProfileModal({ isOpen, onClose, user, onLogout, onChange }) {

  const [changeMode, setChangeMode] = useState(false);
  const [error, setError] = useState('');

  const [fullName, setFullName] = useState(user?.fullName || '');
  const [phone, setPhone] = useState(user?.phone || '');
  const [email, setEmail] = useState(user?.email || '');
  const [password, setPassword] = useState('');

  const [isResetMode, setIsResetMode] = useState(false);
  const [resetStage, setResetStage] = useState('email');
  const [resetEmail, setResetEmail] = useState(user?.email || '');
  const [resetCode, setResetCode] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [resetCooldown, setResetCooldown] = useState(0);

  const handleLogout = async () => {
    setError('');

    try {
      const token = localStorage.getItem('token');
      const deviceToken = localStorage.getItem('deviceToken');
      const headers = {
        'Authorization': `Bearer ${token}`
      };
      if (deviceToken) {
        headers['X-Device-Token'] = deviceToken;
      }

      const response = await fetch(`${API_URL}/api/auth/logout`, {
        method: 'POST',
        headers
      });
      if (response.ok) {
        onLogout();
        // onClose();
        // setTimeout(() => window.location.reload(), 100);

      } else {
        const errorData = await response.json().catch(() => ({}));
        setError(errorData.message || 'Ошибка при выходе');
      }
    } catch (err) {
      console.error('Logout error:', err);
      setError('Ошибка подключения');
    }
  };

  const handleSaveChanges = async () => {
    setError('');

    try {
      const token = localStorage.getItem('token');

      // Всегда отправляем пароль, но пустую строку если не изменялся
      const changeData = {
        email,
        fullName,
        phone,
        password: password, // Отправляем как есть (пустой или заполненный)
        id: user.id
      };

      const response = await fetch(`${API_URL}/api/auth/change`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify(changeData)
      });

      if (response.ok) {
        const data = await response.json();
        if (data.token) {
          localStorage.setItem('token', data.token);
        }
        onChange(data);
        setChangeMode(false);
        setPassword('');
        setTimeout(() => window.location.reload(), 100);

      } else {
        const errorData = await response.json().catch(() => ({}));
        setError(errorData.message || "Ошибка при изменении профиля");
      }
    } catch (err) {
      console.error('Change account error:', err);
      setError('Ошибка подключения');
    }
  }

  const handleChangeMode = () => {
    setChangeMode(prev => !prev);
    // Очищаем пароль и ошибки при отмене редактирования
    if (changeMode) {
      setPassword('');
      setError('');
    }
  };

  useEffect(() => {
    if (resetCooldown <= 0) return;

    const timer = window.setInterval(() => {
      setResetCooldown((prev) => (prev > 0 ? prev - 1 : 0));
    }, 1000);

    return () => window.clearInterval(timer);
  }, [resetCooldown]);

  const handleRequestResetCode = async () => {
    setError('');

    if (!resetEmail || !/\S+@\S+\.\S+/.test(resetEmail)) {
      setError('Введите корректный email для восстановления');
      return;
    }

    setLoading(true);
    try {
      const response = await fetch(`${API_URL}/api/auth/request-password-reset`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ email: resetEmail })
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) {
        setError(result.message || 'Ошибка отправки кода');
        return;
      }
      setResetStage('code');
      setResetCooldown(30);
      setResetCode('');
    } catch (err) {
      setError('Ошибка подключения');
    } finally {
      setLoading(false);
    }
  };

    const verifyResetCode = async (emailArg, codeArg) => {
    if (!emailArg || !codeArg) {
      setError('Введите email и код');
      return;
    }

    setError('');
    try {
      const response = await fetch(`${API_URL}/api/auth/verify-password-reset-code`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ email: emailArg, code: codeArg })
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) {
        setError(result.message || 'Неверный код');
        return;
      }

      setResetStage('verified');
      setError('');
    } catch (err) {
      console.error('Ошибка проверки кода:', err);
      setError('Ошибка проверки кода');
    }
  };

  const handleResetCodeChange = (value) => {
    const digitsOnly = value.replace(/\D/g, '');
    setResetCode(digitsOnly);
  };

  useEffect(() => {
    if (resetStage !== 'code') return;
    if (resetCode.length !== 6) return;
    if (!resetEmail) return;

    verifyResetCode(resetEmail, resetCode);
  }, [resetCode, resetEmail, resetStage]);

  const handleResetPassword = async (e) => {
    e.preventDefault();
    setError('');

    if (!resetEmail || !resetCode || !newPassword) {
      setError('Email, код и новый пароль обязательны');
      return;
    }

    if (newPassword.length < 6) {
      setError('Пароль должен быть не менее 6 символов');
      return;
    }

    setLoading(true);
    try {
      const response = await fetch(`${API_URL}/api/auth/reset-password`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ email: resetEmail, code: resetCode, newPassword })
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) {
        setError(result.message || 'Не удалось сбросить пароль');
        return;
      }

      setError('Пароль сброшен. Введите новый пароль и войдите.');
      setIsResetMode(false);
      setResetStage('email');
      setResetCode('');
      setNewPassword('');
      setPassword('');
    } catch (err) {
      setError('Ошибка подключения');
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen || !user) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-box" onClick={(e) => e.stopPropagation()}>
        {/* Header */}
        <div className="modal-header">
          <h3>Мой профиль</h3>
          <button
            type="button"
            onClick={onClose}
            className="modal-close-btn"
            title="Закрыть"
          >
            ✕
          </button>
        </div>

        {/* Content */}
        <div className="modal-content">
          {isResetMode ? (
            <form onSubmit={handleResetPassword} className="profile-reset-form">
              <FloatingInput
                id="reset-email"
                label="Email"
                type="email"
                value={resetEmail}
                onChange={setResetEmail}
              />


              {resetStage !== 'email' && (
                <FloatingInput
                  id="reset-code"
                  label="Код из письма"
                  type="text"
                  value={resetCode}
                  onChange={handleResetCodeChange}
                />
              )}
              {resetStage !== 'verified' && (
                <button
                  type="button"
                  disabled={loading || resetCooldown > 0}
                  className="btn btn-submit"
                  onClick={handleRequestResetCode}
                >
                  {loading
                    ? 'Загрузка...'
                    : resetCooldown > 0
                      ? `Отправить код (${resetCooldown}s)`
                      : 'Отправить код'}
                </button>
              )}

              {resetStage === 'verified' && (
                <FloatingInput
                  id="reset-new-password"
                  label="Новый пароль"
                  type="password"
                  value={newPassword}
                  onChange={setNewPassword}
                />
              )}

              {resetStage === 'verified' && (
                <button
                  type="submit"
                  disabled={loading}
                  className="btn btn-submit"
                >
                  {loading ? 'Загрузка...' : 'Сохранить пароль'}
                </button>
              )}

              <div className="button-group">
                <button
                  type="button"
                  onClick={() => {
                    setIsResetMode(false);
                    setError('');
                    setResetStage('email');
                    setResetCode('');
                    setNewPassword('');
                  }}
                  disabled={loading}
                  className="btn btn-secondary"
                >
                  ← Вернуться к профилю
                </button>
              </div>
            </form>
          ) : (
            <>
              <FloatingInput
                id="fullName"
                label="Полное имя"
                type="text"
                value={fullName}
                onChange={setFullName}
                disabled={!changeMode}
              />

              <FloatingInput
                id="email"
                label="Email"
                type="email"
                value={email}
                onChange={setEmail}
                disabled={!changeMode}
              />

              <FloatingInput
                id="phone"
                label="Телефон"
                type="text"
                value={phone}
                onChange={setPhone}
                disabled={!changeMode}
              />
              {!isResetMode && (
                <a
                  href="#"
                  className="text-link"
                  onClick={(e) => {
                    e.preventDefault();
                    setIsResetMode(true);
                  }}
                >
                  Сброс пароля
                </a>
              )}
              {/* 
              <FloatingInput
                id="password"
                label="Пароль"
                type="password"
                value={password}
                onChange={setPassword}
                disabled={!changeMode}
                placeholder={changeMode ? "Введите новый пароль" : ""}
              /> */}

            </>
          )}
        </div>

        {/* Error message */}
        {error && <div className="error-message">{error}</div>}

        {/* Actions */}
        <div className="profile-actions">
          <button
            type="button"
            onClick={handleLogout}
            className="btn btn-danger"
          >
            Выход
          </button>

          {isResetMode ? (
            <button
              type="button"
              onClick={() => {
                setIsResetMode(false);
                setError('');
                setResetStage('email');
                setResetCode('');
                setNewPassword('');
              }}
              className="btn btn-secondary"
            >
              Отмена
            </button>
          ) : changeMode ? (
            <>
              <button
                type="button"
                onClick={() => {
                  handleChangeMode();
                  handleSaveChanges();
                }}
                className="btn btn-submit"
              >
                Сохранить
              </button>
              <button
                type="button"
                onClick={handleChangeMode}
                className="btn btn-secondary"
              >
                Отменить
              </button>
            </>
          ) : (
            <button
              type="button"
              onClick={handleChangeMode}
              className="btn btn-submit"
            >
              Изменить
            </button>
          )}
        </div>
      </div>
    </div>
  );
}