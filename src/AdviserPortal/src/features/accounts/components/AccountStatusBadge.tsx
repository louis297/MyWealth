import type { AccountStatus } from '../types'

type AccountStatusBadgeProps = {
  status: AccountStatus
}

export function AccountStatusBadge({ status }: AccountStatusBadgeProps) {
  return (
    <span
      className={
        status === 'Active'
          ? 'inline-flex rounded bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-800'
          : 'inline-flex rounded bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-600'
      }
    >
      {status}
    </span>
  )
}
