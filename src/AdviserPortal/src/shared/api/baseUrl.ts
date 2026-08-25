const FALLBACK_API_BASE_URL = 'http://localhost:5174'

export function getApiBaseUrl(): string {
  const fromEnv = import.meta.env.VITE_API_BASE_URL
  if (typeof fromEnv === 'string' && fromEnv.trim() !== '') {
    return fromEnv.replace(/\/$/, '')
  }

  return FALLBACK_API_BASE_URL
}
