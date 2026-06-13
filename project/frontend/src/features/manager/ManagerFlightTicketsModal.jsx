import { useMemo, useState } from 'react';
import '../admin/admin-panel.css';

const mapPassengerType = (type) => {
  switch (type) {
    case 'Adult':
      return 'Взрослый';
    case 'Child':
      return 'Ребёнок';
    case 'Infant':
      return 'Младенец';
    default:
      return type || '—';
  }
};

export default function ManagerFlightTicketsModal({ isOpen, onClose, flight, tickets = [] }) {
  const [searchQuery, setSearchQuery] = useState('');
  const normalizedQuery = searchQuery.trim().toLowerCase();

  const filteredTickets = useMemo(() => {
    if (!normalizedQuery) return tickets;

    return tickets.filter((ticket) => {
      const ticketNumber = String(ticket.ticketNumber || '').toLowerCase();
      const passengerName = String(ticket.passengerName || '').toLowerCase();
      const fareName = String(ticket.fareName || ticket.fareClass || ticket.fareId || '').toLowerCase();
      const passengerType = String(ticket.passengerType || '').toLowerCase();
      const email = String(ticket.email || '').toLowerCase();
      const status = String(ticket.status || '').toLowerCase();

      return [ticketNumber, passengerName, fareName, passengerType, email, status].some((value) => value.includes(normalizedQuery));
    });
  }, [normalizedQuery, tickets]);

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="manager-modal__box" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <div>
            <h2>{flight ? `Билеты рейса ${flight.flightNumber}` : 'Список билетов'}</h2>
            <p className="flight-edit-modal__subtitle">Список билетов этого рейса.</p>
          </div>
          <button type="button" className="modal-close-btn" onClick={onClose} aria-label="Закрыть">
            ×
          </button>
        </div>

        <div className="admin-panel__table-wrapper">
          <div className="admin-panel__search-row">
            <input
              type="text"
              placeholder="Поиск билетов, пассажиров, тарифов или почты"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="admin-panel__search"
            />
          </div>
          {filteredTickets.length === 0 ? (
            <div className="admin-panel__empty">Билеты не найдены.</div>
          ) : (
            <table className="admin-panel__table">
              <thead>
                <tr>
                  <th>№</th>
                  <th>Номер билета</th>
                  <th>Пассажир</th>
                  <th>Тариф</th>
                  <th>Тип</th>
                  <th>Статус</th>
                  <th>Почта клиента</th>
                </tr>
              </thead>
              <tbody>
                {filteredTickets.map((ticket, index) => (
                  <tr key={ticket.id}>
                    <td>{index + 1}</td>
                    <td>{ticket.ticketNumber}</td>
                    <td>{ticket.passengerName}</td>
                    <td>{ticket.fareName || ticket.fareClass || ticket.fareId || '—'}</td>
                    <td>{mapPassengerType(ticket.passengerType)}</td>
                    <td>{ticket.status === 'Cancelled' ? 'Отменён' : ticket.status === 'Used' ? 'Использован' : 'Активен'}</td>
                    <td>{ticket.email || '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
}
