import { useState } from 'react'
import { Link } from 'react-router-dom'
import { PageHeader } from '../../shared/components/PageHeader'
import { Pagination } from '../../shared/components/Pagination'
import { useGetCustomersQuery } from '../customers/customersApi'
import { useGetAccountsQuery } from './accountsApi'
import { AccountFilters, type StatusFilter } from './components/AccountFilters'
import { AccountTable } from './components/AccountTable'
import type { AccountStatus } from './types'

const PAGE_SIZE = 20

function statusArg(status: StatusFilter): AccountStatus | undefined {
  return status === 'all' ? undefined : status
}

function customerIdArg(customerId: string): number | undefined {
  if (customerId === '') {
    return undefined
  }

  const id = Number(customerId)
  return Number.isInteger(id) && id > 0 ? id : undefined
}

export function AccountListPage() {
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [status, setStatus] = useState<StatusFilter>('all')
  const [customerId, setCustomerId] = useState('')
  const list = useGetAccountsQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    status: statusArg(status),
    customerId: customerIdArg(customerId),
  })
  const customers = useGetCustomersQuery({ isEnabled: true, pageSize: 100 })
  const items = list.data?.items ?? []
  const hasActiveFilters = search !== '' || status !== 'all' || customerId !== ''

  return (
    <div>
      <PageHeader title="Accounts">
        <Link
          to="/accounts/new"
          className="rounded bg-slate-800 px-3 py-2 text-sm text-white hover:bg-slate-700"
        >
          New account
        </Link>
      </PageHeader>

      <AccountFilters
        search={search}
        status={status}
        customerId={customerId}
        customers={customers.data?.items ?? []}
        customersLoading={customers.isLoading}
        customersError={customers.isError}
        onRetryCustomers={() => {
          void customers.refetch()
        }}
        onApply={(next) => {
          setSearch(next.search)
          setStatus(next.status)
          setCustomerId(next.customerId)
          setPage(1)
        }}
      />

      {list.isLoading ? (
        <p className="text-slate-600" aria-busy="true">
          Loading accounts…
        </p>
      ) : null}

      {list.isError ? (
        <div>
          <p
            className="mb-3 rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800"
            role="alert"
          >
            Unable to load accounts.
          </p>
          <button
            type="button"
            className="rounded border border-slate-300 bg-white px-3 py-1.5 text-sm hover:bg-slate-50"
            onClick={() => {
              void list.refetch()
            }}
          >
            Retry
          </button>
        </div>
      ) : null}

      {list.isSuccess && items.length === 0 ? (
        <div className="rounded border border-slate-200 bg-white p-6 text-slate-600">
          <p>{hasActiveFilters ? 'No accounts match these filters.' : 'No accounts yet'}</p>
          {!hasActiveFilters ? (
            <Link
              to="/accounts/new"
              className="mt-3 inline-block text-sm text-slate-800 underline hover:no-underline"
            >
              Create an account
            </Link>
          ) : null}
        </div>
      ) : null}

      {list.isSuccess && items.length > 0 ? (
        <>
          <AccountTable accounts={items} />
          <Pagination
            pageNumber={list.data.pageNumber}
            totalPages={list.data.totalPages}
            hasPreviousPage={list.data.hasPreviousPage}
            hasNextPage={list.data.hasNextPage}
            onPageChange={setPage}
          />
        </>
      ) : null}
    </div>
  )
}
