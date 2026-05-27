import React, { useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { Globe, Ticket, User } from 'lucide-react';
import { useAuth } from '../../../features/auth/useAuth';
import { useTranslation } from '../../../i18n/TranslationProvider';
import AuthModal from '../../../features/auth/AuthModal';
import ProfileModal from '../../../features/auth/ProfileModal';
import './HeaderV2.css';

const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

function FlightTicker({ language }) {
  const { t } = useTranslation();
  const [items, setItems] = React.useState([]);
  const [loading, setLoading] = React.useState(true);

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

  useEffect(() => {
    let active = true;
    fetch(`${API_URL}/api/flights`)
      .then((response) => {
        if (!response.ok) {
          throw new Error('Fetch error');
        }
        return response.json();
      })
      .then((data) => {
        if (!active) return;
        const visible = (data || [])
          .slice(0, 8)
          .map((flight) => {
            const locale = language === 'ru' ? 'ru-RU' : 'en-US';
            const currency = language === 'ru' ? '₽' : 'RUB';
            return {
              id: flight.id,
              text: `${flight.originAirport?.city || flight.originAirport?.iata || '—'} → ${flight.destAirport?.city || flight.destAirport?.iata || '—'} ${t('ticker.from')} ${Number(flight.minPrice || 0).toLocaleString(locale)} ${currency}`,
            };
          });
        setItems(visible.length ? visible : [{ id: 'empty', text: t('ticker.empty') }]);
      })
      .catch((error) => {
        console.error('Flight ticker error:', error);
        if (!active) return;
        setItems([{ id: 'error', text: t('ticker.error') }]);
      })
      .finally(() => active && setLoading(false));

    return () => {
      active = false;
    };
  }, [language]);

  return (
    <div className="ticker-wrapper" aria-label={t('ticker.ariaLabel')}>
      <div className="ticker-fade ticker-fade--left" />
      <div className="ticker-fade ticker-fade--right" />
      <div className="ticker-inner">
        {loading ? (
          <span className="ticker-loading">{t('ticker.loading')}</span>
        ) : (
          <div className="ticker-track">
            {[...items, ...items].map((item, index) => {
              const Icon = item.icon;
              return (
                <span key={`${item.id}-${index}`} className="ticker-item">
                  <span className="ticker-text">
                    {Icon && <Icon className="ticker-icon" />}
                    {item.text}
                  </span>
                  <span className="ticker-dot" />
                </span>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}

export default function HeaderV2() {
  const location = useLocation();
  const { currentUser, isLoading, login, logout, changeUser, fetchCurrentUser } =
    useAuth();

  const { t, language, toggleLanguage } = useTranslation();
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
      <FlightTicker language={language} />

      <header className="headerv2">
        <div className="headerv2__container">
          <Link to="/" className="headerv2__logo">
            <span className="headerv2__logo_icon">
              <img src="/routing-2.ico" className="headerv2__imagelogo" />
            </span>
            <span className="headerv2__name">
              Zet<span className="headerv2__nameAccent">Tech</span>Avio
            </span>
          </Link>

          <nav className="headerv2__nav">
            <Link
              className={`headerv2__navLink ${location.pathname === '/' ? 'headerv2__navLink--active' : ''
                }`}
              to="/"
            >
              {t('header.searchTickets')}
            </Link>

            {currentUser && (
              <>
                <Link
                  className={`headerv2__navLink ${location.pathname === '/bookings'
                    ? 'headerv2__navLink--active'
                    : ''
                    }`}
                  to="/bookings"
                >
                  {t('header.myBookings')}
                </Link>
                {currentUser.role === 'Admin' && (
                  <Link
                    className={`headerv2__navLink ${location.pathname === '/admin'
                      ? 'headerv2__navLink--active'
                      : ''
                      }`}
                    to="/admin"
                  >
                    {t('header.adminPanel')}
                  </Link>
                )}
                {(currentUser.role === 'Manager' || currentUser.role === 'Admin') && (
                  <Link
                    className={`headerv2__navLink ${location.pathname === '/manager'
                      ? 'headerv2__navLink--active'
                      : ''
                      }`}
                    to="/manager"
                  >
                    Панель менеджера
                  </Link>
                )}
              </>
            )}

            <a className="headerv2__navLink" href="mailto:ZetTechAvioBot@mail.ru">
              {t('header.support')}
            </a>
          </nav>

          <div className="headerv2__actions">
            <button
              className="headerv2__lang"
              type="button"
              onClick={toggleLanguage}
              aria-label={t('header.toggleLanguage')}
            >
              <Globe className="headerv2__icon" />
              {language.toUpperCase()}
            </button>

            {currentUser ? (
              <>
                <Link
                  to="/profile"
                  className={`headerv2__user ${location.pathname === '/profile' ? 'headerv2__navLink--active' : ''}`}
                  title={t('header.profile')}
                >
                  <User className="headerv2__icon" />
                  {currentUser.fullName || currentUser.email}
                </Link>
              </>
            ) : (
              <button
                type="button"
                className="headerv2__login"
                onClick={() => setIsAuthModalOpen(true)}
              >
                <User className="headerv2__icon" />
                {t('header.login')}
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

