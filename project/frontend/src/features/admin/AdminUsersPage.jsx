import { useEffect, useState, useCallback } from 'react';
import { useAuth } from '../auth/useAuth';
import { useNavigate } from 'react-router-dom';
import AdminUserModal from './AdminUserModal';
import './admin-panel.css';

const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

export default function AdminUsersPage() {
  const { currentUser, token, isLoading } = useAuth();
  const currentUserId = currentUser?.id;
  const navigate = useNavigate();
  const [users, setUsers] = useState([]);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [sortConfig, setSortConfig] = useState({ field: 'id', direction: 'asc' });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [selectedUser, setSelectedUser] = useState(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [totalCount, setTotalCount] = useState(0);

  const isAdmin = currentUser?.role === 'Admin';

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
    if (selectedUser.id === currentUserId) {
      alert('Нельзя заблокировать или разблокировать собственный аккаунт');
      return;
    }

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

  const handleDelete = async (idToDelete) => {
    if (!token) return;
    if (idToDelete === currentUserId) {
      alert('Нельзя удалить собственный аккаунт');
      return;
    }

    if (!window.confirm('Вы уверены, что хотите удалить этот аккаунт?')) {
      return;
    }

    setLoading(true);
    try {
      const response = await fetch(`${API_URL}/api/admin/users/${idToDelete}`, {
        method: 'DELETE',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        const data = await response.json().catch(() => null);
        throw new Error(data?.message || 'Ошибка удаления аккаунта');
      }

      await loadUsers();
      if (selectedUser?.id === idToDelete) {
        handleCloseModal();
      }
    } catch (err) {
      alert(err.message || 'Ошибка удаления аккаунта');
    } finally {
      setLoading(false);
    }
  };

  const sortedUsers = users.slice().sort((a, b) => {
    const { field, direction } = sortConfig;
    const aValue = a[field];
    const bValue = b[field];

    if (aValue == null && bValue == null) return 0;
    if (aValue == null) return 1;
    if (bValue == null) return -1;

    let compareValue = 0;

    if (field === 'createdAt') {
      compareValue = new Date(aValue) - new Date(bValue);
    } else if (field === 'id') {
      compareValue = Number(aValue) - Number(bValue);
    } else {
      compareValue = String(aValue).localeCompare(String(bValue), 'ru', { sensitivity: 'base' });
    }

    return direction === 'asc' ? compareValue : -compareValue;
  });

  const requestSort = (field) => {
    setSortConfig((prev) => {
      if (prev.field === field) {
        return {
          field,
          direction: prev.direction === 'asc' ? 'desc' : 'asc'
        };
      }
      return {
        field,
        direction: 'asc'
      };
    });
  };

  const renderSortIcon = (field) => {
    if (sortConfig.field !== field) return '';
    return sortConfig.direction === 'asc' ? ' ↑' : ' ↓';
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
                <th className="sortable" onClick={() => requestSort('id')}>
                  ID{renderSortIcon('id')}
                </th>
                <th className="sortable" onClick={() => requestSort('email')}>
                  Email{renderSortIcon('email')}
                </th>
                <th className="sortable" onClick={() => requestSort('fullName')}>
                  ФИО{renderSortIcon('fullName')}
                </th>
                <th className="sortable" onClick={() => requestSort('phone')}>
                  Телефон{renderSortIcon('phone')}
                </th>
                <th className="sortable" onClick={() => requestSort('role')}>
                  Роль{renderSortIcon('role')}
                </th>
                <th className="sortable" onClick={() => requestSort('isActive')}>
                  Статус{renderSortIcon('isActive')}
                </th>
                <th className="sortable" onClick={() => requestSort('createdAt')}>
                  Создан{renderSortIcon('createdAt')}
                </th>
                <th>Действия</th>
              </tr>
            </thead>
            <tbody>
              {sortedUsers.length === 0 && (
                <tr>
                  <td colSpan="8" className="admin-panel__empty">Пользователи не найдены</td>
                </tr>
              )}
              {sortedUsers.map((user) => (
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
                  <td className="admin-panel__action-group">
                    {user.id === currentUserId ? (
                      <span>⠀</span>
                    ) : (
                      <>
                        <button className="admin-panel__action" type="button" onClick={() => handleOpenModal(user)}>
                          Редактировать
                        </button>
                        <button className="admin-panel__action admin-panel__action--danger" type="button" onClick={() => handleDelete(user.id)}>
                          Удалить
                        </button>
                      </>
                    )}
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
        currentUserId={currentUserId}
        onSave={handleSave}
        onToggleBlock={handleToggleBlock}
        onDelete={handleDelete}
      />
    </div>
  );
}
