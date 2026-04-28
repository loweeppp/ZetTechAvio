
import './App.css';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import HomePageV2 from './pages/homeV2/HomePageV2';
import MyBookings from './features/bookings/MyBookings';
import UserProfile from './pages/profile/UserProfile';
import PrivacyPolicy from './pages/privacy/PrivacyPolicy';
import HeaderV2 from './components/layout/header/HeaderV2';
import FooterV2 from './components/layout/footer/FooterV2';

function App() {
  return (
    <BrowserRouter>
      <div className="App">
        <HeaderV2 />
        <main className="main-content main-content--homev2">
          <Routes>
            <Route path="/" element={<HomePageV2 />} />
            <Route path="/bookings" element={<MyBookings />} />
            <Route path="/profile" element={<UserProfile />} />
            <Route path="/privacy" element={<PrivacyPolicy />} />
          </Routes>
        </main>
        <FooterV2 />
      </div>
    </BrowserRouter>
  );
}

export default App;
