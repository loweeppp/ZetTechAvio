import Header from '../../components/layout/Header';
import SearchForm from '../../components/SearchForm';
import FlightsList from '../../components/FlightsList';
import './Home.css';
import RecommendedFlights from '../../components/RecommFlights';
import Footer from '../../components/layout/Footer';

export default function Home() {
  return (
    <div className="App">
      <Header />
      <main className="main-content">
        <section classsName="hero-section">
          <div className="grid-container">
            <SearchForm />
            <RecommendedFlights />
          </div>
        </section>
        <FlightsList />
      </main>
      <Footer />
    </div>
  );
}
