import { useEffect, useState } from 'react';
import '../admin/admin-panel.css';

const weekDays = [
  { label: 'Пн', value: 'Monday' },
  { label: 'Вт', value: 'Tuesday' },
  { label: 'Ср', value: 'Wednesday' },
  { label: 'Чт', value: 'Thursday' },
  { label: 'Пт', value: 'Friday' },
  { label: 'Сб', value: 'Saturday' },
  { label: 'Вс', value: 'Sunday' }
];

const MIN_BOOKING_MINUTES = 120;
const MAX_BOOKING_MONTHS = 6;
const MIN_FLIGHT_DURATION_MINUTES = 15;
const MAX_FLIGHT_DURATION_MINUTES = 1440;

const buildDateTime = (date, time) => {
  if (!date || !time) return null;
  const dateTime = new Date(`${date}T${time}`);
  return Number.isNaN(dateTime.getTime()) ? null : dateTime;
};

const buildDurationMinutes = (departureTime, arrivalTime) => {
  if (!departureTime || !arrivalTime) return null;
  const [depH, depM] = departureTime.split(':').map(Number);
  const [arrH, arrM] = arrivalTime.split(':').map(Number);
  if ([depH, depM, arrH, arrM].some((v) => Number.isNaN(v))) return null;

  const depMinutes = depH * 60 + depM;
  const arrMinutes = arrH * 60 + arrM;
  return arrMinutes <= depMinutes ? arrMinutes + 24 * 60 - depMinutes : arrMinutes - depMinutes;
};

