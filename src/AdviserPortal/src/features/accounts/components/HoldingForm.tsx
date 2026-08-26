import { useState, type FormEvent } from 'react'

export type HoldingFormValues = {
  name: string
  symbol: string
  quantity: string
  amount: string
}

type HoldingFormProps = {
  initial: HoldingFormValues
  currency: string
  isSubmitting: boolean
  error: string | null
  submitLabel: string
  onSubmit: (values: HoldingFormValues) => void
  onCancel?: () => void
}

export function HoldingForm({
  initial,
  currency,
  isSubmitting,
  error,
  submitLabel,
  onSubmit,
  onCancel,
}: HoldingFormProps) {
  const [name, setName] = useState(initial.name)
  const [symbol, setSymbol] = useState(initial.symbol)
  const [quantity, setQuantity] = useState(initial.quantity)
  const [amount, setAmount] = useState(initial.amount)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    onSubmit({
      name: name.trim(),
      symbol: symbol.trim(),
      quantity,
      amount,
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

      <label className="flex flex-col gap-1 text-sm" htmlFor="holding-name">
        Instrument name
        <input
          id="holding-name"
          name="name"
          required
          maxLength={200}
          value={name}
          onChange={(event) => setName(event.target.value)}
          className="rounded border border-slate-300 px-3 py-2"
        />
      </label>

      <label className="flex flex-col gap-1 text-sm" htmlFor="holding-symbol">
        Symbol
        <input
          id="holding-symbol"
          name="symbol"
          maxLength={50}
          value={symbol}
          onChange={(event) => setSymbol(event.target.value)}
          className="rounded border border-slate-300 px-3 py-2"
        />
      </label>

      <label className="flex flex-col gap-1 text-sm" htmlFor="holding-quantity">
        Quantity
        <input
          id="holding-quantity"
          name="quantity"
          type="number"
          required
          min={0}
          step="any"
          value={quantity}
          onChange={(event) => setQuantity(event.target.value)}
          className="rounded border border-slate-300 px-3 py-2"
        />
      </label>

      <label className="flex flex-col gap-1 text-sm" htmlFor="holding-amount">
        Cost basis ({currency})
        <input
          id="holding-amount"
          name="amount"
          type="number"
          required
          min={0}
          step="any"
          value={amount}
          onChange={(event) => setAmount(event.target.value)}
          className="rounded border border-slate-300 px-3 py-2"
        />
      </label>

      <div className="flex gap-2">
        <button
          type="submit"
          disabled={isSubmitting}
          className="rounded bg-slate-800 px-3 py-2 text-sm text-white hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {isSubmitting ? 'Saving…' : submitLabel}
        </button>
        {onCancel ? (
          <button
            type="button"
            disabled={isSubmitting}
            className="rounded border border-slate-300 bg-white px-3 py-2 text-sm hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
            onClick={onCancel}
          >
            Cancel
          </button>
        ) : null}
      </div>
    </form>
  )
}
