import { useState } from 'react';
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
  const [password, setPassword] = useState('')  

  const handleLogout = async () => {
    setError('');

    try {
      const token = localStorage.getItem('token');
      const response = await fetch(`${API_URL}/api/auth/logout`, {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });
      if (response.ok) {
        onLogout();
        onClose();
        setTimeout(() => window.location.reload(), 100);

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

          <FloatingInput
            id="password"
            label="Пароль"
            type="password"
            value={password}
            onChange={setPassword}
            disabled={!changeMode}
            placeholder={changeMode ? "Введите новый пароль" : ""}
          />

          {/* Registration date */}
          <div style={{ marginBottom: '1.5rem' }}>
            <label className="profile-date-label">
              Дата регистрации
            </label>
            <p className="profile-date-value">
              {new Date(user.createdAt).toLocaleDateString('ru-RU')}
            </p>
          </div>
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

          {changeMode ? (
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