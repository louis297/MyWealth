import { Link } from 'react-router-dom'
import { useGetAccountsQuery } from '../../accounts/accountsApi'

type CustomerAccountsSectionProps = {
  customerId: number
}

export function CustomerAccountsSection({ customerId }: CustomerAccountsSectionProps) {
  const accounts = useGetAccountsQuery({ customerId, pageSize: 100 })
  const items = accounts.data?.items ?? []

  return (
    <section className="mt-8">
      <div className="mb-3 flex items-center justify-between gap-3">
        <h2 className="text-lg font-semibold">Accounts</h2>
        <Link
          to={`/accounts/new?customerId=${customerId}`}
          className="text-sm text-slate-800 underline hover:no-underline"
        >
          New account
        </Link>
      </div>

      {accounts.isLoading ? (
        <p className="text-slate-600" aria-busy="true">
          Loading accounts…
        </p>
      ) : null}

      {accounts.isError ? (
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
              void accounts.refetch()
            }}
          >
            Retry
          </button>
        </div>
      ) : null}

      {accounts.isSuccess && items.length === 0 ? (
        <p className="text-slate-600">No accounts yet</p>
      ) : null}

      {accounts.isSuccess && items.length > 0 ? (
        <div className="overflow-x-auto rounded border border-slate-200 bg-white">
          <table className="min-w-full text-left text-sm">
            <caption className="sr-only">Related accounts</caption>
            <thead className="border-b border-slate-200 bg-slate-50 text-slate-600">
              <tr>
                <th scope="col" className="px-4 py-3 font-medium">
                  Name
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
              {items.map((account) => (
                <tr key={account.id} className="border-b border-slate-100 last:border-0">
                  <td className="px-4 py-3">
                    <Link
                      to={`/accounts/${account.id}`}
                      className="font-medium text-slate-800 underline hover:no-underline"
                    >
                      {account.name}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-slate-700">{account.type}</td>
                  <td className="px-4 py-3 text-slate-700">{account.status}</td>
                  <td className="px-4 py-3 text-slate-700">{account.currency}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}
    </section>
  )
}
