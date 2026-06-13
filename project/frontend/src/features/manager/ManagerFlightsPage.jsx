import { useEffect, useCallback, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/useAuth';
import ManagerFlightModal from './ManagerFlightModal';
import ManagerFlightTicketsModal from './ManagerFlightTicketsModal';
import ManagerScheduleModal from './ManagerScheduleModal';
import '../admin/admin-panel.css';
import '../manager/manager-panel.css'

const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

function formatDateTime(dateString) {
  if (!dateString) return '—';
  const date = new Date(dateString);
  return date.toLocaleString('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
}

export default function ManagerFlightsPage() {
  const { currentUser, token, deviceToken, isLoading } = useAuth();
  const navigate = useNavigate();
  const [flights, setFlights] = useState([]);
  const [airlines, setAirlines] = useState([]);
  const [aircrafts, setAircrafts] = useState([]);
  const [airports, setAirports] = useState([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [selectedFlight, setSelectedFlight] = useState(null);
  const [selectedFlightForTickets, setSelectedFlightForTickets] = useState(null);
  const [selectedFlightTickets, setSelectedFlightTickets] = useState([]);
  const [ticketsLoading, setTicketsLoading] = useState(false);
  const [isFlightModalOpen, setIsFlightModalOpen] = useState(false);
  const [isScheduleModalOpen, setIsScheduleModalOpen] = useState(false);
  const [isTicketsModalOpen, setIsTicketsModalOpen] = useState(false);
  const [sortConfig, setSortConfig] = useState({ field: 'departureDt', direction: 'asc' });
  const [statusFilters, setStatusFilters] = useState({
    Scheduled: true,
    Delayed: true,
    Cancelled: true,
    Completed: true
  });
  const [dateRange, setDateRange] = useState({
    from: '',
    to: ''
  });
  const [filtersCollapsed, setFiltersCollapsed] = useState(false);

  const isManager = currentUser?.role === 'Manager' || currentUser?.role === 'Admin';

  useEffect(() => {
    if (isLoading) return;
    if (!currentUser || !isManager) {
      navigate('/');
    }
  }, [currentUser, isManager, isLoading, navigate]);

  const managerHeaders = () => {
    const headers = {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json'
    };

    if (deviceToken) {
      headers['X-Device-Token'] = deviceToken;
    }

    return headers;
  };

  const loadFlights = useCallback(async () => {
    if (!token || !isManager) return;

    setLoading(true);
    setError(null);

    try {
      const response = await fetch(`${API_URL}/api/manager/flights`, {
        headers: managerHeaders()
      });

      if (!response.ok) {
        const payload = await response.json().catch(() => null);
        throw new Error(payload?.message || 'Ошибка загрузки рейсов');
      }

      const data = await response.json();
      setFlights(data || []);
    } catch (err) {
      setError(err.message || 'Ошибка загрузки данных');
      setFlights([]);
    } finally {
      setLoading(false);
    }
  }, [token, isManager]);

  const loadReferenceData = useCallback(async () => {
    if (!token || !isManager) return;

    try {
      const response = await fetch(`${API_URL}/api/manager/flights/references`, {
        headers: managerHeaders()
      });

      if (!response.ok) {
        const payload = await response.json().catch(() => null);
        throw new Error(payload?.message || 'Ошибка загрузки справочных данных');
      }

      const data = await response.json();
      setAirlines(data.airlines || []);
      setAircrafts(data.aircrafts || []);
      setAirports(data.airports || []);
    } catch (err) {
      console.warn(err.message || 'Не удалось загрузить справочные данные');
    }
  }, [token, isManager]);

  useEffect(() => {
    if (!isLoading) {
      loadFlights();
      loadReferenceData();
    }
  }, [isLoading, loadFlights, loadReferenceData]);

  const mapApiFareToFormFare = (fare) => {
    const classValue = fare.class ?? fare.fareClass ?? fare.classType;
    const normalizedClass = typeof classValue === 'number'
      ? classValue === 0 ? 'economy' : classValue === 1 ? 'business' : classValue === 2 ? 'first' : String(classValue)
      : String(classValue || '').toLowerCase();

    const classNames = {
      economy: 'Эконом',
      business: 'Бизнес',
      first: 'Первый'
    };

    const baggage = fare.baggageIncluded
      ? `${fare.baggageWeightKg || 23}кг`
      : 'нет';

    return {
      id: normalizedClass,
      fareId: fare.id,
      name: classNames[normalizedClass] || String(fare.class || fare.fareClass || fare.classType || ''),
      enabled: true,
      price: String(fare.price ?? ''),
      seats: String(fare.seatsAvailable ?? fare.seats ?? ''),
      baggage,
      ticketCount: 0
    };
  };

  const mapTicketCountsByFare = (tickets = []) => {
    return (Array.isArray(tickets) ? tickets : []).reduce((counts, ticket) => {
      const fareId = ticket.fareId ?? ticket.FareId;
      const status = String(ticket.status ?? ticket.Status ?? '').toLowerCase();
      if (!fareId || status !== 'active') {
        return counts;
      }

      counts[fareId] = (counts[fareId] || 0) + 1;
      return counts;
    }, {});
  };

  const mergeFareClassesWithDefaults = (fareClasses, ticketCounts = {}) => {
    const defaultClasses = [
      { id: 'economy', name: 'Эконом', price: '4900', seats: '120', baggage: 'нет' },
      { id: 'business', name: 'Бизнес', price: '8500', seats: '80', baggage: '23кг' },
      { id: 'first', name: 'Первый', price: '12000', seats: '20', baggage: '32кг' }
    ];

    return defaultClasses.map((base) => {
      const existing = Array.isArray(fareClasses) ? fareClasses.find((fare) => fare.id === base.id) : undefined;
      if (existing) {
        return {
          ...base,
          ...existing,
          enabled: true,
          ticketCount: ticketCounts[existing.fareId] || 0
        };
      }

      return {
        ...base,
        enabled: false,
        fareId: undefined,
        ticketCount: 0
      };
    });
  };

  const handleOpenFlightModal = async (flight) => {
    setSelectedFlight(flight || null);
    setIsFlightModalOpen(true);

    if (!flight || !flight.id) return;

    try {
      const [faresResponse, ticketsResponse] = await Promise.all([
        fetch(`${API_URL}/api/flights/${flight.id}/fares`, {
          headers: {
            'Content-Type': 'application/json'
          }
        }),
        fetch(`${API_URL}/api/manager/flights/${flight.id}/tickets`, {
          headers: managerHeaders()
        })
      ]);

      const fares = faresResponse.ok ? await faresResponse.json() : [];
      const ticketsPayload = ticketsResponse.ok ? await ticketsResponse.json() : { tickets: [] };
      const ticketCounts = mapTicketCountsByFare(ticketsPayload?.tickets ?? []);
      const fareClasses = Array.isArray(fares) ? fares.map(mapApiFareToFormFare) : [];
      const mergedFareClasses = mergeFareClassesWithDefaults(fareClasses, ticketCounts);

      setSelectedFlight((prev) => prev ? { ...prev, fareClasses: mergedFareClasses } : null);
    } catch (err) {
      console.warn('Не удалось загрузить тарифы или билеты рейса:', err);
      setSelectedFlight((prev) => prev ? { ...prev, fareClasses: [] } : null);
    }
  };

  const handleCloseFlightModal = () => {
    setSelectedFlight(null);
    setIsFlightModalOpen(false);
  };

  const handleOpenScheduleModal = () => {
    setIsScheduleModalOpen(true);
  };

  const handleCloseScheduleModal = () => {
    setIsScheduleModalOpen(false);
  };

  const handleOpenTicketsModal = async (flight) => {
    if (!token) return;

    setTicketsLoading(true);
    setError(null);

    try {
      const [ticketsResponse, faresResponse] = await Promise.all([
        fetch(`${API_URL}/api/manager/flights/${flight.id}/tickets`, {
          headers: managerHeaders()
        }),
        fetch(`${API_URL}/api/flights/${flight.id}/fares`)
      ]);

      if (!ticketsResponse.ok) {
        const payload = await ticketsResponse.json().catch(() => null);
        throw new Error(payload?.message || 'Ошибка загрузки билетов рейса');
      }

      const ticketsData = await ticketsResponse.json();
      const fares = faresResponse.ok ? await faresResponse.json() : [];
      const fareMap = Array.isArray(fares)
        ? fares.reduce((map, fare) => {
            const mappedFare = mapApiFareToFormFare(fare);
            map[fare.id] = mappedFare.name;
            return map;
          }, {})
        : {};

      const ticketsWithFareNames = (ticketsData.tickets || []).map((ticket) => ({
        ...ticket,
        fareName: fareMap[ticket.fareId ?? ticket.FareId] || String(ticket.fareId ?? ticket.FareId ?? '—')
      }));

      setSelectedFlightTickets(ticketsWithFareNames);
      setSelectedFlightForTickets(flight);
      setIsTicketsModalOpen(true);
    } catch (err) {
      alert(err.message || 'Ошибка загрузки билетов рейса');
    } finally {
      setTicketsLoading(false);
    }
  };

  const handleCloseTicketsModal = () => {
    setSelectedFlightForTickets(null);
    setSelectedFlightTickets([]);
    setIsTicketsModalOpen(false);
  };

  const handleDateRangeChange = (field, value) => {
    setDateRange((prev) => ({ ...prev, [field]: value }));
  };

  const handleResetFilters = () => {
    setStatusFilters({
      Scheduled: true,
      Delayed: true,
      Cancelled: true,
      Completed: true
    });
    setDateRange({ from: '', to: '' });
  };

  const handleSaveFlight = async (payload) => {
    if (!token) return;
    setLoading(true);
    setError(null);

    try {
      let url;
      let method;
      let body = null;

      if (!selectedFlight) {
        method = 'POST';
        url = `${API_URL}/api/manager/flights`;
        body = JSON.stringify(payload);
      } else if (payload.status === 'Cancelled' && selectedFlight.status !== 'Cancelled') {
        method = 'POST';
        url = `${API_URL}/api/manager/flights/${selectedFlight.id}/cancel`;
      } else {
        method = 'PUT';
        url = `${API_URL}/api/manager/flights/${selectedFlight.id}`;
        body = JSON.stringify(payload);
      }

      const response = await fetch(url, {
        method,
        headers: managerHeaders(),
        body
      });

      if (!response.ok) {
        const payload = await response.json().catch(() => null);
        throw new Error(payload?.message || 'Ошибка сохранения рейса');
      }

      handleCloseFlightModal();
      await loadFlights();
    } catch (err) {
      alert(err.message || 'Ошибка сохранения рейса');
    } finally {
      setLoading(false);
    }
  };

  const handleDeleteFlight = async (flight) => {
    if (!token) return;

    if (flight.status === 'Completed') {
      alert('Нельзя удалить или отменить завершённый рейс.');
      return;
    }

    let ticketCount = 0;
    try {
      const countResponse = await fetch(`${API_URL}/api/manager/flights/${flight.id}/ticket-count`, {
        headers: managerHeaders()
      });

      if (!countResponse.ok) {
        const payload = await countResponse.json().catch(() => null);
        throw new Error(payload?.message || 'Ошибка получения количества билетов');
      }

      ({ ticketCount } = await countResponse.json());
    } catch (err) {
      alert(err.message || 'Ошибка удаления рейса');
      return;
    }

    const isCancellation = ticketCount > 0;
    const confirmMessage = isCancellation
      ? `Вы точно хотите отменить рейс ${flight.flightNumber}? С ним связано ${ticketCount} билетов, все активные билеты будут отмечены как отменённые.`
      : `Вы точно хотите удалить рейс ${flight.flightNumber}?`;

    if (!window.confirm(confirmMessage)) return;

    setLoading(true);
    setError(null);

    try {
      const url = isCancellation
        ? `${API_URL}/api/manager/flights/${flight.id}/cancel`
        : `${API_URL}/api/manager/flights/${flight.id}`;
      const method = isCancellation ? 'POST' : 'DELETE';

      const response = await fetch(url, {
        method,
        headers: managerHeaders()
      });

      if (!response.ok) {
        const payload = await response.json().catch(() => null);
        throw new Error(payload?.message || (isCancellation ? 'Ошибка отмены рейса' : 'Ошибка удаления рейса'));
      }

      const payload = await response.json().catch(() => null);
      if (payload?.message) {
        alert(payload.message);
      }
      await loadFlights();
    } catch (err) {
      alert(err.message || 'Ошибка удаления рейса');
    } finally {
      setLoading(false);
    }
  };

  const handleScheduleSave = async (payload) => {
    if (!token) return;
    setLoading(true);
    setError(null);

    try {
      const response = await fetch(`${API_URL}/api/manager/flights/schedule`, {
        method: 'POST',
        headers: managerHeaders(),
        body: JSON.stringify(payload)
      });

      if (!response.ok) {
        const payload = await response.json().catch(() => null);
        throw new Error(payload?.message || 'Ошибка создания расписания');
      }

      handleCloseScheduleModal();
      await loadFlights();
    } catch (err) {
      alert(err.message || 'Ошибка создания расписания');
    } finally {
      setLoading(false);
    }
  };

  const filteredFlights = flights.filter((flight) => {
    const normalized = search.toLowerCase();
    const status = flight.status || 'Scheduled';
    const statusMatches = statusFilters[status];
    const departureDate = flight.departureDt ? new Date(flight.departureDt) : null;
    const fromDate = dateRange.from ? new Date(`${dateRange.from}T00:00:00`) : null;
    const toDate = dateRange.to ? new Date(`${dateRange.to}T23:59:59`) : null;

    const dateMatches = (
      (!fromDate || (departureDate && departureDate >= fromDate)) &&
      (!toDate || (departureDate && departureDate <= toDate))
    );

    return (
      statusMatches &&
      dateMatches &&
      (
        flight.flightNumber.toLowerCase().includes(normalized) ||
        flight.originAirport?.city?.toLowerCase().includes(normalized) ||
        flight.originAirport?.iata?.toLowerCase().includes(normalized) ||
        flight.destAirport?.city?.toLowerCase().includes(normalized) ||
        flight.destAirport?.iata?.toLowerCase().includes(normalized)
      )
    );
  });

  const sortedFlights = filteredFlights.slice().sort((a, b) => {
    const { field, direction } = sortConfig;
    const aValue = a[field];
    const bValue = b[field];

    if (!aValue && !bValue) return 0;
    if (!aValue) return 1;
    if (!bValue) return -1;

    const compareValue = field === 'departureDt' || field === 'arrivalDt'
      ? new Date(aValue) - new Date(bValue)
      : String(aValue).localeCompare(String(bValue));

    return direction === 'asc' ? compareValue : -compareValue;
  });

  const requestSort = (field) => {
    setSortConfig((prev) => {
      if (prev.field === field) {
        return {
          field,
          direction: prev.direction === 'asc' ? 'desc' : 'asc'
        };
      }
      return { field, direction: 'asc' };
    });
  };

  const statusOptions = [
    { value: 'Scheduled', label: 'Запланированные' },
    { value: 'Delayed', label: 'Задержанные' },
    { value: 'Cancelled', label: 'Отменённые' },
    { value: 'Completed', label: 'Выполненные' }
  ];

  const allStatusesSelected = statusOptions.every((option) => statusFilters[option.value]);

  const handleToggleStatus = (value) => {
    setStatusFilters((prev) => ({
      ...prev,
      [value]: !prev[value]
    }));
  };

  const handleToggleAllStatuses = () => {
    const newValue = !allStatusesSelected;
    setStatusFilters(statusOptions.reduce((acc, option) => ({
      ...acc,
      [option.value]: newValue
    }), {}));
  };

  const renderSortIcon = (field) => {
    if (sortConfig.field !== field) return '';
    return sortConfig.direction === 'asc' ? ' ↑' : ' ↓';
  };

  return (
    <div className="admin-panel__container">
      <header className="admin-panel__header">
        <div>
          <h1 className="admin-panel__title">Панель менеджера</h1>
          <p className="admin-panel__subtitle">Создание, редактирование, удаление рейсов и планирование регулярных вылетов.</p>
        </div>

        <div className="admin-panel__controls">
          <button className="admin-panel__btn" type="button" onClick={() => handleOpenFlightModal(null)}>
            Добавить рейс
          </button>
          <button className="admin-panel__btn" type="button" onClick={handleOpenScheduleModal}>
            Планировать рейсы
          </button>
          <button
            className="admin-panel__toggle-filters"
            type="button"
            onClick={() => setFiltersCollapsed((prev) => !prev)}
          >
            {filtersCollapsed ? 'Показать фильтры ›' : 'Скрыть фильтры ‹'}
          </button>
          <input
            type="text"
            placeholder="Поиск по маршруту, номеру, аэропорту"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="admin-panel__search"
          />
        </div>
      </header>

      <div className="admin-panel__main">
        {!filtersCollapsed && (
          <aside className="homev2res__filters">
            <div className="homev2res__filtersHead">
              <div className="homev2res__filtersTitle">Фильтры</div>

              <button
                className="admin-panel__toggle-filters"
                type="button"
                onClick={() => setFiltersCollapsed((prev) => !prev)}
              >
                {'✖'}
              </button>

            </div>

            <div className="homev2res__group">
              <div className="homev2res__groupTitle">Статус рейса</div>
              <div className="admin-panel__airlines">
                <label className="homev2res__check">
                  <span className={`homev2res__box ${allStatusesSelected ? 'homev2res__box--on' : ''}`}>
                    {allStatusesSelected && (
                      <svg viewBox="0 0 12 12" className="homev2res__tick">
                        <path d="M2.5 6.5L5 9l4.5-5" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                      </svg>
                    )}
                  </span>
                  <input
                    type="checkbox"
                    checked={allStatusesSelected}
                    onChange={handleToggleAllStatuses}
                    className="homev2res__sr"
                  />
                  Все статусы
                </label>
                {statusOptions.map((option) => (
                  <label key={option.value} className="homev2res__check">
                    <span className={`homev2res__box ${statusFilters[option.value] ? 'homev2res__box--on' : ''}`}>
                      {statusFilters[option.value] && (
                        <svg viewBox="0 0 12 12" className="homev2res__tick">
                          <path d="M2.5 6.5L5 9l4.5-5" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                        </svg>
                      )}
                    </span>
                    <input
                      type="checkbox"
                      checked={statusFilters[option.value]}
                      onChange={() => handleToggleStatus(option.value)}
                      className="homev2res__sr"
                    />
                    {option.label}
                  </label>
                ))}
              </div>
            </div>

            <div className="homev2res__group">
              <div className="homev2res__groupTitle">Диапазон дат вылета</div>
              <div className="homev2res__timeGrid">
                <input
                  type="date"
                  value={dateRange.from}
                  onChange={(e) => handleDateRangeChange('from', e.target.value)}
                  className="homev2res__timeBtn"
                />
                <input
                  type="date"
                  value={dateRange.to}
                  onChange={(e) => handleDateRangeChange('to', e.target.value)}
                  className="homev2res__timeBtn"
                />
              </div>
            </div>
            <button className="homev2res__filtersReset" type="button" onClick={handleResetFilters}>
              Сбросить
            </button>
          </aside>
        )}

        <div className={filtersCollapsed ? 'admin-panel__full-width' : ''}>
          {error && <div className="admin-panel__error">{error}</div>}
          {loading ? (
            <div className="admin-panel__loading">Загрузка...</div>
          ) : (
            <div className="admin-panel__table-wrapper">
              <table className="admin-panel__table">
                <thead>
                  <tr>
                    <th>№</th>
                    <th>Рейс</th>
                    <th>Маршрут</th>
                    <th className="sortable" onClick={() => requestSort('departureDt')}>
                      Вылет{renderSortIcon('departureDt')}
                    </th>
                    <th className="sortable" onClick={() => requestSort('arrivalDt')}>
                      Прилет{renderSortIcon('arrivalDt')}
                    </th>
                    <th>Статус</th>
                    <th>Продано / Осталось / Всего</th>
                    <th>Действия</th>
                  </tr>
                </thead>
                <tbody>
                  {sortedFlights.length === 0 ? (
                    <tr>
                      <td colSpan="7" className="admin-panel__empty">Рейсы не найдены</td>
                    </tr>
                  ) : (
                    sortedFlights.map((flight, index) => (
                      <tr key={flight.id}>
                        <td>{index + 1}</td>
                        <td>{flight.flightNumber}</td>
                        <td>{`${flight.originAirport?.iata || '—'} → ${flight.destAirport?.iata || '—'}`}</td>
                        <td>{formatDateTime(flight.departureDt)}</td>
                        <td>{formatDateTime(flight.arrivalDt)}</td>
                        <td>
                          <span className={`admin-panel__status admin-panel__status--${flight.status?.toLowerCase() || 'active'}`}>
                            {flight.status === 'Delayed' ? 'Задержан'
                              : flight.status === 'Cancelled' ? 'Отменён'
                                : flight.status === 'Completed' ? 'Выполнен'
                                  : 'Запланирован'}
                          </span>
                        </td>
                        <td>
                          {flight.ticketCount != null && flight.remainingSeats != null && flight.maxSeats != null ? (
                            <button
                              type="button"
                              className="admin-panel__action admin-panel__action--text"
                              onClick={() => handleOpenTicketsModal(flight)}
                              disabled={ticketsLoading}
                            >
                              {`${flight.ticketCount} / ${flight.remainingSeats} / ${flight.maxSeats}`}
                            </button>
                          ) : (
                            '—'
                          )}
                        </td>
                        <td>
                          {flight.status !== 'Completed' && (
                            <>
                              <button className="admin-panel__action" type="button" onClick={() => handleOpenFlightModal(flight)}>
                                Редактировать
                              </button>
                              <button className="admin-panel__action" type="button" onClick={() => handleDeleteFlight(flight)}>
                                {flight.status === 'Cancelled' ? 'Удалить' : 'Удалить / Отменить'}
                              </button>
                            </>
                          )}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      <ManagerFlightModal
        isOpen={isFlightModalOpen}
        onClose={handleCloseFlightModal}
        flight={selectedFlight}
        onSave={handleSaveFlight}
        airlines={airlines}
        aircrafts={aircrafts}
        airports={airports}
      />

      <ManagerFlightTicketsModal
        isOpen={isTicketsModalOpen}
        onClose={handleCloseTicketsModal}
        flight={selectedFlightForTickets}
        tickets={selectedFlightTickets}
      />

      <ManagerScheduleModal
        isOpen={isScheduleModalOpen}
        onClose={handleCloseScheduleModal}
        onSave={handleScheduleSave}
        airlines={airlines}
        aircrafts={aircrafts}
        airports={airports}
      />
    </div>
  );
}
