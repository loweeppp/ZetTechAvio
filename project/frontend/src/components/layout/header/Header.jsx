import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '../../../hooks/useAuth';
import AuthModal from '../../auth/AuthModal';
import ProfileModal from '../../auth/ProfileModal';
import './Header.css';

export default function Header() {
  const location = useLocation();
  const { currentUser, isLoading, login, logout, changeUser, fetchCurrentUser } = useAuth();
  const [isAuthModalOpen, setIsAuthModalOpen] = useState(false);
  const [isProfileModalOpen, setIsProfileModalOpen] = useState(false);
  const [isNavOpen, setIsNavOpen] = useState(true);

  const handleProfileChange = (updatedUser) => {
    changeUser(updatedUser);
  };

  const handleLoginSuccess = async (loginResponse) => {
    login(loginResponse);

    await fetchCurrentUser();
  };

  const handleLogout = () => {
    logout();
  };

  if (isLoading) {
    return <header className="header"><div className="header-content"><h1 className="logo">ZetTechAvio</h1></div></header>;
  }

  return (
    <>
      <header className="header">
        <div className="header-content">
          <h1 className="logo">
            <img src="/routing-2.ico" alt="Logo" className="imagelogo" />
            <span className="logo-text"> ZetTechAvio</span>
          </h1>

          <nav className="header-actions">
            {currentUser ? (
              // Если авторизован
              <>

                <button className="btn-profile" >
                  <svg className="icon-small" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2" />
                    <circle cx="12" cy="7" r="4" />
                  </svg>
                  <Link to="/profile" className={`nav-tab-profile ${location.pathname === '/profile' ? 'active' : ''}`}
                  >
                    {currentUser.fullName || currentUser.email}
                  </Link>
                </button>
              </>
            ) : (
              // Если не авторизован
              <button className="btn-login" onClick={() => setIsAuthModalOpen(true)}>
                <svg className="icon-small" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2" />
                  <circle cx="12" cy="7" r="4" />
                </svg>
                Войти
              </button>
            )}
            <button className="btn-menu" onClick={() => setIsNavOpen(!isNavOpen)}>
              <svg className="icon-small" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" strokeWidth="2">
                <circle cx="12" cy="12" r="1" />
                <circle cx="12" cy="5" r="1" />
                <circle cx="12" cy="19" r="1" />
              </svg>
            </button>
          </nav>
        </div>
      </header>

      {/* Вкладки навигации */}
      <nav className={`nav-tabs ${isNavOpen ? 'open' : ''}`}>
        <Link
          to="/"
          className={`nav-tab ${location.pathname === '/' ? 'profile' : ''}`}
        >
          🔍 Поиск билетов
        </Link>
        {currentUser && (
          <>
            <Link
              to="/bookings"
              className={`nav-tab ${location.pathname === '/bookings' ? 'active' : ''}`}
            >
              🎫 Мои билеты
            </Link>
          </>
        )}
      </nav>

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