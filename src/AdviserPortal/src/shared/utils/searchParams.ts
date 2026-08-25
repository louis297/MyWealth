export function toSearchParams(
  values: Record<string, string | number | boolean | undefined | null>,
): string {
  const params = new URLSearchParams()

  for (const [key, value] of Object.entries(values)) {
    if (value === undefined || value === null || value === '') {
      continue
    }

    params.set(key, String(value))
  }

  const query = params.toString()
  return query ? `?${query}` : ''
}
