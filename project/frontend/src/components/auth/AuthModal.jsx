import { useEffect, useState } from 'react';
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

  const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';


  useEffect(() => {
    setCodeStage('email');
    setError('');
  }, [email]);

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

  const сonfirmEmail = async (email) => {
    if (!validateRegistration()) return;
    setError('');

    try {
      const response = await fetch(`${API_URL}/api/bookings/request-confirmation`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',  // отправляем и получаем cookies
        body: JSON.stringify({ email })
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
  };

  const confirmCode = async (email, code) => {
    if(!validateRegistration()) return;
    setError('');
    try {
      const response = await fetch(`${API_URL}/api/bookings/verify-code`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',  // отправляем и получаем cookies
        body: JSON.stringify({ email, code })
      });
      const result = await response.json();
      if (!response.ok) {
        setError(result.message || 'Ошибка подтверждения кода');
      }

      if (response.ok) {
        setCodeStage('confirmed');
      }

    } catch (err) {
      console.error('Ошибка при подтверждении кода:', err);
      setError('Ошибка при подтверждении кода');
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
        setError('Неверный email или пароль');
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
        setError('Ошибка при регистрации');
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
            <input
              className="input mt-2"
              type="password"
              placeholder="Пароль"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />

            {error && <div className="text-danger mt-2">{error}</div>}

            <div className="mt-3">
              <button type="submit" className="btn btn-primary" disabled={loading}>
                {loading ? 'Загрузка...' : 'Войти'}
              </button>
              <button type="button" className="btn btn-link" onClick={onClose} disabled={loading}>
                Отмена
              </button>
            </div>

            <div className="mt-3 text-center">
              <span>Нет аккаунта? </span>
              <button type="button" className="btn btn-link" onClick={() => setIsLoginMode(false)}>
                Создать
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
                const value = e.target.value.replace(/\D/g, ''); // Удаляем все нецифровые символы
                setPhone(value)}}
              maxLength={12}
            />
            <input
              className="input mt-2"
              type="password"
              placeholder="Пароль"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
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
              <span> Я согласен с политикой конфиденциальности</span>
            </label>

            {error && <div className="text-danger mt-2">{error}</div>}

            <div className="mt-3">
              {/* Кнопка отправить код*/}
              {codeStage === 'email' && (
                <button onClick={() => сonfirmEmail(email)} type="button" className="btn btn-primary" disabled={loading}>
                  {loading ? 'Загрузка...' : 'Подтвердить код'}
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

              <button type="button" className="btn btn-link" onClick={onClose} disabled={loading}>
                Отмена
              </button>
            </div>

            <div className="mt-3 text-center">
              <button type="button" className="btn btn-link" onClick={() => setIsLoginMode(true)}>
                Вернуться к входу
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}