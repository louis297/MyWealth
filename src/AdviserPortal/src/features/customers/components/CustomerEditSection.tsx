import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog'
import { apiErrorMessage } from '../../../shared/utils/apiError'
import { useCurrentUser } from '../../auth/useCurrentUser'
import {
  useDisableCustomerMutation,
  useGetAdvisersQuery,
  useUpdateCustomerMutation,
} from '../customersApi'
import type { AdviserLookup, Customer, UpdateCustomerRequest } from '../types'
import { CustomerForm, type CustomerFormValues } from './CustomerForm'

type CustomerEditSectionProps = {
  customer: Customer
}

function mergeAdvisers(enabled: readonly AdviserLookup[], customer: Customer): AdviserLookup[] {
  if (enabled.some((adviser) => adviser.id === customer.adviserId)) {
    return [...enabled]
  }

  return [
    {
      id: customer.adviserId,
      name: customer.adviserName,
      email: '',
      isEnabled: false,
    },
    ...enabled,
  ]
}

export function CustomerEditSection({ customer }: CustomerEditSectionProps) {
  const navigate = useNavigate()
  const currentUser = useCurrentUser()
  const isTenantAdmin = currentUser?.role === 'TenantAdmin'
  const [formError, setFormError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [updateCustomer, updateState] = useUpdateCustomerMutation()
  const [disableCustomer, disableState] = useDisableCustomerMutation()
  const advisers = useGetAdvisersQuery(
    { isEnabled: true, pageSize: 100 },
    { skip: !isTenantAdmin },
  )

  async function handleSave(values: CustomerFormValues) {
    setFormError(null)

    const body: UpdateCustomerRequest = { id: customer.id }
    if (values.name !== customer.name) {
      body.name = values.name
    }

    if (isTenantAdmin) {
      const adviserId = Number(values.adviserId)
      if (!Number.isInteger(adviserId) || adviserId <= 0) {
        setFormError('Select an adviser.')
        return
      }

      if (adviserId !== customer.adviserId) {
        body.adviserId = adviserId
      }
    }

    if (body.name === undefined && body.adviserId === undefined) {
      return
    }

    try {
      await updateCustomer(body).unwrap()
    } catch (caught) {
      setFormError(apiErrorMessage(caught, 'Unable to update customer.'))
    }
  }

  async function handleEnable() {
    setActionError(null)
    try {
      await updateCustomer({ id: customer.id, isEnabled: true }).unwrap()
    } catch (caught) {
      setActionError(apiErrorMessage(caught, 'Unable to enable customer.'))
    }
  }

  async function handleDisable() {
    setActionError(null)
    try {
      await disableCustomer(customer.id).unwrap()
      setConfirmOpen(false)
      navigate('/customers', { replace: true })
    } catch (caught) {
      setActionError(apiErrorMessage(caught, 'Unable to disable customer.'))
    }
  }

  return (
    <section className="mt-8">
      <h2 className="mb-3 text-lg font-semibold">Edit</h2>
      <CustomerForm
        key={`${customer.id}-${customer.name}-${customer.adviserId}`}
        mode="edit"
        initial={{
          name: customer.name,
          email: customer.email,
          adviserId: String(customer.adviserId),
        }}
        advisers={mergeAdvisers(advisers.data?.items ?? [], customer)}
        isTenantAdmin={isTenantAdmin}
        assignedAdviserName={customer.adviserName}
        isSubmitting={updateState.isLoading}
        error={formError}
        submitLabel="Save changes"
        onSubmit={(values) => {
          void handleSave(values)
        }}
      />

      <div className="mt-4 flex flex-col gap-3">
        {actionError && !confirmOpen ? (
          <p
            className="max-w-xl rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800"
            role="alert"
          >
            {actionError}
          </p>
        ) : null}

        {customer.isEnabled ? (
          <button
            type="button"
            className="w-fit rounded border border-red-200 bg-white px-3 py-1.5 text-sm text-red-800 hover:bg-red-50"
            onClick={() => {
              setActionError(null)
              setConfirmOpen(true)
            }}
          >
            Disable customer
          </button>
        ) : (
          <button
            type="button"
            disabled={updateState.isLoading}
            className="w-fit rounded border border-slate-300 bg-white px-3 py-1.5 text-sm hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
            onClick={() => {
              void handleEnable()
            }}
          >
            {updateState.isLoading ? 'Saving…' : 'Enable customer'}
          </button>
        )}
      </div>

      <ConfirmDialog
        open={confirmOpen}
        title="Disable customer"
        confirmLabel="Disable"
        busy={disableState.isLoading}
        error={actionError}
        onCancel={() => {
          if (!disableState.isLoading) {
            setConfirmOpen(false)
            setActionError(null)
          }
        }}
        onConfirm={() => {
          void handleDisable()
        }}
      >
        <p>
          Disable <span className="font-medium text-slate-800">{customer.name}</span>? They can be
          re-enabled later. This is refused while any active account remains.
        </p>
      </ConfirmDialog>
    </section>
  )
}
