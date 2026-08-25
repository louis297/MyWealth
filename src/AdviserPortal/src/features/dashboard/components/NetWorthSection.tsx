import { formatMoney } from '../../../shared/utils/formatMoney'
import type { NetWorthItem } from '../types'

type NetWorthSectionProps = {
  items: readonly NetWorthItem[]
}

export function NetWorthSection({ items }: NetWorthSectionProps) {
  return (
    <section>
      <h2 className="mb-3 text-lg font-semibold">Net Worth</h2>
      {items.length === 0 ? (
        <p className="text-slate-600">No data yet</p>
      ) : (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          {items.map((item) => (
            <article
              key={item.currency}
              className="rounded border border-slate-200 bg-white p-4"
            >
              <h3 className="text-sm font-medium text-slate-500">{item.currency}</h3>
              <p className="mt-2 text-2xl font-semibold tabular-nums">
                {formatMoney(item.net, item.currency)}
              </p>
              <dl className="mt-4 grid grid-cols-2 gap-y-1 text-sm">
                <dt className="text-slate-500">Assets</dt>
                <dd className="text-right tabular-nums text-slate-800">
                  {formatMoney(item.assets, item.currency)}
                </dd>
                <dt className="text-slate-500">Liabilities</dt>
                <dd className="text-right tabular-nums text-slate-800">
                  {formatMoney(item.liabilities, item.currency)}
                </dd>
              </dl>
            </article>
          ))}
        </div>
      )}
    </section>
  )
}
