import React from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Plane, Globe, User } from 'lucide-react';
import { useAuth } from '../../../hooks/useAuth';
import AuthModal from '../../auth/AuthModal';
import ProfileModal from '../../auth/ProfileModal';
import './HeaderV2.css';

export default function HeaderV2() {
  const location = useLocation();
  const { currentUser, isLoading, login, logout, changeUser, fetchCurrentUser } =
    useAuth();

  const [isAuthModalOpen, setIsAuthModalOpen] = React.useState(false);
  const [isProfileModalOpen, setIsProfileModalOpen] = React.useState(false);

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
    return (
      <header className="headerv2">
        <div className="headerv2__container">
          <div className="headerv2__brand">ZetTechAvio</div>
        </div>
      </header>
    );
  }

  return (
    <>
      <header className="headerv2">
        <div className="headerv2__container">
          <Link to="/" className="headerv2__logo">
            <span className="headerv2__mark">
              <Plane className="headerv2__plane" strokeWidth={2.5} />
            </span>
            <span className="headerv2__name">
              Zet<span className="headerv2__nameAccent">Tech</span>Avio
            </span>
          </Link>

          <nav className="headerv2__nav">
            <Link
              className={`headerv2__navLink ${
                location.pathname === '/' ? 'headerv2__navLink--active' : ''
              }`}
              to="/"
            >
              Поиск билетов
            </Link>

            {currentUser && (
              <Link
                className={`headerv2__navLink ${
                  location.pathname === '/bookings'
                    ? 'headerv2__navLink--active'
                    : ''
                }`}
                to="/bookings"
              >
                Мои билеты
              </Link>
            )}

            <a className="headerv2__navLink" href="/" onClick={(e) => e.preventDefault()}>
              Поддержка
            </a>
          </nav>

          <div className="headerv2__actions">
            <button className="headerv2__lang" type="button">
              <Globe className="headerv2__icon" />
              RU
            </button>

            {currentUser ? (
              <button
                type="button"
                className="headerv2__user"
                onClick={() => setIsProfileModalOpen(true)}
                title="Профиль"
              >
                <User className="headerv2__icon" />
                {currentUser.fullName || currentUser.email}
              </button>
            ) : (
              <button
                type="button"
                className="headerv2__login"
                onClick={() => setIsAuthModalOpen(true)}
              >
                <User className="headerv2__icon" />
                Войти
              </button>
            )}
          </div>
        </div>
      </header>

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

