import { useState } from 'react';
import Header from '../../components/layout/header/Header';
import SearchForm from '../../components/searchform/SearchForm';
import FlightsList from '../../components/flightslist/FlightsList';
import './Home.css';
import RecommendedFlights from '../../components/recommflights/RecommFlights';
import Footer from '../../components/layout/footer/Footer';

export default function Home() {
  const [searchParams, setSearchParams] = useState(null);

  const handleSearch = (params) => {
    setSearchParams(params);
    console.log('Параметры поиска из Home:', params);
  }
  return (
    <div className="App">
      <Header />
      <main className="main-content">
        <section className="hero-section">
          <div className="grid-container">
            <SearchForm onSearch={handleSearch} />
            <RecommendedFlights />
          </div>
        </section>
        <FlightsList searchParams={searchParams}/>
      </main>
      <Footer />
    </div>
  );
}
