import { useEffect, useState } from 'react'
import { Navigate, Outlet, useLocation, useNavigate } from 'react-router-dom'
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
import { Sidebar } from './Sidebar'
import { TopBar } from './TopBar'
import { NAV_ITEMS } from './navItems'

const SIDEBAR_ID = 'app-sidebar'

export function MainLayout() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const location = useLocation()
  const token = useAppSelector(selectToken)
  const status = useAppSelector(selectAuthStatus)
  const currentUser = useCurrentUser()
  const [logoutRequest] = useLogoutMutation()
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const { data, isError, isFetching } = useGetCurrentUserQuery(undefined, {
    skip: !token,
  })

  useEffect(() => {
    if (data) {
      dispatch(setCurrentUser(data))
    }
  }, [data, dispatch])

  useEffect(() => {
    setSidebarOpen(false)
  }, [location.pathname])

  useEffect(() => {
    if (!sidebarOpen) {
      return
    }

    function isDesktop() {
      return window.matchMedia('(min-width: 768px)').matches
    }

    function onKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setSidebarOpen(false)
      }
    }

    function onResize() {
      if (isDesktop()) {
        setSidebarOpen(false)
      }
    }

    const previousOverflow = document.body.style.overflow
    if (!isDesktop()) {
      document.body.style.overflow = 'hidden'
    }

    window.addEventListener('keydown', onKeyDown)
    window.addEventListener('resize', onResize)

    return () => {
      document.body.style.overflow = previousOverflow
      window.removeEventListener('keydown', onKeyDown)
      window.removeEventListener('resize', onResize)
    }
  }, [sidebarOpen])

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
      <a
        href="#main-content"
        className="sr-only focus:not-sr-only focus:absolute focus:left-4 focus:top-4 focus:z-50 focus:rounded focus:bg-white focus:px-3 focus:py-2 focus:text-sm focus:shadow"
      >
        Skip to content
      </a>
      <Sidebar
        id={SIDEBAR_ID}
        items={visibleItems}
        open={sidebarOpen}
        onClose={() => setSidebarOpen(false)}
      />
      <div className="flex min-w-0 flex-1 flex-col">
        <TopBar
          currentUser={currentUser}
          sidebarId={SIDEBAR_ID}
          sidebarOpen={sidebarOpen}
          onToggleSidebar={() => setSidebarOpen((open) => !open)}
          onLogout={() => {
            void handleLogout()
          }}
        />
        <main id="main-content" className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
