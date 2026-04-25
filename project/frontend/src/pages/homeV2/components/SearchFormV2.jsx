import React from 'react';
import {
  ArrowLeftRight,
  Calendar,
  MapPin,
  Users,
  Search,
} from 'lucide-react';

const CITIES = [
  { code: 'MOW', name: 'Moscow', airport: 'SVO/DME/VKO' },
  { code: 'LED', name: 'Saint Petersburg', airport: 'LED' },
  { code: 'KZN', name: 'Kazan', airport: 'KZN' },
  { code: 'AER', name: 'Sochi', airport: 'AER' },
  { code: 'IST', name: 'Istanbul', airport: 'IST' },
  { code: 'DXB', name: 'Dubai', airport: 'DXB' },
  { code: 'LON', name: 'London', airport: 'LHR/LGW' },
  { code: 'PAR', name: 'Paris', airport: 'CDG/ORY' },
  { code: 'BKK', name: 'Bangkok', airport: 'BKK' },
  { code: 'NYC', name: 'New York', airport: 'JFK/LGA/EWR' },
];

function CityInput({ label, value, onChange, placeholder }) {
  const [open, setOpen] = React.useState(false);
  const [query, setQuery] = React.useState('');
  const ref = React.useRef(null);

  React.useEffect(() => {
    const onDoc = (e) => {
      if (ref.current && !ref.current.contains(e.target)) setOpen(false);
    };
    document.addEventListener('mousedown', onDoc);
    return () => document.removeEventListener('mousedown', onDoc);
  }, []);

  const filtered = CITIES.filter((c) => !value || c.code !== value.code).filter(
    (c) =>
      query
        ? `${c.name}${c.code}${c.airport}`
            .toLowerCase()
            .includes(query.toLowerCase())
        : true,
  );

  return (
    <div ref={ref} className="homev2sf__cityWrap">
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="homev2sf__cityBtn"
      >
        <span className="homev2sf__cityLabel">{label}</span>
        {value ? (
          <span className="homev2sf__cityValue">
            <span className="homev2sf__cityName">{value.name}</span>
            <span className="homev2sf__cityCode">{value.code}</span>
          </span>
        ) : (
          <span className="homev2sf__cityPlaceholder">{placeholder}</span>
        )}
      </button>

      {open && (
        <div className="homev2sf__dropdown">
          <div className="homev2sf__searchRow">
            <MapPin className="homev2sf__searchIcon" />
            <input
              autoFocus
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Поиск города или аэропорта"
              className="homev2sf__searchInput"
            />
          </div>

          <ul className="homev2sf__list">
            {filtered.map((c) => (
              <li key={c.code}>
                <button
                  type="button"
                  onClick={() => {
                    onChange(c);
                    setOpen(false);
                    setQuery('');
                  }}
                  className="homev2sf__item"
                >
                  <div className="homev2sf__itemLeft">
                    <MapPin className="homev2sf__itemIcon" />
                    <span className="homev2sf__itemName">{c.name}</span>
                    <span className="homev2sf__itemAirport">{c.airport}</span>
                  </div>
                  <span className="homev2sf__itemCode">{c.code}</span>
                </button>
              </li>
            ))}

            {filtered.length === 0 && (
              <li className="homev2sf__empty">Нет совпадений</li>
            )}
          </ul>
        </div>
      )}
    </div>
  );
}

export default function SearchFormV2({ onSearch }) {
  const [from, setFrom] = React.useState(CITIES[0]);
  const [to, setTo] = React.useState(CITIES[4]);
  const [date, setDate] = React.useState('');
  const [passengers, setPassengers] = React.useState(1);
  const [swapping, setSwapping] = React.useState(false);

  const swap = () => {
    setSwapping(true);
    setTimeout(() => setSwapping(false), 300);
    setFrom(to);
    setTo(from);
  };

  const handleSubmit = (e) => {
    e.preventDefault();
    if (from && to) {
      onSearch?.({ from, to, date: date || 'Apr 28', passengers });
    }
  };

  return (
    <form className="homev2sf" onSubmit={handleSubmit}>
      <div className="homev2sf__row">
        <div className="homev2sf__pair">
          <CityInput
            label="Откуда"
            value={from}
            onChange={setFrom}
            placeholder="Город или аэропорт"
          />

          <button
            type="button"
            onClick={swap}
            className="homev2sf__swap"
            aria-label="Swap"
          >
            <ArrowLeftRight
              className={`homev2sf__swapIcon ${swapping ? 'homev2sf__swapIcon--spin' : ''}`}
            />
          </button>

          <CityInput
            label="Куда"
            value={to}
            onChange={setTo}
            placeholder="Город или аэропорт"
          />
        </div>

        <div className="homev2sf__sep" aria-hidden />

        <label className="homev2sf__date">
          <span className="homev2sf__dateLabel">
            <Calendar className="homev2sf__dateIcon" /> Дата вылета
          </span>
          <input
            type="date"
            value={date}
            onChange={(e) => setDate(e.target.value)}
            className="homev2sf__dateInput"
          />
        </label>

        <div className="homev2sf__sep" aria-hidden />

        <div className="homev2sf__pax">
          <div className="homev2sf__paxLabel">
            <Users className="homev2sf__paxIcon" /> Пассажиры
          </div>
          <div className="homev2sf__paxCtrls">
            <button
              type="button"
              onClick={() => setPassengers((p) => Math.max(1, p - 1))}
              className="homev2sf__paxBtn"
            >
              −
            </button>
            <span className="homev2sf__paxValue">{passengers}</span>
            <button
              type="button"
              onClick={() => setPassengers((p) => Math.min(9, p + 1))}
              className="homev2sf__paxBtn"
            >
              +
            </button>
          </div>
        </div>

        <button type="submit" className="homev2sf__submit">
          <Search className="homev2sf__submitIcon" />
          <span>Поиск авиабилетов</span>
        </button>
      </div>
    </form>
  );
}

