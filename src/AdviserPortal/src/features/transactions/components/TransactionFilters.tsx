import { useState, type FormEvent } from 'react'
import { TRANSACTION_TYPES, type TransactionType } from '../types'

export type TypeFilter = 'all' | TransactionType

export type TransactionFilterValues = {
  accountId: string
  from: string
  to: string
  type: TypeFilter
}

type AccountOption = {
  id: number
  name: string
}

type TransactionFiltersProps = {
  accountId: string
  from: string
  to: string
  type: TypeFilter
  accounts: readonly AccountOption[]
  accountsLoading: boolean
  accountsError: boolean
  onRetryAccounts: () => void
  onApply: (next: TransactionFilterValues) => void
}

export function TransactionFilters({
  accountId,
  from,
  to,
  type,
  accounts,
  accountsLoading,
  accountsError,
  onRetryAccounts,
  onApply,
}: TransactionFiltersProps) {
  const [draftAccountId, setDraftAccountId] = useState(accountId)
  const [draftFrom, setDraftFrom] = useState(from)
  const [draftTo, setDraftTo] = useState(to)
  const [draftType, setDraftType] = useState(type)
  const [error, setError] = useState<string | null>(null)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (draftFrom !== '' && draftTo !== '' && draftFrom > draftTo) {
      setError('From date must be on or before to date.')
      return
    }

    setError(null)
    onApply({
      accountId: draftAccountId,
      from: draftFrom,
      to: draftTo,
      type: draftType,
    })
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="mb-4 flex flex-col gap-3 rounded border border-slate-200 bg-white p-4 md:flex-row md:flex-wrap md:items-end"
    >
      {error ? (
        <p className="w-full rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800" role="alert">
          {error}
        </p>
      ) : null}
      <label className="flex min-w-40 flex-col gap-1 text-sm" htmlFor="transaction-account">
        Account
        <select
          id="transaction-account"
          name="accountId"
          value={draftAccountId}
          disabled={accountsLoading || accountsError}
          onChange={(event) => setDraftAccountId(event.target.value)}
          className="rounded border border-slate-300 bg-white px-3 py-2 disabled:cursor-not-allowed disabled:opacity-60"
        >
          <option value="">All accounts</option>
          {accounts.map((account) => (
            <option key={account.id} value={String(account.id)}>
              {account.name}
            </option>
          ))}
        </select>
      </label>
      {accountsError ? (
        <button
          type="button"
          className="rounded border border-slate-300 bg-white px-3 py-2 text-sm hover:bg-slate-50"
          onClick={onRetryAccounts}
        >
          Retry accounts
        </button>
      ) : null}
      <label className="flex flex-col gap-1 text-sm" htmlFor="transaction-from">
        From
        <input
          id="transaction-from"
          type="date"
          name="from"
          value={draftFrom}
          onChange={(event) => setDraftFrom(event.target.value)}
          className="rounded border border-slate-300 px-3 py-2"
        />
      </label>
      <label className="flex flex-col gap-1 text-sm" htmlFor="transaction-to">
        To
        <input
          id="transaction-to"
          type="date"
          name="to"
          value={draftTo}
          onChange={(event) => setDraftTo(event.target.value)}
          className="rounded border border-slate-300 px-3 py-2"
        />
      </label>
      <label className="flex min-w-40 flex-col gap-1 text-sm" htmlFor="transaction-type">
        Type
        <select
          id="transaction-type"
          name="type"
          value={draftType}
          onChange={(event) => setDraftType(event.target.value as TypeFilter)}
          className="rounded border border-slate-300 bg-white px-3 py-2"
        >
          <option value="all">All</option>
          {TRANSACTION_TYPES.map((transactionType) => (
            <option key={transactionType} value={transactionType}>
              {transactionType}
            </option>
          ))}
        </select>
      </label>
      <button
        type="submit"
        className="rounded border border-slate-300 bg-white px-3 py-2 text-sm hover:bg-slate-50"
      >
        Apply
      </button>
    </form>
  )
}
