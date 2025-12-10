import './Header.css';

export default function Header() {
  return (
    <header className="header">
      <div className="header-content">
        <h1 className="logo"> <img src="/routing-2.png" alt="ZetTechAvio" className="imagelogo" />
          <span className="logo-text"> ZetTechAvio</span></h1>
        <nav className="header-actions">
          <button className="btn-login">
            <svg class="icon-small" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2" />
              <circle cx="12" cy="7" r="4" />
            </svg>
            Войти</button>
          <button className="btn-menu">
            <svg className="icon-small" viewBox="0 0 24 24" fill="currentColor" stroke="currentColor" stroke-width="2">
              <circle cx="12" cy="12" r="1" />
              <circle cx="12" cy="5" r="1" />
              <circle cx="12" cy="19" r="1" />
            </svg>
          </button>
        </nav>
      </div>
    </header>
  );
}