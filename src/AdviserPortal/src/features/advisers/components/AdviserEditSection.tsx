import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog'
import { apiErrorMessage } from '../../../shared/utils/apiError'
import { useDisableAdviserMutation, useUpdateAdviserMutation } from '../advisersApi'
import type { Adviser, UpdateAdviserRequest } from '../types'
import { AdviserForm, type AdviserFormValues } from './AdviserForm'

type AdviserEditSectionProps = {
  adviser: Adviser
}

export function AdviserEditSection({ adviser }: AdviserEditSectionProps) {
  const navigate = useNavigate()
  const [formError, setFormError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [confirmOpen, setConfirmOpen] = useState(false)
  const [updateAdviser, updateState] = useUpdateAdviserMutation()
  const [disableAdviser, disableState] = useDisableAdviserMutation()

  async function handleSave(values: AdviserFormValues) {
    setFormError(null)

    const body: UpdateAdviserRequest = { id: adviser.id }
    if (values.name !== adviser.name) {
      body.name = values.name
    }

    if (body.name === undefined) {
      return
    }

    try {
      await updateAdviser(body).unwrap()
    } catch (caught) {
      setFormError(apiErrorMessage(caught, 'Unable to update adviser.'))
    }
  }

  async function handleEnable() {
    setActionError(null)
    try {
      await updateAdviser({ id: adviser.id, isEnabled: true }).unwrap()
    } catch (caught) {
      setActionError(apiErrorMessage(caught, 'Unable to enable adviser.'))
    }
  }

  async function handleDisable() {
    setActionError(null)
    try {
      await disableAdviser(adviser.id).unwrap()
      setConfirmOpen(false)
      navigate('/advisers', { replace: true })
    } catch (caught) {
      setActionError(apiErrorMessage(caught, 'Unable to disable adviser.'))
    }
  }

  return (
    <section className="mt-8">
      <h2 className="mb-3 text-lg font-semibold">Edit</h2>
      <AdviserForm
        key={`${adviser.id}-${adviser.name}`}
        mode="edit"
        initial={{
          name: adviser.name,
          email: adviser.email,
          password: '',
          confirmPassword: '',
        }}
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

        {adviser.isEnabled ? (
          <button
            type="button"
            className="w-fit rounded border border-red-200 bg-white px-3 py-1.5 text-sm text-red-800 hover:bg-red-50"
            onClick={() => {
              setActionError(null)
              setConfirmOpen(true)
            }}
          >
            Disable adviser
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
            {updateState.isLoading ? 'Saving…' : 'Enable adviser'}
          </button>
        )}
      </div>

      <ConfirmDialog
        open={confirmOpen}
        title="Disable adviser"
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
          Disable <span className="font-medium text-slate-800">{adviser.name}</span>? They can be
          re-enabled later. This is refused while any customer is still assigned.
        </p>
      </ConfirmDialog>
    </section>
  )
}
