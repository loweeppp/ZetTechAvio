import React from 'react';

export default function ResultsFilters({ price,minDurration, maxDuration, minPrice, maxPrice, onPriceChange, duration, onDurationChange, baggageOnly, onBaggageChange, onReset }) {
  return (
    <aside className="homev2res__filters">
      <div className="homev2res__filtersHead">
        <div className="homev2res__filtersTitle">Фильтры</div>
        <button className="homev2res__filtersReset" type="button" onClick={onReset}>
          Сбросить
        </button>
      </div>

      <FilterGroup title="Цена">
        <div className="homev2res__priceRow">
          <span>{minPrice} ₽</span>
          <span className="homev2res__priceValue">до {price} ₽</span>
        </div>
        <input
          type="range"
          min={minPrice}
          max={maxPrice}
          value={price}
          onChange={(e) => onPriceChange(Number(e.target.value))}
          className="homev2res__range"
        />
      </FilterGroup>

      <FilterGroup title="Продолжительность">
        <div className="homev2res__priceRow">
          <span>до {Math.floor(duration / 60)} ч</span>
          <span className="homev2res__priceValue">{duration} мин</span>
        </div>
        <input
          type="range"
          min={60}
          max={720}
          step={30}
          value={duration}
          onChange={(e) => onDurationChange(Number(e.target.value))}
          className="homev2res__range"
        />
      </FilterGroup>

      <FilterGroup title="Багаж">
        <Checkbox
          label="Только с багажом"
          checked={baggageOnly}
          onChange={onBaggageChange}
        />
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

