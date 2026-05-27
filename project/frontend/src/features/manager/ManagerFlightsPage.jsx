import { useEffect, useCallback, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../auth/useAuth';
import ManagerFlightModal from './ManagerFlightModal';
import ManagerScheduleModal from './ManagerScheduleModal';
import '../admin/admin-panel.css';

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
  const { currentUser, token, isLoading } = useAuth();
  const navigate = useNavigate();
  const [flights, setFlights] = useState([]);
  const [airlines, setAirlines] = useState([]);
  const [aircrafts, setAircrafts] = useState([]);
  const [airports, setAirports] = useState([]);
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [selectedFlight, setSelectedFlight] = useState(null);
  const [isFlightModalOpen, setIsFlightModalOpen] = useState(false);
  const [isScheduleModalOpen, setIsScheduleModalOpen] = useState(false);
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

  const loadFlights = useCallback(async () => {
    if (!token || !isManager) return;

    setLoading(true);
    setError(null);

    try {
      const response = await fetch(`${API_URL}/api/manager/flights`, {
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
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
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
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

  const handleOpenFlightModal = (flight) => {
    setSelectedFlight(flight || null);
    setIsFlightModalOpen(true);
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
      const method = selectedFlight ? 'PUT' : 'POST';
      const url = selectedFlight
        ? `${API_URL}/api/manager/flights/${selectedFlight.id}`
        : `${API_URL}/api/manager/flights`;

      const response = await fetch(url, {
        method,
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
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

    try {
      const countResponse = await fetch(`${API_URL}/api/manager/flights/${flight.id}/ticket-count`, {
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (!countResponse.ok) {
        const payload = await countResponse.json().catch(() => null);
        throw new Error(payload?.message || 'Ошибка получения количества билетов');
      }

      const { ticketCount } = await countResponse.json();
      const confirmMessage = ticketCount > 0
        ? `Вы точно хотите удалить рейс ${flight.flightNumber}? С ним связано ${ticketCount} билетов, они будут удалены вместе с рейсом.`
        : `Вы точно хотите удалить рейс ${flight.flightNumber}?`;

      if (!window.confirm(confirmMessage)) return;
    } catch (err) {
      alert(err.message || 'Ошибка удаления рейса');
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await fetch(`${API_URL}/api/manager/flights/${flight.id}`, {
        method: 'DELETE',
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        const payload = await response.json().catch(() => null);
        throw new Error(payload?.message || 'Ошибка удаления рейса');
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
        headers: {
          Authorization: `Bearer ${token}`,
          'Content-Type': 'application/json'
        },
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
                          <button className="admin-panel__action" type="button" onClick={() => handleOpenFlightModal(flight)}>
                            Редактировать
                          </button>
                          <button className="admin-panel__action" type="button" onClick={() => handleDeleteFlight(flight)}>
                            Удалить
                          </button>
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
