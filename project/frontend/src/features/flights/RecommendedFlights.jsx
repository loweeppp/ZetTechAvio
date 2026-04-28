import React from 'react';
import { Zap, Clock, Award, ArrowRight } from 'lucide-react';

const ITEMS = [
  {
    label: 'Самый дешёвый сегодня',
    Icon: Zap,
    tone: 'emerald',
    route: 'MOW → IST',
    airline: 'Turkish Airlines',
    time: '06:40 — 10:25',
    duration: '3ч 45м · Прямой',
    price: 189,
  },
  {
    label: 'Лучшее время',
    Icon: Award,
    tone: 'blue',
    route: 'LED → DXB',
    airline: 'Emirates',
    time: '11:15 — 19:50',
    duration: '5ч 35м · Прямой',
    price: 412,
  },
  {
    label: 'Самый быстрый',
    Icon: Clock,
    tone: 'violet',
    route: 'MOW → PAR',
    airline: 'Air France',
    time: '09:20 — 12:10',
    duration: '3ч 50м · Прямой',
    price: 298,
  },
];

export default function RecommendedFlights() {
  return (
    <section className="homev2__section homev2__section--recommended">
      <div className="homev2__container">
        <div className="homev2__sectionHead">
          <div className="homev2__kicker">Умный выбор</div>
          <h2 className="homev2__h2">Рекомендуется для вас</h2>
        </div>

        <div className="homev2__cards3">
          {ITEMS.map((it) => (
            <RecommendedCard key={it.label} item={it} />
          ))}
        </div>
      </div>
    </section>
  );
}

function RecommendedCard({ item }) {
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
          <div className="homev2__recPriceValue">${item.price}</div>
        </div>
      </div>

      <button className="homev2__recBtn" type="button">
        Смотреть предложение <ArrowRight className="homev2__recBtnIcon" />
      </button>
    </div>
  );
}

