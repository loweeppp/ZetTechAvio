import React from 'react';
import './HomePageV2.css';

import SearchFormV2 from './components/SearchFormV2';
import AISearch from './components/AISearch';
import RecommendedFlights from './components/RecommendedFlights';
import PopularDestinations from './components/PopularDestinations';
import Benefits from './components/Benefits';
import Results from './components/Results';

export default function HomePageV2() {
  const [query, setQuery] = React.useState(null);

  return (
    <section className="homev2">
      {query ? (
        <Results
          query={query}
          onBack={() => setQuery(null)}
          onSearch={(q) => setQuery(q)}
        />
      ) : (
        <>
          <div className="homev2__hero">
            <div className="homev2__bg" aria-hidden />
            <div className="homev2__blob homev2__blob--left" aria-hidden />
            <div className="homev2__blob homev2__blob--right" aria-hidden />

            <div className="homev2__container homev2__heroInner">
              <div className="homev2__heroText">
                <h1 className="homev2__headline">
                  Найдите свой ближайший рейс.
                  <br />
                  <span className="homev2__headlineAccent">
                    Путешествуйте без компромиссов.
                  </span>
                </h1>
                <p className="homev2__lead">
                  Начните поиск — затем сможете выбрать места и оформить покупку
                  билета.
                </p>
              </div>

              <div className="homev2__formWrap">
                <SearchFormV2 onSearch={(q) => setQuery(q)} />
                <AISearch onSearch={(q) => setQuery(q)} />
              </div>
            </div>
          </div>

          <RecommendedFlights />
          <PopularDestinations />
          <Benefits />
        </>
      )}
    </section>
  );
}
