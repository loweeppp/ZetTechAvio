import React, { useEffect, useState } from 'react';
import { ArrowUpRight } from 'lucide-react';
import ImageWithFallback from './ImageWithFallback';

const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

const DESTINATIONS = [
  {
    city: 'Istanbul',
    country: 'Turkey',
    price: 3800,
    img: 'https://images.unsplash.com/photo-1524231757912-21f4fe3a7200?w=800&auto=format&fit=crop',
    search: {
      from: { code: 'MOW', name: 'Москва', query: 'Москва' },
      to: { code: 'IST', name: 'Стамбул', query: 'Стамбул' },
      passengers: 1,
    },
  },
  {
    city: 'Dubai',
    country: 'UAE',
    price: 4349,
    img: 'https://images.unsplash.com/photo-1512453979798-5ea266f8880c?w=800&auto=format&fit=crop',
    search: {
      from: { code: 'MOW', name: 'Москва', query: 'Москва' },
      to: { code: 'DXB', name: 'Дубай', query: 'Дубай ' },
      passengers: 1,
    },
  },
  {
    city: 'Paris',
    country: 'France',
    price: 3279,
    img: 'https://images.unsplash.com/photo-1502602898657-3e91760cbb34?w=800&auto=format&fit=crop',
    search: {
      from: { code: 'MOW', name: 'Москва', query: 'Москва' },
      to: { code: 'PAR', name: 'Париж', query: 'Париж' },
      passengers: 1,
    },
  },
  {
    city: 'Bangkok',
    country: 'Thailand',
    price: 3520,
    img: 'https://images.unsplash.com/photo-1508009603885-50cf7c579365?w=800&auto=format&fit=crop',
    search: {
      from: { code: 'MOW', name: 'Москва', query: 'Москва' },
      to: { code: 'BKK', name: 'Бангкок', query: 'Бангкок' },
      passengers: 1,
    },
  },
  {
    city: 'New York',
    country: 'USA',
    price: 4612,
    img: 'https://images.unsplash.com/photo-1496442226666-8d4d0e62e6e9?w=800&auto=format&fit=crop',
    search: {
      from: { code: 'MOW', name: 'Москва', query: 'Москва' },
      to: { code: 'JFK', name: 'Нью-Йорк', query: 'Нью-Йорк' },
      passengers: 1,
    },
  },
  {
    city: 'Tokyo',
    country: 'Japan',
    price: 3745,
    img: 'https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?w=800&auto=format&fit=crop',
    search: {
      from: { code: 'MOW', name: 'Москва', query: 'Moscow' },
      to: { code: 'HND', name: 'Токио', query: 'Tokyo' },
      passengers: 1,
    },
  },
];

export default function PopularDestinations({ onSearch }) {
  const [destinationPrices, setDestinationPrices] = useState({});
  const [pricesLoading, setPricesLoading] = useState(true);

  useEffect(() => {
    const loadDestinationPrices = async () => {
      try {
        const results = await Promise.all(
          DESTINATIONS.map(async (destination) => {
            const params = new URLSearchParams();
            params.append('from', destination.search.from.query || destination.search.from.code);
            params.append('to', destination.search.to.query || destination.search.to.code);

            const response = await fetch(`${API_URL}/api/flights/search?${params.toString()}`);
            if (!response.ok) {
              return { city: destination.city, price: destination.price };
            }

            const flights = await response.json();
            const activePrices = Array.isArray(flights)
              ? flights
                  .filter((flight) => {
                    const status = String(flight.status || flight.Status || '').toLowerCase();
                    return status !== 'cancelled' && status !== 'completed';
                  })
                  .map((flight) => Number(flight.minPrice || flight.MinPrice || 0))
                  .filter(Boolean)
              : [];
            const price = activePrices.length ? Math.min(...activePrices) : destination.price;
            return { city: destination.city, price };
          }),
        );

        setDestinationPrices(
          results.reduce((acc, item) => {
            acc[item.city] = item.price;
            return acc;
          }, {}),
        );
      } catch (error) {
        // keep default prices if backend fetch fails
      } finally {
        setPricesLoading(false);
      }
    };

    loadDestinationPrices();
  }, []);

  return (
    <section className="homev2__section homev2__section--popular">
      <div className="homev2__container">
        <div className="homev2__popularHead">
          <div>
            <div className="homev2__kicker">Популярно сейчас</div>
            <h2 className="homev2__h2">
              Направления, которые любят путешественники
            </h2>
          </div>
        </div>

        <div className="homev2__destGrid">
          {DESTINATIONS.map((d) => (
            <button
              key={d.city}
              type="button"
              className="homev2__destCard"
              onClick={() => onSearch?.(d.search)}
            >
              <div className="homev2__destMedia">
                <ImageWithFallback
                  src={d.img}
                  alt={d.city}
                  className="homev2__destImg"
                />
                <div className="homev2__destOverlay" aria-hidden />
                <div className="homev2__destMeta">
                  <div>
                    <div className="homev2__destCity">{d.city}</div>
                    <div className="homev2__destCountry">{d.country}</div>
                  </div>
                  <div className="homev2__destPrice">
                    <span className="homev2__destPriceFrom">от </span>
                    {d.price} ₽
                  </div>
                </div>
              </div>
            </button>
          ))}
        </div>
      </div>
    </section>
  );
}

