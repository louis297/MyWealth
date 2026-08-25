const FALLBACK_API_BASE_URL = 'https://localhost:7038'

export function getApiBaseUrl(): string {
  const fromEnv = import.meta.env.VITE_API_BASE_URL
  if (typeof fromEnv === 'string' && fromEnv.trim() !== '') {
    return fromEnv.replace(/\/$/, '')
  }

  return FALLBACK_API_BASE_URL
}
