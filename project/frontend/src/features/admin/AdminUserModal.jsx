import { useEffect, useState } from 'react';
import './admin-panel.css';

export default function AdminUserModal({ isOpen, onClose, user, currentUserId, onSave, onToggleBlock }) {
  const isSelfUser = user?.id === currentUserId;
  const [formState, setFormState] = useState({
    email: '',
    fullName: '',
    phone: '',
    password: '',
    role: 'User'
  });

  useEffect(() => {
    if (isOpen && user) {
      setFormState({
        email: user.email || '',
        fullName: user.fullName || '',
        phone: user.phone || '',
        password: '',
        role: user.role || 'User'
      });
    }
  }, [isOpen, user]);

  if (!isOpen || !user) {
    return null;
  }

  const handleChange = (field, value) => {
    setFormState((prev) => ({ ...prev, [field]: value }));
  };

  const handleSubmit = (event) => {
    event.preventDefault();
    onSave({
      email: formState.email,
      fullName: formState.fullName,
      phone: formState.phone,
      password: formState.password,
      role: formState.role
    });
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="admin-modal__box" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>Пользователь #{user.id}</h2>
          <button type="button" className="modal-close-btn" onClick={onClose}>
            ✕
          </button>
        </div>

        <form className="admin-modal__form" onSubmit={handleSubmit}>
          <div className="floating-input-wrapper">
            <input
              id="admin-email"
              type="email"
              value={formState.email}
              onChange={(e) => handleChange('email', e.target.value)}
              placeholder=" "
              disabled={isSelfUser}
            />
            <label htmlFor="admin-email" className="floating-label">Email</label>
          </div>

          <div className="floating-input-wrapper">
            <input
              id="admin-fullname"
              type="text"
              value={formState.fullName}
              onChange={(e) => handleChange('fullName', e.target.value)}
              placeholder=" "
              disabled={isSelfUser}
            />
            <label htmlFor="admin-fullname" className="floating-label">ФИО</label>
          </div>

          <div className="floating-input-wrapper">
            <input
              id="admin-phone"
              type="text"
              value={formState.phone}
              onChange={(e) => handleChange('phone', e.target.value)}
              placeholder=" "
              disabled={isSelfUser}
            />
            <label htmlFor="admin-phone" className="floating-label">Телефон</label>
          </div>

          <div className="admin-modal__select-wrapper">
            <label htmlFor="admin-role">Роль</label>
            <select
              id="admin-role"
              value={formState.role}
              onChange={(e) => handleChange('role', e.target.value)}
              disabled={isSelfUser}
            >
              <option value="User">User</option>
              <option value="Manager">Manager</option>
              <option value="Admin">Admin</option>
            </select>
          </div>

          {isSelfUser && (
            <div className="admin-modal__warning">Нельзя редактировать, блокировать или удалять собственный аккаунт</div>
          )}

          <div className="admin-modal__actions">
            <button
              type="button"
              className="btn btn-danger"
              onClick={() => onToggleBlock(!user.isActive)}
              disabled={isSelfUser}
            >
              {user.isActive ? 'Заблокировать' : 'Разблокировать'}
            </button>
            <button type="submit" className="btn btn-submit" disabled={isSelfUser}>
              Сохранить
            </button>
            <button type="button" className="btn btn-secondary" onClick={onClose}>
              Отмена
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
