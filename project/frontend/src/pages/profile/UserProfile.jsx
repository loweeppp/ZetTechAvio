import { useState } from 'react';
import { useAuth } from '../../hooks/useAuth';
import AuthModal from '../../components/auth/AuthModal';
import ProfileModal from '../../components/auth/ProfileModal';
import './UserProfile.css';

export default function UserProfile() {
    const { currentUser, logout, changeUser } = useAuth();
    const [isAuthModalOpen, setIsAuthModalOpen] = useState(false);
    const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);

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
                                <p>{currentUser.phoneNumber || 'Не указан'}</p>
                            </div>
                        </div>
                    </div>

                    {/* Статистика */}
                    <div className="profile-section">
                        <h2>📊 Статистика</h2>
                        <div className="stats-grid">
                            <div className="stat-card">
                                <div className="stat-value">42</div>
                                <div className="stat-label">Всего билетов</div>
                            </div>
                            <div className="stat-card">
                                <div className="stat-value">450,000₽</div>
                                <div className="stat-label">Потрачено</div>
                            </div>
                            <div className="stat-card">
                                <div className="stat-value">12</div>
                                <div className="stat-label">Активные рейсы</div>
                            </div>
                            <div className="stat-card">
                                <div className="stat-value">28</div>
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
                                <span className="icon">📱</span>
                                <span className="btn-text">Двухфакторная аутентификация</span>
                            </button>
                        </div>
                    </div>

                    {/* Избранные маршруты */}
                    <div className="profile-section">
                        <h2>❤️ Избранные маршруты</h2>
                        <div className="favorites-list">
                            <div className="favorite-item">
                                <span className="route">🌍 Москва (MOW) → Барселона (BCN)</span>
                                <span className="count">5 полетов</span>
                            </div>
                            <div className="favorite-item">
                                <span className="route">🌍 Санкт-Петербург (SPB) → Мюнхен (MUC)</span>
                                <span className="count">3 полета</span>
                            </div>
                            <div className="favorite-item">
                                <span className="route">🌍 Казань (KZN) → Берлин (BER)</span>
                                <span className="count">2 полета</span>
                            </div>
                        </div>
                    </div>

                    {/* Уведомления */}
                    <div className="profile-section">
                        <h2>🔔 Уведомления</h2>
                        <div className="notification-settings">
                            <div className="notification-item">
                                <label className="checkbox-label">
                                    <input type="checkbox" defaultChecked />
                                    <span>Напоминание за 24 часа до вылета</span>
                                </label>
                            </div>
                            <div className="notification-item">
                                <label className="checkbox-label">
                                    <input type="checkbox" defaultChecked />
                                    <span>Уведомления об изменении рейса</span>
                                </label>
                            </div>
                            <div className="notification-item">
                                <label className="checkbox-label">
                                    <input type="checkbox" />
                                    <span>Предложения и спецпредложения</span>
                                </label>
                            </div>
                            <div className="notification-item">
                                <label className="checkbox-label">
                                    <input type="checkbox" />
                                    <span>Новости о компании</span>
                                </label>
                            </div>
                        </div>
                    </div>

                    {/* Выход */}
                    <div className="profile-section logout-section">
                        <button className="btn-logout" onClick={handleLogout}>
                            🚪 Выход из аккаунта
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

