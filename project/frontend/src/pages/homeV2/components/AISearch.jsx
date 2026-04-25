import React from 'react';
import { Sparkles, ArrowRight, Loader2, RotateCcw } from 'lucide-react';

const CITIES = [
  { code: 'MOW', name: 'Moscow', airport: 'SVO/DME/VKO' },
  { code: 'LED', name: 'Saint Petersburg', airport: 'LED' },
  { code: 'KZN', name: 'Kazan', airport: 'KZN' },
  { code: 'AER', name: 'Sochi', airport: 'AER' },
  { code: 'IST', name: 'Istanbul', airport: 'IST' },
  { code: 'DXB', name: 'Dubai', airport: 'DXB' },
  { code: 'LON', name: 'London', airport: 'LHR/LGW' },
  { code: 'PAR', name: 'Paris', airport: 'CDG/ORY' },
  { code: 'BKK', name: 'Bangkok', airport: 'BKK' },
  { code: 'NYC', name: 'New York', airport: 'JFK/LGA/EWR' },
];

const EXAMPLES = [
  'Хочу слетать из Москвы в Стамбул на майские праздники',
  'Ищу рейс в Дубай на 2 человека в конце апреля',
  'Романтическая поездка в Париж на выходные',
  'Из Петербурга в Бангкок на двоих, начало мая',
];

function parseLLMResponse(text) {
  const lower = String(text ?? '').toLowerCase();

  let from = CITIES.find((c) => c.code === 'MOW');
  if (lower.includes('петербург') || lower.includes('спб') || lower.includes('led')) {
    from = CITIES.find((c) => c.code === 'LED');
  } else if (lower.includes('казань')) {
    from = CITIES.find((c) => c.code === 'KZN');
  } else if (lower.includes('сочи')) {
    from = CITIES.find((c) => c.code === 'AER');
  }

  let to = CITIES.find((c) => c.code === 'IST');
  if (lower.includes('дубай') || lower.includes('dubai') || lower.includes('дxб')) {
    to = CITIES.find((c) => c.code === 'DXB');
  } else if (lower.includes('париж') || lower.includes('paris')) {
    to = CITIES.find((c) => c.code === 'PAR');
  } else if (lower.includes('бангкок') || lower.includes('таиланд') || lower.includes('тайланд')) {
    to = CITIES.find((c) => c.code === 'BKK');
  } else if (lower.includes('лондон') || lower.includes('london')) {
    to = CITIES.find((c) => c.code === 'LON');
  } else if (lower.includes('нью-йорк') || lower.includes('нью йорк') || lower.includes('new york')) {
    to = CITIES.find((c) => c.code === 'NYC');
  } else if (lower.includes('стамбул') || lower.includes('istanbul')) {
    to = CITIES.find((c) => c.code === 'IST');
  }

  let passengers = 1;
  const passMatch = lower.match(/(\d+)\s*(чел|пасс|человек|пассажир)/);
  if (passMatch) passengers = Math.min(9, parseInt(passMatch[1], 10));
  else if (lower.includes('двоих') || lower.includes('двух') || lower.includes('вдвоём') || lower.includes('на двоих')) passengers = 2;
  else if (lower.includes('троих') || lower.includes('трёх') || lower.includes('на троих')) passengers = 3;

  let date = '2026-05-01';
  if (lower.includes('апрел')) date = '2026-04-28';
  else if (lower.includes('июн')) date = '2026-06-01';
  else if (lower.includes('июл')) date = '2026-07-01';
  else if (lower.includes('август')) date = '2026-08-01';

  const reasoning = `Определил маршрут ${from.name} → ${to.name}, ${passengers} пасс., дата — ${date
    .split('-')
    .reverse()
    .join('.')}`;

  return { from, to, date, passengers, reasoning };
}

