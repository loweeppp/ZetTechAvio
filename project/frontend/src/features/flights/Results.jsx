import React from 'react';
import { Plane, ArrowLeft, Wifi, Utensils, Luggage } from 'lucide-react';
import { useAuth } from '../auth/useAuth';
import BookingModal from '../bookings/BookingModal';
import ResultsFilters from './ResultsFilters';
import SearchFormV2 from './SearchFormV2';

const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

const SORTS = [
  { id: 'cheapest', label: 'Дешевле' },
  { id: 'fastest', label: 'Быстрее' },
  { id: 'best', label: 'Лучшие' },
];

export default function Results({ query, onBack, onSearch }) {
  const { currentUser } = useAuth();
  const [sort, setSort] = React.useState('cheapest');
  const [loading, setLoading] = React.useState(true);
  const [flights, setFlights] = React.useState([]);
  const [error, setError] = React.useState(null);
  const [selectedFlight, setSelectedFlight] = React.useState(null);
  const [isBookingOpen, setIsBookingOpen] = React.useState(false);

  React.useEffect(() => {
    if (!query) return;

    const fetchFlights = async () => {
      setLoading(true);
      setError(null);
      try {
        const params = new URLSearchParams();
        const resolveValue = (field) => {
          if (!field) return '';
          if (typeof field === 'object') {
            return field.query || field.name || field.code || '';
          }
          return field;
        };

        const fromValue = resolveValue(query.from);
        const toValue = resolveValue(query.to);

        if (fromValue) params.append('from', fromValue);
        if (toValue) params.append('to', toValue);
        if (query.date) params.append('date', query.date);

        const response = await fetch(`${API_URL}/api/flights/search?${params.toString()}`);
        if (!response.ok) {
          const payload = await response.json().catch(() => null);
          throw new Error(payload?.message || 'Ошибка загрузки рейсов');
        }

        const data = await response.json();
        setFlights(data || []);
      } catch (err) {
        console.error('Results fetch error:', err);
        setError(err instanceof Error ? err.message : 'Ошибка сети');
        setFlights([]);
      } finally {
        setLoading(false);
      }
    };

    fetchFlights();
  }, [query]);

  const sortedFlights = React.useMemo(() => {
    if (sort === 'fastest') {
      return [...flights].sort((a, b) => a.durationMinutes - b.durationMinutes);
    }
    if (sort === 'best') {
      return [...flights].sort((a, b) => a.minPrice - b.minPrice);
    }
    return [...flights].sort((a, b) => a.minPrice - b.minPrice);
  }, [flights, sort]);

  const resultCount = flights.length;

  const handleBuyClick = (flight) => {
    setSelectedFlight(flight);
    setIsBookingOpen(true);
  };

  return (
    <section className="homev2res">
      <div className="homev2__container homev2res__container">
        <button onClick={onBack} className="homev2res__back" type="button">
          <ArrowLeft className="homev2res__backIcon" /> Новый поиск
        </button>

        <div className="homev2res__searchCard">
          <SearchFormV2 onSearch={onSearch} />
        </div>

        <div className="homev2res__titleRow">
          <div>
            <h2 className="homev2res__title">
              {([query.from?.name ?? query.from, query.to?.name ?? query.to].filter(Boolean).join(' → ')) || 'Рейсы'}
            </h2>
            <div className="homev2res__sub">
              {[
                query.date || null,
                `${query.passengers} пассажир${query.passengers === 1 ? '' : query.passengers < 5 ? 'а' : 'ов'}`,
                `${resultCount} результатов`
              ].filter(Boolean).join(' · ')}
            </div>
          </div>
        </div>

        <div className="homev2res__grid">
          <ResultsFilters />

          <div>
            <div className="homev2res__sorts">
              {SORTS.map((s) => (
                <button
                  key={s.id}
                  type="button"
                  onClick={() => setSort(s.id)}
                  className={`homev2res__sortBtn ${
                    sort === s.id ? 'homev2res__sortBtn--active' : ''
                  }`}
                >
                  <div className="homev2res__sortLbl">{s.label}</div>
                </button>
              ))}
            </div>

            <div className="homev2res__list">
              {loading ? (
                Array.from({ length: 4 }).map((_, i) => <SkeletonCard key={i} />)
              ) : error ? (
                <p>Ошибка: {error}</p>
              ) : resultCount === 0 ? (
                <p>Рейсы не найдены по заданным параметрам.</p>
              ) : (
                sortedFlights.map((flight) => (
                  <FlightCard key={flight.id} flight={flight} onSelect={handleBuyClick} />
                ))
              )}
            </div>
          </div>
        </div>
      </div>
      {selectedFlight && (
        <BookingModal
          flight={selectedFlight}
          isOpen={isBookingOpen}
          onClose={() => setIsBookingOpen(false)}
          onBook={() => setIsBookingOpen(false)}
          user={currentUser}
        />
      )}
    </section>
  );
}

