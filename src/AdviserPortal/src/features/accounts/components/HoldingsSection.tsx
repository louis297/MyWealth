import { useGetHoldingsByAccountQuery } from '../accountsApi'
import { HoldingsTable } from './HoldingsTable'

type HoldingsSectionProps = {
  accountId: number
}

export function HoldingsSection({ accountId }: HoldingsSectionProps) {
  const holdings = useGetHoldingsByAccountQuery(accountId)
  const items = holdings.data ?? []

  return (
    <section className="mt-8">
      <h2 className="mb-3 text-lg font-semibold">Holdings</h2>

      {holdings.isLoading ? (
        <p className="text-slate-600" aria-busy="true">
          Loading holdings…
        </p>
      ) : null}

      {holdings.isError ? (
        <div>
          <p
            className="mb-3 rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800"
            role="alert"
          >
            Unable to load holdings.
          </p>
          <button
            type="button"
            className="rounded border border-slate-300 bg-white px-3 py-1.5 text-sm hover:bg-slate-50"
            onClick={() => {
              void holdings.refetch()
            }}
          >
            Retry
          </button>
        </div>
      ) : null}

      {holdings.isSuccess && items.length === 0 ? (
        <p className="text-slate-600">No holdings yet</p>
      ) : null}

      {holdings.isSuccess && items.length > 0 ? <HoldingsTable holdings={items} /> : null}
    </section>
  )
}
