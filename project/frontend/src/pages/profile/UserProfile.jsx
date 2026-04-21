import { useState, useEffect } from 'react';
import { useAuth } from '../../hooks/useAuth';
import AuthModal from '../../components/auth/AuthModal';
import ProfileModal from '../../components/auth/ProfileModal';
import './UserProfile.css';

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
        // Обработка успешного входа если нужна
    };

    const handleLogout = () => {
        if (window.confirm('Вы уверены, что хотите выйти?')) {
            logout();
            window.location.href = '/';
        }
    };

    if (!currentUser) {
        return (
            <div className="profile-container">
                <p>Пожалуйста, авторизуйтесь для просмотра профиля</p>
            </div>
        );
    }

    return (
        <>
            <div className="profile-page">
                <div className="profile-container">
                    <h1>👤 Мой Профиль</h1>

                    {/* Основная информация */}
                    <div className="profile-section">
                        <h2>Личная информация</h2>
                        <div className="profile-info">
                            <div className="info-group">
                                <label>Имя и фамилия:</label>
                                <p>{currentUser.fullName}</p>
                            </div>
                            <div className="info-group">
                                <label>Email:</label>
                                <p>{currentUser.email}</p>
                            </div>
                            <div className="info-group">
                                <label>Номер телефона:</label>
                                <p>{currentUser.phone || currentUser.phoneNumber || 'Не указан'}</p>
                            </div>
                        </div>
                    </div>

                    {/* Статистика */}
                    <div className="profile-section">
                        <h2>📊 Статистика</h2>
                        <div className="stats-grid">
                            <div className="stat-card">
                                <div className="stat-value">{loadingStats ? '...' : stats.totalTickets}</div>
                                <div className="stat-label">Всего билетов</div>
                            </div>
                            <div className="stat-card">
                                <div className="stat-value">{loadingStats ? '...' : stats.spent.toLocaleString('ru-RU')}₽</div>
                                <div className="stat-label">Потрачено</div>
                            </div>
                            <div className="stat-card">
                                <div className="stat-value">{loadingStats ? '...' : stats.activeFlights}</div>
                                <div className="stat-label">Активные рейсы</div>
                            </div>
                            <div className="stat-card">
                                <div className="stat-value">{loadingStats ? '...' : stats.completed}</div>
                                <div className="stat-label">Завершено</div>
                            </div>
                        </div>
                    </div>

                    {/* Параметры безопасности */}
                    <div className="profile-section">
                        <h2>🔒 Безопасность</h2>
                        <div className="security-options">
                            <button className="security-btn" onClick={() => setIsProfileModalOpen(true)}>
                                <span className="icon">🔐</span>
                                <span className="btn-text">Изменить пароль</span>
                            </button>

                            <button className="security-btn">
                                <span className="icon"></span>
                                <span className="btn-text">Двухфакторная аутентификация</span>
                            </button>
                        </div>
                    </div>



                    {/* Выход */}
                    <div className="profile-section logout-section">
                        <button className="btn-logout" onClick={handleLogout}>
                            Выход из аккаунта
                        </button>
                    </div>
                </div>
            </div>
            <AuthModal
                isOpen={isAuthModalOpen}
                onClose={() => setIsAuthModalOpen(false)}
                onLoginSuccess={handleLoginSuccess}
            />

            <ProfileModal
                isOpen={isProfileModalOpen}
                onClose={() => setIsProfileModalOpen(false)}
                user={currentUser}
                onLogout={handleLogout}
                onChange={handleProfileChange}
            />
        </>
    );
}

