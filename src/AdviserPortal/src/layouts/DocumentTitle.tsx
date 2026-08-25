import { useEffect } from 'react'
import { useLocation, useMatches } from 'react-router-dom'
import { matchNavItem } from './matchNavItem'

export type RouteHandle = {
  title?: string
}

function titleFromHandle(handle: unknown): string | undefined {
  if (typeof handle !== 'object' || handle === null || !('title' in handle)) {
    return undefined
  }

  const title = (handle as RouteHandle).title
  return typeof title === 'string' && title.length > 0 ? title : undefined
}

export function DocumentTitle() {
  const location = useLocation()
  const matches = useMatches()
  const handleTitle = [...matches]
    .reverse()
    .map((match) => titleFromHandle(match.handle))
    .find((title) => title !== undefined)
  const title = handleTitle ?? matchNavItem(location.pathname)?.label

  useEffect(() => {
    document.title = title ? `${title} · MyWealth` : 'MyWealth Adviser Portal'
  }, [title])

  return null
}
