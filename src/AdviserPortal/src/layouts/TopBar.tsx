import type { CurrentUser } from '../features/auth/types'

type TopBarProps = {
  currentUser: CurrentUser | null
  onLogout: () => void
}

export function TopBar({ currentUser, onLogout }: TopBarProps) {
  return (
    <header className="flex items-center justify-end gap-4 border-b border-slate-200 bg-white px-6 py-3">
      <div className="text-right text-sm">
        <div className="font-medium text-slate-800">{currentUser?.displayName ?? ''}</div>
        <div className="text-slate-500">{currentUser?.role ?? ''}</div>
      </div>
      <button
        type="button"
        className="rounded border border-slate-300 px-3 py-1.5 text-sm hover:bg-slate-50"
        onClick={onLogout}
      >
        Log out
      </button>
    </header>
  )
}
