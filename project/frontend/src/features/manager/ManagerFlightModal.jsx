import { useEffect, useState } from 'react';
import '../admin/admin-panel.css';

const statusOptions = [
  { value: 'Scheduled', label: 'Запланирован' },
  { value: 'Delayed', label: 'Задержан' },
  { value: 'Cancelled', label: 'Отменён' },
  { value: 'Completed', label: 'Выполнен' }
];

const MIN_BOOKING_MINUTES = 120;
const MAX_BOOKING_MONTHS = 6;
const MIN_FLIGHT_DURATION_MINUTES = 15;
const MAX_FLIGHT_DURATION_MINUTES = 1440;
const DURATION_TOLERANCE_MINUTES = 200;

const getLocalDateTimeString = (date) => {
  const pad = (value) => String(value).padStart(2, '0');
  const year = date.getFullYear();
  const month = pad(date.getMonth() + 1);
  const day = pad(date.getDate());
  const hours = pad(date.getHours());
  const minutes = pad(date.getMinutes());
  return `${year}-${month}-${day}T${hours}:${minutes}`;
};

const emptyFlightForm = {
  flightNumber: '',
  airlineId: '',
  aircraftId: '',
  originAirportId: '',
  destAirportId: '',
  departureDt: '',
  arrivalDt: '',
  durationMinutes: '',
  status: 'Scheduled'
};

