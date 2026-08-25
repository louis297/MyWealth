import { useEffect } from 'react'
import { Navigate, NavLink, Outlet, useNavigate } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '../app/hooks'
import { useGetCurrentUserQuery, useLogoutMutation } from '../features/auth/authApi'
import {
  logout,
  selectAuthStatus,
  selectToken,
  setCurrentUser,
} from '../features/auth/authSlice'
import { useCurrentUser } from '../features/auth/useCurrentUser'
import { baseApi } from '../shared/api/baseApi'
import { NAV_ITEMS } from './navItems'

export function MainLayout() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const token = useAppSelector(selectToken)
  const status = useAppSelector(selectAuthStatus)
  const currentUser = useCurrentUser()
  const [logoutRequest] = useLogoutMutation()
  const { data, isError, isFetching } = useGetCurrentUserQuery(undefined, {
    skip: !token,
  })

  useEffect(() => {
    if (data) {
      dispatch(setCurrentUser(data))
    }
  }, [data, dispatch])

  if (!token) {
    return <Navigate to="/login" replace />
  }

  async function handleLogout() {
    try {
      await logoutRequest().unwrap()
    } catch {
      // Client session still ends even if the API call fails.
    } finally {
      dispatch(logout())
      dispatch(baseApi.util.resetApiState())
      navigate('/login', { replace: true })
    }
  }

  if (status === 'loading' || (isFetching && currentUser === null)) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-slate-100 text-sm text-slate-600">
        Loading session…
      </div>
    )
  }

  if (isError && currentUser === null) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-3 bg-slate-100 p-6 text-sm text-slate-700">
        <p>Unable to load your session.</p>
        <button
          type="button"
          className="rounded border border-slate-300 bg-white px-3 py-1.5 hover:bg-slate-50"
          onClick={() => {
            void handleLogout()
          }}
        >
          Sign in again
        </button>
      </div>
    )
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
          <div className="text-right text-sm">
            <div className="font-medium text-slate-800">{currentUser?.displayName ?? ''}</div>
            <div className="text-slate-500">{currentUser?.role ?? ''}</div>
          </div>
          <button
            type="button"
            className="rounded border border-slate-300 px-3 py-1.5 text-sm hover:bg-slate-50"
            onClick={() => {
              void handleLogout()
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
