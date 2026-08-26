import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { PageHeader } from '../../shared/components/PageHeader'
import { apiErrorMessage } from '../../shared/utils/apiError'
import { useGetAccountsQuery, useGetHoldingsByAccountQuery } from '../accounts/accountsApi'
import { TransactionForm, type TransactionFormValues } from './components/TransactionForm'
import { useCreateTransactionMutation } from './transactionsApi'
import { isPositionType, type CreateTransactionRequest, type TransactionType } from './types'

const DEFAULT_TYPE: TransactionType = 'Buy'

function todayIsoDate(): string {
  const now = new Date()
  const year = String(now.getFullYear())
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${year}-${month}-${day}`
}

function parsePositiveNumber(value: string, label: string): number | string {
  const parsed = Number(value)
  if (!Number.isFinite(parsed) || parsed <= 0) {
    return `${label} must be greater than zero.`
  }

  return parsed
}

export function TransactionCreatePage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const presetAccountId = searchParams.get('accountId') ?? ''
  const [error, setError] = useState<string | null>(null)
  const [createTransaction, createState] = useCreateTransactionMutation()
  const accounts = useGetAccountsQuery({ status: 'Active', pageSize: 100 })
  const accountItems = accounts.data?.items ?? []
  const initialAccountId = accountItems.some((account) => String(account.id) === presetAccountId)
    ? presetAccountId
    : ''
  const [accountOverride, setAccountOverride] = useState<string | null>(null)
  const selectedAccountId = accountOverride ?? initialAccountId
  const selectedAccountNumber = Number(selectedAccountId)
  const holdings = useGetHoldingsByAccountQuery(selectedAccountNumber, {
    skip: !Number.isInteger(selectedAccountNumber) || selectedAccountNumber <= 0,
  })

  async function handleSubmit(values: TransactionFormValues) {
    setError(null)

    const accountId = Number(values.accountId)
    if (!Number.isInteger(accountId) || accountId <= 0) {
      setError('Select an account.')
      return
    }

    const account = accountItems.find((item) => item.id === accountId)
    if (account === undefined) {
      setError('Select an account.')
      return
    }

    const amount = parsePositiveNumber(values.amount, 'Amount')
    if (typeof amount === 'string') {
      setError(amount)
      return
    }

    const body: CreateTransactionRequest = {
      accountId,
      bookedOn: values.bookedOn,
      type: values.type,
      amount: {
        amount,
        currency: account.currency,
      },
    }

    if (isPositionType(values.type)) {
      const holdingId = Number(values.holdingId)
      if (!Number.isInteger(holdingId) || holdingId <= 0) {
        setError('Select a holding.')
        return
      }

      const quantity = parsePositiveNumber(values.quantity, 'Quantity')
      if (typeof quantity === 'string') {
        setError(quantity)
        return
      }

      body.holdingId = holdingId
      body.quantity = quantity
    }

    if (values.note !== '') {
      body.note = values.note
    }

    try {
      await createTransaction(body).unwrap()
      navigate('/transactions', { replace: true })
    } catch (caught) {
      setError(apiErrorMessage(caught, 'Unable to create transaction.'))
    }
  }

  if (accounts.isLoading) {
    return (
      <div>
        <PageHeader title="New transaction" />
        <p className="text-slate-600" aria-busy="true">
          Loading accounts…
        </p>
      </div>
    )
  }

  if (accounts.isError) {
    return (
      <div>
        <PageHeader title="New transaction" />
        <p
          className="mb-3 rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800"
          role="alert"
        >
          Unable to load accounts.
        </p>
        <button
          type="button"
          className="rounded border border-slate-300 bg-white px-3 py-1.5 text-sm hover:bg-slate-50"
          onClick={() => {
            void accounts.refetch()
          }}
        >
          Retry
        </button>
      </div>
    )
  }

  return (
    <div>
      <PageHeader title="New transaction">
        <Link to="/transactions" className="text-sm text-slate-700 underline hover:no-underline">
          Back to transactions
        </Link>
      </PageHeader>
      <TransactionForm
        initial={{
          accountId: initialAccountId,
          type: DEFAULT_TYPE,
          bookedOn: todayIsoDate(),
          amount: '',
          holdingId: '',
          quantity: '',
          note: '',
        }}
        accounts={accountItems}
        holdings={holdings.data ?? []}
        holdingsLoading={holdings.isLoading || holdings.isFetching}
        holdingsError={holdings.isError}
        onRetryHoldings={() => {
          void holdings.refetch()
        }}
        isSubmitting={createState.isLoading}
        error={error}
        submitLabel="Create transaction"
        onAccountChange={setAccountOverride}
        onSubmit={(values) => {
          void handleSubmit(values)
        }}
      />
    </div>
  )
}
