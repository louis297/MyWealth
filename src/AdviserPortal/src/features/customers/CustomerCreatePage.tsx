import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { PageHeader } from '../../shared/components/PageHeader'
import { apiErrorMessage } from '../../shared/utils/apiError'
import { useCurrentUser } from '../auth/useCurrentUser'
import { CustomerForm, type CustomerFormValues } from './components/CustomerForm'
import { useCreateCustomerMutation, useGetAdvisersQuery } from './customersApi'

export function CustomerCreatePage() {
  const navigate = useNavigate()
  const currentUser = useCurrentUser()
  const isTenantAdmin = currentUser?.role === 'TenantAdmin'
  const [error, setError] = useState<string | null>(null)
  const [createCustomer, createState] = useCreateCustomerMutation()
  const advisers = useGetAdvisersQuery(
    { isEnabled: true, pageSize: 100 },
    { skip: !isTenantAdmin },
  )

  async function handleSubmit(values: CustomerFormValues) {
    setError(null)

    const adviserId = isTenantAdmin
      ? Number(values.adviserId)
      : currentUser?.domainUserId ?? null

    if (adviserId === null || !Number.isInteger(adviserId) || adviserId <= 0) {
      setError(
        isTenantAdmin
          ? 'Select an adviser.'
          : 'Your adviser profile is missing, so a customer cannot be created.',
      )
      return
    }

    try {
      const created = await createCustomer({
        name: values.name,
        email: values.email,
        adviserId,
      }).unwrap()
      navigate(`/customers/${created.id}`, { replace: true })
    } catch (caught) {
      setError(apiErrorMessage(caught, 'Unable to create customer.'))
    }
  }

  if (isTenantAdmin && advisers.isLoading) {
    return (
      <div>
        <PageHeader title="New customer" />
        <p className="text-slate-600" aria-busy="true">
          Loading advisers…
        </p>
      </div>
    )
  }

  if (isTenantAdmin && advisers.isError) {
    return (
      <div>
        <PageHeader title="New customer" />
        <p
          className="mb-3 rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800"
          role="alert"
        >
          Unable to load advisers.
        </p>
        <button
          type="button"
          className="rounded border border-slate-300 bg-white px-3 py-1.5 text-sm hover:bg-slate-50"
          onClick={() => {
            void advisers.refetch()
          }}
        >
          Retry
        </button>
      </div>
    )
  }

  return (
    <div>
      <PageHeader title="New customer">
        <Link to="/customers" className="text-sm text-slate-700 underline hover:no-underline">
          Back to customers
        </Link>
      </PageHeader>
      <CustomerForm
        mode="create"
        initial={{ name: '', email: '', adviserId: '' }}
        advisers={advisers.data?.items ?? []}
        isTenantAdmin={isTenantAdmin}
        assignedAdviserName={currentUser?.displayName}
        isSubmitting={createState.isLoading}
        error={error}
        submitLabel="Create customer"
        onSubmit={(values) => {
          void handleSubmit(values)
        }}
      />
    </div>
  )
}
