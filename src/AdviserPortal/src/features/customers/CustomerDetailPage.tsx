import { Link, useParams } from 'react-router-dom'
import type { FetchBaseQueryError } from '@reduxjs/toolkit/query'
import { PageHeader } from '../../shared/components/PageHeader'
import { CustomerAccountsSection } from './components/CustomerAccountsSection'
import { useGetCustomerByIdQuery } from './customersApi'

function isNotFoundError(error: unknown): boolean {
  return (
    typeof error === 'object' &&
    error !== null &&
    'status' in error &&
    (error as FetchBaseQueryError).status === 404
  )
}

function parseCustomerId(value: string | undefined): number | null {
  if (value === undefined) {
    return null
  }

  const id = Number(value)
  return Number.isInteger(id) && id > 0 ? id : null
}

export function CustomerDetailPage() {
  const { customerId: rawId } = useParams()
  const customerId = parseCustomerId(rawId)
  const customer = useGetCustomerByIdQuery(customerId ?? 0, { skip: customerId === null })

  if (customerId === null || isNotFoundError(customer.error)) {
    return (
      <div>
        <PageHeader title="Customer not found" />
        <p className="text-slate-600">
          That customer does not exist or is not visible to you.{' '}
          <Link to="/customers" className="text-slate-800 underline hover:no-underline">
            Back to customers
          </Link>
        </p>
      </div>
    )
  }

  if (customer.isLoading) {
    return (
      <div>
        <PageHeader title="Customer" />
        <p className="text-slate-600" aria-busy="true">
          Loading customer…
        </p>
      </div>
    )
  }

  if (customer.isError || !customer.data) {
    return (
      <div>
        <PageHeader title="Customer" />
        <p
          className="mb-3 rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800"
          role="alert"
        >
          Unable to load customer.
        </p>
        <button
          type="button"
          className="rounded border border-slate-300 bg-white px-3 py-1.5 text-sm hover:bg-slate-50"
          onClick={() => {
            void customer.refetch()
          }}
        >
          Retry
        </button>
      </div>
    )
  }

  const data = customer.data

  return (
    <div>
      <PageHeader title={data.name}>
        <Link to="/customers" className="text-sm text-slate-700 underline hover:no-underline">
          Back to customers
        </Link>
      </PageHeader>

      <dl className="grid max-w-xl grid-cols-1 gap-4 rounded border border-slate-200 bg-white p-4 text-sm sm:grid-cols-2">
        <div>
          <dt className="text-slate-500">Email</dt>
          <dd className="mt-1 text-slate-800">{data.email}</dd>
        </div>
        <div>
          <dt className="text-slate-500">Status</dt>
          <dd className="mt-1">
            <span
              className={
                data.isEnabled
                  ? 'inline-flex rounded bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-800'
                  : 'inline-flex rounded bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600'
              }
            >
              {data.isEnabled ? 'Enabled' : 'Disabled'}
            </span>
          </dd>
        </div>
        <div>
          <dt className="text-slate-500">Adviser</dt>
          <dd className="mt-1 text-slate-800">{data.adviserName}</dd>
        </div>
        <div>
          <dt className="text-slate-500">Id</dt>
          <dd className="mt-1 text-slate-800">{data.id}</dd>
        </div>
      </dl>

      <CustomerAccountsSection customerId={data.id} />
    </div>
  )
}
