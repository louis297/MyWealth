import { Link } from 'react-router-dom'
import type { Account } from '../types'
import { AccountStatusBadge } from './AccountStatusBadge'

type AccountTableProps = {
  accounts: readonly Account[]
}

export function AccountTable({ accounts }: AccountTableProps) {
  return (
    <div className="overflow-x-auto rounded border border-slate-200 bg-white">
      <table className="min-w-full text-left text-sm">
        <caption className="sr-only">Accounts</caption>
        <thead className="border-b border-slate-200 bg-slate-50 text-slate-600">
          <tr>
            <th scope="col" className="px-4 py-3 font-medium">
              Name
            </th>
            <th scope="col" className="px-4 py-3 font-medium">
              Customer
            </th>
            <th scope="col" className="px-4 py-3 font-medium">
              Type
            </th>
            <th scope="col" className="px-4 py-3 font-medium">
              Status
            </th>
            <th scope="col" className="px-4 py-3 font-medium">
              Currency
            </th>
          </tr>
        </thead>
        <tbody>
          {accounts.map((account) => (
            <tr key={account.id} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-3">
                <Link
                  to={`/accounts/${account.id}`}
                  className="font-medium text-slate-800 underline hover:no-underline"
                >
                  {account.name}
                </Link>
              </td>
              <td className="px-4 py-3">
                <Link
                  to={`/customers/${account.customerId}`}
                  className="text-slate-700 underline hover:no-underline"
                >
                  {account.customerName}
                </Link>
              </td>
              <td className="px-4 py-3 text-slate-700">{account.type}</td>
              <td className="px-4 py-3">
                <AccountStatusBadge status={account.status} />
              </td>
              <td className="px-4 py-3 text-slate-700">{account.currency}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
