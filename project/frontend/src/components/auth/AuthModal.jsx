import { useEffect, useState, useRef, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { EyeIcon } from '../../images/EyeIcon';
import './AuthModal.css';
import { hover } from '@testing-library/user-event/dist/hover';

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

  // Автоматическая проверка кода при заполнении 6 цифр
  useEffect(() => {
    if (code.length === 6 && codeStage === 'code' && !isVerifyingCode.current) {
      isVerifyingCode.current = true;
      confirmCode(email, code);
    }
  }, [code, codeStage, email]);

  const validateRegistration = () => {

    if (!fullName || !email || !password) {
      setError('Поля не могут быть пустыми');
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
      return /^\+?[0-9]{10,12}$/.test(p);
    }
    if (phone && !isvalidatePhone(phone)) {
      setError('Неверный формат телефона');
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
    if(!validateRegistration()) return;
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
        setError(errorData.message || 'Неверный email или пароль');
      }
    } catch (err) {
      setError('Ошибка подключения');
    } finally {
      setLoading(false);
    }
  };

  //Переключение на режим регистрации
  const handleRegister = async (e) => {
    if(!validateRegistration()) return;
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
      <div className="modal-box-register" onClick={(e) => e.stopPropagation()}>
        {isLoginMode ? (
          // РЕЖИМ ВХОДА
          <form onSubmit={handleLogin}>
            <h3>Вход в аккаунт</h3>
            <input
              className="input"
              type="email"
              placeholder="Email"
              value={loginEmail}
              onChange={(e) => setLoginEmail(e.target.value)}
            />
            <div className="password-field-wrapper">
              <input
                className="input mt-2 password-input"
                type={showLoginPassword ? 'text' : 'password'}
                placeholder="Пароль"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
              <button
                type="button"
                className="password-toggle-btn"
                onClick={() => setShowLoginPassword(!showLoginPassword)}
                tabIndex="-1"
                title={showLoginPassword ? 'Скрыть пароль' : 'Показать пароль'}
              >
                <EyeIcon isVisible={showLoginPassword} size={20} />
              </button>
            </div>

            {error && <div className="text-danger mt-2">{error}</div>}

            <div className="mt-3 button-group">
              <button type="submit" className="btn btn-primary" disabled={loading}>
                {loading ? 'Загрузка...' : 'Войти'}
              </button>
              <button type="button" className="btn btn-secondary" onClick={onClose} disabled={loading}>
                Отмена
              </button>
              <button type="button" className="btn btn-secondary" onClick={() => setIsLoginMode(false)}>
                Создать аккаунт →
              </button>
            </div>
          </form>
        ) : (
          // РЕЖИМ РЕГИСТРАЦИИ
          <form onSubmit={handleRegister}>
            <h3>Регистрация</h3>
            <input
              className="input"
              type="text"
              placeholder="Имя"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
            />
            <input
              className="input mt-2"
              type="email"
              placeholder="Email"
              value={email}
              codeStage={email}
              onChange={(e) => setEmail(e.target.value)}
            />
            <input
              className="input mt-2"
              type="tel"
              placeholder="Телефон"
              value={phone}
              onChange={(e) => {
                const value = e.target.value.replace(/\D/g, '');
                setPhone(value)}}
              maxLength={11}
            />
            <div className="password-field-wrapper">
              <input
                className="input mt-2 password-input"
                type={showRegisterPassword ? 'text' : 'password'}
                placeholder="Пароль"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
              />
              <button
                type="button"
                className="password-toggle-btn"
                onClick={() => setShowRegisterPassword(!showRegisterPassword)}
                tabIndex="-1"
                title={showRegisterPassword ? 'Скрыть пароль' : 'Показать пароль'}
              >
                <EyeIcon isVisible={showRegisterPassword} size={20} />
              </button>
            </div>
            {hovered !== false && (
              <input
                className="input mt-2"
                type="code"
                placeholder="Код подтверждения"
                value={code}
                maxLength={6}
                onChange={(e) => {
                  const value = e.target.value.replace(/\D/g, ''); // Удаляем все нецифровые символы
                  setCode(value)}}
              />
            )}


            <label className="mt-2">
              <input
                type="checkbox"
                checked={agreeToPolicy}
                onChange={(e) => setAgreeToPolicy(e.target.checked)}
              />
              <span> Я согласен с <Link to="/privacy" target="_blank" className="privacy-link">политикой конфиденциальности</Link></span>
            </label>

            {error && <div className="text-danger mt-2">{error}</div>}

            <div className="mt-3 button-group">
              {/* Кнопка отправить код*/}
              {codeStage === 'email' && (
                <button onClick={() => сonfirmEmail(email)} type="button" className="btn btn-primary" disabled={loading}>
                  {loading ? 'Загрузка...' : 'Подтвердить email'}
                </button>
              )}

              {/* Кнопка подтвердить код*/}
              {codeStage === 'code' && (
                <button onClick={() => confirmCode(email, code)} type="button" className="btn btn-primary" disabled={loading}>
                  {loading ? 'Загрузка...' : 'Подтвердить код'}
                </button>
              )}

              {/* Кнопка Создать аккаунт*/}
              {codeStage === 'confirmed' && (
                <button onClick={() => handleRegister()} type="button" className="btn btn-primary" disabled={loading}>
                  {loading ? 'Загрузка...' : 'Создать аккаунт'}
                </button>
              )}

              <button type="button" className="btn btn-secondary" onClick={onClose} disabled={loading}>
                Отмена
              </button>
              <button type="button" className="btn btn-secondary" onClick={() => setIsLoginMode(true)}>
                ← Вернуться к входу
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}