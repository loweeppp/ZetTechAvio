import { useEffect, useState } from 'react';
import { PlaneTakeoff, Plane, PlaneLanding, Clock, Info, ArrowRight, TagIcon, CheckCircle2Icon, AlertCircleIcon } from 'lucide-react';
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

const FARE_CLASSES_BASE = [
  { id: 'economy', name: 'Эконом', price: '5900', seats: '120', baggage: 'нет' },
  { id: 'business', name: 'Бизнес', price: '14900', seats: '20', baggage: '23кг' },
  { id: 'first', name: 'Первый', price: '35000', seats: '8', baggage: '32кг' },
];

const MIN_FARE_PRICES = {
  economy: 5900,
  business: 14900,
  first: 35000
};

const FARE_PRESETS = [
  { label: 'Только эконом', enabled: ['economy'] },
  { label: 'Эконом + Бизнес', enabled: ['economy', 'business'] },
  { label: 'Все классы', enabled: ['economy', 'business', 'first'] },
];

const makeFareClasses = (enabledIds) =>
  FARE_CLASSES_BASE.map((fc) => ({ ...fc, enabled: enabledIds.includes(fc.id) }));


const getLocalDateTimeString = (date) => {
  const pad = (value) => String(value).padStart(2, '0');
  const year = date.getFullYear();
  const month = pad(date.getMonth() + 1);
  const day = pad(date.getDate());
  const hours = pad(date.getHours());
  const minutes = pad(date.getMinutes());
  return `${year}-${month}-${day}T${hours}:${minutes}`;
};

