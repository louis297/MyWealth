import { formatMoney } from '../../../shared/utils/formatMoney'
import type { Holding } from '../types'

type HoldingsTableProps = {
  holdings: readonly Holding[]
  onEdit?: (holding: Holding) => void
  onDelete?: (holding: Holding) => void
}

export function HoldingsTable({ holdings, onEdit, onDelete }: HoldingsTableProps) {
  const showActions = onEdit !== undefined || onDelete !== undefined

  return (
    <div className="overflow-x-auto rounded border border-slate-200 bg-white">
      <table className="min-w-full text-left text-sm">
        <caption className="sr-only">Holdings</caption>
        <thead className="border-b border-slate-200 bg-slate-50 text-slate-600">
          <tr>
            <th scope="col" className="px-4 py-3 font-medium">
              Instrument
            </th>
            <th scope="col" className="px-4 py-3 font-medium">
              Symbol
            </th>
            <th scope="col" className="px-4 py-3 font-medium">
              Quantity
            </th>
            <th scope="col" className="px-4 py-3 font-medium">
              Cost basis
            </th>
            {showActions ? (
              <th scope="col" className="px-4 py-3 font-medium">
                Actions
              </th>
            ) : null}
          </tr>
        </thead>
        <tbody>
          {holdings.map((holding) => (
            <tr key={holding.id} className="border-b border-slate-100 last:border-0">
              <td className="px-4 py-3 text-slate-800">{holding.instrument.name}</td>
              <td className="px-4 py-3 text-slate-700">{holding.instrument.symbol ?? '—'}</td>
              <td className="px-4 py-3 text-slate-700">{holding.quantity.toLocaleString()}</td>
              <td className="px-4 py-3 text-slate-700">
                {formatMoney(holding.costBasis.amount, holding.costBasis.currency)}
              </td>
              {showActions ? (
                <td className="px-4 py-3">
                  <div className="flex gap-2">
                    {onEdit ? (
                      <button
                        type="button"
                        className="text-slate-800 underline hover:no-underline"
                        onClick={() => onEdit(holding)}
                      >
                        Edit
                      </button>
                    ) : null}
                    {onDelete ? (
                      <button
                        type="button"
                        className="text-red-800 underline hover:no-underline"
                        onClick={() => onDelete(holding)}
                      >
                        Delete
                      </button>
                    ) : null}
                  </div>
                </td>
              ) : null}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
