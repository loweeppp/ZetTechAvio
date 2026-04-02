
import './App.css';
import './pages/home/Home.css';
import Home from './pages/home/Home';

const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru' || 'http://localhost:3000';

function App() {
  return (
      <Home />
  );
}

export default App;