export default function AISearch({ onSearch }) {
  const [query, setQuery] = React.useState('');
  const [loading, setLoading] = React.useState(false);
  const [result, setResult] = React.useState(null);
  const [exampleIdx, setExampleIdx] = React.useState(0);
  const textareaRef = React.useRef(null);

  React.useEffect(() => {
    const id = setInterval(
      () => setExampleIdx((i) => (i + 1) % EXAMPLES.length),
      3500,
    );
    return () => clearInterval(id);
  }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!query.trim()) return;
    setLoading(true);
    setResult(null);
    await new Promise((r) => setTimeout(r, 1400));
    const parsed = parseLLMResponse(query);
    setResult(parsed);
    setLoading(false);
  };

  const handleReset = () => {
    setQuery('');
    setResult(null);
    textareaRef.current?.focus?.();
  };

  return (
    <div className="homev2__ai">
      <div className="homev2__aiDivider">
        <div className="homev2__aiDividerLine" />
        <div className="homev2__aiDividerPill">
          <Sparkles className="homev2__aiDividerIcon" />
          <span>или спросите AI</span>
        </div>
        <div className="homev2__aiDividerLine" />
      </div>

      <form className="homev2__aiCard" onSubmit={handleSubmit}>
        <div className="homev2__aiTopBar" />

        <div className="homev2__aiBody">
          <div className="homev2__aiRow">
            <div className="homev2__aiMark">
              <Sparkles className="homev2__aiMarkIcon" />
            </div>

            <div className="homev2__aiInput">
              <textarea
                ref={textareaRef}
                rows={1}
                value={query}
                onChange={(e) => {
                  setQuery(e.target.value);
                  e.target.style.height = 'auto';
                  e.target.style.height = `${e.target.scrollHeight}px`;
                }}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    handleSubmit(e);
                  }
                }}
                placeholder={EXAMPLES[exampleIdx]}
                className="homev2__aiTextarea"
                style={{ minHeight: 28 }}
                disabled={loading}
              />

              {!query && !result && (
                <p className="homev2__aiHint">
                  Опишите поездку своими словами — AI подберёт параметры поиска
                </p>
              )}
            </div>

            <div className="homev2__aiActions">
              {result && (
                <button
                  type="button"
                  onClick={handleReset}
                  className="homev2__aiReset"
                  title="Новый запрос"
                >
                  <RotateCcw className="homev2__aiResetIcon" />
                </button>
              )}

              <button
                type="submit"
                disabled={loading || !query.trim()}
                className="homev2__aiSubmit"
              >
                {loading ? (
                  <Loader2 className="homev2__aiSubmitSpin" />
                ) : (
                  <>
                    <span>Найти</span>
                    <ArrowRight className="homev2__aiSubmitArrow" />
                  </>
                )}
              </button>
            </div>
          </div>

          {result && (
            <div className="homev2__aiResult">
              <div>
                <div className="homev2__aiReason">
                  <Sparkles className="homev2__aiReasonIcon" />
                  <span>{result.reasoning}</span>
                </div>
                <div className="homev2__aiChips">
                  <Chip>{result.from.name}</Chip>
                  <span className="homev2__aiArrow">→</span>
                  <Chip>{result.to.name}</Chip>
                  <Chip>{String(result.date).split('-').reverse().join('.')}</Chip>
                  <Chip>{result.passengers} пасс.</Chip>
                </div>
              </div>

              <button
                type="button"
                className="homev2__aiShow"
                onClick={() =>
                  onSearch?.({
                    from: result.from,
                    to: result.to,
                    date: result.date,
                    passengers: result.passengers,
                  })
                }
              >
                Показать рейсы
              </button>
            </div>
          )}

          {loading && (
            <div className="homev2__aiLoading">
              <div className="homev2__aiLoadingRow">
                <Loader2 className="homev2__aiLoadingSpin" />
                <span>AI анализирует запрос...</span>
              </div>
              <div className="homev2__aiSkeletonRow">
                {[80, 60, 48, 40].map((w, i) => (
                  <div
                    key={i}
                    className="homev2__aiSkeleton"
                    style={{ width: w }}
                  />
                ))}
              </div>
            </div>
          )}
        </div>
      </form>
    </div>
  );
}

function Chip({ children }) {
  return <span className="homev2__aiChip">{children}</span>;
}

