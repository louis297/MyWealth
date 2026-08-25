export type Customer = {
  id: number
  name: string
  email: string
  isEnabled: boolean
  adviserId: number
  adviserName: string
}

export type CreateCustomerRequest = {
  name: string
  email: string
  adviserId: number
}

export type UpdateCustomerRequest = {
  id: number
  name?: string
  isEnabled?: boolean
  adviserId?: number
}

export type CreatedId = {
  id: number
}

export type GetCustomersArgs = {
  page?: number
  pageSize?: number
  isEnabled?: boolean
  search?: string
}

export type AdviserLookup = {
  id: number
  name: string
  email: string
  isEnabled: boolean
}

export type GetAdvisersArgs = {
  page?: number
  pageSize?: number
  isEnabled?: boolean
  search?: string
}

export type AccountType = 'Bank' | 'Cash' | 'Brokerage' | 'Property' | 'Credit' | 'Other'

export type AccountStatus = 'Active' | 'Closed'

export type AccountSummary = {
  id: number
  customerId: number
  customerName: string
  name: string
  type: AccountType
  status: AccountStatus
  currency: string
}

export type GetAccountsArgs = {
  page?: number
  pageSize?: number
  customerId?: number
  status?: AccountStatus
  search?: string
}
