import { formatMoney } from '../../../shared/utils/formatMoney'
import type { Holding } from '../types'

type HoldingsTableProps = {
  holdings: readonly Holding[]
}

export function HoldingsTable({ holdings }: HoldingsTableProps) {
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
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
