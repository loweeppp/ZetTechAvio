import { useEffect, useMemo, useState } from 'react';
import {
  CalendarDaysIcon,
  PlaneTakeoffIcon,
  PlaneLandingIcon,
  ClockIcon,
  ArrowRightIcon,
  CalendarRangeIcon,
  TagIcon,
  CheckCircle2Icon,
  AlertCircleIcon,
} from 'lucide-react';
import '../admin/admin-panel.css';

const weekDays = [
  { label: 'Пн', value: 0 },
  { label: 'Вт', value: 1 },
  { label: 'Ср', value: 2 },
  { label: 'Чт', value: 3 },
  { label: 'Пт', value: 4 },
  { label: 'Сб', value: 5 },
  { label: 'Вс', value: 6 },
];

const MIN_BOOKING_MINUTES = 120;
const MAX_BOOKING_MONTHS = 6;
const MIN_FLIGHT_DURATION_MINUTES = 15;
const MAX_FLIGHT_DURATION_MINUTES = 1440;

const FARE_CLASSES_BASE = [
  { id: 'economy', name: 'Эконом', price: '5900', seats: '120', baggage: 'нет' },
  { id: 'business', name: 'Бизнес', price: '14900', seats: '20', baggage: '23кг' },
  { id: 'first', name: 'Первый', price: '35000', seats: '8', baggage: '32кг' },
];

const FARE_PRESETS = [
  { label: 'Только эконом', enabled: ['economy'] },
  { label: 'Эконом + Бизнес', enabled: ['economy', 'business'] },
  { label: 'Все классы', enabled: ['economy', 'business', 'first'] },
];

const makeFareClasses = (enabledIds) =>
  FARE_CLASSES_BASE.map((fc) => ({ ...fc, enabled: enabledIds.includes(fc.id) }));

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

const initialState = {
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
  fareClasses: [],
  status: 'Scheduled',
};

function SectionHeader({ icon, label }) {
  return (
    <div className="flight-section-header">
      <span className="flight-section-header__icon">{icon}</span>
      <span className="flight-section-header__text">{label}</span>
      <div className="flight-section-header__divider" />
    </div>
  );
}

function FormSelect({ id, label, value, onChange, placeholder, children }) {
  return (
    <div className="admin-modal__select-wrapper">
      <label htmlFor={id}>{label}</label>
      <select id={id} value={value} onChange={(e) => onChange(e.target.value)} required>
        <option value="">{placeholder}</option>
        {children}
      </select>
    </div>
  );
}

function WeekdayPicker({ value, onChange }) {
  const toggle = (day) => {
    onChange(value.includes(day) ? value.filter((d) => d !== day) : [...value, day]);
  };

  const allSelected = value.length === 7;
  const workdaysSelected = [0, 1, 2, 3, 4].every((d) => value.includes(d));
  const weekendsSelected = [5, 6].every((d) => value.includes(d));

  const quickSelect = (days) => {
    const alreadyAll = days.every((d) => value.includes(d)) && value.length === days.length;
    onChange(alreadyAll ? [] : days);
  };

  return (
    <div className="schedule-weekday-block">
      <div className="schedule-weekday-grid">
        {weekDays.map((day) => {
          const active = value.includes(day.value);
          return (
            <button
              key={day.value}
              type="button"
              onClick={() => toggle(day.value)}
              className={`schedule-weekday-button ${active ? 'active' : ''}`}
            >
              {day.label}
            </button>
          );
        })}
      </div>
      <div className="schedule-weekday-actions">
        <span>Быстро:</span>
        <button
          type="button"
          onClick={() => quickSelect([0, 1, 2, 3, 4])}
          className={`schedule2-quick-button ${workdaysSelected ? 'active' : ''}`}
        >
          Будни
        </button>
        <button
          type="button"
          onClick={() => quickSelect([5, 6])}
          className={`schedule-quick-button ${weekendsSelected ? 'active' : ''}`}
        >
          Выходные
        </button>
        <button
          type="button"
          onClick={() => quickSelect([0, 1, 2, 3, 4, 5, 6])}
          className={`schedule-quick-button ${allSelected ? 'active' : ''}`}
        >
          Каждый день
        </button>
        {value.length > 0 && (
          <button
            type="button"
            onClick={() => onChange([])}
            className="schedule-quick-button schedule-quick-button--reset"
          >
            Сбросить
          </button>
        )}
      </div>
    </div>
  );
}

