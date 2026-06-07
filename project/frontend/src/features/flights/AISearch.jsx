import React from 'react';
import { Sparkles, ArrowRight, Loader2, RotateCcw, Mic, MicOff } from 'lucide-react';

import { resolveCity } from './cities';

function audioBufferToWav(buffer) {
  const numChannels = 1;
  const sampleRate = buffer.sampleRate;
  const samples = buffer.getChannelData(0);
  const pcm = new Int16Array(samples.length);

  for (let i = 0; i < samples.length; i++) {
    const s = Math.max(-1, Math.min(1, samples[i]));
    pcm[i] = s < 0 ? s * 0x8000 : s * 0x7fff;
  }

  const dataLength = pcm.length * 2;
  const wavBuffer = new ArrayBuffer(44 + dataLength);
  const view = new DataView(wavBuffer);

  const write = (offset, str) => {
    for (let i = 0; i < str.length; i++) {
      view.setUint8(offset + i, str.charCodeAt(i));
    }
  };

  write(0, 'RIFF');
  view.setUint32(4, 36 + dataLength, true);
  write(8, 'WAVE');
  write(12, 'fmt ');
  view.setUint32(16, 16, true);
  view.setUint16(20, 1, true);
  view.setUint16(22, numChannels, true);
  view.setUint32(24, sampleRate, true);
  view.setUint32(28, sampleRate * 2, true);
  view.setUint16(32, 2, true);
  view.setUint16(34, 16, true);
  write(36, 'data');
  view.setUint32(40, dataLength, true);

  const wavPcm = new Int16Array(wavBuffer, 44);
  wavPcm.set(pcm);

  return new Blob([wavBuffer], { type: 'audio/wav' });
}

const API_URL = process.env.REACT_APP_API_URL || 'https://api.zettechavio.ru';

const EXAMPLES = [
  'Хочу слетать из Москвы в Стамбул на майские праздники',
  'Ищу рейс в Дубай на 2 человека в конце апреля',
  'Романтическая поездка в Париж на выходные',
  'Из Петербурга в Бангкок на двоих, начало мая',
];

const prepareSearchValue = (field) => {
  if (!field) return '';
  if (typeof field === 'object') return field.query || field.name || field.code || '';
  return String(field);
};

const isRussianText = (text) => /[а-яА-ЯЁё]/.test(String(text || ''));

const buildFallbackReasoning = ({ from, to, date, dateFrom, dateTo, passengers, explicitPassengers }) => {
  const parts = [];
  if (from) parts.push(`от ${from.name}`);
  if (to) parts.push(`в ${to.name}`);
  if (date) parts.push(`дата ${formatDate(date)}`);
  else if (dateFrom && dateTo) parts.push(`с ${formatDate(dateFrom)} по ${formatDate(dateTo)}`);
  if (explicitPassengers || passengers > 1) {
    parts.push(`${passengers} пассажир${passengers === 1 ? '' : passengers < 5 ? 'а' : 'ов'}`);
  }

  if (parts.length > 0) {
    return `Распознан запрос: ${parts.join(', ')}.`;
  }

  return 'Распознан запрос. Уточните маршрут, дату или количество пассажиров.';
};

const formatDate = (value) => {
  if (!value) return '';
  const dateOnly = String(value).split('T')[0];
  return dateOnly.split('-').reverse().join('.');
};

const extractDateOnly = (value) => {
  if (!value) return '';
  const dateOnly = String(value).split('T')[0];
  return dateOnly;
};

