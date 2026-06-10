import React, { useEffect, useState } from 'react';
import { Zap, Clock, Award, ArrowRight } from 'lucide-react';
import { CITIES } from './cities';
import { SORTS } from './flightSorting';

const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

const getCity = (code) => CITIES.find((city) => city.code === code) || { code, name: code, query: code };

const formatRouteLabel = (code, airport) => {
  const city = airport?.city
    ? { code: airport.iata, name: airport.city }
    : getCity(code);
  return city.name ? `${city.name} (${city.code})` : code;
};

const formatRouteCodes = (from, to) => {
  const fromCity = getCity(from);
  const toCity = getCity(to);
  return `${fromCity.name ? `${fromCity.name} (${fromCity.code})` : from} → ${toCity.name ? `${toCity.name} (${toCity.code})` : to}`;
};

const ROUTES = [
  {
    id: 'cheapest',
    Icon: Zap,
    tone: 'emerald',
  },
  {
    id: 'best',
    Icon: Award,
    tone: 'blue',
  },
  {
    id: 'fastest',
    Icon: Clock,
    tone: 'violet',
  },
];

const FALLBACK_ROUTE = { from: 'MOW', to: 'IST' };

const DEFAULT_ITEMS = ROUTES.map((route) => ({
  label: SORTS.find((sort) => sort.id === route.id)?.label || route.id,
  Icon: route.Icon,
  tone: route.tone,
  route: `${FALLBACK_ROUTE.from} → ${FALLBACK_ROUTE.to}`,
  airline: '—',
  time: '—',
  duration: '—',
  price: '—',
  search: {
    from: getCity(FALLBACK_ROUTE.from),
    to: getCity(FALLBACK_ROUTE.to),
    passengers: 1,
  },
}));

function getRouteValues(currentSearch) {
  const routeFrom = resolveValue(currentSearch?.from) || FALLBACK_ROUTE.from;
  const routeTo = resolveValue(currentSearch?.to) || FALLBACK_ROUTE.to;
  return { routeFrom, routeTo };
}

function buildPlaceholderItem(route, currentSearch) {
  const { routeFrom, routeTo } = getRouteValues(currentSearch);
  return {
    label: SORTS.find((sort) => sort.id === route.id)?.label || route.id,
    Icon: route.Icon,
    tone: route.tone,
    route: formatRouteCodes(routeFrom, routeTo),
    airline: '—',
    time: '—',
    duration: '—',
    price: '—',
    search: {
      from: getCity(routeFrom),
      to: getCity(routeTo),
      date: resolveValue(currentSearch?.date) || '',
      passengers: currentSearch?.passengers ?? 1,
    },
  };
}

function formatTime(dateString) {
  const date = new Date(dateString);
  if (Number.isNaN(date.getTime())) return '-';
  return date.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' });
}

function formatDuration(minutes) {
  if (!Number.isFinite(minutes)) return '—';
  const hours = Math.floor(minutes / 60);
  const mins = minutes % 60;
  return `${hours}ч ${mins.toString().padStart(2, '0')}м`;
}

function resolveValue(field) {
  if (!field) return '';
  if (typeof field === 'object') {
    return field.query || field.name || field.code || '';
  }
  return field;
}

function resolveRouteCode(field) {
  if (!field) return '';
  if (typeof field === 'object') {
    return field.query || field.code || field.name || '';
  }
  return field;
}

async function fetchAllFlights() {
  const response = await fetch(`${API_URL}/api/flights`);
  if (!response.ok) return [];

  const flights = await response.json();
  return Array.isArray(flights) ? flights : [];
}

function chooseFlight(flights, routeId) {
  const activeFlights = flights.filter((flight) => {
    const status = String(flight.status || flight.Status || '').toLowerCase();
    return status !== 'cancelled' && status !== 'completed';
  });

  if (activeFlights.length === 0) return null;

  return activeFlights.reduce((best, flight) => {
    if (!best) return flight;
    const price = Number(flight.minPrice || flight.MinPrice || 0);
    const bestPrice = Number(best.minPrice || best.MinPrice || 0);
    const duration = Number(flight.durationMinutes || 0);
    const bestDuration = Number(best.durationMinutes || 0);

    if (routeId === 'fastest') {
      return duration < bestDuration ? flight : best;
    }
    if (routeId === 'best') {
      const score = price + duration * 10;
      const bestScore = bestPrice + bestDuration * 10;
      return score < bestScore ? flight : best;
    }
    return price < bestPrice ? flight : best;
  }, null);
}