const buildFareClassesForForm = (loadedFareClasses = []) => {
  const defaults = makeFareClasses([]);
  return defaults.map((base) => {
    const existing = Array.isArray(loadedFareClasses) ? loadedFareClasses.find((fare) => fare.id === base.id) : undefined;
    if (existing) {
      return {
        ...base,
        ...existing,
        enabled: true,
        fareId: existing.fareId,
        ticketCount: existing.ticketCount ?? 0
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

const emptyFlightForm = {
  flightNumber: '',
  airlineId: '',
  aircraftId: '',
  originAirportId: '',
  destAirportId: '',
  departureDt: '',
  arrivalDt: '',
  durationMinutes: '',
  fareClasses: makeFareClasses([]),
  status: 'Scheduled'
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

function FareClassesSection({ fareClasses = [], aircraftCapacity, onChange, onValidationError }) {
  const sanitizedFareClasses = Array.isArray(fareClasses) ? fareClasses : [];
  const enabledIds = sanitizedFareClasses.filter((fare) => fare.enabled).map((fare) => fare.id);
  const activePreset = FARE_PRESETS.findIndex(
    (preset) => preset.enabled.length === enabledIds.length && preset.enabled.every((id) => enabledIds.includes(id)),
  );

  const applyPreset = (index) => {
    const preset = FARE_PRESETS[index];
    onChange(
      sanitizedFareClasses.length === 0
        ? makeFareClasses(preset.enabled)
        : sanitizedFareClasses.map((fare) => ({ ...fare, enabled: preset.enabled.includes(fare.id) })),
    );
  };

  const handleToggleEnabled = (fare, value) => {
    if (!value && fare.ticketCount > 0) {
      onValidationError?.(`Нельзя удалить тариф «${fare.name}»: по нему уже есть активные билеты.`);
      return;
    }
    onChange(sanitizedFareClasses.map((item) => (item.id === fare.id ? { ...item, enabled: value } : item)));
  };

  const updateFare = (id, field, value) =>
    onChange(sanitizedFareClasses.map((fare) => (fare.id === id ? { ...fare, [field]: value } : fare)));

  const activeFares = sanitizedFareClasses.filter((fare) => fare.enabled);
  const totalAvailableSeats = activeFares.reduce((sum, fare) => sum + (Number(fare.seats) || 0), 0);
  const totalSoldSeats = activeFares.reduce((sum, fare) => sum + (Number(fare.ticketCount) || 0), 0);
  const totalAllocatedSeats = totalAvailableSeats + totalSoldSeats;
  const seatsOk = aircraftCapacity === undefined || totalAllocatedSeats <= aircraftCapacity;
  const freeCapacity = aircraftCapacity === undefined ? undefined : aircraftCapacity - totalAllocatedSeats;

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
      {sanitizedFareClasses.length > 0 && (
        <div className="fare-table">
          <div className="fare-table__header">
            <div />
            <div>Класс</div>
            <div>Цена (₽)</div>
            <div>Мест</div>
            <div>Багаж</div>
          </div>

          {sanitizedFareClasses.map((fare, idx) => (
            <div
              key={fare.id}
              className={`fare-table__row ${!fare.enabled ? 'fare-table__row--disabled' : ''} ${idx < sanitizedFareClasses.length - 1 ? 'fare-table__row--bordered' : ''}`}
            >
              <label className="fare-table__checkbox-label">
                <input
                  type="checkbox"
                  checked={fare.enabled}
                  onChange={(e) => handleToggleEnabled(fare, e.target.checked)}
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

              <div className="fare-table__name-wrapper">
                <span className="fare-table__name">{fare.name}</span>
                {fare.ticketCount > 0 && (
                  <span className="fare-table__ticket-count">Продано: {fare.ticketCount}</span>
                )}
              </div>

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
                min="0"
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

              {/* {fare.enabled && fare.ticketCount === 0 ? (
                <button
                  type="button"
                  className="fare-table__remove-btn"
                  onClick={() => handleToggleEnabled(fare, false)}
                >
                  Удалить
                </button>
              ) : (
                <div className="fare-table__action-placeholder" />
              )} */}
            </div>
          ))}

          <div className={`fare-seat-counter ${seatsOk ? 'fare-seat-counter--ok' : 'fare-seat-counter--error'}`}>
            {seatsOk ? <CheckCircle2Icon className="fare-seat-counter__icon" /> : <AlertCircleIcon className="fare-seat-counter__icon" />}
            <span className="fare-seat-counter__text">
              Итого свободных мест: <strong>{totalAvailableSeats}</strong>
              {totalSoldSeats > 0 && (
                <span> + продано <strong>{totalSoldSeats}</strong></span>
              )}
              {aircraftCapacity !== undefined && (
                <span className="fare-seat-counter__limit"> / вместимость {aircraftCapacity}</span>
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
        fareClasses: buildFareClassesForForm(flight.fareClasses ?? []),
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
  const selectedAircraftCapacity = aircrafts.find((aircraft) => String(aircraft.id) === String(formState.aircraftId))?.totalSeats ?? aircrafts.find((aircraft) => String(aircraft.id) === String(formState.aircraftId))?.TotalSeats;

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

    if (!flight && departure < minDepartureDate) {
      return `Вылет должен быть не менее чем через ${MIN_BOOKING_MINUTES} минут.`;
    }

    if (!flight && departure > maxDepartureDate) {
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

    if (formState.fareClasses.length > 0) {
      const activeClasses = formState.fareClasses.filter((fare) => fare.enabled);
      if (activeClasses.length === 0) {
        return 'Включите хотя бы один класс тарифа.';
      }

      for (const fare of formState.fareClasses) {
        if (fare.ticketCount > 0 && fare.enabled === false) {
          return `Нельзя удалить тариф «${fare.name}»: по нему уже есть активные билеты.`;
        }
      }

      const totalAvailableSeats = activeClasses.reduce((sum, fare) => sum + (Number(fare.seats) || 0), 0);
      const totalSoldSeats = activeClasses.reduce((sum, fare) => sum + (Number(fare.ticketCount) || 0), 0);
      const totalAllocatedSeats = totalAvailableSeats + totalSoldSeats;

      if (selectedAircraftCapacity !== undefined && totalAllocatedSeats > selectedAircraftCapacity) {
        return `Суммарное количество мест тарифов и проданных билетов (${totalAllocatedSeats}) превышает вместимость самолёта (${selectedAircraftCapacity}).`;
      }

      const economyFare = activeClasses.find((fare) => fare.id === 'economy');
      const businessFare = activeClasses.find((fare) => fare.id === 'business');
      const firstFare = activeClasses.find((fare) => fare.id === 'first');

      for (const fare of activeClasses) {
        const price = Number(fare.price);
        const seats = Number(fare.seats);

        if (Number.isNaN(price) || price <= 0) {
          return `Укажите корректную цену для класса «${fare.name}».`;
        }

        if (Number.isNaN(seats) || seats < 0) {
          return `Укажите корректное количество мест для класса «${fare.name}».`;
        }

        const minPrice = MIN_FARE_PRICES[fare.id];
        if (minPrice && price < minPrice) {
          return `Минимальная цена для класса «${fare.name}» — ${minPrice} ₽.`;
        }
      }

      if (businessFare && economyFare && Number(businessFare.price) <= Number(economyFare.price)) {
        return 'Цена Бизнес должна быть выше цены Эконом.';
      }
      if (firstFare && businessFare && Number(firstFare.price) <= Number(businessFare.price)) {
        return 'Цена Первый должна быть выше цены Бизнес.';
      }
      if (firstFare && economyFare && Number(firstFare.price) <= Number(economyFare.price)) {
        return 'Цена Первый должна быть выше цены Эконом.';
      }

      if (selectedAircraftCapacity !== undefined) {
        const activePaidSeatCount = totalAllocatedSeats;
        const freeCapacity = selectedAircraftCapacity - activePaidSeatCount;
        if (freeCapacity < 0) {
          return 'Суммарное количество мест тарифов и проданных билетов не может превышать вместимость самолёта.';
        }
      }
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
      fareClasses: formState.fareClasses
        .filter((fare) => fare.enabled)
        .map((fare) => ({
          classType: fare.id,
          name: fare.name,
          price: Number(fare.price),
          seats: Number(fare.seats),
          baggage: fare.baggage,
        })),
      status: formState.status
    });
  };

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="manager-modal__box flight-edit-modal" onClick={(event) => event.stopPropagation()}>
        <div className="modal-header">
          <div>
            <h2>{flight ? `Редактировать рейс #${flight.id}` : 'Новый рейс'}</h2>
            <p className="flight-edit-modal__subtitle">Создайте или отредактируйте сведения о рейсе</p>
          </div>
          <button type="button" className="modal-close-btn" onClick={onClose} aria-label="Закрыть">
            ×
          </button>
        </div>

        <form className="admin-modal__form" onSubmit={handleSubmit}>
          <div className="flight-edit-modal__section">
            <SectionHeader icon={<Info />} label="Основная информация" />
            <div className="flight-edit-modal__grid-3">
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
            </div>
          </div>

          <div className="flight-edit-modal__section">
            <SectionHeader icon={<Plane />} label="Маршрут" />
            <div className="flight-edit-modal__route-grid">
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

              <div className="flight-route-preview">
                <div className="flight-route-preview__top">
                  <span>{originAirport?.iata || '---'}</span>
                  <ArrowRight className="flight-route-preview__arrow" />
                  <span>{destAirport?.iata || '---'}</span>
                </div>
                <div className="flight-route-preview__icons">
                  <PlaneTakeoff />
                  <Plane className="flight-route-preview__plane" />
                  <PlaneLanding />
                </div>
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
            </div>
          </div>

          <div className="flight-edit-modal__section">
            <SectionHeader icon={<Clock />} label="Расписание" />
            <div className="flight-edit-modal__grid-3">
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


            </div>
            <FareClassesSection
              fareClasses={formState.fareClasses ?? []}
              aircraftCapacity={selectedAircraftCapacity}
              onChange={(classes) => handleChange('fareClasses', classes)}
              onValidationError={(message) => setValidationError(message)}
            />
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
            <button type="button" className="btn btn-secondary" onClick={onClose}>Отмена</button>
            <button type="submit" className="btn btn-submit">Сохранить</button>
          </div>
        </form>
      </div>
    </div>
  );
}
