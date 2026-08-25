export type UserRole = 'SystemAdmin' | 'TenantAdmin' | 'Adviser'

export type CurrentUser = {
  userId: string
  email: string
  displayName: string
  role: UserRole
  tenantId: number | null
  isEnabled: boolean
  domainUserId: number | null
}

export type LoginRequest = {
  email: string
  password: string
}

export type LoginResult = {
  accessToken: string
  tokenType: string
  expiresIn: number
  userId: string
  email: string
  displayName: string
  role: UserRole
  tenantId: number | null
}