export default function ManagerFlightModal({ isOpen, onClose, flight, onSave, airlines = [], aircrafts = [], airports = [] }) {
  const [formState, setFormState] = useState(emptyFlightForm);
  const [validationError, setValidationError] = useState('');

  const fixedAirline = airlines.length === 1 ? airlines[0] : null;

  const now = new Date();
  const minDepartureDate = new Date(now.getTime() + MIN_BOOKING_MINUTES * 60000);
  const maxDepartureDate = new Date(now);
  maxDepartureDate.setMonth(maxDepartureDate.getMonth() + MAX_BOOKING_MONTHS);

  const localMinDepartureValue = getLocalDateTimeString(minDepartureDate);
  const localMaxDepartureValue = getLocalDateTimeString(maxDepartureDate);
  const localMaxArrivalValue = formState.departureDt
    ? getLocalDateTimeString(new Date(new Date(formState.departureDt).getTime() + MAX_FLIGHT_DURATION_MINUTES * 60000))
    : getLocalDateTimeString(new Date(now.getTime() + MAX_FLIGHT_DURATION_MINUTES * 60000));

  useEffect(() => {
    if (!isOpen) return;

    if (flight) {
      setFormState({
        flightNumber: flight.flightNumber || '',
        airlineId: flight.airlineId || fixedAirline?.id || '',
        aircraftId: flight.aircraftId || '',
        originAirportId: flight.originAirport?.id || '',
        destAirportId: flight.destAirport?.id || '',
        departureDt: flight.departureDt || '',
        arrivalDt: flight.arrivalDt || '',
        durationMinutes: flight.durationMinutes || '',
        status: flight.status || 'Scheduled'
      });
    } else {
      setFormState({
        ...emptyFlightForm,
        airlineId: fixedAirline?.id || ''
      });
    }
  }, [isOpen, flight, fixedAirline]);

  if (!isOpen) return null;

  const handleChange = (field, value) => {
    setFormState((prev) => ({ ...prev, [field]: value }));
    setValidationError('');
  };

  const getSelectedAirport = (airportId) => airports.find((airport) => String(airport.id) === String(airportId));
  const originAirport = getSelectedAirport(formState.originAirportId);
  const destAirport = getSelectedAirport(formState.destAirportId);

  const calculateDuration = () => {
    const departure = new Date(formState.departureDt);
    const arrival = new Date(formState.arrivalDt);
    if (!isNaN(departure) && !isNaN(arrival)) {
      const diff = Math.round((arrival - departure) / 60000);
      handleChange('durationMinutes', diff > 0 ? diff : 0);
    }
  };

  const validateFlight = () => {
    const departure = new Date(formState.departureDt);
    const arrival = new Date(formState.arrivalDt);
    const duration = Number(formState.durationMinutes);

    if (isNaN(departure.getTime()) || isNaN(arrival.getTime())) {
      return 'Укажите корректные дату и время вылета и прилёта.';
    }

    if (departure < minDepartureDate) {
      return `Вылет должен быть не менее чем через ${MIN_BOOKING_MINUTES} минут.`;
    }

    if (departure > maxDepartureDate) {
      return `Рейс можно запланировать не позже чем через ${MAX_BOOKING_MONTHS} месяцев.`;
    }

    const departureDateOnly = formState.departureDt.split('T')[0];
    const arrivalDateOnly = formState.arrivalDt.split('T')[0];
    const departureDate = new Date(departureDateOnly);
    const arrivalDate = new Date(arrivalDateOnly);

    if (arrivalDate < departureDate) {
      return 'Дата прилёта не может быть раньше даты вылета.';
    }

    if (!formState.airlineId || Number(formState.airlineId) <= 0) {
      return 'Выберите авиакомпанию.';
    }

    if (!formState.originAirportId || Number(formState.originAirportId) <= 0 || !formState.destAirportId || Number(formState.destAirportId) <= 0) {
      return 'Выберите аэропорт отправления и прибытия.';
    }

    if (formState.originAirportId === formState.destAirportId) {
      return 'Аэропорт отправления и прибытия не могут совпадать.';
    }

    if (originAirport?.city && destAirport?.city && originAirport.city.trim().toLowerCase() === destAirport.city.trim().toLowerCase()) {
      return 'Аэропорт вылета и прибытия не могут находиться в одном городе.';
    }

    const selectedAirline = airlines.find((airline) => String(airline.id) === String(formState.airlineId));

    if (departureDateOnly === arrivalDateOnly && arrival <= departure) {
      return 'Прилет на той же календарной дате должен быть позже времени вылета.';
    }

    const actualDuration = Math.round((arrival - departure) / 60000);
    if (actualDuration <= 0) {
      return 'Время прилёта должно быть позже времени вылета.';
    }

    if (actualDuration > MAX_FLIGHT_DURATION_MINUTES) {
      return `Время между вылетом и прилётом не может превышать ${MAX_FLIGHT_DURATION_MINUTES} минут.`;
    }

    if (duration < MIN_FLIGHT_DURATION_MINUTES) {
      return `Длительность рейса должна быть не менее ${MIN_FLIGHT_DURATION_MINUTES} минут.`;
    }

    if (duration > MAX_FLIGHT_DURATION_MINUTES) {
      return `Длительность рейса не может превышать ${MAX_FLIGHT_DURATION_MINUTES} минут.`;
    }

    if (Math.abs(actualDuration - duration) > DURATION_TOLERANCE_MINUTES) {
      return `Длительность рейса должна соответствовать времени между вылетом и прилётом с точностью до ${DURATION_TOLERANCE_MINUTES} минут.`;
    }

    return '';
  };

  const handleSubmit = (event) => {
    event.preventDefault();
    const error = validateFlight();
    if (error) {
      setValidationError(error);
      return;
    }

    onSave({
      flightNumber: formState.flightNumber.trim(),
      airlineId: Number(formState.airlineId),
      aircraftId: Number(formState.aircraftId),
      originAirportId: Number(formState.originAirportId),
      destAirportId: Number(formState.destAirportId),
      departureDt: formState.departureDt,
      arrivalDt: formState.arrivalDt,
      durationMinutes: Number(formState.durationMinutes),
      status: formState.status
    });
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="admin-modal__box" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>{flight ? `Редактировать рейс #${flight.id}` : 'Новый рейс'}</h2>
          <button type="button" className="modal-close-btn" onClick={onClose}>
            ✕
          </button>
        </div>

        <form className="admin-modal__form" onSubmit={handleSubmit}>
          <div className="floating-input-wrapper">
            <input
              id="flight-number"
              type="text"
              value={formState.flightNumber}
              onChange={(e) => handleChange('flightNumber', e.target.value)}
              placeholder=" "
            />
            <label htmlFor="flight-number" className="floating-label">Номер рейса</label>
          </div>

          <div className="admin-modal__select-wrapper">
            <label htmlFor="airline-id">Авиакомпания</label>
            {fixedAirline ? (
              <>
                <input type="hidden" id="airline-id" value={formState.airlineId} />
                <div className="admin-modal__fixed-value">
                  {fixedAirline.name} ({fixedAirline.iataCode})
                </div>
              </>
            ) : (
              <select
                id="airline-id"
                value={formState.airlineId}
                onChange={(e) => handleChange('airlineId', e.target.value)}
                required
              >
                <option value="">Выберите авиакомпанию</option>
                {airlines.map((airline) => (
                  <option key={airline.id} value={airline.id}>
                    {airline.name} ({airline.iataCode})
                  </option>
                ))}
              </select>
            )}
          </div>

          <div className="admin-modal__select-wrapper">
            <label htmlFor="aircraft-id">Самолёт</label>
            <select
              id="aircraft-id"
              value={formState.aircraftId}
              onChange={(e) => handleChange('aircraftId', e.target.value)}
              required
            >
              <option value="">Выберите самолёт</option>
              {aircrafts.map((aircraft) => (
                <option key={aircraft.id} value={aircraft.id}>
                  {aircraft.manufacturer} {aircraft.model}
                </option>
              ))}
            </select>
          </div>

          <div className="admin-modal__select-wrapper">
            <label htmlFor="origin-airport-id">Аэропорт отправления</label>
            <select
              id="origin-airport-id"
              value={formState.originAirportId}
              onChange={(e) => handleChange('originAirportId', e.target.value)}
              required
            >
              <option value="">Выберите аэропорт отправления</option>
              {airports.map((airport) => (
                <option key={airport.id} value={airport.id}>
                  {airport.iata} — {airport.city}
                </option>
              ))}
            </select>
          </div>

          <div className="admin-modal__select-wrapper">
            <label htmlFor="dest-airport-id">Аэропорт прибытия</label>
            <select
              id="dest-airport-id"
              value={formState.destAirportId}
              onChange={(e) => handleChange('destAirportId', e.target.value)}
              required
            >
              <option value="">Выберите аэропорт прибытия</option>
              {airports.map((airport) => (
                <option key={airport.id} value={airport.id}>
                  {airport.iata} — {airport.city}
                </option>
              ))}
            </select>
          </div>

          <div className="floating-input-wrapper">
            <input
              id="departure-dt"
              type="datetime-local"
              value={formState.departureDt}
              onChange={(e) => handleChange('departureDt', e.target.value)}
              placeholder=" "
              required
              min={localMinDepartureValue}
              max={localMaxDepartureValue}
            />
            <label htmlFor="departure-dt" className="floating-label">Дата и время вылета</label>
          </div>

          <div className="floating-input-wrapper">
            <input
              id="arrival-dt"
              type="datetime-local"
              value={formState.arrivalDt}
              onChange={(e) => handleChange('arrivalDt', e.target.value)}
              onBlur={calculateDuration}
              placeholder=" "
              required
              min={formState.departureDt || localMinDepartureValue}
              max={localMaxArrivalValue}
            />
            <label htmlFor="arrival-dt" className="floating-label">Дата и время прилёта</label>
          </div>

          <div className="floating-input-wrapper">
            <input
              id="duration-minutes"
              type="number"
              value={formState.durationMinutes}
              onChange={(e) => handleChange('durationMinutes', e.target.value)}
              placeholder=" "
              required
              min="1"
              max={MAX_FLIGHT_DURATION_MINUTES}
            />
            <label htmlFor="duration-minutes" className="floating-label">Продолжительность (мин)</label>
          </div>

          {flight && (
            <div className="admin-modal__select-wrapper">
              <label htmlFor="flight-status">Статус рейса</label>
              <select
                id="flight-status"
                value={formState.status}
                onChange={(e) => handleChange('status', e.target.value)}
              >
                {statusOptions.map((option) => (
                  <option key={option.value} value={option.value}>{option.label}</option>
                ))}
              </select>
            </div>
          )}

          {validationError && (
            <div className="admin-panel__error" role="alert" style={{ marginBottom: '1rem' }}>
              {validationError}
            </div>
          )}

          <div className="admin-modal__actions">
            <button type="submit" className="btn btn-submit">Сохранить</button>
            <button type="button" className="btn btn-secondary" onClick={onClose}>Отмена</button>
          </div>
        </form>
      </div>
    </div>
  );
}
