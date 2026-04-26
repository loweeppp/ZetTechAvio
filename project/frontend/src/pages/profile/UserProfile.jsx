import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { ArrowLeft, BarChart3, Lock, LogOut, User } from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';
import AuthModal from '../../components/auth/AuthModal';
import ProfileModal from '../../components/auth/ProfileModal';
import './UserProfile.css';

const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

function formatMoney(value) {
  return Number(value || 0).toLocaleString('ru-RU');
}

function getUserInitials(fullName, email) {
  if (fullName) {
    return fullName
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]?.toUpperCase())
      .join('');
  }
  return email?.[0]?.toUpperCase() || 'U';
}

export default function UserProfile() {
    const { currentUser, logout, changeUser } = useAuth();
    const [isAuthModalOpen, setIsAuthModalOpen] = useState(false);
    const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);
    const [stats, setStats] = useState({
        totalTickets: 0,
        spent: 0,
        activeFlights: 0,
        completed: 0
    });
    const [loadingStats, setLoadingStats] = useState(true);

    const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

    // Загружаем статистику при загрузке страницы
    useEffect(() => {
        if (!currentUser?.id) {
            setLoadingStats(false);
            return;
        }

        const loadStats = async () => {
            try {
                const token = localStorage.getItem('token');
                const response = await fetch(`${API_URL}/api/users/${currentUser.id}/bookings`, {
                    headers: {
                        'Authorization': `Bearer ${token}`
                    },
                    credentials: 'include'
                });

                if (!response.ok) {
                    console.error('Ошибка загрузки статистики:', response.status);
                    setLoadingStats(false);
                    return;
                }

                const bookings = await response.json();
                const now = new Date();
                
                // Подсчитываем статистику
                let totalTickets = 0;
                let spent = 0;
                let activeFlights = 0;
                let completed = 0;

                bookings.forEach(booking => {
                    totalTickets += booking.quantity || 1;
                    spent += booking.totalPrice || 0;
                    
                    const departureDate = new Date(booking.flight?.departureDt);
                    if (departureDate > now) {
                        activeFlights++;
                    } else {
                        completed++;
                    }
                });

                setStats({
                    totalTickets,
                    spent,
                    activeFlights,
                    completed
                });
            } catch (err) {
                console.error('Ошибка при загрузке статистики:', err);
            } finally {
                setLoadingStats(false);
            }
        };

        loadStats();
    }, [currentUser?.id]);

    const handleProfileChange = (updatedUser) => {
        changeUser(updatedUser);
    };

    const handleLoginSuccess = async (loginResponse) => {
        setIsAuthModalOpen(false);
        if (loginResponse?.user) {
            changeUser(loginResponse.user);
        }
    };

    const navigate = useNavigate();

    const handleLogout = () => {
        if (window.confirm('Вы уверены, что хотите выйти?')) {
            logout();
            navigate('/');
        }
    };

    if (!currentUser) {
        return (
            <div className="profile-page profile-page--empty">
                <div className="profile-shell">
                    <div className="profile-card profile-card--empty">
                        <h1>Пожалуйста, авторизуйтесь</h1>
                        <p>Чтобы просмотреть профиль и историю поездок, войдите в аккаунт.</p>
                        <button className="btn-primary" onClick={() => setIsAuthModalOpen(true)}>
                            Войти
                        </button>
                    </div>
                </div>
                <AuthModal
                    isOpen={isAuthModalOpen}
                    onClose={() => setIsAuthModalOpen(false)}
                    onLoginSuccess={handleLoginSuccess}
                />
            </div>
        );
    }

    return (
        <div className="profile-page">
            <div className="profile-shell">
                <div className="profile-header">
                    <button className="profile-back" onClick={() => navigate(-1)}>
                        <ArrowLeft className="profile-back-icon" />
                        Назад
                    </button>
                    <div className="profile-header-copy">
                        <h1>Мой профиль</h1>
                        <p className="profile-subtitle">Ваши данные, история поездок и настройки безопасности.</p>
                    </div>
                </div>

                <div className="profile-grid">
                    <section className="profile-card profile-card--info">
                        <div className="profile-card-header">
                            <div className="profile-avatar">{getUserInitials(currentUser.fullName, currentUser.email)}</div>
                            <div>
                                <div className="profile-name">{currentUser.fullName || currentUser.email}</div>
                                <div className="profile-role">Пассажир</div>
                            </div>
                        </div>

                        <div className="profile-details">
                            <div className="profile-detail-row">
                                <div className="profile-detail-label">Имя и фамилия</div>
                                <div className="profile-detail-value">{currentUser.fullName || 'Не указано'}</div>
                            </div>
                            <div className="profile-detail-row">
                                <div className="profile-detail-label">Email</div>
                                <div className="profile-detail-value">{currentUser.email}</div>
                            </div>
                            <div className="profile-detail-row">
                                <div className="profile-detail-label">Телефон</div>
                                <div className="profile-detail-value">{currentUser.phone || currentUser.phoneNumber || 'Не указан'}</div>
                            </div>
                        </div>
                    </section>

                    <section className="profile-card profile-card--stats">
                        <div className="profile-card-header">
                            <BarChart3 className="profile-card-icon" />
                            <div>
                                <h2>Статистика</h2>
                                <p>Показатели вашей активности и расходов.</p>
                            </div>
                        </div>

                        <div className="stats-grid stats-grid--profile">
                            <div className="stat-tile">
                                <div className="stat-value">{loadingStats ? '...' : stats.totalTickets}</div>
                                <div className="stat-label">Всего билетов</div>
                            </div>
                            <div className="stat-tile">
                                <div className="stat-value">{loadingStats ? '...' : `${formatMoney(stats.spent)} ₽`}</div>
                                <div className="stat-label">Потрачено</div>
                            </div>
                            <div className="stat-tile">
                                <div className="stat-value">{loadingStats ? '...' : stats.activeFlights}</div>
                                <div className="stat-label">Активные рейсы</div>
                            </div>
                            <div className="stat-tile">
                                <div className="stat-value">{loadingStats ? '...' : stats.completed}</div>
                                <div className="stat-label">Завершено</div>
                            </div>
                        </div>
                    </section>
                </div>

                <div className="profile-grid profile-grid--tight">
                    <section className="profile-card profile-card--security">
                        <div className="profile-card-header">
                            <Lock className="profile-card-icon" />
                            <div>
                                <h2>Безопасность</h2>
                                <p>Настройте доступ и защиту аккаунта.</p>
                            </div>
                        </div>

                        <div className="security-list">
                            <button className="security-action" onClick={() => setIsProfileModalOpen(true)}>
                                <span className="security-action-icon"></span>
                                Изменить пароль
                            </button>
                            <button className="security-action">
                                <span className="security-action-icon"></span>
                                Двухфакторная аутентификация
                            </button>
                        </div>
                    </section>

                    <section className="profile-card profile-card--logout">
                        <div className="logout-panel">
                            <div>
                                <h2>Выйти из аккаунта</h2>
                                <p>Безопасно завершите сессию и защитите данные.</p>
                            </div>
                            <button className="btn-logout" onClick={handleLogout}>
                                <LogOut className="btn-logout-icon" />
                                Выйти
                            </button>
                        </div>
                    </section>
                </div>
            </div>

            <ProfileModal
                isOpen={isProfileModalOpen}
                onClose={() => setIsProfileModalOpen(false)}
                user={currentUser}
                onLogout={handleLogout}
                onChange={handleProfileChange}
            />
        </div>
    );
}

