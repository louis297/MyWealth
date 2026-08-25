import type { CurrentUser } from '../features/auth/types'

type TopBarProps = {
  currentUser: CurrentUser | null
  onLogout: () => void
  sidebarOpen: boolean
  onToggleSidebar: () => void
  sidebarId: string
}

export function TopBar({
  currentUser,
  onLogout,
  sidebarOpen,
  onToggleSidebar,
  sidebarId,
}: TopBarProps) {
  return (
    <header className="flex items-center gap-4 border-b border-slate-200 bg-white px-4 py-3 md:px-6">
      <button
        type="button"
        className="rounded p-2 text-slate-700 hover:bg-slate-50 md:hidden"
        aria-label="Main menu"
        aria-expanded={sidebarOpen}
        aria-controls={sidebarId}
        onClick={onToggleSidebar}
      >
        <svg
          xmlns="http://www.w3.org/2000/svg"
          viewBox="0 0 24 24"
          fill="none"
          stroke="currentColor"
          strokeWidth="2"
          className="h-5 w-5"
          aria-hidden="true"
        >
          <path strokeLinecap="round" d="M4 7h16M4 12h16M4 17h16" />
        </svg>
      </button>
      <div className="ml-auto flex items-center gap-4">
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
      </div>
    </header>
  )
}
