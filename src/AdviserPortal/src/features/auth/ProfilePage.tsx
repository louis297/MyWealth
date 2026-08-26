import { useState, type FormEvent } from 'react'
import { PageHeader } from '../../shared/components/PageHeader'
import { apiErrorMessage } from '../../shared/utils/apiError'
import { useChangePasswordMutation, useUpdateCurrentUserMutation } from './authApi'
import type { CurrentUser } from './types'
import { useCurrentUser } from './useCurrentUser'

function StatusBadge({ enabled }: { enabled: boolean }) {
  return (
    <span
      className={
        enabled
          ? 'inline-flex rounded bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-800'
          : 'inline-flex rounded bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600'
      }
    >
      {enabled ? 'Enabled' : 'Disabled'}
    </span>
  )
}

function DisplayNameForm({ currentUser }: { currentUser: CurrentUser }) {
  const [displayName, setDisplayName] = useState(currentUser.displayName)
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)
  const [updateCurrentUser, updateState] = useUpdateCurrentUserMutation()

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setSaved(false)

    const next = displayName.trim()
    if (next === currentUser.displayName) {
      return
    }

    try {
      await updateCurrentUser({ displayName: next }).unwrap()
      setSaved(true)
    } catch (caught) {
      setError(apiErrorMessage(caught, 'Unable to update display name.'))
    }
  }

  return (
    <section className="mt-8">
      <h2 className="mb-3 text-lg font-semibold">Display name</h2>
      <form
        onSubmit={(event) => {
          void handleSubmit(event)
        }}
        className="flex max-w-xl flex-col gap-4 rounded border border-slate-200 bg-white p-4"
      >
        {error ? (
          <p className="rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800" role="alert">
            {error}
          </p>
        ) : null}
        {saved && !error ? (
          <p className="rounded border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-800">
            Display name saved.
          </p>
        ) : null}

        <label className="flex flex-col gap-1 text-sm" htmlFor="profile-display-name">
          Display name
          <input
            id="profile-display-name"
            name="displayName"
            required
            maxLength={200}
            value={displayName}
            onChange={(event) => {
              setDisplayName(event.target.value)
              setSaved(false)
            }}
            className="rounded border border-slate-300 px-3 py-2"
          />
        </label>

        <button
          type="submit"
          disabled={updateState.isLoading}
          className="rounded bg-slate-800 px-3 py-2 text-sm text-white hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {updateState.isLoading ? 'Saving…' : 'Save display name'}
        </button>
      </form>
    </section>
  )
}

function ChangePasswordForm({ email }: { email: string }) {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)
  const [changePassword, changeState] = useChangePasswordMutation()

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setSaved(false)

    if (newPassword !== confirmPassword) {
      setError('New password and confirmation do not match.')
      return
    }

    try {
      await changePassword({ currentPassword, newPassword }).unwrap()
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      setSaved(true)
    } catch (caught) {
      setError(apiErrorMessage(caught, 'Unable to change password.'))
    }
  }

  return (
    <section className="mt-8">
      <h2 className="mb-3 text-lg font-semibold">Change password</h2>
      <form
        onSubmit={(event) => {
          void handleSubmit(event)
        }}
        className="flex max-w-xl flex-col gap-4 rounded border border-slate-200 bg-white p-4"
      >
        {error ? (
          <p className="rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800" role="alert">
            {error}
          </p>
        ) : null}
        {saved && !error ? (
          <p className="rounded border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-800">
            Password changed.
          </p>
        ) : null}

        <input
          type="text"
          name="username"
          autoComplete="username"
          value={email}
          readOnly
          tabIndex={-1}
          aria-hidden="true"
          className="sr-only"
        />

        <label className="flex flex-col gap-1 text-sm" htmlFor="profile-current-password">
          Current password
          <input
            id="profile-current-password"
            name="currentPassword"
            type="password"
            autoComplete="current-password"
            required
            value={currentPassword}
            onChange={(event) => {
              setCurrentPassword(event.target.value)
              setSaved(false)
            }}
            className="rounded border border-slate-300 px-3 py-2"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm" htmlFor="profile-new-password">
          New password
          <input
            id="profile-new-password"
            name="newPassword"
            type="password"
            autoComplete="new-password"
            required
            value={newPassword}
            onChange={(event) => {
              setNewPassword(event.target.value)
              setSaved(false)
            }}
            className="rounded border border-slate-300 px-3 py-2"
          />
        </label>

        <label className="flex flex-col gap-1 text-sm" htmlFor="profile-confirm-password">
          Confirm new password
          <input
            id="profile-confirm-password"
            name="confirmPassword"
            type="password"
            autoComplete="new-password"
            required
            value={confirmPassword}
            onChange={(event) => {
              setConfirmPassword(event.target.value)
              setSaved(false)
            }}
            className="rounded border border-slate-300 px-3 py-2"
          />
        </label>

        <button
          type="submit"
          disabled={changeState.isLoading}
          className="rounded bg-slate-800 px-3 py-2 text-sm text-white hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {changeState.isLoading ? 'Saving…' : 'Change password'}
        </button>
      </form>
    </section>
  )
}

export function ProfilePage() {
  const currentUser = useCurrentUser()

  if (currentUser === null) {
    return (
      <div>
        <PageHeader title="Profile" />
        <p className="text-slate-600">Unable to load your profile.</p>
      </div>
    )
  }

  return (
    <div>
      <PageHeader title="Profile" />

      <dl className="grid max-w-xl grid-cols-1 gap-4 rounded border border-slate-200 bg-white p-4 text-sm sm:grid-cols-2">
        <div>
          <dt className="text-slate-500">Email</dt>
          <dd className="mt-1 text-slate-800">{currentUser.email}</dd>
        </div>
        <div>
          <dt className="text-slate-500">Role</dt>
          <dd className="mt-1 text-slate-800">{currentUser.role}</dd>
        </div>
        <div>
          <dt className="text-slate-500">Tenant</dt>
          <dd className="mt-1 text-slate-800">{currentUser.tenantId ?? '—'}</dd>
        </div>
        <div>
          <dt className="text-slate-500">Status</dt>
          <dd className="mt-1">
            <StatusBadge enabled={currentUser.isEnabled} />
          </dd>
        </div>
      </dl>

      <DisplayNameForm currentUser={currentUser} />
      <ChangePasswordForm email={currentUser.email} />
    </div>
  )
}
