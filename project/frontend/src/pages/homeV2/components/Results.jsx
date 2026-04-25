import React from 'react';
import { Plane, ArrowLeft, Wifi, Utensils, Luggage } from 'lucide-react';
import ResultsFilters from './ResultsFilters';
import SearchFormV2 from './SearchFormV2';

const FLIGHTS = [
  {
    id: '1',
    airline: 'Turkish Airlines',
    depTime: '06:40',
    arrTime: '10:25',
    depCode: 'DME',
    arrCode: 'IST',
    duration: '3ч 45м',
    stops: 'Прямой',
    price: 189,
    tag: { label: 'Дешевле всего', tone: 'emerald' },
  },
  {
    id: '2',
    airline: 'Aeroflot',
    depTime: '09:10',
    arrTime: '12:55',
    depCode: 'SVO',
    arrCode: 'IST',
    duration: '3ч 45м',
    stops: 'Прямой',
    price: 215,
    tag: { label: 'Лучшее время', tone: 'blue' },
  },
  {
    id: '3',
    airline: 'Pegasus',
    depTime: '14:25',
    arrTime: '17:55',
    depCode: 'VKO',
    arrCode: 'SAW',
    duration: '3ч 30м',
    stops: 'Прямой',
    price: 168,
    tag: { label: 'Быстрее всего', tone: 'violet' },
  },
  {
    id: '4',
    airline: 'Lufthansa',
    depTime: '21:05',
    arrTime: '04:40',
    depCode: 'DME',
    arrCode: 'IST',
    duration: '6ч 35м',
    stops: '1 пересадка · FRA',
    price: 242,
  },
  {
    id: '5',
    airline: 'Qatar Airways',
    depTime: '18:50',
    arrTime: '09:20',
    depCode: 'DME',
    arrCode: 'IST',
    duration: '13ч 30м',
    stops: '1 пересадка · DOH',
    price: 278,
  },
];

const SORTS = [
  { id: 'cheapest', label: 'Дешевле', sub: 'от $168' },
  { id: 'fastest', label: 'Быстрее', sub: '3ч 30м' },
  { id: 'best', label: 'Лучшие', sub: 'сбалансировано' },
];

export default function Results({ query, onBack, onSearch }) {
  const [sort, setSort] = React.useState('cheapest');
  const [loading, setLoading] = React.useState(true);

  React.useEffect(() => {
    setLoading(true);
    const t = setTimeout(() => setLoading(false), 900);
    return () => clearTimeout(t);
  }, [query]);

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
              {query.from?.name ?? query.from} → {query.to?.name ?? query.to}
            </h2>
            <div className="homev2res__sub">
              {query.date} · {query.passengers} пассажир
              {query.passengers === 1 ? '' : query.passengers < 5 ? 'а' : 'ов'} ·{' '}
              {FLIGHTS.length} результатов
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
                  <div className="homev2res__sortSub">{s.sub}</div>
                </button>
              ))}
            </div>

            <div className="homev2res__list">
              {loading
                ? Array.from({ length: 4 }).map((_, i) => (
                    <SkeletonCard key={i} />
                  ))
                : FLIGHTS.map((f) => <FlightCard key={f.id} flight={f} />)}
            </div>
          </div>
        </div>
      </div>
    </section>
  );
}

function FlightCard({ flight }) {
  return (
    <article className="homev2res__card">
      <div className="homev2res__cardMain">
        <div className="homev2res__air">
          <div className="homev2res__airIcon">
            <Plane className="homev2res__airSvg" />
          </div>
          <div className="homev2res__airName">{flight.airline}</div>
        </div>

        <div className="homev2res__times">
          <div>
            <div className="homev2res__t">{flight.depTime}</div>
            <div className="homev2res__code">{flight.depCode}</div>
          </div>
          <div className="homev2res__line">
            <div className="homev2res__dur">{flight.duration}</div>
            <div className="homev2res__bar" aria-hidden>
              <span className="homev2res__dot homev2res__dot--l" />
              <span className="homev2res__dot homev2res__dot--r" />
            </div>
            <div className="homev2res__stops">{flight.stops}</div>
          </div>
          <div className="homev2res__arr">
            <div className="homev2res__t">{flight.arrTime}</div>
            <div className="homev2res__code">{flight.arrCode}</div>
          </div>
        </div>

        <div className="homev2res__amenities">
          <Wifi className="homev2res__amenity" />
          <Utensils className="homev2res__amenity" />
          <Luggage className="homev2res__amenity" />
        </div>
      </div>

      <div className="homev2res__cardSide">
        {flight.tag && (
          <span className={`homev2res__tag homev2res__tag--${flight.tag.tone}`}>
            {flight.tag.label}
          </span>
        )}
        <div className="homev2res__sidePrice">
          <div className="homev2res__price">${flight.price}</div>
          <div className="homev2res__per">за пассажира</div>
        </div>
        <button className="homev2res__choose" type="button">
          Выбрать
        </button>
      </div>
    </article>
  );
}

function SkeletonCard() {
  return (
    <div className="homev2res__skeleton">
      <div className="homev2res__skRow">
        <div className="homev2res__skBox homev2res__skBox--icon" />
        <div className="homev2res__skMid">
          <div className="homev2res__skBox homev2res__skBox--t" />
          <div className="homev2res__skBox homev2res__skBox--line" />
          <div className="homev2res__skBox homev2res__skBox--t" />
        </div>
        <div className="homev2res__skBox homev2res__skBox--btn" />
      </div>
    </div>
  );
}

