import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Pagination } from '../../../shared/components/Pagination'
import { formatMoney } from '../../../shared/utils/formatMoney'
import { useGetTransactionsQuery } from '../../transactions/transactionsApi'
import type { Account } from '../types'

type AccountTransactionsSectionProps = {
  account: Account
}

const PAGE_SIZE = 20

function formatBookedOn(value: string): string {
  const date = new Date(`${value}T00:00:00`)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  return date.toLocaleDateString()
}

export function AccountTransactionsSection({ account }: AccountTransactionsSectionProps) {
  const [page, setPage] = useState(1)
  const canWrite = account.status === 'Active'
  const list = useGetTransactionsQuery({
    accountId: account.id,
    page,
    pageSize: PAGE_SIZE,
  })
  const items = list.data?.items ?? []

  return (
    <section className="mt-8">
      <div className="mb-3 flex items-center justify-between gap-3">
        <h2 className="text-lg font-semibold">Transactions</h2>
        {canWrite ? (
          <Link
            to={`/transactions/new?accountId=${account.id}`}
            className="text-sm text-slate-800 underline hover:no-underline"
          >
            New transaction
          </Link>
        ) : null}
      </div>

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
        <p className="text-slate-600">No transactions</p>
      ) : null}

      {list.isSuccess && items.length > 0 ? (
        <>
          <div className="overflow-x-auto rounded border border-slate-200 bg-white">
            <table className="min-w-full text-left text-sm">
              <caption className="sr-only">Related transactions</caption>
              <thead className="border-b border-slate-200 bg-slate-50 text-slate-600">
                <tr>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Booked on
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Type
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Amount
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Quantity
                  </th>
                  <th scope="col" className="px-4 py-3 font-medium">
                    Note
                  </th>
                </tr>
              </thead>
              <tbody>
                {items.map((transaction) => (
                  <tr key={transaction.id} className="border-b border-slate-100 last:border-0">
                    <td className="px-4 py-3 text-slate-700">{formatBookedOn(transaction.bookedOn)}</td>
                    <td className="px-4 py-3 text-slate-700">{transaction.type}</td>
                    <td className="px-4 py-3 text-slate-700">
                      {formatMoney(transaction.amount.amount, transaction.amount.currency)}
                    </td>
                    <td className="px-4 py-3 text-slate-700">
                      {transaction.quantity === null ? '—' : transaction.quantity.toLocaleString()}
                    </td>
                    <td className="px-4 py-3 text-slate-700">{transaction.note ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination
            pageNumber={list.data.pageNumber}
            totalPages={list.data.totalPages}
            hasPreviousPage={list.data.hasPreviousPage}
            hasNextPage={list.data.hasNextPage}
            onPageChange={setPage}
          />
        </>
      ) : null}
    </section>
  )
}
