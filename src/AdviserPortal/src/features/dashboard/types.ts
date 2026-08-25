export type AccountType = 'Bank' | 'Cash' | 'Brokerage' | 'Property' | 'Credit' | 'Other'

export type NetWorthItem = {
  currency: string
  assets: number
  liabilities: number
  net: number
}

export type NetWorthVm = {
  items: NetWorthItem[]
}

export type AllocationItem = {
  accountType: AccountType
  currency: string
  value: number
}

export type AssetAllocationVm = {
  items: AllocationItem[]
}
