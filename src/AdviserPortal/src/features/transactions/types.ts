export const TRANSACTION_TYPES = [
  'Buy',
  'Sell',
  'TransferIn',
  'TransferOut',
  'Dividend',
  'Interest',
] as const

export type TransactionType = (typeof TRANSACTION_TYPES)[number]

export const POSITION_TYPES = ['Buy', 'Sell'] as const

export type PositionType = (typeof POSITION_TYPES)[number]

export function isPositionType(type: TransactionType): type is PositionType {
  return type === 'Buy' || type === 'Sell'
}

export type Money = {
  amount: number
  currency: string
}

export type Transaction = {
  id: number
  accountId: number
  holdingId: number | null
  bookedOn: string
  type: TransactionType
  amount: Money
  quantity: number | null
  note: string | null
}

export type CreateTransactionRequest = {
  accountId: number
  bookedOn: string
  type: TransactionType
  amount: Money
  holdingId?: number
  quantity?: number
  note?: string
}

export type CreatedId = {
  id: number
}

export type GetTransactionsArgs = {
  page?: number
  pageSize?: number
  accountId?: number
  from?: string
  to?: string
  type?: TransactionType
}
