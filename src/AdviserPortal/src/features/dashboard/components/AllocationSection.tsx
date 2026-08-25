import { formatMoney } from '../../../shared/utils/formatMoney'
import type { AllocationItem } from '../types'

type AllocationSectionProps = {
  items: readonly AllocationItem[]
}

type CurrencyGroup = {
  currency: string
  items: AllocationItem[]
  total: number
}

function groupByCurrency(items: readonly AllocationItem[]): CurrencyGroup[] {
  const groups = new Map<string, CurrencyGroup>()

  for (const item of items) {
    const existing = groups.get(item.currency)
    if (existing) {
      existing.items.push(item)
      existing.total += item.value
    } else {
      groups.set(item.currency, {
        currency: item.currency,
        items: [item],
        total: item.value,
      })
    }
  }

  return [...groups.values()]
}

function percentLabel(value: number, total: number): string {
  if (total === 0) {
    return '—'
  }

  return `${Math.round((value / total) * 100)}%`
}

function barPercent(value: number, total: number): number {
  if (total <= 0 || value <= 0) {
    return 0
  }

  return Math.min(100, (value / total) * 100)
}

export function AllocationSection({ items }: AllocationSectionProps) {
  const groups = groupByCurrency(items)

  return (
    <section>
      <h2 className="mb-3 text-lg font-semibold">Asset Allocation</h2>
      {groups.length === 0 ? (
        <p className="text-slate-600">No data yet</p>
      ) : (
        <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
          {groups.map((group) => (
            <article
              key={group.currency}
              className="rounded border border-slate-200 bg-white p-4"
            >
              <h3 className="mb-3 text-sm font-medium text-slate-500">{group.currency}</h3>
              <ul className="flex flex-col gap-3">
                {group.items.map((item) => {
                  const width = barPercent(item.value, group.total)

                  return (
                    <li key={`${item.accountType}-${item.currency}`}>
                      <div className="flex items-baseline justify-between gap-3 text-sm">
                        <span className="font-medium">{item.accountType}</span>
                        <span className="tabular-nums text-slate-700">
                          {formatMoney(item.value, item.currency)}
                          <span className="ml-2 text-slate-500">
                            {percentLabel(item.value, group.total)}
                          </span>
                        </span>
                      </div>
                      <div
                        className="mt-1 h-2 overflow-hidden rounded bg-slate-100"
                        aria-hidden="true"
                      >
                        <div
                          className="h-full rounded bg-slate-700"
                          style={{ width: `${width}%` }}
                        />
                      </div>
                    </li>
                  )
                })}
              </ul>
            </article>
          ))}
        </div>
      )}
    </section>
  )
}
