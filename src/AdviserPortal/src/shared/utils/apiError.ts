import type { FetchBaseQueryError } from '@reduxjs/toolkit/query'

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null
}

function messagesFromErrors(errors: unknown): string[] {
  if (!isRecord(errors)) {
    return []
  }

  return Object.values(errors)
    .flatMap((value) => (Array.isArray(value) ? value : [value]))
    .filter((value): value is string => typeof value === 'string' && value.trim() !== '')
}

export function apiErrorMessage(error: unknown, fallback: string): string {
  if (typeof error !== 'object' || error === null || !('data' in error)) {
    return fallback
  }

  const data = (error as FetchBaseQueryError).data
  if (!isRecord(data)) {
    return fallback
  }

  const fromErrors = messagesFromErrors(data.errors)
  if (fromErrors.length > 0) {
    return fromErrors.join(' ')
  }

  if (typeof data.detail === 'string' && data.detail.trim() !== '') {
    return data.detail
  }

  if (typeof data.title === 'string' && data.title.trim() !== '') {
    return data.title
  }

  return fallback
}