function FareClassesSection({ fareClasses = [], aircraftCapacity, onChange }) {
  const sanitizedFareClasses = Array.isArray(fareClasses) ? fareClasses : [];
  const enabledIds = sanitizedFareClasses.filter((fare) => fare.enabled).map((fare) => fare.id);
  const activePreset = FARE_PRESETS.findIndex(
    (preset) => preset.enabled.length === enabledIds.length && preset.enabled.every((id) => enabledIds.includes(id)),
  );

  const applyPreset = (index) => {
    const preset = FARE_PRESETS[index];
    onChange(
      fareClasses.length === 0
        ? makeFareClasses(preset.enabled)
        : fareClasses.map((fare) => ({ ...fare, enabled: preset.enabled.includes(fare.id) })),
    );
  };

  const updateFare = (id, field, value) =>
    onChange(fareClasses.map((fare) => (fare.id === id ? { ...fare, [field]: value } : fare)));

  const totalSeats = fareClasses.filter((fare) => fare.enabled).reduce((sum, fare) => sum + (Number(fare.seats) || 0), 0);
  const seatsOk = aircraftCapacity === undefined || totalSeats <= aircraftCapacity;
  const freeCapacity = aircraftCapacity === undefined ? undefined : aircraftCapacity - totalSeats;


  return (
    <div className="flight-edit-modal__section">
      <SectionHeader icon={<TagIcon />} label="Тарифы" />

      <div className="fare-presets">
        {FARE_PRESETS.map((preset, index) => (
          <button
            key={preset.label}
            type="button"
            onClick={() => applyPreset(index)}
            className={`fare-preset-btn ${activePreset === index ? 'active' : ''}`}
          >
            {preset.label}
          </button>
        ))}
      </div>

      {fareClasses.length > 0 && (
        <div className="fare-table">
          <div className="fare-table__header">
            <div />
            <div>Класс</div>
            <div>Цена (₽)</div>
            <div>Мест</div>
            <div>Багаж</div>
          </div>

          {fareClasses.map((fare, idx) => (
            <div
              key={fare.id}
              className={`fare-table__row ${!fare.enabled ? 'fare-table__row--disabled' : ''} ${idx < fareClasses.length - 1 ? 'fare-table__row--bordered' : ''}`}
            >
              <label className="fare-table__checkbox-label">
                <input
                  type="checkbox"
                  checked={fare.enabled}
                  onChange={(e) => updateFare(fare.id, 'enabled', e.target.checked)}
                  className="fare-table__checkbox-input"
                />
                <span className={`fare-table__checkbox-box ${fare.enabled ? 'fare-table__checkbox-box--checked' : ''}`}>
                  {fare.enabled && (
                    <svg viewBox="0 0 12 12" className="fare-table__checkbox-tick">
                      <path d="M2.5 6.5L5 9l4.5-5" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
                    </svg>
                  )}
                </span>
              </label>

              <span className="fare-table__name">{fare.name}</span>

              <input
                type="number"
                value={fare.price}
                onChange={(e) => updateFare(fare.id, 'price', e.target.value)}
                disabled={!fare.enabled}
                className="fare-table__input"
                placeholder="0"
                min="0"
              />

              <input
                type="number"
                value={fare.seats}
                onChange={(e) => updateFare(fare.id, 'seats', e.target.value)}
                disabled={!fare.enabled}
                className="fare-table__input"
                placeholder="0"
                min="1"
              />

              <select
                value={fare.baggage}
                onChange={(e) => updateFare(fare.id, 'baggage', e.target.value)}
                disabled={!fare.enabled}
                className="fare-table__select"
              >
                {['нет', '10кг', '20кг', '23кг', '32кг'].map((option) => (
                  <option key={option} value={option}>
                    {option}
                  </option>
                ))}
              </select>

              {fare.enabled && (fare.ticketCount ?? 0) === 0 ? (
                <button
                  type="button"
                  className="fare-table__remove-btn"
                  onClick={() => updateFare(fare.id, 'enabled', false)}
                >
                  Удалить
                </button>
              ) : (
                <div className="fare-table__action-placeholder" />
              )}
            </div>
          ))}

          <div className={`fare-seat-counter ${seatsOk ? 'fare-seat-counter--ok' : 'fare-seat-counter--error'}`}>
            {seatsOk ? <CheckCircle2Icon className="fare-seat-counter__icon" /> : <AlertCircleIcon className="fare-seat-counter__icon" />}
            <span className="fare-seat-counter__text">
              Итого мест: <strong>{totalSeats}</strong>
              {aircraftCapacity !== undefined && (
                <span className="fare-seat-counter__limit"> / {aircraftCapacity} в самолёте</span>
              )}
              {!seatsOk && <span className="fare-seat-counter__warning"> — превышает вместимость</span>}
              {aircraftCapacity !== undefined && freeCapacity !== undefined && (
                <div className={`fare-seat-remaining ${freeCapacity >= 0 ? 'fare-seat-remaining--ok' : 'fare-seat-remaining--error'}`}>
                  {freeCapacity >= 0 ? (
                    <span>Осталось свободных мест: <strong>{freeCapacity}</strong></span>
                  ) : (
                    <span>Отрицательное свободное место: <strong>{freeCapacity}</strong></span>
                  )}
                </div>
              )}
            </span>
          </div>
        </div>
      )}
    </div>
  );
}

