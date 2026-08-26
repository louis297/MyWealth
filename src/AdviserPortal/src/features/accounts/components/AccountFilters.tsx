import { useState, type FormEvent } from 'react'
import type { AccountStatus } from '../types'

export type StatusFilter = 'all' | AccountStatus

export type AccountFilterValues = {
  search: string
  status: StatusFilter
  customerId: string
}

type CustomerOption = {
  id: number
  name: string
}

type AccountFiltersProps = {
  search: string
  status: StatusFilter
  customerId: string
  customers: readonly CustomerOption[]
  customersLoading: boolean
  customersError: boolean
  onRetryCustomers: () => void
  onApply: (next: AccountFilterValues) => void
}

export function AccountFilters({
  search,
  status,
  customerId,
  customers,
  customersLoading,
  customersError,
  onRetryCustomers,
  onApply,
}: AccountFiltersProps) {
  const [draftSearch, setDraftSearch] = useState(search)
  const [draftStatus, setDraftStatus] = useState(status)
  const [draftCustomerId, setDraftCustomerId] = useState(customerId)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    onApply({
      search: draftSearch.trim(),
      status: draftStatus,
      customerId: draftCustomerId,
    })
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="mb-4 flex flex-col gap-3 rounded border border-slate-200 bg-white p-4 md:flex-row md:items-end"
    >
      <label className="flex min-w-0 flex-1 flex-col gap-1 text-sm" htmlFor="account-search">
        Search
        <input
          id="account-search"
          type="search"
          name="search"
          value={draftSearch}
          placeholder="Name or id"
          onChange={(event) => setDraftSearch(event.target.value)}
          className="rounded border border-slate-300 px-3 py-2"
        />
      </label>
      <label className="flex flex-col gap-1 text-sm" htmlFor="account-status">
        Status
        <select
          id="account-status"
          name="status"
          value={draftStatus}
          onChange={(event) => setDraftStatus(event.target.value as StatusFilter)}
          className="rounded border border-slate-300 bg-white px-3 py-2"
        >
          <option value="all">All</option>
          <option value="Active">Active</option>
          <option value="Closed">Closed</option>
        </select>
      </label>
      <label className="flex min-w-40 flex-col gap-1 text-sm" htmlFor="account-customer">
        Customer
        <select
          id="account-customer"
          name="customerId"
          value={draftCustomerId}
          disabled={customersLoading || customersError}
          onChange={(event) => setDraftCustomerId(event.target.value)}
          className="rounded border border-slate-300 bg-white px-3 py-2 disabled:cursor-not-allowed disabled:opacity-60"
        >
          <option value="">All customers</option>
          {customers.map((customer) => (
            <option key={customer.id} value={String(customer.id)}>
              {customer.name}
            </option>
          ))}
        </select>
      </label>
      {customersError ? (
        <button
          type="button"
          className="rounded border border-slate-300 bg-white px-3 py-2 text-sm hover:bg-slate-50"
          onClick={onRetryCustomers}
        >
          Retry customers
        </button>
      ) : null}
      <button
        type="submit"
        className="rounded border border-slate-300 bg-white px-3 py-2 text-sm hover:bg-slate-50"
      >
        Apply
      </button>
    </form>
  )
}
