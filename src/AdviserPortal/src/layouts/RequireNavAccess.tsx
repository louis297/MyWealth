import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useCurrentUser } from '../features/auth/useCurrentUser'
import { matchNavItem } from './matchNavItem'

export function RequireNavAccess() {
  const currentUser = useCurrentUser()
  const location = useLocation()
  const item = matchNavItem(location.pathname)

  if (item && currentUser !== null && !item.roles.includes(currentUser.role)) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}
