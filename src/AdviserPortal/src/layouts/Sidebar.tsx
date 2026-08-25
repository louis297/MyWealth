import { NavLink } from 'react-router-dom'
import type { NavItem } from './navItems'

type SidebarProps = {
  items: readonly NavItem[]
  open: boolean
  onClose: () => void
  id: string
}

export function Sidebar({ items, open, onClose, id }: SidebarProps) {
  return (
    <>
      {open ? (
        <button
          type="button"
          className="fixed inset-0 z-30 bg-black/40 md:hidden"
          aria-label="Close menu"
          onClick={onClose}
        />
      ) : null}
      <aside
        id={id}
        className={[
          'flex w-56 flex-col bg-slate-800 text-slate-100',
          'fixed inset-y-0 left-0 z-40 transform transition-transform duration-200 md:static md:translate-x-0',
          open ? 'translate-x-0' : '-translate-x-full',
        ].join(' ')}
      >
        <div className="border-b border-slate-700 px-4 py-4">
          <NavLink to="/" className="text-lg font-semibold text-slate-100">
            MyWealth
          </NavLink>
        </div>
        <nav aria-label="Main" className="flex flex-1 flex-col gap-1 p-2">
          {items.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              className={({ isActive }) =>
                [
                  'rounded px-3 py-2 text-sm',
                  isActive ? 'bg-slate-600 font-medium text-white' : 'text-slate-200 hover:bg-slate-700',
                ].join(' ')
              }
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>
    </>
  )
}
