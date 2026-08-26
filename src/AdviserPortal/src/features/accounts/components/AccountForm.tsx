import { useState, type FormEvent } from 'react'
import { ACCOUNT_TYPES, type AccountType } from '../types'

export type AccountFormValues = {
  customerId: string
  name: string
  type: AccountType
  currency: string
}

type CustomerOption = {
  id: number
  name: string
}

type AccountFormProps = {
  mode: 'create' | 'edit'
  initial: AccountFormValues
  customers: readonly CustomerOption[]
  isSubmitting: boolean
  error: string | null
  submitLabel: string
  onSubmit: (values: AccountFormValues) => void
}

export function AccountForm({
  mode,
  initial,
  customers,
  isSubmitting,
  error,
  submitLabel,
  onSubmit,
}: AccountFormProps) {
  const [customerId, setCustomerId] = useState(initial.customerId)
  const [name, setName] = useState(initial.name)
  const [type, setType] = useState<AccountType>(initial.type)
  const [currency, setCurrency] = useState(initial.currency)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    onSubmit({
      customerId,
      name: name.trim(),
      type,
      currency: currency.trim().toUpperCase(),
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

      {mode === 'create' ? (
        <label className="flex flex-col gap-1 text-sm" htmlFor="account-customer">
          Customer
          <select
            id="account-customer"
            name="customerId"
            required
            value={customerId}
            onChange={(event) => setCustomerId(event.target.value)}
            className="rounded border border-slate-300 bg-white px-3 py-2"
          >
            <option value="">Select a customer</option>
            {customers.map((customer) => (
              <option key={customer.id} value={String(customer.id)}>
                {customer.name}
              </option>
            ))}
          </select>
        </label>
      ) : null}

      <label className="flex flex-col gap-1 text-sm" htmlFor="account-name">
        Name
        <input
          id="account-name"
          name="name"
          required
          maxLength={200}
          value={name}
          onChange={(event) => setName(event.target.value)}
          className="rounded border border-slate-300 px-3 py-2"
        />
      </label>

      <label className="flex flex-col gap-1 text-sm" htmlFor="account-type">
        Type
        <select
          id="account-type"
          name="type"
          required
          value={type}
          onChange={(event) => setType(event.target.value as AccountType)}
          className="rounded border border-slate-300 bg-white px-3 py-2"
        >
          {ACCOUNT_TYPES.map((accountType) => (
            <option key={accountType} value={accountType}>
              {accountType}
            </option>
          ))}
        </select>
      </label>

      {mode === 'create' ? (
        <label className="flex flex-col gap-1 text-sm" htmlFor="account-currency">
          Currency
          <input
            id="account-currency"
            name="currency"
            required
            maxLength={3}
            value={currency}
            onChange={(event) => setCurrency(event.target.value.toUpperCase())}
            className="rounded border border-slate-300 px-3 py-2 uppercase"
            autoComplete="off"
            spellCheck={false}
          />
        </label>
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
