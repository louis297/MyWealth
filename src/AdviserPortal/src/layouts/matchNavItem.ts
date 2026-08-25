import { NAV_ITEMS, type NavItem } from './navItems'

export function matchNavItem(pathname: string): NavItem | undefined {
  if (pathname === '/') {
    return NAV_ITEMS.find((item) => item.to === '/')
  }

  let best: NavItem | undefined
  for (const item of NAV_ITEMS) {
    if (item.to === '/') {
      continue
    }

    if (pathname === item.to || pathname.startsWith(`${item.to}/`)) {
      if (!best || item.to.length > best.to.length) {
        best = item
      }
    }
  }

  return best
}
