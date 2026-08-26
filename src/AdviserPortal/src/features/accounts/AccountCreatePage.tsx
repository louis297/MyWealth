import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { PageHeader } from '../../shared/components/PageHeader'
import { apiErrorMessage } from '../../shared/utils/apiError'
import { useGetCustomersQuery } from '../customers/customersApi'
import { useCreateAccountMutation } from './accountsApi'
import { AccountForm, type AccountFormValues } from './components/AccountForm'
import type { AccountType } from './types'

const DEFAULT_CURRENCY = 'NZD'
const DEFAULT_TYPE: AccountType = 'Bank'

export function AccountCreatePage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const presetCustomerId = searchParams.get('customerId') ?? ''
  const [error, setError] = useState<string | null>(null)
  const [createAccount, createState] = useCreateAccountMutation()
  const customers = useGetCustomersQuery({ isEnabled: true, pageSize: 100 })
  const customerItems = customers.data?.items ?? []
  const initialCustomerId =
    customerItems.some((customer) => String(customer.id) === presetCustomerId) ? presetCustomerId : ''

  async function handleSubmit(values: AccountFormValues) {
    setError(null)

    const customerId = Number(values.customerId)
    if (!Number.isInteger(customerId) || customerId <= 0) {
      setError('Select a customer.')
      return
    }

    if (values.currency.length !== 3) {
      setError('Currency must be a 3-letter ISO 4217 code.')
      return
    }

    try {
      const created = await createAccount({
        customerId,
        name: values.name,
        type: values.type,
        currency: values.currency,
      }).unwrap()
      navigate(`/accounts/${created.id}`, { replace: true })
    } catch (caught) {
      setError(apiErrorMessage(caught, 'Unable to create account.'))
    }
  }

  if (customers.isLoading) {
    return (
      <div>
        <PageHeader title="New account" />
        <p className="text-slate-600" aria-busy="true">
          Loading customers…
        </p>
      </div>
    )
  }

  if (customers.isError) {
    return (
      <div>
        <PageHeader title="New account" />
        <p
          className="mb-3 rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800"
          role="alert"
        >
          Unable to load customers.
        </p>
        <button
          type="button"
          className="rounded border border-slate-300 bg-white px-3 py-1.5 text-sm hover:bg-slate-50"
          onClick={() => {
            void customers.refetch()
          }}
        >
          Retry
        </button>
      </div>
    )
  }

  return (
    <div>
      <PageHeader title="New account">
        <Link to="/accounts" className="text-sm text-slate-700 underline hover:no-underline">
          Back to accounts
        </Link>
      </PageHeader>
      <AccountForm
        mode="create"
        initial={{
          customerId: initialCustomerId,
          name: '',
          type: DEFAULT_TYPE,
          currency: DEFAULT_CURRENCY,
        }}
        customers={customerItems}
        isSubmitting={createState.isLoading}
        error={error}
        submitLabel="Create account"
        onSubmit={(values) => {
          void handleSubmit(values)
        }}
      />
    </div>
  )
}
