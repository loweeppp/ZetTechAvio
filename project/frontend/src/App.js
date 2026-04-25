
import './App.css';
import './pages/home/Home.css';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import HomePageLegacy from './pages/home/HomePageLegacy';
import HomePageV2 from './pages/homeV2/HomePageV2';
import MyBookings from './pages/bookings/MyBookings';
import UserProfile from './pages/profile/UserProfile';
import PrivacyPolicy from './pages/privacy/PrivacyPolicy';
import Header from './components/layout/header/Header';
import Footer from './components/layout/footer/Footer';
import { isHomeV2Enabled } from './features/featureFlags';
import HeaderV2 from './components/layout/header/HeaderV2';
import FooterV2 from './components/layout/footer/FooterV2';

// const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru' || 'http://localhost:3000';

function App() {
  const homeV2Enabled = isHomeV2Enabled();
  const homeElement = homeV2Enabled ? <HomePageV2 /> : <HomePageLegacy />;
  const mainClassName = homeV2Enabled
    ? 'main-content main-content--homev2'
    : 'main-content';

  return (
    <BrowserRouter>
      <div className="App">
        {homeV2Enabled ? <HeaderV2 /> : <Header />}
        <main className={mainClassName}>
          <Routes>
            <Route path="/" element={homeElement} />
            <Route path="/bookings" element={<MyBookings />} />
            <Route path="/profile" element={<UserProfile />} />
            <Route path="/privacy" element={<PrivacyPolicy />} />
          </Routes>
        </main>
        {homeV2Enabled ? <FooterV2 /> : <Footer />}
      </div>
    </BrowserRouter>
  );
}

export default App;
