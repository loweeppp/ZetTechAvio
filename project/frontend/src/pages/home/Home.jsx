import Header from '../../components/layout/Header';
import SearchForm from '../../components/SearchForm';
import FlightsList from '../../components/FlightsList';

export default function Home() {
  return (
    <div className="page-container">
      <Header />
      <main>
        <SearchForm />
        {/* <RecommendedFlights /> */}
        <FlightsList />
      </main>
    </div>
  );
}