export default function ManagerScheduleModal({ isOpen, onClose, onSave, airlines = [], aircrafts = [], airports = [] }) {
  const [formState, setFormState] = useState({
    flightNumber: '',
    airlineId: '',
    aircraftId: '',
    originAirportId: '',
    destAirportId: '',
    departureTime: '09:00',
    arrivalTime: '12:00',
    weekdays: [],
    startDate: '',
    endDate: '',
    status: 'Scheduled'
  });
  const [validationError, setValidationError] = useState('');

  const now = new Date();
  const minDepartureDate = new Date(now.getTime() + MIN_BOOKING_MINUTES * 60000);
  const maxStartDate = new Date(now);
  maxStartDate.setMonth(maxStartDate.getMonth() + MAX_BOOKING_MONTHS);

  const pad = (value) => String(value).padStart(2, '0');
  const toDateString = (date) => `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;

  const minStartDateValue = toDateString(minDepartureDate);
  const maxStartDateValue = toDateString(maxStartDate);

  useEffect(() => {
    if (!isOpen) {
      setFormState((prev) => ({
        ...prev,
        flightNumber: '',
        airlineId: '',
        aircraftId: '',
        originAirportId: '',
        destAirportId: '',
        departureTime: '09:00',
        arrivalTime: '12:00',
        weekdays: [],
        startDate: '',
        endDate: '',
        status: 'Scheduled'
      }));
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const handleChange = (field, value) => {
    setFormState((prev) => ({ ...prev, [field]: value }));
    setValidationError('');
  };

  const getSelectedAirport = (airportId) => airports.find((airport) => String(airport.id) === String(airportId));
  const originAirport = getSelectedAirport(formState.originAirportId);
  const destAirport = getSelectedAirport(formState.destAirportId);
  const selectedAirline = airlines.find((airline) => String(airline.id) === String(formState.airlineId));

  const toggleWeekday = (day) => {
    setFormState((prev) => {
      const selected = prev.weekdays.includes(day)
        ? prev.weekdays.filter((value) => value !== day)
        : [...prev.weekdays, day];
      return { ...prev, weekdays: selected };
    });
  };

  const validateSchedule = () => {
    if (!formState.airlineId || Number(formState.airlineId) <= 0) {
      return 'Выберите авиакомпанию.';
    }

    if (!formState.aircraftId || Number(formState.aircraftId) <= 0) {
      return 'Выберите самолёт.';
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

    if (!formState.startDate || !formState.endDate) {
      return 'Укажите даты начала и окончания.';
    }

    const firstDeparture = buildDateTime(formState.startDate, formState.departureTime);
    if (!firstDeparture) {
      return 'Укажите корректную дату и время вылета.';
    }

    if (firstDeparture < minDepartureDate) {
      return `Первый вылет должен быть не ранее чем через ${MIN_BOOKING_MINUTES} минут от текущего времени.`;
    }

    if (new Date(formState.startDate) > new Date(formState.endDate)) {
      return 'Дата начала не может быть позже даты окончания.';
    }

    const flightDuration = buildDurationMinutes(formState.departureTime, formState.arrivalTime);
    if (flightDuration === null) {
      return 'Укажите корректные времена вылета и прилёта.';
    }

    if (flightDuration < MIN_FLIGHT_DURATION_MINUTES) {
      return `Время между вылетом и прилётом должно быть не менее ${MIN_FLIGHT_DURATION_MINUTES} минут.`;
    }

    if (flightDuration > MAX_FLIGHT_DURATION_MINUTES) {
      return `Время между вылетом и прилётом не может превышать ${MAX_FLIGHT_DURATION_MINUTES} минут.`;
    }

    if (formState.weekdays.length === 0) {
      return 'Выберите хотя бы один день недели.';
    }

    return '';
  };

  const handleSubmit = (event) => {
    event.preventDefault();
    const error = validateSchedule();
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
      departureTime: formState.departureTime,
      arrivalTime: formState.arrivalTime,
      weekdays: formState.weekdays,
      startDate: formState.startDate,
      endDate: formState.endDate,
      status: formState.status
    });
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="admin-modal__box" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>Планирование рейсов</h2>
          <button type="button" className="modal-close-btn" onClick={onClose}>
            ✕
          </button>
        </div>

        <form className="admin-modal__form" onSubmit={handleSubmit}>
          <div className="floating-input-wrapper">
            <input
              id="schedule-flight-number"
              type="text"
              value={formState.flightNumber}
              onChange={(e) => handleChange('flightNumber', e.target.value)}
              placeholder=" "
            />
            <label htmlFor="schedule-flight-number" className="floating-label">Номер рейса</label>
          </div>

          <div className="admin-modal__select-wrapper">
            <label htmlFor="schedule-airline-id">Авиакомпания</label>
            <select
              id="schedule-airline-id"
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
          </div>

          <div className="admin-modal__select-wrapper">
            <label htmlFor="schedule-aircraft-id">Самолёт</label>
            <select
              id="schedule-aircraft-id"
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
            <label htmlFor="schedule-origin-id">Аэропорт отправления</label>
            <select
              id="schedule-origin-id"
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
            <label htmlFor="schedule-dest-id">Аэропорт прибытия</label>
            <select
              id="schedule-dest-id"
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

          <div className="admin-modal__select-wrapper">
            <label>Дни недели</label>
            <div className="admin-modal__actions" style={{ flexWrap: 'wrap' }}>
              {weekDays.map((day) => (
                <button
                  key={day.value}
                  type="button"
                  className={`admin-panel__action ${formState.weekdays.includes(day.value) ? 'admin-panel__action--active' : ''}`}
                  onClick={() => toggleWeekday(day.value)}
                >
                  {day.label}
                </button>
              ))}
            </div>
          </div>

          <div className="floating-input-wrapper">
            <input
              id="departure-time"
              type="time"
              value={formState.departureTime}
              onChange={(e) => handleChange('departureTime', e.target.value)}
              placeholder=" "
              required
            />
            <label htmlFor="departure-time" className="floating-label">Время вылета</label>
          </div>

          <div className="floating-input-wrapper">
            <input
              id="arrival-time"
              type="time"
              value={formState.arrivalTime}
              onChange={(e) => handleChange('arrivalTime', e.target.value)}
              placeholder=" "
              required
            />
            <label htmlFor="arrival-time" className="floating-label">Время прилёта</label>
          </div>

          <div className="floating-input-wrapper">
            <input
              id="schedule-start-date"
              type="date"
              value={formState.startDate}
              onChange={(e) => handleChange('startDate', e.target.value)}
              placeholder=" "
              required
              min={minStartDateValue}
              max={maxStartDateValue}
            />
            <label htmlFor="schedule-start-date" className="floating-label">Дата начала</label>
          </div>

          <div className="floating-input-wrapper">
            <input
              id="schedule-end-date"
              type="date"
              value={formState.endDate}
              onChange={(e) => handleChange('endDate', e.target.value)}
              placeholder=" "
              required
              min={formState.startDate || minStartDateValue}
              max={maxStartDateValue}
            />
            <label htmlFor="schedule-end-date" className="floating-label">Дата окончания</label>
          </div>

          {validationError && (
            <div className="admin-panel__error" role="alert" style={{ marginBottom: '1rem' }}>
              {validationError}
            </div>
          )}

          <div className="admin-modal__actions">
            <button type="submit" className="btn btn-submit">Создать</button>
            <button type="button" className="btn btn-secondary" onClick={onClose}>Отмена</button>
          </div>
        </form>
      </div>
    </div>
  );
}
