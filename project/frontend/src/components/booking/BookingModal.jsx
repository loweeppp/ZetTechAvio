import { useState, useEffect } from 'react';
import { useAuth } from '../../hooks/useAuth';
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

export default function BookingModal({ flight, isOpen, onClose, onBook }) {
  const { currentUser } = useAuth();
  const [selectedClass, setSelectedClass] = useState('Economy');
  const [quantity, setQuantity] = useState(1);
  const [fares, setFares] = useState([]);
  const [loading, setLoading] = useState(true);
  const [passengers, setPassengers] = useState([]);
  const [bookingInProgress, setBookingInProgress] = useState(false);

  // Получаем тарифы для этого рейса
  useEffect(() => {
    if (!isOpen || !flight) return;

    fetch(`http://localhost:5151/api/flights/${flight.id}/fares`)
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

  const handleBook = async () => {
    if (!selectedFare || !currentUser) return;

    setBookingInProgress(true);

    try {
      const bookingRequest = {
        flightId: flight.id,
        fareId: selectedFare.id,
        quantity,
        passengers: passengers.map(p => ({
          fullName: p.fullName,
          passengerType: p.passengerType,
          passportSeries: p.passportSeries || '',
          passportNumber: p.passportNumber || ''
        }))
      };

      const response = await fetch('http://localhost:5151/api/bookings', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${localStorage.getItem('token')}`
        },
        body: JSON.stringify(bookingRequest)
      });

      if (!response.ok) {
        const error = await response.json();
        alert(`Ошибка: ${error.message || 'Не удалось создать бронирование'}`);
        return;
      }

      const booking = await response.json();
      onBook(booking);
      onClose();
      alert(`Бронирование успешно! Номер: ${booking.bookingReference}`);
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
                      onChange={(e) => handlePassengerChange(index, 'fullName', e.target.value)}
                      required
                    />
                    <select
                      value={passenger.passengerType}
                      onChange={(e) => handlePassengerChange(index, 'passengerType', e.target.value)}
                    >
                      <option value="Adult">Взрослый</option>
                      <option value="Child">Ребёнок</option>
                      <option value="Infant">Младенец</option>
                    </select>
                    <input
                      type="text"
                      placeholder="Серия паспорта"
                      value={passenger.passportSeries}
                      onChange={(e) => handlePassengerChange(index, 'passportSeries', e.target.value)}
                      maxLength="4"
                    />
                    <input
                      type="text"
                      placeholder="Номер паспорта"
                      value={passenger.passportNumber}
                      onChange={(e) => handlePassengerChange(index, 'passportNumber', e.target.value)}
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
              <div className="total">
                <span>Итого:</span>
                <span className="price">{totalPrice}₽</span>
              </div>
            </div>

            {/* Кнопка бронирования */}
            <button 
              className="btn-book-confirm"
              onClick={handleBook}
              disabled={!selectedFare || quantity > (selectedFare?.seatsAvailable || 0) || bookingInProgress || !currentUser}
            >
              {bookingInProgress ? 'Обработка...' : 'Забронировать'}
            </button>
          </>
        )}
      </div>
    </div>
  );
}
