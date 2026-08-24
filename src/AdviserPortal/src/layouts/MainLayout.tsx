import { Navigate, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '../app/hooks'
import { logout, selectToken } from '../features/auth/authSlice'
import { useCurrentUser } from '../features/auth/useCurrentUser'
import { NAV_ITEMS } from './navItems'

export function MainLayout() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const token = useAppSelector(selectToken)
  const currentUser = useCurrentUser()

  if (!token) {
    return <Navigate to="/login" replace />
  }

  const visibleItems = NAV_ITEMS.filter(
    (item) => currentUser !== null && item.roles.includes(currentUser.role),
  )

  return (
    <div className="flex min-h-screen bg-slate-100 text-slate-900">
      <aside className="flex w-56 flex-col bg-slate-800 text-slate-100">
        <div className="border-b border-slate-700 px-4 py-4 text-lg font-semibold">
          MyWealth
        </div>
        <nav className="flex flex-1 flex-col gap-1 p-2">
          {visibleItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) =>
                [
                  'rounded px-3 py-2 text-sm',
                  isActive ? 'bg-slate-600 font-medium text-white' : 'text-slate-200 hover:bg-slate-700',
                ].join(' ')
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="flex items-center justify-end gap-4 border-b border-slate-200 bg-white px-6 py-3">
          <span className="text-sm text-slate-600">{currentUser?.displayName ?? ''}</span>
          <button
            type="button"
            className="rounded border border-slate-300 px-3 py-1.5 text-sm hover:bg-slate-50"
            onClick={() => {
              dispatch(logout())
              navigate('/login')
            }}
          >
            Log out
          </button>
        </header>
        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