async function fetchRouteRecommendation(route, currentSearch) {
  try {
    const routeFrom = resolveRouteCode(currentSearch?.from) || '';
    const routeTo = resolveRouteCode(currentSearch?.to) || '';
    let flights = [];

    if (routeFrom || routeTo) {
      const params = new URLSearchParams();
      if (routeFrom) params.append('from', routeFrom);
      if (routeTo) params.append('to', routeTo);

      const dateValue = resolveValue(currentSearch?.date);
      if (dateValue) {
        params.append('date', dateValue);
      }

      const response = await fetch(`${API_URL}/api/flights/search?${params.toString()}`);
      if (!response.ok) {
        flights = [];
      } else {
        flights = await response.json();
      }
    } else {
      flights = await fetchAllFlights();
    }

    if (!Array.isArray(flights) || flights.length === 0) {
      return null;
    }

    const selectedFlight = chooseFlight(flights, route.id);
    if (!selectedFlight) {
      return null;
    }

    const finalRouteFrom = resolveRouteCode(currentSearch?.from) || selectedFlight.originAirport?.iata || FALLBACK_ROUTE.from;
    const finalRouteTo = resolveRouteCode(currentSearch?.to) || selectedFlight.destAirport?.iata || FALLBACK_ROUTE.to;
    const dateValue = resolveValue(currentSearch?.date);

    return {
      label: SORTS.find((sort) => sort.id === route.id)?.label || route.id,
      Icon: route.Icon,
      tone: route.tone,
      route: formatRouteLabel(finalRouteFrom, selectedFlight.originAirport) + ' → ' + formatRouteLabel(finalRouteTo, selectedFlight.destAirport),
      airline: selectedFlight.flightNumber || 'Рейс',
      time: `${formatTime(selectedFlight.departureDt)} - ${formatTime(selectedFlight.arrivalDt)}`,
      duration: `${formatDuration(Number(selectedFlight.durationMinutes))} · Прямой`,
      price: Number(selectedFlight.minPrice || selectedFlight.MinPrice || 0),
      search: {
        from: getCity(finalRouteFrom),
        to: getCity(finalRouteTo),
        date: dateValue || '',
        passengers: currentSearch?.passengers ?? 1,
      },
    };
  } catch (err) {
    return null;
  }
}

export default function RecommendedFlights({ currentSearch, onSearch }) {
  const [items, setItems] = useState(
    ROUTES.map((route) => buildPlaceholderItem(route, currentSearch)),
  );

  useEffect(() => {
    let isActive = true;
    setItems(ROUTES.map((route) => buildPlaceholderItem(route, currentSearch)));

    const loadRecommendations = async () => {
      const loadedItems = await Promise.all(
        ROUTES.map(async (route) => {
          const recommended = await fetchRouteRecommendation(route, currentSearch);
          return (
            recommended || buildPlaceholderItem(route, currentSearch)
          );
        }),
      );

      if (isActive) {
        setItems(loadedItems);
      }
    };

    loadRecommendations();

    return () => {
      isActive = false;
    };
  }, [currentSearch]);

  return (
    <section className="homev2__section homev2__section--recommended">
      <div className="homev2__container">
        <div className="homev2__sectionHead">
          <div className="homev2__kicker">Умный выбор</div>
          <h2 className="homev2__h2">Рекомендуется для вас</h2>
        </div>

        <div className="homev2__cards3">
          {items.map((it) => (
            <RecommendedCard key={it.label} item={it} onSearch={onSearch} />
          ))}
        </div>
      </div>
    </section>
  );
}

function RecommendedCard({ item, onSearch }) {
  const { Icon } = item;

  return (
    <div className="homev2__recCard">
      <div className={`homev2__badge homev2__badge--${item.tone}`}>
        <Icon className="homev2__badgeIcon" />
        {item.label}
      </div>

      <div className="homev2__recMain">
        <div className="homev2__recRoute">{item.route}</div>
        <div className="homev2__recAirline">{item.airline}</div>
      </div>

      <div className="homev2__recMeta">
        <div>
          <div className="homev2__recTime">{item.time}</div>
          <div className="homev2__recDuration">{item.duration}</div>
        </div>
        <div className="homev2__recPrice">
          <div className="homev2__recPriceLabel">от</div>
          <div className="homev2__recPriceValue">
            {typeof item.price === 'number' ? `${item.price} ₽` : `${item.price}`}
          </div>
        </div>
      </div>

      <button
        className="homev2__recBtn"
        type="button"
        onClick={() => onSearch?.(item.search)}
      >
        Смотреть предложение <ArrowRight className="homev2__recBtnIcon" />
      </button>
    </div>
  );
}

