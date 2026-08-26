import { Link, useParams } from 'react-router-dom'
import type { FetchBaseQueryError } from '@reduxjs/toolkit/query'
import { PageHeader } from '../../shared/components/PageHeader'
import { useGetAccountByIdQuery } from './accountsApi'
import { AccountEditSection } from './components/AccountEditSection'
import { AccountStatusBadge } from './components/AccountStatusBadge'
import { AccountTransactionsSection } from './components/AccountTransactionsSection'
import { HoldingsSection } from './components/HoldingsSection'

function isNotFoundError(error: unknown): boolean {
  return (
    typeof error === 'object' &&
    error !== null &&
    'status' in error &&
    (error as FetchBaseQueryError).status === 404
  )
}

function parseAccountId(value: string | undefined): number | null {
  if (value === undefined) {
    return null
  }

  const id = Number(value)
  return Number.isInteger(id) && id > 0 ? id : null
}

export function AccountDetailPage() {
  const { accountId: rawId } = useParams()
  const accountId = parseAccountId(rawId)
  const account = useGetAccountByIdQuery(accountId ?? 0, { skip: accountId === null })

  if (accountId === null || isNotFoundError(account.error)) {
    return (
      <div>
        <PageHeader title="Account not found" />
        <p className="text-slate-600">
          That account does not exist or is not visible to you.{' '}
          <Link to="/accounts" className="text-slate-800 underline hover:no-underline">
            Back to accounts
          </Link>
        </p>
      </div>
    )
  }

  if (account.isLoading) {
    return (
      <div>
        <PageHeader title="Account" />
        <p className="text-slate-600" aria-busy="true">
          Loading account…
        </p>
      </div>
    )
  }

  if (account.isError || !account.data) {
    return (
      <div>
        <PageHeader title="Account" />
        <p
          className="mb-3 rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800"
          role="alert"
        >
          Unable to load account.
        </p>
        <button
          type="button"
          className="rounded border border-slate-300 bg-white px-3 py-1.5 text-sm hover:bg-slate-50"
          onClick={() => {
            void account.refetch()
          }}
        >
          Retry
        </button>
      </div>
    )
  }

  const data = account.data

  return (
    <div>
      <PageHeader title={data.name}>
        <Link to="/accounts" className="text-sm text-slate-700 underline hover:no-underline">
          Back to accounts
        </Link>
      </PageHeader>

      {data.status === 'Closed' ? (
        <p
          className="mb-4 rounded border border-slate-200 bg-slate-50 px-3 py-2 text-sm text-slate-700"
          role="status"
        >
          This account is closed. New holdings and transactions are not allowed.
        </p>
      ) : null}

      <dl className="grid max-w-xl grid-cols-1 gap-4 rounded border border-slate-200 bg-white p-4 text-sm sm:grid-cols-2">
        <div>
          <dt className="text-slate-500">Customer</dt>
          <dd className="mt-1">
            <Link
              to={`/customers/${data.customerId}`}
              className="text-slate-800 underline hover:no-underline"
            >
              {data.customerName}
            </Link>
          </dd>
        </div>
        <div>
          <dt className="text-slate-500">Type</dt>
          <dd className="mt-1 text-slate-800">{data.type}</dd>
        </div>
        <div>
          <dt className="text-slate-500">Status</dt>
          <dd className="mt-1">
            <AccountStatusBadge status={data.status} />
          </dd>
        </div>
        <div>
          <dt className="text-slate-500">Currency</dt>
          <dd className="mt-1 text-slate-800">{data.currency}</dd>
        </div>
        <div>
          <dt className="text-slate-500">Id</dt>
          <dd className="mt-1 text-slate-800">{data.id}</dd>
        </div>
      </dl>

      <AccountEditSection account={data} />
      <HoldingsSection account={data} />
      <AccountTransactionsSection account={data} />
    </div>
  )
}
