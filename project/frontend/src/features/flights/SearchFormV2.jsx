import React from 'react';
import {
  ArrowLeftRight,
  Calendar,
  MapPin,
  Users,
  Search,
} from 'lucide-react';

import { useTranslation } from '../../i18n/TranslationProvider';
import { CITIES } from './cities';

const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

const formatDate = (value) => {
  if (!value) return '';
  const parts = String(value).split('-');
  return parts.length === 3 ? `${parts[2]}.${parts[1]}.${parts[0]}` : String(value);
};

function CityInput({ label, value, onChange, placeholder, items = CITIES }) {
  const { t } = useTranslation();
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

  const filtered = items.filter((c) => !value || c.code !== value.code).filter(
    (c) =>
      query
        ? `${c.name}${c.query ?? c.name}${c.code}${c.airport}`
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
              placeholder={t('searchForm.cityPlaceholder')}
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
              <li className="homev2sf__empty">{t('searchForm.noMatches')}</li>
            )}
          </ul>
        </div>
      )}
    </div>
  );
}

export default function SearchFormV2({ onSearch, onDraftChange }) {
  const { t } = useTranslation();
  const [from, setFrom] = React.useState(null);
  const [to, setTo] = React.useState(null);
  const [date, setDate] = React.useState('');
  const [passengers, setPassengers] = React.useState(1);
  const [swapping, setSwapping] = React.useState(false);
  const [error, setError] = React.useState('');
  const [cities, setCities] = React.useState(CITIES);
  const [routeMap, setRouteMap] = React.useState({});
  const [routeDates, setRouteDates] = React.useState({});
  const [dateOpen, setDateOpen] = React.useState(false);
  const [dateQuery, setDateQuery] = React.useState('');
  const dateDropdownRef = React.useRef(null);

  React.useEffect(() => {
    if (!onDraftChange) return;
    onDraftChange({ from, to, date, passengers });
  }, [from, to, date, passengers, onDraftChange]);

  const swap = () => {
    setSwapping(true);
    setTimeout(() => setSwapping(false), 300);
    setFrom(to);
    setTo(from);
  };

  React.useEffect(() => {
    const loadRoutes = async () => {
      try {
        const response = await fetch(`${API_URL}/api/flights`);
        if (!response.ok) return;

        const flights = await response.json();
        const now = new Date();
        const startOfToday = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        const activeFlights = Array.isArray(flights)
          ? flights.filter((flight) => {
              const status = String(flight.status || flight.Status || '').toLowerCase();
              if (status === 'cancelled' || status === 'completed') return false;

              const departureDate = flight.departureDt ? new Date(flight.departureDt) : null;
              return departureDate && departureDate >= startOfToday;
            })
          : [];

        const cityMap = new Map(CITIES.map((item) => [item.code, item]));
        const map = {};
        const dates = {};

        const addRoute = (key, city) => {
          if (!map[key]) map[key] = [];
          if (!map[key].some((entry) => entry.code === city.code)) {
            map[key].push(city);
          }
        };

        const addDate = (key, dateString) => {
          if (!dates[key]) dates[key] = new Set();
          dates[key].add(dateString);
        };

        activeFlights.forEach((flight) => {
          const origin = {
            code: flight.originAirport.iata,
            name: flight.originAirport.city,
            airport: flight.originAirport.iata,
            query: flight.originAirport.city,
          };
          const dest = {
            code: flight.destAirport.iata,
            name: flight.destAirport.city,
            airport: flight.destAirport.iata,
            query: flight.destAirport.city,
          };

          if (!cityMap.has(origin.code)) cityMap.set(origin.code, origin);
          if (!cityMap.has(dest.code)) cityMap.set(dest.code, dest);

          const originCodeKey = origin.code.toLowerCase();
          const originCityKey = origin.query.toLowerCase();
          const destCodeKey = dest.code.toLowerCase();
          const destCityKey = (dest.query || dest.name).toLowerCase();
          const departureDate = flight.departureDt?.slice(0, 10);

          addRoute(originCodeKey, dest);
          addRoute(originCityKey, dest);

          if (departureDate) {
            addDate(`${originCodeKey}:${destCodeKey}`, departureDate);
            addDate(`${originCityKey}:${destCodeKey}`, departureDate);
            addDate(`${originCodeKey}:${destCityKey}`, departureDate);
            addDate(`${originCityKey}:${destCityKey}`, departureDate);
          }
        });

        setCities([...cityMap.values()]);
        setRouteMap(map);
        setRouteDates(
          Object.fromEntries(
            Object.entries(dates).map(([key, set]) => [key, [...set].sort()]),
          ),
        );
      } catch (err) {
        // ignore route fetch errors, keep static cities
      }
    };

    loadRoutes();
  }, []);

  React.useEffect(() => {
    const onDoc = (e) => {
      if (dateDropdownRef.current && !dateDropdownRef.current.contains(e.target)) {
        setDateOpen(false);
      }
    };

    document.addEventListener('mousedown', onDoc);
    return () => document.removeEventListener('mousedown', onDoc);
  }, []);

  const availableDestinations = React.useMemo(() => {
    if (!from) return cities;

    const originKeys = [
      from.code?.toLowerCase(),
      (from.query || from.name || '').toLowerCase(),
    ].filter(Boolean);

    const destinations = [];
    const added = new Set();

    originKeys.forEach((key) => {
      const items = routeMap[key] || [];
      items.forEach((item) => {
        if (!added.has(item.code)) {
          added.add(item.code);
          destinations.push(item);
        }
      });
    });

    return destinations.length > 0 ? destinations : cities;
  }, [from, cities, routeMap]);

  const availableDates = React.useMemo(() => {
    if (!from || !to) return [];

    const routeKeys = [
      `${from.code?.toLowerCase()}:${to.code?.toLowerCase()}`,
      `${from.query?.toLowerCase()}:${to.code?.toLowerCase()}`,
      `${from.code?.toLowerCase()}:${to.query?.toLowerCase()}`,
      `${from.query?.toLowerCase()}:${to.query?.toLowerCase()}`,
    ].filter(Boolean);

    const dates = new Set();

    routeKeys.forEach((key) => {
      (routeDates[key] || []).forEach((dateItem) => dates.add(dateItem));
    });

    return [...dates].sort();
  }, [from, to, routeDates]);

  const filteredDates = React.useMemo(() => {
    if (!dateQuery) return availableDates;
    const search = dateQuery.toLowerCase();
    return availableDates.filter((availableDate) =>
      formatDate(availableDate).toLowerCase().includes(search),
    );
  }, [availableDates, dateQuery]);

  const handleSubmit = (e) => {
    e.preventDefault();
    setError('');
    onSearch?.({ from, to, date, passengers });
  };

  return (
    <form className="homev2sf" onSubmit={handleSubmit}>
      <div className="homev2sf__row">
        <div className="homev2sf__pair">
          <CityInput
            label={t('searchForm.from')}
            value={from}
            onChange={setFrom}
            placeholder={t('searchForm.cityPlaceholder')}
            items={cities}
          />

          <button
            type="button"
            onClick={swap}
            className="homev2sf__swap"
            aria-label={t('searchForm.swap')}
          >
            <ArrowLeftRight
              className={`homev2sf__swapIcon ${swapping ? 'homev2sf__swapIcon--spin' : ''}`}
            />
          </button>

          <CityInput
            label={t('searchForm.to')}
            value={to}
            onChange={setTo}
            placeholder={t('searchForm.cityPlaceholder')}
            items={availableDestinations}
          />
        </div>

        <div className="homev2sf__sep" aria-hidden />

        <label className="homev2sf__date">
          <span className="homev2sf__dateLabel">
            <Calendar className="homev2sf__dateIcon" /> {t('searchForm.departureDate')}
          </span>
          <input
            type="date"
            value={date}
            onChange={(e) => setDate(e.target.value)}
            className="homev2sf__dateInput"
            min={availableDates[0] || undefined}
            max={availableDates[availableDates.length - 1] || undefined}
          />
          {availableDates.length > 0 && (
            <div ref={dateDropdownRef} className="homev2sf__dateHints">
              <button
                type="button"
                className="homev2__destPrice"
                onClick={() => setDateOpen((current) => !current)}
              >
                {date ? `${t('searchForm.selectedDate')}: ${formatDate(date)}` : t('searchForm.chooseDate')}
              </button>

              {dateOpen && (
                <div className="homev2sf__dropdown homev2sf__dateDropdown">
                  <div className="homev2sf__searchRow">
                    <Calendar className="homev2sf__searchIcon" />
                    <input
                      autoFocus
                      value={dateQuery}
                      onChange={(e) => setDateQuery(e.target.value)}
                      placeholder={t('searchForm.filterDate')}
                      className="homev2sf__searchInput"
                    />
                  </div>

                  <ul className="homev2sf__list">
                    {filteredDates.map((availableDate) => (
                      <li key={availableDate}>
                        <button
                          type="button"
                          className={`homev2sf__item ${date === availableDate ? 'homev2sf__item--active' : ''}`}
                          onClick={() => {
                            setDate(availableDate);
                            setDateOpen(false);
                            setDateQuery('');
                          }}
                        >
                          <span>{formatDate(availableDate)}</span>
                        </button>
                      </li>
                    ))}

                    {filteredDates.length === 0 && (
                      <li className="homev2sf__empty">{t('searchForm.noDates')}</li>
                    )}
                  </ul>
                </div>
              )}
            </div>
          )}
        </label>

        <div className="homev2sf__sep" aria-hidden />

        <div className="homev2sf__pax">
          <div className="homev2sf__paxLabel">
            <Users className="homev2sf__paxIcon" /> {t('searchForm.passengers')}
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
              onClick={() => setPassengers((p) => Math.min(5, p + 1))}
              className="homev2sf__paxBtn"
            >
              +
            </button>
          </div>
        </div>

        <button type="submit" className="homev2sf__submit">
          <Search className="homev2sf__submitIcon" />
          <span>{t('searchForm.searchFlights')}</span>
        </button>
      </div>
      {error && <div className="homev2sf__error">{error}</div>}
    </form>
  );
}

