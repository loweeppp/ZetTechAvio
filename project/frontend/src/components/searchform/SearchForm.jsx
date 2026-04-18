import { useState, useRef } from 'react';
import './SearchForm.css';

export default function SearchForm({onSearch}) {
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [date, setDate] = useState('');
  const [passengers, setPassengers] = useState(1);

  const handleFromChange = (e) => {
    setFrom(e.target.value);
  };

  const handleToChange = (e) => {
    setTo(e.target.value);
  };

  const handleDateChange = (e) => {
    setDate(e.target.value);
  };

  const handlePassengersChange = (e) => {
    setPassengers(e.target.value);
  };

  const handleSearch = () => {
    // Check if at least one search field is filled
    if (!from && !to && !date) {
      console.warn('Please fill at least one search field');
      return;
    }

    onSearch({from, to, date, passengers});
  };

  return (
    <div className="search-column">
      <div className="search-card">
        <h1 className="title">Найди свой рейс быстро<br />удобно – и без заморочек</h1>

        <div className="form-container">
          <div className="form-grid">

            {/* Откуда */}
            <div className='input-wrapper'>
              <svg className="input-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" >
                <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z" />
                <circle cx="12" cy="10" r="3" />
              </svg>
              <input className="input" placeholder="Откуда" value={from} onChange={handleFromChange} />
            </div>

            {/* Куда */}
            <div className='input-wrapper'>
              <svg className="input-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" >
                <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z" />
                <circle cx="12" cy="10" r="3" />
              </svg>
              <input className="input" placeholder="Куда" value={to} onChange={handleToChange} />
            </div>

            {/* Дата */}
            <div className='input-wrapper'>
              <svg className="input-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
                <line x1="16" y1="2" x2="16" y2="6" />
                <line x1="8" y1="2" x2="8" y2="6" />
                <line x1="3" y1="10" x2="21" y2="10" />
              </svg>
              <input className="input input-date" type="date" value={date} onChange={handleDateChange} />
            </div>

            {/* Пассажиры */}
            <div className='input-wrapper'>
              <svg className="input-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
                <circle cx="9" cy="7" r="4" />
                <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
                <path d="M16 3.13a4 4 0 0 1 0 7.75" />
              </svg>
              <select className="input" value={passengers} onChange={handlePassengersChange}>
                <option value="1">1 пассажир</option>
                <option value="2">2 пассажира</option>
                <option value="3">3 пассажира</option>
                <option value="4">4 пассажира</option>
              </select>
            </div>

          </div>

          {/* Найти */}
          <button className="btn-search" onClick={handleSearch}>Найти</button>
          <span className="hint-text" >
            Начните поиск — затем сможете выбрать места и оформить покупку билета.
          </span>
        </div>
      </div>


    </div>

  );
}