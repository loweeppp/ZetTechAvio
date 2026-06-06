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
  const normalizedUpper = normalized.toUpperCase();
  const normalizedLower = normalized.toLowerCase();

  const byCode = CITIES.find((c) => c.code === normalizedUpper);
  if (byCode) return byCode;

  const byAirport = CITIES.find((c) =>
    (c.airport || '')
      .split('/')
      .map((code) => code.trim().toUpperCase())
      .includes(normalizedUpper),
  );
  if (byAirport) return byAirport;

  const byName = CITIES.find(
    (c) =>
      c.name.toLowerCase() === normalizedLower ||
      (c.query || '').toLowerCase() === normalizedLower,
  );
  if (byName) return byName;

  return { code: normalizedUpper, name: normalized, airport: normalized, query: normalized };
}
