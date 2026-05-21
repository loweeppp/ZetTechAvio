import { useState, useEffect, useCallback } from 'react';
import { jsPDF } from 'jspdf';
import QRCode from 'qrcode';
import { useAuth } from '../auth/useAuth';
import { verifyPaymentStatus } from './paymentService';
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
        <h1>Мои Билеты</h1>
        
        {/* Фильтры */}
        <div className="filter-tabs">
          {[
            { id: 'all', label: 'Все',  },
            { id: 'active', label: 'Активные',  },
            { id: 'completed', label: 'Завершенные', icon: '✅' },
            { id: 'cancelled', label: 'Отменены', icon: '❌' }
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

  const downloadBookingPdf = async (booking) => {
    const transliterate = (value) => {
      if (!value) return '';
      const map = {
        А: 'A', Б: 'B', В: 'V', Г: 'G', Д: 'D', Е: 'E', Ё: 'E', Ж: 'ZH', З: 'Z', И: 'I', Й: 'Y', К: 'K', Л: 'L', М: 'M', Н: 'N', О: 'O', П: 'P', Р: 'R', С: 'S', Т: 'T', У: 'U', Ф: 'F', Х: 'KH', Ц: 'TS', Ч: 'CH', Ш: 'SH', Щ: 'SHCH', Ъ: '', Ы: 'Y', Ь: '', Э: 'E', Ю: 'YU', Я: 'YA',
        а: 'a', б: 'b', в: 'v', г: 'g', д: 'd', е: 'e', ё: 'e', ж: 'zh', з: 'z', и: 'i', й: 'y', к: 'k', л: 'l', м: 'm', н: 'n', о: 'o', п: 'p', р: 'r', с: 's', т: 't', у: 'u', ф: 'f', х: 'kh', ц: 'ts', ч: 'ch', ш: 'sh', щ: 'shch', ъ: '', ы: 'y', ь: '', э: 'e', ю: 'yu', я: 'ya'
      };
      return value.split('').map((ch) => map[ch] ?? ch).join('');
    };

    const normalize = (value) => transliterate(String(value || ''));
    const doc = new jsPDF({ unit: 'px', format: 'a4' });
    const padding = 30;
    let y = padding;
    const pageWidth = 450;
    const contentWidth = pageWidth - padding * 2;

    const headerHeight = 90;
    doc.setFillColor(36, 103, 255);
    doc.roundedRect(padding, y, contentWidth, headerHeight, 12, 12, 'F');

    doc.setTextColor('#ffffff');
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(28);
    doc.text('ZetTechAvio', padding + 18, y + 38);

    doc.setFont('helvetica', 'normal');
    doc.setFontSize(11);
    doc.text('Electronic Boarding Pass', padding + 18, y + 55);
    doc.setFontSize(10);
    doc.text(`Booking Ref: ${normalize(booking.bookingReference)}`, padding + 300, y + 28);
    doc.text(`Status: ${normalize(booking.status)}`, padding + 300, y + 44);
    doc.text(`Amount: ${booking.totalAmount?.toLocaleString('ru-RU') || '0'} RUB`, padding + 300, y + 60);
    y += headerHeight + 20;

    doc.setTextColor('#000000');
    doc.setLineWidth(0.75);
    doc.line(padding, y, padding + contentWidth, y);
    y += 22;

    const firstTicket = booking.tickets?.[0];
    const flight = firstTicket?.flight;
    const departure = flight?.departureAirport?.code || 'N/A';
    const arrival = flight?.arrivalAirport?.code || 'N/A';
    const departureCity = normalize(flight?.departureAirport?.city || 'Unknown');
    const arrivalCity = normalize(flight?.arrivalAirport?.city || 'Unknown');
    const flightDate = flight?.departureTime ? new Date(flight.departureTime).toLocaleDateString('ru-RU') : 'Unknown';
    const flightTime = flight?.departureTime ? new Date(flight.departureTime).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' }) : 'Unknown';
    const ticketCount = booking.tickets?.length || 0;
    const seatValue = normalize(firstTicket?.seat || 'N/A');
    const gateValue = normalize(flight?.gate || 'N/A');

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.text('Flight details', padding, y);
    doc.text('Ticket info', padding + 320, y);
    y += 18;

    doc.setFont('helvetica', 'normal');
    doc.setFontSize(11);
    doc.text(`Route: ${departure} - ${arrival}`, padding, y);
    doc.text(`Passengers: ${ticketCount}`, padding + 320, y);
    y += 16;
    doc.text(`Cities: ${departureCity} - ${arrivalCity}`, padding, y);
    doc.text(`Departure: ${flightTime}`, padding + 320, y);
    y += 16;
    doc.text(`Date: ${flightDate}`, padding, y);
    doc.text(`Seat: ${seatValue}`, padding + 320, y);
    y += 16;
    doc.text(`Gate: ${gateValue}`, padding, y);
    y += 24;

    doc.setDrawColor(220);
    doc.line(padding, y, padding + contentWidth, y);
    y += 22;

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.text('Passenger details', padding, y);
    y += 18;

    doc.setFont('helvetica', 'normal');
    doc.setFontSize(11);
    (booking.tickets || []).forEach((ticket, index) => {
      const passengerName = normalize(ticket.passengerName || `Passenger ${index + 1}`);
      const seat = normalize(ticket.seat || 'N/A');
      doc.text(`${index + 1}. ${passengerName}`, padding, y);
      doc.text(`Seat: ${seat}`, padding + 320, y);
      y += 16;
      if (y > 760) {
        doc.addPage();
        y = padding;
      }
    });

    if (y + 70 > 840) {
      doc.addPage();
      y = padding;
    }

    const qrText = `BOOKING:${normalize(booking.bookingReference)}`;
    const qrDataUrl = await QRCode.toDataURL(qrText, { margin: 1, width: 140 });
    const qrX = padding;
    const qrY = y;
    doc.addImage(qrDataUrl, 'PNG', qrX, qrY, 120, 120);


    doc.setFont('helvetica', 'normal');
    doc.setFontSize(10);
    doc.setTextColor('#666666');
    doc.text('Please keep this PDF ticket for airport registration.', padding, qrY + 140 + 20);

    doc.save(`ZetTechAvio_Ticket_${normalize(booking.bookingReference)}.pdf`);
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
            <span className="label">Дата</span>
            <span className="value">{flightDate}</span>
          </div>
          <div className="detail">
            <span className="label">Время</span>
            <span className="value">{flightTime}</span>
          </div>
          <div className="detail">
            <span className="label">Пассажиры</span>
            <span className="value">{booking.tickets?.length || 0} шт.</span>
          </div>
          <div className="detail">
            <span className="label">Сумма</span>
            <span className="value">{booking.totalAmount.toLocaleString('ru-RU')} ₽</span>
          </div>
        </div>
      </div>

      <div className="card-footer">
        <button
          className="btn btn-primary"
          onClick={() => downloadBookingPdf(booking)}
          type="button"
        >
          Скачать билет PDF
        </button>
      </div>
    </div>
  );
}