function FlightCard({ flight, onSelect }) {
  return (
    <article className="homev2res__card">
      <div className="homev2res__cardMain">
        <div className="homev2res__air">
          <div className="homev2res__airIcon">
            <Plane className="homev2res__airSvg" />
          </div>
          <div className="homev2res__airName">{flight.flightNumber}</div>
        </div>

        <div className="homev2res__times">
          <div>
            <div className="homev2res__t">{formatTime(flight.departureDt)}</div>
            <div className="homev2res__code">{flight.originAirport?.iata}</div>
          </div>
          <div className="homev2res__line">
            <div className="homev2res__dur">{formatDuration(flight.durationMinutes)}</div>
            <div className="homev2res__bar" aria-hidden>
              <span className="homev2res__dot homev2res__dot--l" />
              <span className="homev2res__dot homev2res__dot--r" />
            </div>
            <div className="homev2res__stops">{flight.baggageInfo}</div>
          </div>
          <div className="homev2res__arr">
            <div className="homev2res__t">{formatTime(flight.arrivalDt)}</div>
            <div className="homev2res__code">{flight.destAirport?.iata}</div>
          </div>
        </div>

        <div className="homev2res__amenities">
          <Wifi className="homev2res__amenity" />
          <Utensils className="homev2res__amenity" />
          <Luggage className="homev2res__amenity" />
        </div>
      </div>

      <div className="homev2res__cardSide">
        <div className="homev2res__sidePrice">
          <div className="homev2res__price">{flight.minPrice ? `${flight.minPrice}₽` : '—'}</div>
          <div className="homev2res__per">за пассажира</div>
        </div>
        <button
          className="homev2res__choose"
          type="button"
          onClick={() => onSelect(flight)}
        >
          Выбрать
        </button>
      </div>
    </article>
  );
}

function SkeletonCard() {
  return (
    <article className="homev2res__card homev2res__card--skeleton">
      <div className="homev2res__cardMain">
        <div className="homev2res__air">
          <div className="homev2res__airIcon homev2res__skeletonBox" />
          <div className="homev2res__airName homev2res__skeletonBox" />
        </div>

        <div className="homev2res__times">
          <div>
            <div className="homev2res__t homev2res__skeletonBox" />
            <div className="homev2res__code homev2res__skeletonBox" />
          </div>
          <div className="homev2res__line">
            <div className="homev2res__dur homev2res__skeletonBox" />
            <div className="homev2res__stops homev2res__skeletonBox" />
          </div>
          <div className="homev2res__arr">
            <div className="homev2res__t homev2res__skeletonBox" />
            <div className="homev2res__code homev2res__skeletonBox" />
          </div>
        </div>
      </div>
      <div className="homev2res__cardSide">
        <div className="homev2res__sidePrice">
          <div className="homev2res__price homev2res__skeletonBox" />
          <div className="homev2res__per homev2res__skeletonBox" />
        </div>
      </div>
    </article>
  );
}

function formatDuration(durationMinutes) {
  const hours = Math.floor(durationMinutes / 60);
  const minutes = durationMinutes % 60;
  return `${hours}ч ${minutes}м`;
}

function formatTime(dateString) {
  const date = new Date(dateString);
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `${hours}:${minutes}`;
}

