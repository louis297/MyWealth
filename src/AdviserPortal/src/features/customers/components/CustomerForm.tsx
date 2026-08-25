import { useState, type FormEvent } from 'react'
import type { AdviserLookup } from '../types'

export type CustomerFormValues = {
  name: string
  email: string
  adviserId: string
}

type CustomerFormProps = {
  mode: 'create' | 'edit'
  initial: CustomerFormValues
  advisers: readonly AdviserLookup[]
  isTenantAdmin: boolean
  assignedAdviserName?: string
  isSubmitting: boolean
  error: string | null
  submitLabel: string
  onSubmit: (values: CustomerFormValues) => void
}

export function CustomerForm({
  mode,
  initial,
  advisers,
  isTenantAdmin,
  assignedAdviserName,
  isSubmitting,
  error,
  submitLabel,
  onSubmit,
}: CustomerFormProps) {
  const [name, setName] = useState(initial.name)
  const [email, setEmail] = useState(initial.email)
  const [adviserId, setAdviserId] = useState(initial.adviserId)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    onSubmit({
      name: name.trim(),
      email: email.trim(),
      adviserId,
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

      <label className="flex flex-col gap-1 text-sm" htmlFor="customer-name">
        Name
        <input
          id="customer-name"
          name="name"
          required
          maxLength={200}
          value={name}
          onChange={(event) => setName(event.target.value)}
          className="rounded border border-slate-300 px-3 py-2"
        />
      </label>

      {mode === 'create' ? (
        <label className="flex flex-col gap-1 text-sm" htmlFor="customer-email">
          Email
          <input
            id="customer-email"
            name="email"
            type="email"
            required
            maxLength={256}
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            className="rounded border border-slate-300 px-3 py-2"
          />
        </label>
      ) : null}

      {isTenantAdmin ? (
        <label className="flex flex-col gap-1 text-sm" htmlFor="customer-adviser">
          Adviser
          <select
            id="customer-adviser"
            name="adviserId"
            required
            value={adviserId}
            onChange={(event) => setAdviserId(event.target.value)}
            className="rounded border border-slate-300 bg-white px-3 py-2"
          >
            <option value="">Select an adviser</option>
            {advisers.map((adviser) => (
              <option key={adviser.id} value={String(adviser.id)}>
                {adviser.name}
                {adviser.isEnabled ? '' : ' (disabled)'}
              </option>
            ))}
          </select>
        </label>
      ) : (
        <p className="text-sm text-slate-600">
          Assigned adviser: {assignedAdviserName ?? 'you'}
        </p>
      )}

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
