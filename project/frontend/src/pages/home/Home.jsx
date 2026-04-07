import { useState } from 'react';
import SearchForm from '../../components/searchform/SearchForm';
import FlightsList from '../../components/flightslist/FlightsList';
import './Home.css';
import RecommendedFlights from '../../components/recommflights/RecommFlights';

export default function Home() {
  const [searchParams, setSearchParams] = useState(null);

  const handleSearch = (params) => {
    setSearchParams(params);
    console.log('Параметры поиска из Home:', params);
  }
  return (
    <>
      <section className="hero-section">
        <div className="grid-container">
          <SearchForm onSearch={handleSearch} />
          <RecommendedFlights />
        </div>
      </section>
      <FlightsList searchParams={searchParams}/>
    </>
  );
}
