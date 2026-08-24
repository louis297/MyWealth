import type { UserRole } from '../features/auth/types'

export type NavItem = {
  label: string
  to: string
  roles: readonly UserRole[]
}

export const NAV_ITEMS: readonly NavItem[] = [
  { label: 'Dashboard', to: '/', roles: ['SystemAdmin', 'TenantAdmin', 'Adviser'] },
  { label: 'Customers', to: '/customers', roles: ['TenantAdmin', 'Adviser'] },
  { label: 'Accounts', to: '/accounts', roles: ['TenantAdmin', 'Adviser'] },
  { label: 'Transactions', to: '/transactions', roles: ['TenantAdmin', 'Adviser'] },
  { label: 'Advisers', to: '/advisers', roles: ['TenantAdmin'] },
  { label: 'Profile', to: '/profile', roles: ['SystemAdmin', 'TenantAdmin', 'Adviser'] },
]