export default function ManagerScheduleModal({ isOpen, onClose, onSave, airlines = [], aircrafts = [], airports = [] }) {
  const [formState, setFormState] = useState(initialState);
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
      setFormState(initialState);
      setValidationError('');
    }
  }, [isOpen]);

  const handleChange = (field, value) => {
    setFormState((prev) => ({ ...prev, [field]: value }));
    setValidationError('');
  };

  const getSelectedAirport = (airportId) => airports.find((airport) => String(airport.id) === String(airportId));
  const originAirport = getSelectedAirport(formState.originAirportId);
  const destAirport = getSelectedAirport(formState.destAirportId);
  const selectedAircraft = aircrafts.find((aircraft) => String(aircraft.id) === String(formState.aircraftId));

  const flightsCount = useMemo(() => {
    if (!formState.startDate || !formState.endDate || formState.weekdays.length === 0) return null;
    const from = new Date(formState.startDate);
    const to = new Date(formState.endDate);
    if (to < from) return null;
    let count = 0;
    const cur = new Date(from);
    while (cur <= to) {
      const dow = (cur.getDay() + 6) % 7;
      if (formState.weekdays.includes(dow)) count++;
      cur.setDate(cur.getDate() + 1);
    }
    return count;
  }, [formState.startDate, formState.endDate, formState.weekdays]);

  if (!isOpen) return null;

  const validateSchedule = () => {
    if (!formState.airlineId || Number(formState.airlineId) <= 0) return 'Выберите авиакомпанию.';
    if (!formState.aircraftId || Number(formState.aircraftId) <= 0) return 'Выберите самолёт.';
    if (!formState.originAirportId || Number(formState.originAirportId) <= 0 || !formState.destAirportId || Number(formState.destAirportId) <= 0) return 'Выберите аэропорт отправления и прибытия.';
    if (formState.originAirportId === formState.destAirportId) return 'Аэропорт отправления и прибытия не могут совпадать.';
    if (originAirport?.city && destAirport?.city && originAirport.city.trim().toLowerCase() === destAirport.city.trim().toLowerCase()) return 'Аэропорт вылета и прибытия не могут находиться в одном городе.';
    if (!formState.startDate || !formState.endDate) return 'Укажите даты начала и окончания.';
    const firstDeparture = buildDateTime(formState.startDate, formState.departureTime);
    if (!firstDeparture) return 'Укажите корректные дату и время вылета.';
    if (firstDeparture < minDepartureDate) return `Первый вылет должен быть не ранее чем через ${MIN_BOOKING_MINUTES} минут от текущего времени.`;
    if (new Date(formState.startDate) > new Date(formState.endDate)) return 'Дата начала не может быть позже даты окончания.';
    const flightDuration = buildDurationMinutes(formState.departureTime, formState.arrivalTime);
    if (flightDuration === null) return 'Укажите корректные времена вылета и прилёта.';
    if (flightDuration < MIN_FLIGHT_DURATION_MINUTES) return `Время между вылетом и прилётом должно быть не менее ${MIN_FLIGHT_DURATION_MINUTES} минут.`;
    if (flightDuration > MAX_FLIGHT_DURATION_MINUTES) return `Время между вылетом и прилётом не может превышать ${MAX_FLIGHT_DURATION_MINUTES} минут.`;
    if (formState.fareClasses.length > 0) {
      const activeClasses = formState.fareClasses.filter((f) => f.enabled);
      if (activeClasses.length === 0) return 'Включите хотя бы один класс тарифа.';
      for (const fare of activeClasses) {
        if (!fare.price || Number(fare.price) <= 0) return `Укажите цену для класса «${fare.name}».`;
        if (!fare.seats || Number(fare.seats) <= 0) return `Укажите количество мест для класса «${fare.name}».`;
      }
    }
    if (formState.weekdays.length === 0) return 'Выберите хотя бы один день недели.';
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
      fareClasses: formState.fareClasses
        .filter((f) => f.enabled)
        .map((fare) => ({
          classType: fare.id,
          name: fare.name,
          price: Number(fare.price),
          seats: Number(fare.seats),
          baggage: fare.baggage,
        })),
      status: formState.status,
    });
  };

  const computedArrival = () => {
    const duration = buildDurationMinutes(formState.departureTime, formState.arrivalTime);
    if (duration === null || !formState.departureTime) return '—';
    const [h, m] = formState.departureTime.split(':').map(Number);
    const total = h * 60 + m + duration;
    const rh = Math.floor(total / 60) % 24;
    const rm = total % 60;
    return `${String(rh).padStart(2, '0')}:${String(rm).padStart(2, '0')}${total >= 1440 ? ' +1' : ''}`;
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="manager-modal__box" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <h2>Планирование регулярных рейсов</h2>
          <button type="button" className="modal-close-btn" onClick={onClose} aria-label="Закрыть">
            ×
          </button>
        </div>

        <form className="admin-modal__form" onSubmit={handleSubmit}>
          <div className="flight-edit-modal__grid-3">
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

            <FormSelect id="schedule-airline-id" label="Авиакомпания" value={formState.airlineId} onChange={(value) => handleChange('airlineId', value)} placeholder="Выберите авиакомпанию">
              {airlines.map((airline) => (
                <option key={airline.id} value={String(airline.id)}>
                  {airline.name} ({airline.iataCode})
                </option>
              ))}
            </FormSelect>

            <FormSelect id="schedule-aircraft-id" label="Самолёт" value={formState.aircraftId} onChange={(value) => handleChange('aircraftId', value)} placeholder="Выберите самолёт">
              {aircrafts.map((aircraft) => (
                <option key={aircraft.id} value={String(aircraft.id)}>
                  {aircraft.manufacturer} {aircraft.model}
                </option>
              ))}
            </FormSelect>
          </div>

          <div className="flight-edit-modal__section">
            <SectionHeader icon={<PlaneTakeoffIcon />} label="Маршрут" />
            <div className="flight-edit-modal__route-grid">
              <FormSelect id="schedule-origin-id" label="Аэропорт отправления" value={formState.originAirportId} onChange={(value) => handleChange('originAirportId', value)} placeholder="Откуда">
                {airports.map((airport) => (
                  <option key={airport.id} value={String(airport.id)}>
                    {airport.iata} — {airport.city}
                  </option>
                ))}
              </FormSelect>

              <div className="flight-route-preview">

                <div className="flight-route-preview__icons">
                  <span className="flight-route-preview__code">{originAirport?.iata || '---'}</span>
                  <PlaneTakeoffIcon />
                  <ArrowRightIcon />
                  <PlaneLandingIcon className="flight-route-preview__plane" />
                  <span className="flight-route-preview__code">{destAirport?.iata || '---'}</span>
                </div>
              </div>

              <FormSelect id="schedule-dest-id" label="Аэропорт прибытия" value={formState.destAirportId} onChange={(value) => handleChange('destAirportId', value)} placeholder="Куда">
                {airports.map((airport) => (
                  <option key={airport.id} value={String(airport.id)}>
                    {airport.iata} — {airport.city}
                  </option>
                ))}
              </FormSelect>
            </div>
          </div>

          <div className="flight-edit-modal__section">
            <SectionHeader icon={<ClockIcon />} label="Расписание" />
            <div className="admin-modal__select-wrapper">
              <label>Дни вылета</label>
              <WeekdayPicker value={formState.weekdays} onChange={(days) => handleChange('weekdays', days)} />
            </div>

            <div className="flight-edit-modal__grid-3">
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

              <div className="schedule-summary-box">
                <label className="schedule-summary-label">Расчётное время прилёта</label>
                <div>{computedArrival()}</div>
              </div>
            </div>

            <FareClassesSection
              fareClasses={formState.fareClasses ?? []}
              aircraftCapacity={selectedAircraft?.capacity}
              onChange={(classes) => handleChange('fareClasses', classes)}
            />
          </div>

          <div className="flight-edit-modal__section">
            <SectionHeader icon={<CalendarRangeIcon />} label="Период действия" />
            <div className="flight-edit-modal__grid-3">
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

              <div className="schedule-summary-box">
                <label className="schedule-summary-label">Количество рейсов</label>
                <div>
                  {flightsCount === null ? '—' : `${flightsCount} ${flightsCount === 1 ? 'рейс' : flightsCount >= 2 && flightsCount <= 4 ? 'рейса' : 'рейсов'}`}
                </div>
              </div>
            </div>
          </div>

          {validationError && (
            <div className="admin-panel__error" role="alert" style={{ marginBottom: '1rem' }}>
              {validationError}
            </div>
          )}

          <div className="admin-modal__actions">
            <button type="button" className="btn btn-secondary" onClick={onClose}>Отмена</button>
            <button type="submit" className="btn btn-submit">Создать рейсы</button>
          </div>
        </form>
      </div>
    </div>
  );
}
