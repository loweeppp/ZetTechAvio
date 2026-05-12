import { useEffect, useState, useCallback } from 'react';
import { useAuth } from '../auth/useAuth';
import { useNavigate } from 'react-router-dom';
import AdminUserModal from './AdminUserModal';
import './admin-panel.css';

const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

export default function AdminUsersPage() {
  const { currentUser, token, isLoading } = useAuth();
  const navigate = useNavigate();
  const [users, setUsers] = useState([]);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [selectedUser, setSelectedUser] = useState(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [totalCount, setTotalCount] = useState(0);

  const isAdmin = currentUser && ['Admin', 'Manager'].includes(currentUser.role);

  useEffect(() => {
    if (isLoading) return;
    if (!currentUser || !isAdmin) {
      navigate('/');
    }
  }, [currentUser, isAdmin, isLoading, navigate]);

  const loadUsers = useCallback(async () => {
    if (!token || !isAdmin) return;

    setLoading(true);
    setError(null);

    try {
      const params = new URLSearchParams({ page: String(page), pageSize: '20', search }).toString();
      const response = await fetch(`${API_URL}/api/admin/users?${params}`, {
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        const payload = await response.json().catch(() => null);
        throw new Error(payload?.message || 'Ошибка загрузки списка пользователей');
      }

      const data = await response.json();
      setUsers(data.data || []);
      setTotalCount(data.total || 0);
    } catch (err) {
      setError(err.message || 'Ошибка загрузки данных');
      setUsers([]);
    } finally {
      setLoading(false);
    }
  }, [token, page, search, isAdmin]);

  useEffect(() => {
    if (!isLoading) {
      loadUsers();
    }
  }, [isLoading, loadUsers]);

  const handleOpenModal = (user) => {
    setSelectedUser(user);
    setIsModalOpen(true);
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
    setSelectedUser(null);
  };

  const handleSave = async (payload) => {
    if (!selectedUser) return;
    setLoading(true);
    try {
      const response = await fetch(`${API_URL}/api/admin/users/${selectedUser.id}`, {
        method: 'PUT',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
      });

      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.message || 'Ошибка обновления пользователя');
      }

      await loadUsers();
      handleCloseModal();
    } catch (err) {
      alert(err.message || 'Ошибка обновления пользователя');
    } finally {
      setLoading(false);
    }
  };

  const handleToggleBlock = async (isBlocked) => {
    if (!selectedUser) return;
    setLoading(true);
    try {
      const response = await fetch(`${API_URL}/api/admin/users/${selectedUser.id}/toggle-block`, {
        method: 'POST',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ isBlocked })
      });

      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.message || 'Ошибка изменения статуса');
      }

      await loadUsers();
      handleCloseModal();
    } catch (err) {
      alert(err.message || 'Ошибка изменения статуса');
    } finally {
      setLoading(false);
    }
  };

  if (isLoading) {
    return <div className="admin-panel__loading">Загрузка...</div>;
  }

  return (
    <div className="admin-panel__container">
      <header className="admin-panel__header">
        <div>
          <h1 className="admin-panel__title">Управление пользователями</h1>
          <p className="admin-panel__subtitle">Редактирование профилей, изменение ролей и блокировка пользователей.</p>
        </div>

        <div className="admin-panel__controls">
          <input
            type="text"
            placeholder="Поиск по имени, email или телефону"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
            className="admin-panel__search"
          />
          <div className="admin-panel__paging">
            <button type="button" className="admin-panel__btn" disabled={page <= 1} onClick={() => setPage((prev) => Math.max(1, prev - 1))}>
              ←
            </button>
            <span className="admin-panel__page">{page}</span>
            <button type="button" className="admin-panel__btn" disabled={users.length < 20} onClick={() => setPage((prev) => prev + 1)}>
              →
            </button>
          </div>
        </div>
      </header>

      {error && <div className="admin-panel__error">{error}</div>}
      {loading ? (
        <div className="admin-panel__loading">Загрузка...</div>
      ) : (
        <div className="admin-panel__table-wrapper">
          <table className="admin-panel__table">
            <thead>
              <tr>
                <th>ID</th>
                <th>Email</th>
                <th>ФИО</th>
                <th>Телефон</th>
                <th>Роль</th>
                <th>Статус</th>
                <th>Создан</th>
                <th>Действия</th>
              </tr>
            </thead>
            <tbody>
              {users.length === 0 && (
                <tr>
                  <td colSpan="8" className="admin-panel__empty">Пользователи не найдены</td>
                </tr>
              )}
              {users.map((user) => (
                <tr key={user.id} className={!user.isActive ? 'admin-panel__row--disabled' : ''}>
                  <td>{user.id}</td>
                  <td>{user.email}</td>
                  <td>{user.fullName}</td>
                  <td>{user.phone}</td>
                  <td>{user.role}</td>
                  <td>
                    <span className={`admin-panel__status admin-panel__status--${user.isActive ? 'active' : 'blocked'}`}>
                      {user.isActive ? 'Активен' : 'Заблокирован'}
                    </span>
                  </td>
                  <td>{new Date(user.createdAt).toLocaleDateString('ru-RU')}</td>
                  <td>
                    <button className="admin-panel__action" type="button" onClick={() => handleOpenModal(user)}>
                      Редактировать
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <AdminUserModal
        isOpen={isModalOpen}
        onClose={handleCloseModal}
        user={selectedUser}
        onSave={handleSave}
        onToggleBlock={handleToggleBlock}
      />
    </div>
  );
}
