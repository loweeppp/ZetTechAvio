import { useState } from 'react';
import './ProfileModal.css';

const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

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
      const response = await fetch(`${API_URL}/api/auth/change`, {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          email,
          fullName,
          phone,
          password: password || '', 
          id: user.id
        })
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
  };

  if (!isOpen || !user) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-box" onClick={(e) => e.stopPropagation()}>
        <div className="profile-header">
          <h3>Мой профиль</h3>
          <button type="button" className="btn-close" onClick={onClose}> ✕ </button>
        </div>

        <div className="profile-content">
          <div className="profile-item">
            <label>Имя:</label>
            <input type="text" value={fullName} onChange={(e) => setFullName(e.target.value)}
              disabled={!changeMode} className={changeMode ? ("profile-item-change") : ("profile-item-input")} />
          </div>

          <div className="profile-item">
            <label>Email:</label>
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)}
              disabled={!changeMode} className={changeMode ? ("profile-item-change") : ("profile-item-input")} />
          </div>

          <div className="profile-item">
            <label>Телефон:</label>
            <input type="text" value={phone} onChange={(e) => setPhone(e.target.value)}
              disabled={!changeMode} className={changeMode ? ("profile-item-change") : ("profile-item-input")} />
          </div>

          <div className="profile-item">
            <label>Пароль:</label>
            <input type="password" value={password} onChange={(e) => setPassword(e.target.value)}
              disabled={!changeMode} placeholder={changeMode ? "Введите новый пароль" : "••••••••"} className={changeMode ? ("profile-item-change") : ("profile-item-input")} />
          </div>

          <div className="profile-item">
            <label>Дата регистрации:</label>
            <p>{new Date(user.createdAt).toLocaleDateString('ru-RU')}</p>
          </div>
        </div>

        {error && <div className="text-danger mt-2">{error}</div>}


        <div className="profile-actions">
          <button type="button" className="btn btn-danger" onClick={handleLogout}>
            Выход
          </button>

          {changeMode ? (
            <button type="button" className="btn btn-secondary" onClick={() => { handleChangeMode(); handleSaveChanges(); }}> Сохранить </button>
          ) : (
            <button type="button" className="btn btn-change" onClick={handleChangeMode}> Изменить</button>
          )}

          {changeMode ? (
            <button type="button" className="btn btn-change" onClick={handleChangeMode}> Отменить</button>
          ) : (<></>)}
        </div>
      </div>
    </div>
  );
}