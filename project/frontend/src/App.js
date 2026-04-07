
import './App.css';
import './pages/home/Home.css';
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import Home from './pages/home/Home';
import MyBookings from './pages/bookings/MyBookings';
import UserProfile from './pages/profile/UserProfile';
import PrivacyPolicy from './pages/privacy/PrivacyPolicy';
import Header from './components/layout/header/Header';
import Footer from './components/layout/footer/Footer';

// const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru' || 'http://localhost:3000';

function App() {
  return (
    <BrowserRouter>
      <div className="App">
        <Header />
        <main className="main-content">
          <Routes>
            <Route path="/" element={<Home />} />
            <Route path="/bookings" element={<MyBookings />} />
            <Route path="/profile" element={<UserProfile />} />
            <Route path="/privacy" element={<PrivacyPolicy />} />
          </Routes>
        </main>
        <Footer />
      </div>
    </BrowserRouter>
  );
}

export default App;
