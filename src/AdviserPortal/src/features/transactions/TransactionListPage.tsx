import { useState } from 'react'
import { Link } from 'react-router-dom'
import { PageHeader } from '../../shared/components/PageHeader'
import { Pagination } from '../../shared/components/Pagination'
import { useGetAccountsQuery } from '../accounts/accountsApi'
import { TransactionFilters, type TypeFilter } from './components/TransactionFilters'
import { TransactionTable } from './components/TransactionTable'
import { useGetTransactionsQuery } from './transactionsApi'
import type { TransactionType } from './types'

const PAGE_SIZE = 20

function typeArg(type: TypeFilter): TransactionType | undefined {
  return type === 'all' ? undefined : type
}

function accountIdArg(accountId: string): number | undefined {
  if (accountId === '') {
    return undefined
  }

  const id = Number(accountId)
  return Number.isInteger(id) && id > 0 ? id : undefined
}

function dateArg(value: string): string | undefined {
  return value === '' ? undefined : value
}

export function TransactionListPage() {
  const [page, setPage] = useState(1)
  const [accountId, setAccountId] = useState('')
  const [from, setFrom] = useState('')
  const [to, setTo] = useState('')
  const [type, setType] = useState<TypeFilter>('all')
  const list = useGetTransactionsQuery({
    page,
    pageSize: PAGE_SIZE,
    accountId: accountIdArg(accountId),
    from: dateArg(from),
    to: dateArg(to),
    type: typeArg(type),
  })
  const accounts = useGetAccountsQuery({ pageSize: 100 })
  const items = list.data?.items ?? []
  const hasActiveFilters = accountId !== '' || from !== '' || to !== '' || type !== 'all'
  const accountLookup = new Map(
    (accounts.data?.items ?? []).map((account) => [account.id, { name: account.name }]),
  )

  return (
    <div>
      <PageHeader title="Transactions">
        <Link
          to="/transactions/new"
          className="rounded bg-slate-800 px-3 py-2 text-sm text-white hover:bg-slate-700"
        >
          New transaction
        </Link>
      </PageHeader>

      <TransactionFilters
        accountId={accountId}
        from={from}
        to={to}
        type={type}
        accounts={accounts.data?.items ?? []}
        accountsLoading={accounts.isLoading}
        accountsError={accounts.isError}
        onRetryAccounts={() => {
          void accounts.refetch()
        }}
        onApply={(next) => {
          setAccountId(next.accountId)
          setFrom(next.from)
          setTo(next.to)
          setType(next.type)
          setPage(1)
        }}
      />

      {list.isLoading ? (
        <p className="text-slate-600" aria-busy="true">
          Loading transactions…
        </p>
      ) : null}

      {list.isError ? (
        <div>
          <p
            className="mb-3 rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800"
            role="alert"
          >
            Unable to load transactions.
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
          <p>{hasActiveFilters ? 'No transactions match these filters.' : 'No transactions'}</p>
          {!hasActiveFilters ? (
            <Link
              to="/transactions/new"
              className="mt-3 inline-block text-sm text-slate-800 underline hover:no-underline"
            >
              Create a transaction
            </Link>
          ) : null}
        </div>
      ) : null}

      {list.isSuccess && items.length > 0 ? (
        <>
          <TransactionTable transactions={items} accounts={accountLookup} />
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
