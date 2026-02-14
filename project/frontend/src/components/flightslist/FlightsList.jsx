import { useState, useEffect, } from 'react';
import './FlightsList.css';

export default function FlightsList({ searchParams }) {


  const [hoveredFlightId, setHoveredFlightId] = useState(null)

  const isHovered = (flightId) => hoveredFlightId === flightId;


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
      <h2 className="section-title">Рейсы</h2>
      <div className="flights-list" >
        


        {flights.map(flight => (
          // карточка полета
          <div key={flight.id} className="flight-card" onMouseEnter={() => setHoveredFlightId(flight.id)} onMouseLeave={() => setHoveredFlightId(null)}>

            <div className="flight-card-left">
              <h3 className="flight-title">{flight.flightNumber} • {flight.originAirport?.city} → {flight.destAirport?.city}</h3>

              {isHovered(flight.id) ? (
                <div className="flight-card">
                  <div className="Flight-card-left">
                    <p className="flight-title-hours"> {formatTime(flight.departureDt)} <img src="/Sterlka.svg" className="imagelogo" /> {formatTime(flight.arrivalDt)}</p>
                    <p className="flight-subtitle-bold"> Время в пути: <span>{formatDuration(flight.durationMinutes)}</span></p>
                    <p className="flight-subtitle-card">{Directflight(flight)} • {flight.baggageInfo}</p>
                  </div>
                  <svg className="divider" viewBox="0 0 1 100" preserveAspectRatio="none">
                    <path d="M 0 0 L 0 100" stroke="currentColor" strokeWidth="6" />
                  </svg>
                  <div className="flight-card-center">

                    <p className="flight-subtitle-bold">{formatTime(flight.departureDt)} — <span>{flight.originAirport?.name || '—'}</span></p>
                    <p className="flight-subtitle-bold">{formatTime(flight.arrivalDt)} — <span>{flight.destAirport?.name || '—'}</span></p>
                  </div>

                  <div className="flight-card-right">
                    <div className="price-card">{flight.minPrice ? `${flight.minPrice}₽` : '—'}</div>
                    <button className="btn-buy">Купить</button>
                  </div>
                </div>



              ) : (
                // при наведении показываем время вылета и прилета
                <p className="flight-subtitle">•{formatDuration(flight.durationMinutes)} • {Directflight(flight)} • {flight.baggageInfo}</p>
              )}
            </div>


            {isHovered(flight.id) ? (null)
              : (
                <div className="flight-card-right">
                  <div className="price">{flight.minPrice ? `${flight.minPrice}₽` : '—'}</div>
                  <button className="btn-buy">Купить</button>
                </div>)}
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

function Directflight(flight) {
  return flight.isDirect ? 'С пересадками' : 'Прямой рейс';
}

function formatTime(dateString) {
  const date = new Date(dateString);
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');
  return `${hours}:${minutes}`;
}

