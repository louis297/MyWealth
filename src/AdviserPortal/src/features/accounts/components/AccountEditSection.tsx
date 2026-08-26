import { useState } from 'react'
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog'
import { apiErrorMessage } from '../../../shared/utils/apiError'
import { useCloseAccountMutation, useUpdateAccountMutation } from '../accountsApi'
import type { Account, UpdateAccountRequest } from '../types'
import { AccountForm, type AccountFormValues } from './AccountForm'

type AccountEditSectionProps = {
  account: Account
}

export function AccountEditSection({ account }: AccountEditSectionProps) {
  const [formError, setFormError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [updateAccount, updateState] = useUpdateAccountMutation()
  const [closeAccount, closeState] = useCloseAccountMutation()

  async function handleSave(values: AccountFormValues) {
    setFormError(null)

    const body: UpdateAccountRequest = { id: account.id }
    if (values.name !== account.name) {
      body.name = values.name
    }

    if (values.type !== account.type) {
      body.type = values.type
    }

    if (body.name === undefined && body.type === undefined) {
      return
    }

    try {
      await updateAccount(body).unwrap()
    } catch (caught) {
      setFormError(apiErrorMessage(caught, 'Unable to update account.'))
    }
  }

  async function handleClose() {
    setActionError(null)
    try {
      await closeAccount(account.id).unwrap()
      setConfirmOpen(false)
    } catch (caught) {
      setActionError(apiErrorMessage(caught, 'Unable to close account.'))
    }
  }

  return (
    <section className="mt-8">
      <h2 className="mb-3 text-lg font-semibold">Edit</h2>
      <AccountForm
        key={`${account.id}-${account.name}-${account.type}`}
        mode="edit"
        initial={{
          customerId: String(account.customerId),
          name: account.name,
          type: account.type,
          currency: account.currency,
        }}
        customers={[]}
        isSubmitting={updateState.isLoading}
        error={formError}
        submitLabel="Save changes"
        onSubmit={(values) => {
          void handleSave(values)
        }}
      />

      {account.status === 'Active' ? (
        <div className="mt-4 flex flex-col gap-3">
          {actionError && !confirmOpen ? (
            <p
              className="max-w-xl rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800"
              role="alert"
            >
              {actionError}
            </p>
          ) : null}

          <button
            type="button"
            className="w-fit rounded border border-red-200 bg-white px-3 py-1.5 text-sm text-red-800 hover:bg-red-50"
            onClick={() => {
              setActionError(null)
              setConfirmOpen(true)
            }}
          >
            Close account
          </button>
        </div>
      ) : null}

      <ConfirmDialog
        open={confirmOpen}
        title="Close account"
        confirmLabel="Close account"
        busy={closeState.isLoading}
        error={actionError}
        onCancel={() => {
          if (!closeState.isLoading) {
            setConfirmOpen(false)
            setActionError(null)
          }
        }}
        onConfirm={() => {
          void handleClose()
        }}
      >
        <p>
          Close <span className="font-medium text-slate-800">{account.name}</span>? This cannot be
          undone. Existing holdings and history are kept, but no further holdings or transactions
          can be added.
        </p>
      </ConfirmDialog>
    </section>
  )
}
