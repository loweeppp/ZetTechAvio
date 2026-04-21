import { useState, useEffect, useCallback } from 'react';
import { useAuth } from '../../hooks/useAuth';
import { verifyPaymentStatus } from '../../services/paymentService';
import './MyBookings.css';

const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

export default function MyBookings() {
  const { currentUser } = useAuth();
  const [bookings, setBookings] = useState([]);
  const [filter, setFilter] = useState('all'); // all, active, completed, cancelled
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const loadBookings = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const token = localStorage.getItem('token');
      
      const response = await fetch(
        `${API_URL}/api/bookings/my`,
        {
          headers: { 'Authorization': `Bearer ${token}` }
        }
      );

      if (!response.ok) {
        throw new Error('Ошибка загрузки бронирований');
      }

      const data = await response.json();
      setBookings(data || []);
    } catch (err) {
      setError(err.message);
      console.error('Error loading bookings:', err);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!currentUser) return;

    const init = async () => {
      try {
        // Проверяем платёж если вернулись из YooKassa
        const pending = sessionStorage.getItem('pendingPaymentVerification');
        if (pending) {
          const { bookingId, yooKassaPaymentId } = JSON.parse(pending);
          const token = localStorage.getItem('token');

          console.log('Проверка платежа после возврата из YooKassa...');
          const result = await verifyPaymentStatus(bookingId, yooKassaPaymentId, token);
          
          console.log('Платеж проверен:', result.status);
          sessionStorage.removeItem('pendingPaymentVerification');
        }
      } catch (err) {
        console.error('Ошибка при проверке платежа:', err);
        // Не показываем ошибку пользователю - платеж может быть в процессе
      }
      
      // Загружаем бронирования
      await loadBookings();
    };

    init();
  }, [currentUser, loadBookings]);

  const filteredBookings = bookings.filter(b => {
    if (filter === 'active') return b.status === 'Confirmed' || b.status === 'Created';
    if (filter === 'completed') return b.status === 'Completed';
    if (filter === 'cancelled') return b.status === 'Cancelled';
    return true;
  });

  return (
    <div className="my-bookings">
      <div className="bookings-container">
        <h1>🎫 Мои Билеты</h1>
        
        {/* Фильтры */}
        <div className="filter-tabs">
          {[
            { id: 'all', label: '📋 Все', icon: '📋' },
            { id: 'active', label: '✈️ Активные', icon: '✈️' },
            { id: 'completed', label: '✅ Завершенные', icon: '✅' },
            { id: 'cancelled', label: '❌ Отменены', icon: '❌' }
          ].map(f => (
            <button
              key={f.id}
              className={`filter-btn ${filter === f.id ? 'active' : ''}`}
              onClick={() => setFilter(f.id)}
            >
              {f.label}
            </button>
          ))}
        </div>

        {/* Статус загрузки */}
        {loading && <p className="status-message loading">⏳ Загрузка...</p>}
        {error && <p className="status-message error">❌ {error}</p>}

        {/* Список билетов */}
        {!loading && filteredBookings.length === 0 && (
          <p className="status-message empty">
            {bookings.length === 0 ? '📭 У вас пока нет билетов' : `📭 Нет билетов в категории "${filter}"`}
          </p>
        )}

        {!loading && filteredBookings.length > 0 && (
          <div className="bookings-list">
            {filteredBookings.map(booking => (
              <BookingCard key={booking.id} booking={booking} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

// Компонент карточки билета
function BookingCard({ booking }) {
  const getStatusBadge = (status) => {
    const statusMap = {
      'Created': { label: '⏳ Ожидание оплаты', color: '#ff9800' },
      'Confirmed': { label: '✅ Подтверждено', color: '#4caf50' },
      'Completed': { label: '✈️ Завершено', color: '#2196f3' },
      'Cancelled': { label: '❌ Отменено', color: '#f44336' }
    };
    return statusMap[status] || { label: status, color: '#999' };
  };

  // Получаем информацию о полете из первого билета
  const firstTicket = booking.tickets?.[0];
  const flight = firstTicket?.flight;

  // Парсим дату правильно
  const flightDate = flight?.departureTime 
    ? new Date(flight.departureTime).toLocaleDateString('ru-RU')
    : 'Unknown';
  
  const flightTime = flight?.departureTime
    ? new Date(flight.departureTime).toLocaleTimeString('ru-RU', { 
      hour: '2-digit', 
      minute: '2-digit' 
    })
    : 'Unknown';

  const status = getStatusBadge(booking.status);

  return (
    <div className="booking-card">
      <div className="card-header">
        <div className="booking-ref">
          <span className="ref-label">Бронирование:</span>
          <span className="ref-code">{booking.bookingReference}</span>
        </div>
        <div className="status-badges">
          <span className="status-badge" style={{ backgroundColor: status.color }}>
            {status.label}
          </span>
        </div>
      </div>

      <div className="card-body">
        <div className="flight-info">
          <div className="route">
            <span className="airport">{flight?.departureAirport?.code || 'N/A'}</span>
            <span className="arrow">→</span>
            <span className="airport">{flight?.arrivalAirport?.code || 'N/A'}</span>
          </div>
          <div className="cities">
            <span>{flight?.departureAirport?.city || 'Unknown'}</span>
            <span> → </span>
            <span>{flight?.arrivalAirport?.city || 'Unknown'}</span>
          </div>
        </div>

        <div className="flight-details">
          <div className="detail">
            <span className="label">📅 Дата:</span>
            <span className="value">{flightDate}</span>
          </div>
          <div className="detail">
            <span className="label">🕐 Время:</span>
            <span className="value">{flightTime}</span>
          </div>
          <div className="detail">
            <span className="label">🎫 Билеты:</span>
            <span className="value">{booking.tickets?.length || 0} шт.</span>
          </div>
          <div className="detail">
            <span className="label">💵 Сумма:</span>
            <span className="value">{booking.totalAmount.toLocaleString('ru-RU')} ₽</span>
          </div>
        </div>
      </div>

      <div className="card-footer">
        <button className="btn btn-primary">
          Посмотреть билеты →
        </button>
      </div>
    </div>
  );
}
