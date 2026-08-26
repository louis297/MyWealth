import { Link } from 'react-router-dom'
import { formatMoney } from '../../../shared/utils/formatMoney'
import type { Transaction } from '../types'

type AccountLookup = {
  name: string
}

type TransactionTableProps = {
  transactions: readonly Transaction[]
  accounts: ReadonlyMap<number, AccountLookup>
}

function formatBookedOn(value: string): string {
  const date = new Date(`${value}T00:00:00`)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  return date.toLocaleDateString()
}

export function TransactionTable({ transactions, accounts }: TransactionTableProps) {
  return (
    <div className="overflow-x-auto rounded border border-slate-200 bg-white">
      <table className="min-w-full text-left text-sm">
        <caption className="sr-only">Transactions</caption>
        <thead className="border-b border-slate-200 bg-slate-50 text-slate-600">
          <tr>
            <th scope="col" className="px-4 py-3 font-medium">
              Booked on
            </th>
            <th scope="col" className="px-4 py-3 font-medium">
              Type
            </th>
            <th scope="col" className="px-4 py-3 font-medium">
              Account
            </th>
            <th scope="col" className="px-4 py-3 font-medium">
              Amount
            </th>
            <th scope="col" className="px-4 py-3 font-medium">
              Quantity
            </th>
            <th scope="col" className="px-4 py-3 font-medium">
              Holding
            </th>
            <th scope="col" className="px-4 py-3 font-medium">
              Note
            </th>
          </tr>
        </thead>
        <tbody>
          {transactions.map((transaction) => {
            const account = accounts.get(transaction.accountId)
            return (
              <tr key={transaction.id} className="border-b border-slate-100 last:border-0">
                <td className="px-4 py-3 text-slate-700">{formatBookedOn(transaction.bookedOn)}</td>
                <td className="px-4 py-3 text-slate-700">{transaction.type}</td>
                <td className="px-4 py-3">
                  <Link
                    to={`/accounts/${transaction.accountId}`}
                    className="text-slate-800 underline hover:no-underline"
                  >
                    {account?.name ?? `Account ${transaction.accountId}`}
                  </Link>
                </td>
                <td className="px-4 py-3 text-slate-700">
                  {formatMoney(transaction.amount.amount, transaction.amount.currency)}
                </td>
                <td className="px-4 py-3 text-slate-700">
                  {transaction.quantity === null ? '—' : transaction.quantity.toLocaleString()}
                </td>
                <td className="px-4 py-3 text-slate-700">
                  {transaction.holdingId === null ? '—' : transaction.holdingId}
                </td>
                <td className="px-4 py-3 text-slate-700">{transaction.note ?? '—'}</td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
