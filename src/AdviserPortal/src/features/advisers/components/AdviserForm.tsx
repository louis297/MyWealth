import { useState, type FormEvent } from 'react'

export type AdviserFormValues = {
  name: string
  email: string
  password: string
  confirmPassword: string
}

type AdviserFormProps = {
  mode: 'create' | 'edit'
  initial: AdviserFormValues
  isSubmitting: boolean
  error: string | null
  submitLabel: string
  onSubmit: (values: AdviserFormValues) => void
}

export function AdviserForm({
  mode,
  initial,
  isSubmitting,
  error,
  submitLabel,
  onSubmit,
}: AdviserFormProps) {
  const [name, setName] = useState(initial.name)
  const [email, setEmail] = useState(initial.email)
  const [password, setPassword] = useState(initial.password)
  const [confirmPassword, setConfirmPassword] = useState(initial.confirmPassword)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    onSubmit({
      name: name.trim(),
      email: email.trim(),
      password,
      confirmPassword,
    })
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="flex max-w-xl flex-col gap-4 rounded border border-slate-200 bg-white p-4"
    >
      {error ? (
        <p className="rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800" role="alert">
          {error}
        </p>
      ) : null}

      <label className="flex flex-col gap-1 text-sm" htmlFor="adviser-name">
        Name
        <input
          id="adviser-name"
          name="name"
          required
          maxLength={200}
          value={name}
          onChange={(event) => setName(event.target.value)}
          className="rounded border border-slate-300 px-3 py-2"
        />
      </label>

      {mode === 'create' ? (
        <>
          <label className="flex flex-col gap-1 text-sm" htmlFor="adviser-email">
            Email
            <input
              id="adviser-email"
              name="email"
              type="email"
              required
              maxLength={256}
              autoComplete="off"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              className="rounded border border-slate-300 px-3 py-2"
            />
          </label>

          <label className="flex flex-col gap-1 text-sm" htmlFor="adviser-password">
            Password
            <input
              id="adviser-password"
              name="password"
              type="password"
              required
              autoComplete="new-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              className="rounded border border-slate-300 px-3 py-2"
            />
          </label>

          <label className="flex flex-col gap-1 text-sm" htmlFor="adviser-confirm-password">
            Confirm password
            <input
              id="adviser-confirm-password"
              name="confirmPassword"
              type="password"
              required
              autoComplete="new-password"
              value={confirmPassword}
              onChange={(event) => setConfirmPassword(event.target.value)}
              className="rounded border border-slate-300 px-3 py-2"
            />
          </label>
        </>
      ) : null}

      <button
        type="submit"
        disabled={isSubmitting}
        className="rounded bg-slate-800 px-3 py-2 text-sm text-white hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isSubmitting ? 'Saving…' : submitLabel}
      </button>
    </form>
  )
}
