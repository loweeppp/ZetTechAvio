export const CITIES = [
  { code: 'MOW', name: 'Москва', airport: 'SVO/DME/VKO', query: 'Москва' },
  { code: 'LED', name: 'Санкт-Петербург', airport: 'LED', query: 'Санкт-Петербург' },
  { code: 'KZN', name: 'Казань', airport: 'KZN', query: 'Казань' },
  { code: 'AER', name: 'Сочи', airport: 'AER', query: 'Сочи' },
  { code: 'IST', name: 'Стамбул', airport: 'IST', query: 'Стамбул' },
  { code: 'DXB', name: 'Дубай', airport: 'DXB', query: 'Дубай' },
  { code: 'LON', name: 'Лондон', airport: 'LHR/LGW', query: 'Лондон' },
  { code: 'PAR', name: 'Париж', airport: 'CDG/ORY', query: 'Париж' },
  { code: 'BKK', name: 'Бангкок', airport: 'BKK', query: 'Бангкок' },
  { code: 'NYC', name: 'Нью-Йорк', airport: 'JFK/LGA/EWR', query: 'Нью-Йорк' },
];

export function resolveCity(value) {
  if (!value) return null;
  const normalized = String(value).trim();
  const byCode = CITIES.find((c) => c.code === normalized.toUpperCase());
  if (byCode) return byCode;
  const byName = CITIES.find(
    (c) =>
      c.name.toLowerCase() === normalized.toLowerCase() ||
      (c.query || '').toLowerCase() === normalized.toLowerCase(),
  );
  if (byName) return byName;
  return { code: normalized.toUpperCase(), name: normalized, airport: normalized, query: normalized };
}
