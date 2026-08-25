import { Link } from 'react-router-dom'
import type { Customer } from '../types'

type CustomerTableProps = {
  customers: readonly Customer[]
}

function StatusBadge({ enabled }: { enabled: boolean }) {
  return (
    <span
      className={
        enabled
          ? 'inline-flex rounded bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-800'
          : 'inline-flex rounded bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600'
      }
    >
      {enabled ? 'Enabled' : 'Disabled'}
    </span>
  )
}

export function CustomerTable({ customers }: CustomerTableProps) {
  return (
    <div className="overflow-x-auto rounded border border-slate-200 bg-white">
      <table className="min-w-full text-left text-sm">
        <caption className="sr-only">Customers</caption>
        <thead className="border-b border-slate-200 bg-slate-50 text-slate-600">
          <tr>
            <th scope="col" className="px-4 py-3 font-medium">
              Name
            </th>
            <th scope="col" className="px-4 py-3 font-medium">
              Email
            </th>
            <th scope="col" className="px-4 py-3 font-medium">
              Adviser
            </th>
            <th scope="col" className="px-4 py-3 font-medium">
              Status
            </th>
          </tr>
        </thead>
        <tbody>
          {customers.map((customer) => (
            <tr key={customer.id} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-3">
                <Link
                  to={`/customers/${customer.id}`}
                  className="font-medium text-slate-800 underline hover:no-underline"
                >
                  {customer.name}
                </Link>
              </td>
              <td className="px-4 py-3 text-slate-700">{customer.email}</td>
              <td className="px-4 py-3 text-slate-700">{customer.adviserName}</td>
              <td className="px-4 py-3">
                <StatusBadge enabled={customer.isEnabled} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