export default function AISearch({ onSearch }) {
  const [query, setQuery] = React.useState('');
  const [loading, setLoading] = React.useState(false);
  const [result, setResult] = React.useState(null);
  const [error, setError] = React.useState('');
  const [exampleIdx, setExampleIdx] = React.useState(0);
  const [listening, setListening] = React.useState(false);
  const mediaRecorderRef = React.useRef(null);
  const chunksRef = React.useRef([]);
  const audioContextRef = React.useRef(null);
  const analyserRef = React.useRef(null);
  const vadIntervalRef = React.useRef(null);
  const silenceStartRef = React.useRef(null);
  const voiceDetectedRef = React.useRef(false);
  const stopTimeoutRef = React.useRef(null);
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
    setError('');
    setResult(null);

    try {
      const response = await fetch(`${API_URL}/api/ai/parse`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ text: query.trim() }),
      });

      if (!response.ok) {
        const payload = await response.json().catch(() => null);
        throw new Error(payload?.message || 'Ошибка анализа запроса');
      }

      const payload = await response.json();
      const hasSearchValue = payload?.from || payload?.to || payload?.date || payload?.dateFrom || payload?.dateTo;
      if (!hasSearchValue) {
        throw new Error('ИИ не смог понять параметры поиска. Попробуйте переформулировать запрос.');
      }

      const explicitPassengers = payload?.passengers != null;
      const parsed = {
        from: payload?.from ? resolveCity(payload.from) : null,
        to: payload?.to ? resolveCity(payload.to) : null,
        date: payload?.date || '',
        dateFrom: payload?.dateFrom || '',
        dateTo: payload?.dateTo || '',
        passengers: payload?.passengers || 1,
        minPrice: payload?.minPrice != null ? Number(payload.minPrice) : undefined,
        maxPrice: payload?.maxPrice != null ? Number(payload.maxPrice) : undefined,
        showPassengers: explicitPassengers || (payload?.passengers || 1) > 1,
        reasoning: isRussianText(payload?.reasoning)
          ? payload.reasoning
          : buildFallbackReasoning({
              from: payload?.from ? resolveCity(payload.from) : null,
              to: payload?.to ? resolveCity(payload.to) : null,
              date: payload?.date || '',
              dateFrom: payload?.dateFrom || '',
              dateTo: payload?.dateTo || '',
              passengers: payload?.passengers || 1,
              explicitPassengers,
            }),
        isValid: true, // Пометить что результат от API
      };

      if (parsed.from && parsed.to) {
        try {
          const routeParams = new URLSearchParams();
          routeParams.append('from', prepareSearchValue(parsed.from));
          routeParams.append('to', prepareSearchValue(parsed.to));
          const routeResponse = await fetch(`${API_URL}/api/flights/search?${routeParams.toString()}`);
          if (routeResponse.ok) {
            const routeFlights = await routeResponse.json() || [];
            const availableDates = Array.from(
              new Set(
                routeFlights
                  .map((flight) => flight.departureDt?.slice(0, 10))
                  .filter(Boolean),
              ),
            ).sort();

            const hasDateRange = !!(parsed.dateFrom && parsed.dateTo);
            const parsedDate = extractDateOnly(parsed.date);
            if (!hasDateRange && parsedDate && availableDates.length > 0 && !availableDates.includes(parsedDate)) {
              const dateList = availableDates.slice(0, 5).map(formatDate).join(', ');
              parsed.reasoning = `На эту дату нет рейсов, но есть на эти даты: ${dateList}`;
            } else if (!parsed.date && !hasDateRange && availableDates.length > 0) {
              const dateList = availableDates.slice(0, 5).map(formatDate).join(', ');
              parsed.reasoning = `Есть рейсы на маршруте ${parsed.from.name} → ${parsed.to.name}. Доступные даты: ${dateList}`;
            } else if (routeFlights.length === 0) {
              parsed.reasoning = `По маршруту ${parsed.from.name} → ${parsed.to.name} рейсов не найдено.`;
            }
          }
        } catch (fetchError) {
          console.warn('Route check failed', fetchError);
        }
      }

      setResult(parsed);
    } catch (err) {
      console.error('AI parse error:', err);
      setError(err instanceof Error ? err.message : 'Не удалось распознать запрос');
      setResult(null); // Не показывать результат при ошибке
    } finally {
      setLoading(false);
    }
  };

  const handleVoice = async () => {
    if (listening) {
      mediaRecorderRef.current?.stop();
      return;
    }

    try {
      const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
      const recorder = new MediaRecorder(stream);
      chunksRef.current = [];
      mediaRecorderRef.current = recorder;

      recorder.ondataavailable = (e) => {
        if (e.data.size > 0) chunksRef.current.push(e.data);
      };

      const cleanupVad = () => {
        if (vadIntervalRef.current) {
          window.clearInterval(vadIntervalRef.current);
          vadIntervalRef.current = null;
        }

        if (audioContextRef.current) {
          audioContextRef.current.close().catch(() => null);
          audioContextRef.current = null;
        }

        analyserRef.current = null;
        silenceStartRef.current = null;
        voiceDetectedRef.current = false;
      };

      const stopRecording = () => {
        if (mediaRecorderRef.current?.state === 'recording') {
          mediaRecorderRef.current.stop();
        }

        if (stopTimeoutRef.current) {
          window.clearTimeout(stopTimeoutRef.current);
          stopTimeoutRef.current = null;
        }

        cleanupVad();
      };

      const startVad = () => {
        try {
          const audioCtx = new AudioContext();
          const source = audioCtx.createMediaStreamSource(stream);
          const analyser = audioCtx.createAnalyser();
          analyser.fftSize = 512;
          source.connect(analyser);

          audioContextRef.current = audioCtx;
          analyserRef.current = analyser;

          const dataArray = new Float32Array(analyser.fftSize);
          const threshold = 0.013;
          const silenceTimeoutMs = 5000;
          const vadIntervalMs = 150;

          vadIntervalRef.current = window.setInterval(() => {
            analyser.getFloatTimeDomainData(dataArray);
            let sum = 0;
            for (let i = 0; i < dataArray.length; i += 1) {
              sum += dataArray[i] * dataArray[i];
            }
            const rms = Math.sqrt(sum / dataArray.length);

            if (rms > threshold) {
              voiceDetectedRef.current = true;
              silenceStartRef.current = null;
            } else if (voiceDetectedRef.current) {
              if (silenceStartRef.current === null) {
                silenceStartRef.current = Date.now();
              } else if (Date.now() - silenceStartRef.current >= silenceTimeoutMs) {
                stopRecording();
              }
            }
          }, vadIntervalMs);
        } catch (vadError) {
          console.warn('VAD start failed', vadError);
        }
      };

      recorder.onstop = async () => {
        cleanupVad();
        stream.getTracks().forEach((t) => t.stop());
        setListening(false);
        setLoading(true);
        setError('');

        try {
          const blob = new Blob(chunksRef.current, { type: 'audio/webm' });
          const arrayBuffer = await blob.arrayBuffer();
          const audioCtx = new AudioContext({ sampleRate: 16000 });
          const audioBuffer = await audioCtx.decodeAudioData(arrayBuffer);
          await audioCtx.close();

          const wavBlob = audioBufferToWav(audioBuffer);
          const form = new FormData();
          form.append('audio', wavBlob, 'voice.wav');

          const res = await fetch(`${API_URL}/api/ai/transcribe`, {
            method: 'POST',
            body: form,
          });

          if (!res.ok) throw new Error('Ошибка транскрипции');
          const data = await res.json();

          if (!data.text) throw new Error('Не удалось распознать речь');
          setQuery(data.text);
        } catch (err) {
          setError(err instanceof Error ? err.message : 'Ошибка транскрипции');
        } finally {
          setLoading(false);
        }
      };

      recorder.start();
      setListening(true);
      startVad();

      stopTimeoutRef.current = window.setTimeout(() => {
        stopRecording();
      }, 30000);
    } catch (err) {
      setError('Нет доступа к микрофону');
    }
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
              <button
                type="button"
                onClick={handleVoice}
                className={`homev2__aiReset ${listening ? 'homev2__aiReset--listening' : ''}`}
                title={listening ? 'Остановить запись' : 'Голосовой ввод'}
              >
                {listening ? <MicOff className="homev2__aiResetIcon" /> : <Mic className="homev2__aiResetIcon" />}
              </button>

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

          {error && <div className="homev2__aiError">{error}</div>}
          {result && (
            <div className="homev2__aiResult">
              <div>
                {result.reasoning && (
                <div className="homev2__aiReason">
                  <Sparkles className="homev2__aiReasonIcon" />
                  <span>{result.reasoning}</span>
                </div>
              )}
              <div className="homev2__aiChips">
                {result.from && <Chip>{result.from.name}</Chip>}
                {result.from && result.to && <span className="homev2__aiArrow">→</span>}
                {result.to && <Chip>{result.to.name}</Chip>}
                {result.date && <Chip>{formatDate(result.date)}</Chip>}
                {!result.date && result.dateFrom && result.dateTo && (
                  <Chip>{`${formatDate(result.dateFrom)} — ${formatDate(result.dateTo)}`}</Chip>
                )}
                {result.minPrice != null && result.maxPrice != null && (
                  <Chip>{`от ${result.minPrice} до ${result.maxPrice} ₽`}</Chip>
                )}
                {result.showPassengers && <Chip>{result.passengers} пасс.</Chip>}
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
                    dateFrom: result.dateFrom,
                    dateTo: result.dateTo,
                    passengers: result.passengers,
                    minPrice: result.minPrice,
                    maxPrice: result.maxPrice,
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

