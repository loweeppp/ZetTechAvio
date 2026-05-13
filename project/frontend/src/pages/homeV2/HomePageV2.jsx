import React from 'react';
import './HomePageV2.css';
import { useTranslation } from '../../i18n/TranslationProvider';

import SearchFormV2 from '../../features/flights/SearchFormV2';
import AISearch from '../../features/flights/AISearch';
import RecommendedFlights from '../../features/flights/RecommendedFlights';
import PopularDestinations from '../../features/flights/PopularDestinations';
import Benefits from '../../features/flights/Benefits';
import Results from '../../features/flights/Results';

export default function HomePageV2() {
  const { t } = useTranslation();
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
                  {t('home.headlineLine1')}
                  <br />
                  <span className="homev2__headlineAccent">
                    {t('home.headlineAccent')}
                  </span>
                </h1>
                <p className="homev2__lead">{t('home.lead')}</p>
              </div>

              <div className="homev2__formWrap">
                <SearchFormV2 onSearch={(q) => setQuery(q)} />
                <AISearch onSearch={(q) => setQuery(q)} />
              </div>
            </div>
          </div>

          <RecommendedFlights onSearch={(q) => setQuery(q)} />
          <PopularDestinations onSearch={(q) => setQuery(q)} />
          <Benefits />
        </>
      )}
    </section>
  );
}
