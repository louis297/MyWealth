import { useState, type FormEvent } from 'react'
import { Navigate, useNavigate } from 'react-router-dom'
import type { FetchBaseQueryError } from '@reduxjs/toolkit/query'
import { useAppDispatch, useAppSelector } from '../../app/hooks'
import { useLazyGetCurrentUserQuery, useLoginMutation } from './authApi'
import { logout, selectIsAuthenticated, setAuthToken, setCurrentUser } from './authSlice'

const CREDENTIALS_ERROR = 'Invalid credentials or no access.'
const GENERIC_ERROR = 'Unable to sign in. Try again.'

function isFetchBaseQueryError(error: unknown): error is FetchBaseQueryError {
  return typeof error === 'object' && error !== null && 'status' in error
}

function loginErrorMessage(error: unknown): string {
  if (isFetchBaseQueryError(error) && (error.status === 401 || error.status === 403)) {
    return CREDENTIALS_ERROR
  }

  return GENERIC_ERROR
}

export function LoginPage() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const isAuthenticated = useAppSelector(selectIsAuthenticated)
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [login, { isLoading: isLoggingIn }] = useLoginMutation()
  const [getCurrentUser, { isLoading: isLoadingUser }] = useLazyGetCurrentUserQuery()

  if (isAuthenticated) {
    return <Navigate to="/" replace />
  }

  const isSubmitting = isLoggingIn || isLoadingUser

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)

    try {
      const result = await login({
        email: email.trim(),
        password,
      }).unwrap()

      dispatch(setAuthToken(result.accessToken))

      const currentUser = await getCurrentUser().unwrap()
      dispatch(setCurrentUser(currentUser))
      navigate('/', { replace: true })
    } catch (caught) {
      dispatch(logout())
      setError(loginErrorMessage(caught))
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-100 p-6">
      <form
        onSubmit={handleSubmit}
        className="flex w-full max-w-sm flex-col gap-4 rounded border border-slate-200 bg-white p-6"
      >
        <h1 className="text-xl font-semibold">Sign in</h1>
        <p className="text-sm text-slate-600">Sign in to the MyWealth Adviser Portal.</p>

        {error ? (
          <p className="rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800" role="alert">
            {error}
          </p>
        ) : null}

        <label className="flex flex-col gap-1 text-sm" htmlFor="email">
          Email
          <input
            id="email"
            type="email"
            name="email"
            autoComplete="email"
            required
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            className="rounded border border-slate-300 px-3 py-2"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm" htmlFor="password">
          Password
          <input
            id="password"
            type="password"
            name="password"
            autoComplete="current-password"
            required
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            className="rounded border border-slate-300 px-3 py-2"
          />
        </label>

        <button
          type="submit"
          disabled={isSubmitting}
          className="rounded bg-slate-800 px-3 py-2 text-sm text-white hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {isSubmitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
    </div>
  )
}
