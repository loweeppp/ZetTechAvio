import React from 'react';

const AIRLINES = [
  'Aeroflot',
  'Turkish Airlines',
  'Emirates',
  'Air France',
  'Lufthansa',
];

export default function ResultsFilters() {
  const [price, setPrice] = React.useState(800);
  const [direct, setDirect] = React.useState(false);
  const [times, setTimes] = React.useState([]);
  const [airlines, setAirlines] = React.useState([]);

  const toggle = (arr, v, set) =>
    set(arr.includes(v) ? arr.filter((x) => x !== v) : [...arr, v]);

  return (
    <aside className="homev2res__filters">
      <div className="homev2res__filtersHead">
        <div className="homev2res__filtersTitle">Фильтры</div>
        <button className="homev2res__filtersReset" type="button">
          Сбросить
        </button>
      </div>

      <FilterGroup title="Цена">
        <div className="homev2res__priceRow">
          <span>$50</span>
          <span className="homev2res__priceValue">до ${price}</span>
        </div>
        <input
          type="range"
          min={50}
          max={1500}
          value={price}
          onChange={(e) => setPrice(Number(e.target.value))}
          className="homev2res__range"
        />
      </FilterGroup>

      <FilterGroup title="Пересадки">
        <Checkbox
          label="Только прямые рейсы"
          checked={direct}
          onChange={() => setDirect(!direct)}
        />
      </FilterGroup>

      <FilterGroup title="Время вылета">
        <div className="homev2res__timeGrid">
          {[
            { id: 'morning', label: 'Утро', sub: '06–12' },
            { id: 'afternoon', label: 'День', sub: '12–18' },
            { id: 'evening', label: 'Вечер', sub: '18–00' },
            { id: 'night', label: 'Ночь', sub: '00–06' },
          ].map((t) => {
            const active = times.includes(t.id);
            return (
              <button
                key={t.id}
                type="button"
                onClick={() => toggle(times, t.id, setTimes)}
                className={`homev2res__timeBtn ${
                  active ? 'homev2res__timeBtn--active' : ''
                }`}
              >
                <div className="homev2res__timeLbl">{t.label}</div>
                <div className="homev2res__timeSub">{t.sub}</div>
              </button>
            );
          })}
        </div>
      </FilterGroup>

      <FilterGroup title="Авиакомпании" last>
        <div className="homev2res__airlines">
          {AIRLINES.map((a) => (
            <Checkbox
              key={a}
              label={a}
              checked={airlines.includes(a)}
              onChange={() => toggle(airlines, a, setAirlines)}
            />
          ))}
        </div>
      </FilterGroup>
    </aside>
  );
}

function FilterGroup({ title, children, last }) {
  return (
    <div className={last ? '' : 'homev2res__group'}>
      <div className="homev2res__groupTitle">{title}</div>
      {children}
    </div>
  );
}

function Checkbox({ label, checked, onChange }) {
  return (
    <label className="homev2res__check">
      <span
        className={`homev2res__box ${checked ? 'homev2res__box--on' : ''}`}
      >
        {checked && (
          <svg viewBox="0 0 12 12" className="homev2res__tick">
            <path
              d="M2.5 6.5L5 9l4.5-5"
              fill="none"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        )}
      </span>
      <input
        type="checkbox"
        checked={checked}
        onChange={onChange}
        className="homev2res__sr"
      />
      {label}
    </label>
  );
}

