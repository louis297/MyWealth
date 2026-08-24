export type UserRole = 'SystemAdmin' | 'TenantAdmin' | 'Adviser'

export type CurrentUser = {
  userId: string
  email: string
  displayName: string
  role: UserRole
  tenantId: number | null
  isEnabled: boolean
}
