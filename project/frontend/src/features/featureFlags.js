const truthy = new Set(['1', 'true', 'yes', 'on']);

export function isHomeV2Enabled() {
  const raw = process.env.REACT_APP_HOME_V2;
  if (!raw) return false;
  return truthy.has(String(raw).trim().toLowerCase());
}

