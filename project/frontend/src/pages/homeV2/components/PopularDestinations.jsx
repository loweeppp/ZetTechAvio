import React from 'react';
import { ArrowUpRight } from 'lucide-react';
import ImageWithFallback from './ImageWithFallback';

const DESTINATIONS = [
  {
    city: 'Istanbul',
    country: 'Turkey',
    price: 189,
    img: 'https://images.unsplash.com/photo-1524231757912-21f4fe3a7200?w=800&auto=format&fit=crop',
  },
  {
    city: 'Dubai',
    country: 'UAE',
    price: 349,
    img: 'https://images.unsplash.com/photo-1512453979798-5ea266f8880c?w=800&auto=format&fit=crop',
  },
  {
    city: 'Paris',
    country: 'France',
    price: 279,
    img: 'https://images.unsplash.com/photo-1502602898657-3e91760cbb34?w=800&auto=format&fit=crop',
  },
  {
    city: 'Bangkok',
    country: 'Thailand',
    price: 520,
    img: 'https://images.unsplash.com/photo-1508009603885-50cf7c579365?w=800&auto=format&fit=crop',
  },
  {
    city: 'New York',
    country: 'USA',
    price: 612,
    img: 'https://images.unsplash.com/photo-1496442226666-8d4d0e62e6e9?w=800&auto=format&fit=crop',
  },
  {
    city: 'Tokyo',
    country: 'Japan',
    price: 745,
    img: 'https://images.unsplash.com/photo-1540959733332-eab4deabeeaf?w=800&auto=format&fit=crop',
  },
];

export default function PopularDestinations() {
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
          <a className="homev2__seeAll" href="/" onClick={(e) => e.preventDefault()}>
            Смотреть все <ArrowUpRight className="homev2__seeAllIcon" />
          </a>
        </div>

        <div className="homev2__destGrid">
          {DESTINATIONS.map((d) => (
            <a
              key={d.city}
              className="homev2__destCard"
              href="/"
              onClick={(e) => e.preventDefault()}
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
                    <span className="homev2__destPriceFrom">от </span>$
                    {d.price}
                  </div>
                </div>
              </div>
            </a>
          ))}
        </div>
      </div>
    </section>
  );
}

