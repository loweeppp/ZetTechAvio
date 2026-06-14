import { useEffect, useState, useRef, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { EyeIcon } from '../../images/EyeIcon';
import './AuthModal.css';

function FloatingInput({
  id,
  label,
  type = 'text',
  value,
  onChange,
  disabled = false,
  placeholder,
  maxLength,
}) {
  const count = value?.length || 0;

  return (
    <div className="floating-input-wrapper">
      <input
        id={id}
        type={type}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        disabled={disabled}
        maxLength={maxLength}
        placeholder=" "
        className="floating-input"
      />
      <label htmlFor={id} className="floating-label">
        {label}
      </label>
      {maxLength ? (
        <div className="floating-input-counter">
          {count}/{maxLength}
        </div>
      ) : null}
    </div>
  );
}

function PasswordInput({
  id,
  label,
  value,
  onChange,
  isVisible,
  onToggleVisibility,
  maxLength,
}) {
  const count = value?.length || 0;

  return (
    <div className="password-input-wrapper">
      <input
        id={id}
        type={isVisible ? 'text' : 'password'}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        maxLength={maxLength}
        placeholder=" "
        className="password-input"
      />
      <label htmlFor={id} className="password-label">
        {label}
      </label>
      <button
        type="button"
        onClick={onToggleVisibility}
        tabIndex="-1"
        className="password-toggle"
      >
        <EyeIcon isVisible={isVisible} size={20} />
      </button>
      {maxLength ? (
        <div className="floating-input-counter">
          {count}/{maxLength}
        </div>
      ) : null}
    </div>
  );
}

export default function AuthModal({ isOpen, onClose, onLoginSuccess }) {

  const [isLoginMode, setIsLoginMode] = useState(true);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  // Поля для входа
  const [loginEmail, setLoginEmail] = useState('');
  // const [loginPassword, setLoginPassword] = useState('');

  // Поля для регистрации
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [phone, setPhone] = useState('');
  const [password, setPassword] = useState('');
  const [agreeToPolicy, setAgreeToPolicy] = useState(false);

  const [code, setCode] = useState('');
  const [codeStage, setCodeStage] = useState('email');
  const [hovered, isHovered] = useState(false);
  const [showLoginPassword, setShowLoginPassword] = useState(false);
  const [showRegisterPassword, setShowRegisterPassword] = useState(false);
  const isVerifyingCode = useRef(false);

  const [isResetMode, setIsResetMode] = useState(false);
  const [resetEmail, setResetEmail] = useState('');
  const [resetCode, setResetCode] = useState('');
  const [resetStage, setResetStage] = useState('email');
  const [resetCodeRequested, setResetCodeRequested] = useState(false);
  const [newPassword, setNewPassword] = useState('');
  const [resetCooldown, setResetCooldown] = useState(0);

  const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

  // Очистка полей при открытии модального окна
  useEffect(() => {
    if (isOpen) {
      setIsLoginMode(true);
      setLoginEmail('');
      setFullName('');
      setEmail('');
      setPhone('');
      setPassword('');
      setCode('');
      setCodeStage('email');
      setError('');
      setAgreeToPolicy(false);
      setShowLoginPassword(false);
      setShowRegisterPassword(false);
      setIsResetMode(false);
      setResetEmail('');
      setResetCode('');
      setResetStage('email');
      setResetCodeRequested(false);
      setNewPassword('');
      isHovered(false);
      isVerifyingCode.current = false;
    }
  }, [isOpen]);

  useEffect(() => {
    setCodeStage('email');
    setError('');
    isVerifyingCode.current = false;
  }, [email]);

  // Сброс при закрытии модального окна
  useEffect(() => {
    if (!isOpen) {
      isVerifyingCode.current = false;
    }
  }, [isOpen]);

  useEffect(() => {
    if (resetCooldown <= 0) return;

    const timerId = window.setInterval(() => {
      setResetCooldown((prev) => (prev > 0 ? prev - 1 : 0));
    }, 1000);

    return () => window.clearInterval(timerId);
  }, [resetCooldown]);

  const handleRequestResetCode = async () => {
    setError('');

    if (!resetEmail || !/\S+@\S+\.\S+/.test(resetEmail)) {
      setError('Введите корректный email для восстановления');
      return;
    }

    await sendResetCode(resetEmail);
  };

  // Автоматическая проверка кода при заполнении 6 цифр
  useEffect(() => {
    if (resetCode.length === 6 && resetStage === 'code' && !isVerifyingCode.current) {
      isVerifyingCode.current = true;
      verifyResetCode(resetEmail, resetCode);
    }
  }, [resetCode, resetStage, resetEmail]);

  const validateRegistration = () => {

    if (!fullName || !email || !password) {
      setError('Поля не могут быть пустыми');
      return false;
    }

    if (fullName.length < 3) {
      setError('Имя должно содержать минимум 3 символа');
      return false;
    }

    function isvalidateEmail(e) {
      return /\S+@\S+\.\S+/.test(e);
    }
    if (!isvalidateEmail(email)) {
      setError('Неверный формат email');
      return false;
    }

    function isvalidatePhone(p) {
      return /^(?:\+7|8)\d{10}$/.test(p);
    }
    if (phone && !isvalidatePhone(phone)) {
      setError('Номер должен начинаться с +7 или 8 и содержать 11 цифр');
      return false;
    }

    if (!agreeToPolicy) {
      setError('Вы должны согласиться с политикой');
      return false;
    }

    if (fullName.length > 19) {
      setError('Неверный формат имени, не больше 19 символов');
      return false;
    }

    if (password.length < 6) {
      setError('Пароль должен быть не менее 6 символов');
      return false;
    }

    return true;
  };

  const сonfirmEmail = useCallback(async (emailArg) => {
    if (!validateRegistration()) return;
    setError('');

    try {
      const response = await fetch(`${API_URL}/api/bookings/request-confirmation`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ email: emailArg })
      });
      const result = await response.json();
      if (!response.ok) {
        setError(result.message || 'Ошибка подтверждения email');
        return;
      }

      setCodeStage('code')
      isHovered(true);

    }
    catch (error) {
      setError('Ошибка подключения');
    }
  }, [API_URL, validateRegistration]);

  const confirmCode = useCallback(async (emailArg, codeArg) => {
    if (!validateRegistration()) return;
    setError('');
    try {
      const response = await fetch(`${API_URL}/api/bookings/verify-code`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ email: emailArg, code: codeArg })
      });
      const result = await response.json();
      if (!response.ok) {
        setError(result.message || 'Ошибка подтверждения кода');
        isVerifyingCode.current = false;
        return;
      }

      if (response.ok) {
        setCodeStage('confirmed');
        isVerifyingCode.current = false;
      }

    } catch (err) {
      console.error('Ошибка при подтверждении кода:', err);
      setError('Ошибка при подтверждении кода');
      isVerifyingCode.current = false;
    }
  }, [API_URL, validateRegistration]);

  const sendResetCode = useCallback(async (emailArg) => {
    if (!emailArg || !/\S+@\S+\.\S+/.test(emailArg)) {
      setError('Введите корректный email для восстановления');
      return;
    }

    setError('');
    setLoading(true);
    try {
      const response = await fetch(`${API_URL}/api/auth/request-password-reset`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ email: emailArg })
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) {
        setError(result.message || 'Ошибка отправки кода');
        return;
      }
      setResetStage('code');
      setResetCodeRequested(true);
      setResetCooldown(30);
    } catch (err) {
      setError('Ошибка подключения');
    } finally {
      setLoading(false);
    }
  }, [API_URL]);

  const verifyResetCode = useCallback(async (emailArg, codeArg) => {
    if (!emailArg || !codeArg) {
      setError('Введите email и код');
      isVerifyingCode.current = false;
      return;
    }

    setError('');
    try {
      const response = await fetch(`${API_URL}/api/auth/verify-password-reset-code`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ email: emailArg, code: codeArg })
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) {
        setError(result.message || 'Неверный код');
        isVerifyingCode.current = false;
        return;
      }

      setResetStage('verified');
      setError('');
      isVerifyingCode.current = false;
    } catch (err) {
      console.error('Ошибка проверки кода:', err);
      setError('Ошибка проверки кода');
      isVerifyingCode.current = false;
    }
  }, [API_URL]);

  const handleResetPassword = async (e) => {
    e.preventDefault();
    setError('');

    if (!resetEmail || !resetCode || !newPassword) {
      setError('Email, код и новый пароль обязательны');
      return;
    }

    if (newPassword.length < 6) {
      setError('Пароль должен быть не менее 6 символов');
      return;
    }

    setLoading(true);
    try {
      const response = await fetch(`${API_URL}/api/auth/reset-password`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ email: resetEmail, code: resetCode, newPassword })
      });
      const result = await response.json().catch(() => ({}));
      if (!response.ok) {
        setError(result.message || 'Не удалось сбросить пароль');
        return;
      }

      setError('');
      setIsResetMode(false);
      setResetStage('email');
      setResetCode('');
      setNewPassword('');
      setPassword('');
      setError('Пароль сброшен. Введите новый пароль и войдите.');
    } catch (err) {
      setError('Ошибка подключения');
    } finally {
      setLoading(false);
    }
  };

  //Переключение на режим входа
  const handleLogin = async (e) => {
    e.preventDefault();
    setError('');

    if (!loginEmail || !password) {
      setError('Email и пароль обязательны');
      return;
    }

    // Выполнение запроса на вход
    setLoading(true);
    try {
      const response = await fetch(`${API_URL}/api/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          email: loginEmail,
          password: password
        })
      });


      if (response.ok) {
        const data = await response.json();
        onLoginSuccess(data);
        onClose();
        setTimeout(() => window.location.reload(), 100);
      } else {
        const errorData = await response.json().catch(() => ({}));
        const message =
          errorData.message === 'Invalid email or password'
            ? 'Неверный email или пароль'
            : errorData.message || 'Неверный email или пароль';
        setError(message);
      }
    } catch (err) {
      setError('Ошибка подключения');
    } finally {
      setLoading(false);
    }
  };

  //Переключение на режим регистрации
  const handleRegister = async (e) => {
    if (!validateRegistration()) return;
    setError('');

    setLoading(true);
    try {
      const response = await fetch(`${API_URL}/api/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          fullName,
          email,
          password,
          phone
        })
      });

      if (response.ok) {
        const data = await response.json();
        onLoginSuccess(data);
        onClose();
        setTimeout(() => window.location.reload(), 100);
      } else {
        const errorData = await response.json().catch(() => ({}));
        setError(errorData.message || 'Ошибка при регистрации');
      }
    } catch (err) {
      setError('Ошибка подключения');
    } finally {
      setLoading(false);
    }
  };


  //
  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-box" onClick={(e) => e.stopPropagation()}>
        {isLoginMode ? (
          isResetMode ? (
            <form onSubmit={handleResetPassword}>
              <h3 className="modal-title">Сброс пароля</h3>

              <FloatingInput
                id="reset-email"
                label="Email"
                type="email"
                value={resetEmail}
                onChange={setResetEmail}
                maxLength={100}
              />

              {resetStage !== 'email' && (
                <PasswordInput
                  id="reset-code"
                  label="Код из письма"
                  value={resetCode}
                  onChange={setResetCode}
                  isVisible={false}
                  onToggleVisibility={() => { }}
                  maxLength={6}
                />
              )}
              {resetStage !== 'verified' && (
                <button
                  type="button"
                  disabled={loading || resetCooldown > 0}
                  className="btn btn-submit"
                  onClick={handleRequestResetCode}
                >
                  {loading
                    ? 'Загрузка...'
                    : resetCooldown > 0
                      ? `Отправить код (${resetCooldown}s)`
                      : 'Отправить код'}
                </button>
              )}

              {resetStage === 'verified' && (
                <PasswordInput
                  id="reset-new-password"
                  label="Новый пароль"
                  value={newPassword}
                  onChange={setNewPassword}
                  isVisible={showLoginPassword}
                  onToggleVisibility={() => setShowLoginPassword(!showLoginPassword)}
                  maxLength={50}
                />
              )}

              {resetStage === 'verified' && (
                <button
                  type="submit"
                  disabled={loading}
                  className="btn btn-submit"
                >
                  {loading ? 'Загрузка...' : 'Сохранить пароль'}
                </button>
              )}

              {error && <div className="error-message">{error}</div>}

              <div className="button-group">
                <button
                  type="button"
                  onClick={() => {
                    setIsResetMode(false);
                    setError('');
                    setResetStage('email');
                    setResetCode('');
                    setNewPassword('');
                  }}
                  disabled={loading}
                  className="btn btn-secondary"
                >
                  ← Вернуться к входу
                </button>

                <button
                  type="button"
                  onClick={onClose}
                  disabled={loading}
                  className="btn btn-outline"
                >
                  Отмена
                </button>
              </div>
            </form>
          ) : (
            <form onSubmit={handleLogin}>
              <h3 className="modal-title">Вход в аккаунт</h3>

              <FloatingInput
                id="login-email"
                label="Email"
                type="email"
                value={loginEmail}
                onChange={setLoginEmail}
                maxLength={100}
              />

              <PasswordInput
                id="login-password"
                label="Пароль"
                value={password}
                onChange={setPassword}
                isVisible={showLoginPassword}
                onToggleVisibility={() => setShowLoginPassword(!showLoginPassword)}
                maxLength={50}
              />
              <a
                href="#"
                className="text-link"
                onClick={(e) => {
                  e.preventDefault();
                  setIsResetMode(true);
                }}
              >
                Забыли пароль?
              </a>

              {error && <div className="error-message">{error}</div>}

              <div className="button-group">
                <button
                  type="submit"
                  disabled={loading}
                  className="btn btn-submit"
                >
                  {loading ? 'Загрузка...' : 'Войти'}
                </button>

                <button
                  type="button"
                  onClick={onClose}
                  disabled={loading}
                  className="btn btn-secondary"
                >
                  Отмена
                </button>



                <button
                  type="button"
                  onClick={() => setIsLoginMode(false)}
                  className="btn btn-outline"
                >
                  Создать аккаунт →
                </button>
              </div>
            </form>
          )
        ) : (
          // РЕЖИМ РЕГИСТРАЦИИ
          <form onSubmit={handleRegister}>
            <h3 className="modal-title">Регистрация</h3>

            <FloatingInput
              id="reg-fullname"
              label="Полное имя"
              type="text"
              value={fullName}
              onChange={setFullName}
              maxLength={50}
            />

            <FloatingInput
              id="reg-email"
              label="Email"
              type="email"
              value={email}
              onChange={setEmail}
              maxLength={100}
            />

            <FloatingInput
              id="reg-phone"
              label="Телефон"
              type="tel"
              value={phone}
              onChange={(val) => {
                let value = val.replace(/[^0-9+]/g, '');
                value = value.replace(/\+/g, (match, index) => (index === 0 ? match : ''));
                setPhone(value);
              }}
              maxLength={12}
            />

            <PasswordInput
              id="reg-password"
              label="Пароль"
              value={password}
              onChange={setPassword}
              isVisible={showRegisterPassword}
              onToggleVisibility={() => setShowRegisterPassword(!showRegisterPassword)}
              maxLength={50}
            />

            {hovered !== false && (
              <FloatingInput
                id="reg-code"
                label="Код подтверждения"
                type="text"
                value={code}
                onChange={(val) => {
                  const value = val.replace(/\D/g, '');
                  setCode(value);
                }}
                maxLength={6}
              />
            )}

            <label className="policy-checkbox-label">
              <input
                type="checkbox"
                checked={agreeToPolicy}
                onChange={(e) => setAgreeToPolicy(e.target.checked)}
              />
              <span>
                Я согласен с{' '}
                <Link
                  to="/privacy"
                  target="_blank"
                  className="privacy-link"
                >
                  политикой конфиденциальности
                </Link>
              </span>
            </label>

            {error && <div className="error-message">{error}</div>}

            <div className="button-group">
              {/* Кнопка отправить код*/}
              {codeStage === 'email' && (
                <button
                  onClick={() => сonfirmEmail(email)}
                  type="button"
                  disabled={loading}
                  className="btn btn-submit"
                >
                  {loading ? 'Загрузка...' : 'Подтвердить email'}
                </button>
              )}

              {/* Кнопка подтвердить код*/}
              {codeStage === 'code' && (
                <button
                  onClick={() => confirmCode(email, code)}
                  type="button"
                  disabled={loading}
                  className="btn btn-submit"
                >
                  {loading ? 'Загрузка...' : 'Подтвердить код'}
                </button>
              )}

              {/* Кнопка Создать аккаунт*/}
              {codeStage === 'confirmed' && (
                <button
                  onClick={() => handleRegister()}
                  type="button"
                  disabled={loading}
                  className="btn btn-submit"
                >
                  {loading ? 'Загрузка...' : 'Создать аккаунт'}
                </button>
              )}

              <button
                type="button"
                onClick={onClose}
                disabled={loading}
                className="btn btn-secondary"
              >
                Отмена
              </button>

              <button
                type="button"
                onClick={() => setIsLoginMode(true)}
                className="btn btn-outline"
              >
                ← Вернуться к входу
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}