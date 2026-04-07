import { useState, useEffect } from 'react';
import { createPayment } from '../../services/paymentService';
import './BookingModal.css';

// Маппинг между цифровым enum и названиями
const CLASS_NAMES = {
  0: 'Economy',
  1: 'Business',
  2: 'First'
};

const classToNumber = {
  'Economy': 0,
  'Business': 1,
  'First': 2
};

export default function BookingModal({ flight, isOpen, onClose, onBook, user }) {
  const [error, setError] = useState('');
  const [selectedClass, setSelectedClass] = useState('Economy');
  const [quantity, setQuantity] = useState(1);
  const [fares, setFares] = useState([]);
  const [loading, setLoading] = useState(true);
  const [passengers, setPassengers] = useState([]);
  const [bookingInProgress, setBookingInProgress] = useState(false);
  const [isConfirmingEmail, setIsConfirmingEmail] = useState(false);
  const [isConfirmingCode, setIsConfirmingCode] = useState(false);

  const [email, setEmail] = useState(user?.email || '');
  const [code, setCode] = useState('');

  const [hovered, isHovered] = useState(false);
  const [codeStage, setCodeStage] = useState('email')

  const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';


  // Получаем тарифы для этого рейса
  useEffect(() => {
    if (!isOpen || !flight) return;

    fetch(`${API_URL}/api/flights/${flight.id}/fares`)
      .then(r => r.json())
      .then(data => {
        setFares(data);
        setLoading(false);
      })
      .catch(err => {
        console.error('Ошибка загрузки тарифов:', err);
        setLoading(false);
      });
  }, [isOpen, flight]);

  // Инициализируем массив пассажиров при изменении количества
  useEffect(() => {
    const newPassengers = Array(quantity).fill(null).map((_, i) =>
      passengers[i] || { fullName: '', passengerType: 'Adult', passportSeries: '', passportNumber: '' }
    );
    setPassengers(newPassengers);
  }, [quantity]);

  const selectedFare = fares.find(f => CLASS_NAMES[f.class] === selectedClass);
  const totalPrice = selectedFare ? selectedFare.price * quantity : 0;

  const handlePassengerChange = (index, field, value) => {
    const updated = [...passengers];
    updated[index] = { ...updated[index], [field]: value };
    setPassengers(updated);
  };

  // если email изменился, сбрасываем стадию и ошибки
  useEffect(() => {
    setCodeStage('email');
    setError('');
    setIsConfirmingEmail(false);
    setIsConfirmingCode(false);
  }, [email]);

  // Валидация данных 
  const validateBooking = () => {

    function isvalidateEmail(e) {
      return /\S+@\S+\.\S+/.test(e);

    }
    if (!isvalidateEmail(email)) {
      setError('Неверный формат email');
      return false;
    }

    if( !email || !passengers[0]?.fullName || !passengers[0]?.passportSeries || !passengers[0]?.passportNumber) {
      setError('Поля не могут быть пустыми');
      return false;
    }
    return true;
  };


  const сonfirmEmail = async (email) => {
    if(!validateBooking()) return;

    setError('');
    setIsConfirmingEmail(true);
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
        setIsConfirmingEmail(false);
        return;
      }

      setCodeStage('code');
      isHovered(true);

    } catch (err) {
      console.error('Ошибка при подтверждении email:', err);
      setError('Ошибка при подтверждении email');
    } finally {
      setIsConfirmingEmail(false);
    }
  };

  const confirmCode = async (email, code) => {
    setError('');
    setIsConfirmingCode(true);
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
        setIsConfirmingCode(false);
        return;
      }

      if (response.ok) {
        setCodeStage('confirmed');
      }

    } catch (err) {
      console.error('Ошибка при подтверждении кода:', err);
      setError('Ошибка при подтверждении кода');
    } finally {
      setIsConfirmingCode(false);
    }
  };


  const handleBook = async () => {
    if (!selectedFare || !validateBooking()) return;

    setBookingInProgress(true);

    try {
      // ШАГ 1: Создаём бронирование
      const bookingRequest = {
        flightId: flight.id,
        fareId: selectedFare.id,
        quantity,
        email, // Отправляем email для гостей
        passengers: passengers.map(p => ({
          fullName: p.fullName,
          passengerType: p.passengerType,
          passportSeries: p.passportSeries || '',
          passportNumber: p.passportNumber || ''
        }))
      };

      // Подготавливаем headers - авторизация если пользователь авторизован, иначе используем cookies
      const headers = {
        'Content-Type': 'application/json'
      };

      const token = localStorage.getItem('token');
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const bookingResponse = await fetch(`${API_URL}/api/bookings`, {
        method: 'POST',
        headers,
        credentials: 'include', // Отправляем cookies с подтвержденным email/кодом
        body: JSON.stringify(bookingRequest)
      });

      if (!bookingResponse.ok) {
        const error = await bookingResponse.json();
        alert(`Ошибка: ${error.message || 'Не удалось создать бронирование'}`);
        return;
      }

      const booking = await bookingResponse.json();
      console.log('✅ Бронирование создано:', booking);

      // ШАГ 2: Создаём платеж
      try {
        const token = localStorage.getItem('token');
        const paymentData = await createPayment(booking.id, token);
        console.log('✅ Платеж создан:', paymentData);

        // ШАГ 3: Редиректим на YooKassa форму
        if (paymentData.confirmationUrl) {
          window.location.href = paymentData.confirmationUrl;
        } else {
          alert('Ошибка: нет ссылки на оплату');
        }
      } catch (paymentError) {
        console.error('❌ Ошибка при создании платежа:', paymentError);
        alert(`Ошибка платежа: ${paymentError.message}`);
      }

    } catch (err) {
      console.error('Ошибка при бронировании:', err);
      alert('Ошибка при создании бронирования');
    } finally {
      setBookingInProgress(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={e => e.stopPropagation()}>
        <button className="modal-close" onClick={onClose}>✕</button>

        <h2>Бронирование рейса</h2>
        <div className="flight-info">
          <p><strong>{flight.flightNumber}</strong> • {flight.originAirport?.name} → {flight.destAirport?.name}</p>
          <p className="flight-time">
            {new Date(flight.departureDt).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })} -
            {new Date(flight.arrivalDt).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}
          </p>
        </div>

        {loading ? (
          <p>Загрузка тарифов...</p>
        ) : (
          <>
            {/* Выбор класса */}
            <div className="booking-section">
              <label>Класс билета:</label>
              <div className="class-selector">
                {fares.map(fare => (
                  <button
                    key={fare.class}
                    className={`class-btn ${selectedClass === CLASS_NAMES[fare.class] ? 'active' : ''}`}
                    onClick={() => setSelectedClass(CLASS_NAMES[fare.class])}>
                    <div className="class-name">{CLASS_NAMES[fare.class]}</div>
                    <div className="class-price">{fare.price}₽</div>
                    <div className="seats-info">{fare.seatsAvailable} мест</div>
                  </button>
                ))}
              </div>
            </div>

            {/* Выбор количества */}
            <div className="booking-section">
              <label>Количество мест:</label>
              <div className="quantity-selector">
                <button onClick={() => setQuantity(Math.max(1, quantity - 1))} disabled={quantity === 1}>−</button>
                <input type="number" value={quantity} readOnly />
                <button onClick={() => setQuantity(quantity + 1)} disabled={quantity >= (selectedFare?.seatsAvailable || 0)}>+</button>
              </div>
              {selectedFare && quantity > selectedFare.seatsAvailable && (
                <p className="error">Недостаточно мест (доступно: {selectedFare.seatsAvailable})</p>
              )}
            </div>

            {/* Данные пассажиров */}
            <div className="booking-section">
              <label>Пассажиры:</label>
              <div className="passengers-list">
                {passengers.map((passenger, index) => (
                  <div key={index} className="passenger-form">
                    <h4>Пассажир {index + 1}</h4>
                    <input
                      type="text"
                      placeholder="Полное имя"
                      value={passenger.fullName}
                      onChange={(e) => {
                        const value = e.target.value.replace(/[^a-zA-Zа-яА-ЯёЁ\s]/g, ''); // Разрешаем только буквы и пробелы
                        handlePassengerChange(index, 'fullName', value)}}
                      required
                    />
                    <select
                      value={passenger.passengerType}
                      onChange={(e) => handlePassengerChange(index, 'passengerType', e.target.value)}
                    >
                      <option value="Adult">Взрослый</option>
                      <option value="Child">Ребёнок</option>
                    </select>
                    <input
                      type="text"
                      placeholder="Серия паспорта"
                      value={passenger.passportSeries}
                      onChange={(e) => {
                        const value = e.target.value.replace(/\D/g, ''); // Удаляем все нецифровые символы
                        handlePassengerChange(index, 'passportSeries', value)}}
                      maxLength="4"
                    />
                    <input
                      type="text"
                      placeholder="Номер паспорта"
                      value={passenger.passportNumber}
                      onChange={(e) => {
                        const value = e.target.value.replace(/\D/g, ''); // Удаляем все нецифровые символы
                        handlePassengerChange(index, 'passportNumber', value)}}
                      maxLength="6"
                    />
                  </div>
                ))}
              </div>
            </div>

            {/* Сумма */}
            <div className="booking-total">
              <p>Класс: <strong>{selectedClass}</strong></p>
              <p>Билеты: <strong>{quantity}</strong></p>

              {/* Почта */}
              <p><input value={email} type="email" onChange={(e) => setEmail(e.target.value)}  className=".passenger-form input" placeholder="Введите email" /></p>

              {/* Код */}
              {hovered !== false && (
                <p><input
                maxLength={6}
                onChange={(e) => {
                  const value = e.target.value.replace(/\D/g, ''); // Удаляем все нецифровые символы
                  setCode(value)}} 
                  value={code} type="code" className=".passenger-form input" placeholder="Введите код подтверждения" /></p>
              )}

              {error && <div className="text-danger mt-2">{error}</div>}

              <div className="total">
                <span>Итого:</span>
                <span className="price">{totalPrice}₽</span>
              </div>
            </div>

            {/* Кнопка отправить код  */}
            {codeStage === 'email' && (
              <button
                className="btn-book-confirm"
                onClick={() => сonfirmEmail(email)}
                disabled={!selectedFare || quantity > (selectedFare?.seatsAvailable || 0) || !email || isConfirmingEmail}
              >
                {isConfirmingEmail ? '⏳ Отправка...' : 'Подтвердить почту'}
              </button>
            )}

            {/* Кнопка проверить код  */}
            {codeStage === 'code' && (
              <button
                className="btn-book-confirm"
                onClick={() => confirmCode(email, code)}
                disabled={!selectedFare || quantity > (selectedFare?.seatsAvailable || 0) || !email || !code || isConfirmingCode}
              >
                {isConfirmingCode ? '⏳ Проверка...' : 'Подтвердить код'}
              </button>
            )}

            {/* Кнопка забронировать  */}
            {codeStage === 'confirmed' && (
              <button
                className="btn-book-confirm"
                onClick={handleBook}
                disabled={bookingInProgress}
              >
                {bookingInProgress ? '⏳ Обработка платежа...' : 'Перейти к оплате'}
              </button>
            )}

          </>
        )}
      </div>
    </div>
  );
}
