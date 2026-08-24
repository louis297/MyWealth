import { useState, type FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '../../app/hooks'
import { selectIsAuthenticated, setCredentials } from './authSlice'
import type { UserRole } from './types'

const ROLES: readonly UserRole[] = ['SystemAdmin', 'TenantAdmin', 'Adviser']

export function LoginPage() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const isAuthenticated = useAppSelector(selectIsAuthenticated)
  const [displayName, setDisplayName] = useState('Demo User')
  const [role, setRole] = useState<UserRole>('TenantAdmin')

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    dispatch(
      setCredentials({
        token: 'dev-token',
        currentUser: {
          userId: 'dev-user',
          email: 'dev@local',
          displayName,
          role,
          tenantId: role === 'SystemAdmin' ? null : 1,
          isEnabled: true,
        },
      }),
    )
    navigate('/')
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-100 p-6">
      <form
        onSubmit={handleSubmit}
        className="flex w-full max-w-sm flex-col gap-4 rounded border border-slate-200 bg-white p-6"
      >
        <h1 className="text-xl font-semibold">Sign in</h1>
        <p className="text-sm text-slate-600">Stub login for layout development. Not the real Login page.</p>

        <label className="flex flex-col gap-1 text-sm">
          Display name
          <input
            type="text"
            name="displayName"
            value={displayName}
            onChange={(event) => setDisplayName(event.target.value)}
            className="rounded border border-slate-300 px-3 py-2"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm">
          Role
          <select
            name="role"
            value={role}
            onChange={(event) => setRole(event.target.value as UserRole)}
            className="rounded border border-slate-300 px-3 py-2"
          >
            {ROLES.map((value) => (
              <option key={value} value={value}>
                {value}
              </option>
            ))}
          </select>
        </label>

        <button type="submit" className="rounded bg-slate-800 px-3 py-2 text-sm text-white hover:bg-slate-700">
          Continue
        </button>
      </form>
    </div>
  )
}
