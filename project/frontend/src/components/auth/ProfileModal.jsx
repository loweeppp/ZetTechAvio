import './ProfileModal.css';

export default function ProfileModal({ isOpen, onClose, user, onLogout }) {
  const handleLogout = async () => {
    try {
      await fetch('http://localhost:5151/api/auth/logout', {
        method: 'POST'
      });
      onLogout();
      onClose();
    } catch (err) {
      console.error('Logout error:', err);
    }
  };

  if (!isOpen || !user) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-box" onClick={(e) => e.stopPropagation()}>
        <div className="profile-header">
          <h3>Мой профиль</h3>
          <button type="button" className="btn-close" onClick={onClose}>✕</button>
        </div>

        <div className="profile-content">
          <div className="profile-item">
            <label>Имя:</label>
            <p>{user.fullName}</p>
          </div>

          <div className="profile-item">
            <label>Email:</label>
            <p>{user.email}</p>
          </div>

          {user.phone && (
            <div className="profile-item">
              <label>Телефон:</label>
              <p>{user.phone}</p>
            </div>
          )}

          <div className="profile-item">
            <label>Дата регистрации:</label>
            <p>{new Date(user.createdAt).toLocaleDateString('ru-RU')}</p>
          </div>
        </div>

        <div className="profile-actions">
          <button type="button" className="btn btn-danger" onClick={handleLogout}>
            Выход
          </button>
          <button type="button" className="btn btn-secondary" onClick={onClose}>
            Закрыть
          </button>
        </div>
      </div>
    </div>
  );
}