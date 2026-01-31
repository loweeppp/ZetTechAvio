import { useState, useEffect } from 'react';
import './FlightsList.css';

export default function FlightsList({ searchParams }) {
  const [flights, setFlights] = useState([]);
  const [loading, setLoading] = useState(true);


  useEffect(() => {
    let url = 'http://localhost:5151/api/flights';

    if (searchParams) {
      const params = new URLSearchParams();
      if (searchParams.from && searchParams.from.trim()) params.append('from', searchParams.from);
      if (searchParams.to && searchParams.to.trim()) params.append('to', searchParams.to);
      if (searchParams.date && searchParams.date.trim()) params.append('date', searchParams.date);
      url += '/search?' + params.toString();
    }

    fetch(url)
      .then(r => {
        if (!r.ok) throw new Error('Ошибка сети');
        return r.json();
      })
      .then(data => {
        setFlights(data);
        setLoading(false);
      })
      .catch(err => {
        console.error(err);
        setLoading(false);
      });
  }, [searchParams]);

  if (loading) return <p>Загрузка рейсов...</p>;
  if (flights.length === 0) return <p>Рейсы не найдены</p>;

  return (
    <section className="flights-section">
      <div className="flights-list" >
        <h2 className="section-title">Рейсы</h2>
        {flights.map(flight => (
          <div key={flight.id} className="flight-card">
            <div className="flight-card-left">
              <h3 className="flight-title">{flight.flightNumber} • {flight.originAirport?.name} → {flight.destAirport?.name}</h3>
              <p className="flight-subtitle">•{formatDuration(flight.durationMinutes)}</p>
            </div>
            <div className="flight-card-right">
              <div className="price">{flight.minPrice ? `${flight.minPrice}₽` : '—'}</div>
              <button className="btn-buy">Купить</button>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

function formatDuration(durationMinutes) {
  const hours = Math.floor(durationMinutes / 60);
  const minutes = durationMinutes % 60;
  return `${hours}ч ${minutes}м`;
}

