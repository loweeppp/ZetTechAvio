import React from 'react';
import { ArrowUpRight } from 'lucide-react';
import ImageWithFallback from './ImageWithFallback';

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
          <button className="homev2__seeAll" type="button" onClick={() => {}}>
            Смотреть все <ArrowUpRight className="homev2__seeAllIcon" />
          </button>
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

