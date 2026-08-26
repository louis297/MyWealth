import { useState, type FormEvent } from 'react'
import { isPositionType, TRANSACTION_TYPES, type TransactionType } from '../types'

export type TransactionFormValues = {
  accountId: string
  type: TransactionType
  bookedOn: string
  amount: string
  holdingId: string
  quantity: string
  note: string
}

type AccountOption = {
  id: number
  name: string
  currency: string
}

type HoldingOption = {
  id: number
  instrument: {
    name: string
    symbol?: string | null
  }
}

type TransactionFormProps = {
  initial: TransactionFormValues
  accounts: readonly AccountOption[]
  holdings: readonly HoldingOption[]
  holdingsLoading: boolean
  holdingsError: boolean
  onRetryHoldings: () => void
  isSubmitting: boolean
  error: string | null
  submitLabel: string
  onAccountChange: (accountId: string) => void
  onSubmit: (values: TransactionFormValues) => void
}

export function TransactionForm({
  initial,
  accounts,
  holdings,
  holdingsLoading,
  holdingsError,
  onRetryHoldings,
  isSubmitting,
  error,
  submitLabel,
  onAccountChange,
  onSubmit,
}: TransactionFormProps) {
  const [accountId, setAccountId] = useState(initial.accountId)
  const [type, setType] = useState<TransactionType>(initial.type)
  const [bookedOn, setBookedOn] = useState(initial.bookedOn)
  const [amount, setAmount] = useState(initial.amount)
  const [holdingId, setHoldingId] = useState(initial.holdingId)
  const [quantity, setQuantity] = useState(initial.quantity)
  const [note, setNote] = useState(initial.note)

  const selectedAccount = accounts.find((account) => String(account.id) === accountId)
  const needsHolding = isPositionType(type)
  const noHoldings = needsHolding && accountId !== '' && !holdingsLoading && !holdingsError && holdings.length === 0
  const submitBlocked =
    isSubmitting ||
    accountId === '' ||
    (needsHolding && (holdingId === '' || holdingsLoading || holdingsError || holdings.length === 0))

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    onSubmit({
      accountId,
      type,
      bookedOn,
      amount,
      holdingId,
      quantity,
      note: note.trim(),
    })
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="flex max-w-xl flex-col gap-4 rounded border border-slate-200 bg-white p-4"
    >
      {error ? (
        <p className="rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800" role="alert">
          {error}
        </p>
      ) : null}

      <label className="flex flex-col gap-1 text-sm" htmlFor="transaction-account">
        Account
        <select
          id="transaction-account"
          name="accountId"
          required
          value={accountId}
          onChange={(event) => {
            const next = event.target.value
            setAccountId(next)
            setHoldingId('')
            onAccountChange(next)
          }}
          className="rounded border border-slate-300 bg-white px-3 py-2"
        >
          <option value="">Select an account</option>
          {accounts.map((account) => (
            <option key={account.id} value={String(account.id)}>
              {account.name}
            </option>
          ))}
        </select>
      </label>

      <label className="flex flex-col gap-1 text-sm" htmlFor="transaction-type">
        Type
        <select
          id="transaction-type"
          name="type"
          required
          value={type}
          onChange={(event) => setType(event.target.value as TransactionType)}
          className="rounded border border-slate-300 bg-white px-3 py-2"
        >
          {TRANSACTION_TYPES.map((transactionType) => (
            <option key={transactionType} value={transactionType}>
              {transactionType}
            </option>
          ))}
        </select>
      </label>

      <label className="flex flex-col gap-1 text-sm" htmlFor="transaction-booked-on">
        Booked on
        <input
          id="transaction-booked-on"
          type="date"
          name="bookedOn"
          required
          value={bookedOn}
          onChange={(event) => setBookedOn(event.target.value)}
          className="rounded border border-slate-300 px-3 py-2"
        />
      </label>

      <label className="flex flex-col gap-1 text-sm" htmlFor="transaction-amount">
        Amount
        <input
          id="transaction-amount"
          type="number"
          name="amount"
          required
          min="0"
          step="any"
          value={amount}
          onChange={(event) => setAmount(event.target.value)}
          className="rounded border border-slate-300 px-3 py-2"
        />
      </label>

      <p className="text-sm text-slate-600">
        Currency:{' '}
        <span className="font-medium text-slate-800">{selectedAccount?.currency ?? '—'}</span>
        {selectedAccount ? ' (from the selected account)' : ''}
      </p>

      {needsHolding ? (
        <>
          <label className="flex flex-col gap-1 text-sm" htmlFor="transaction-holding">
            Holding
            <select
              id="transaction-holding"
              name="holdingId"
              required
              value={holdingId}
              disabled={accountId === '' || holdingsLoading || holdingsError || holdings.length === 0}
              onChange={(event) => setHoldingId(event.target.value)}
              className="rounded border border-slate-300 bg-white px-3 py-2 disabled:cursor-not-allowed disabled:opacity-60"
            >
              <option value="">Select a holding</option>
              {holdings.map((holding) => (
                <option key={holding.id} value={String(holding.id)}>
                  {holding.instrument.symbol
                    ? `${holding.instrument.name} (${holding.instrument.symbol})`
                    : holding.instrument.name}
                </option>
              ))}
            </select>
          </label>
          {holdingsError ? (
            <button
              type="button"
              className="self-start rounded border border-slate-300 bg-white px-3 py-1.5 text-sm hover:bg-slate-50"
              onClick={onRetryHoldings}
            >
              Retry holdings
            </button>
          ) : null}
          {noHoldings ? (
            <p className="text-sm text-slate-600">
              This account has no holdings. Add a holding on the account first.
            </p>
          ) : null}
          <label className="flex flex-col gap-1 text-sm" htmlFor="transaction-quantity">
            Quantity
            <input
              id="transaction-quantity"
              type="number"
              name="quantity"
              required
              min="0"
              step="any"
              value={quantity}
              onChange={(event) => setQuantity(event.target.value)}
              className="rounded border border-slate-300 px-3 py-2"
            />
          </label>
        </>
      ) : null}

      <label className="flex flex-col gap-1 text-sm" htmlFor="transaction-note">
        Note
        <input
          id="transaction-note"
          name="note"
          maxLength={500}
          value={note}
          onChange={(event) => setNote(event.target.value)}
          className="rounded border border-slate-300 px-3 py-2"
        />
      </label>

      <button
        type="submit"
        disabled={submitBlocked}
        className="rounded bg-slate-800 px-3 py-2 text-sm text-white hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-60"
      >
        {isSubmitting ? 'Saving…' : submitLabel}
      </button>
    </form>
  )
}
