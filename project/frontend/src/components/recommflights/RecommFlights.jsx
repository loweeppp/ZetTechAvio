import { useState, useEffect } from 'react';
import './RecommFlights.css';

export default function RecommendedFlights() {
    const [flights, setFlights] = useState([]);
    const [loading, setLoading] = useState(true);

    useEffect(() => {
        fetch('http://localhost:5151/api/flights')
            .then(r => r.json())
            .then(data => {
                setFlights(data);
                setLoading(false);
            })
            .catch(err => {
                console.error(err);
                setLoading(false);
            });
    }, []);

    if (loading) return <p>Загрузка рейсов...</p>;
    if (flights.length === 0) return <p>Рейсы не найдены</p>;
    return (
        <div className="recommended-flights">
            {/* Рекомендуемый рейс */}
            <div className="recommended-column">
                <div className="recommended-card">
                    <h3 className="card-title">Рекомендуем рейс</h3>

                    <div className="map-preview">
                        <svg className="map-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                            <rect x="3" y="3" width="18" height="18" rx="2" ry="2" />
                            <circle cx="8.5" cy="8.5" r="1.5" />
                            <polyline points="21 15 16 10 5 21" />
                        </svg>
                    </div>

                    {/* {flights.map(flight => (
                        <div key={flight.id}> */}
                        
                            <div className="flight-info">
                                <div>
                                    <div className="flight-route">DME → EMT</div>
                                    <div className="flight-time">12 мая • 09:40 — 11:10</div>
                                </div>
                                <div className="flight-price">4 299 ₽</div>
                            </div>
                        </div>
                    {/* ))} */}


                </div>
            </div>
       
    );
}