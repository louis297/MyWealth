import { NavLink } from 'react-router-dom'
import type { NavItem } from './navItems'

type SidebarProps = {
  items: readonly NavItem[]
}

export function Sidebar({ items }: SidebarProps) {
  return (
    <aside className="flex w-56 flex-col bg-slate-800 text-slate-100">
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
  )
}
