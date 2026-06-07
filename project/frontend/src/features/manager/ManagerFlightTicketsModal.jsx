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
          {tickets.length === 0 ? (
            <div className="admin-panel__empty">Билеты не найдены.</div>
          ) : (
            <table className="admin-panel__table">
              <thead>
                <tr>
                  <th>№</th>
                  <th>Номер билета</th>
                  <th>Пассажир</th>
                  <th>Тип</th>
                  <th>Статус</th>
                  <th>Почта клиента</th>
                </tr>
              </thead>
              <tbody>
                {tickets.map((ticket, index) => (
                  <tr key={ticket.id}>
                    <td>{index + 1}</td>
                    <td>{ticket.ticketNumber}</td>
                    <td>{ticket.passengerName}</td>
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
