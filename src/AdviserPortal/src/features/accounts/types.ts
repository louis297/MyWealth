export const ACCOUNT_TYPES = ['Bank', 'Cash', 'Brokerage', 'Property', 'Credit', 'Other'] as const

export type AccountType = (typeof ACCOUNT_TYPES)[number]

export const ACCOUNT_STATUSES = ['Active', 'Closed'] as const

export type AccountStatus = (typeof ACCOUNT_STATUSES)[number]

export type Account = {
  id: number
  customerId: number
  customerName: string
  name: string
  type: AccountType
  status: AccountStatus
  currency: string
}

export type CreateAccountRequest = {
  customerId: number
  name: string
  type: AccountType
  currency: string
}

export type UpdateAccountRequest = {
  id: number
  name?: string
  type?: AccountType
}

export type CreatedId = {
  id: number
}

export type GetAccountsArgs = {
  page?: number
  pageSize?: number
  customerId?: number
  status?: AccountStatus
  search?: string
}

export type Instrument = {
  name: string
  symbol?: string | null
}

export type Money = {
  amount: number
  currency: string
}

export type Holding = {
  id: number
  accountId: number
  instrument: Instrument
  quantity: number
  costBasis: Money
}

export type CreateHoldingRequest = {
  accountId: number
  instrument: {
    name: string
    symbol?: string
  }
  quantity: number
  costBasis: {
    amount: number
    currency: string
  }
}

export type UpdateHoldingRequest = {
  accountId: number
  id: number
  instrument?: {
    name: string
    symbol?: string
  }
  quantity?: number
  costBasis?: {
    amount: number
  }
}

export type DeleteHoldingRequest = {
  accountId: number
  id: number
}
