import { useState } from 'react'
import { ConfirmDialog } from '../../../shared/components/ConfirmDialog'
import { apiErrorMessage } from '../../../shared/utils/apiError'
import {
  useCreateHoldingMutation,
  useDeleteHoldingMutation,
  useGetHoldingsByAccountQuery,
  useUpdateHoldingMutation,
} from '../accountsApi'
import type { Account, Holding, UpdateHoldingRequest } from '../types'
import { HoldingForm, type HoldingFormValues } from './HoldingForm'
import { HoldingsTable } from './HoldingsTable'

type HoldingsSectionProps = {
  account: Account
}

const emptyForm: HoldingFormValues = {
  name: '',
  symbol: '',
  quantity: '0',
  amount: '0',
}

function parseNonNegativeNumber(value: string, label: string): number | string {
  const parsed = Number(value)
  if (!Number.isFinite(parsed) || parsed < 0) {
    return `${label} must be zero or greater.`
  }

  return parsed
}

function valuesFromHolding(holding: Holding): HoldingFormValues {
  return {
    name: holding.instrument.name,
    symbol: holding.instrument.symbol ?? '',
    quantity: String(holding.quantity),
    amount: String(holding.costBasis.amount),
  }
}

export function HoldingsSection({ account }: HoldingsSectionProps) {
  const canWrite = account.status === 'Active'
  const holdings = useGetHoldingsByAccountQuery(account.id)
  const items = holdings.data ?? []
  const [formError, setFormError] = useState<string | null>(null)
  const [createKey, setCreateKey] = useState(0)
  const [editing, setEditing] = useState<Holding | null>(null)
  const [pendingDelete, setPendingDelete] = useState<Holding | null>(null)
  const [deleteError, setDeleteError] = useState<string | null>(null)
  const [createHolding, createState] = useCreateHoldingMutation()
  const [updateHolding, updateState] = useUpdateHoldingMutation()
  const [deleteHolding, deleteState] = useDeleteHoldingMutation()

  async function handleCreate(values: HoldingFormValues) {
    setFormError(null)

    const quantity = parseNonNegativeNumber(values.quantity, 'Quantity')
    if (typeof quantity === 'string') {
      setFormError(quantity)
      return
    }

    const amount = parseNonNegativeNumber(values.amount, 'Cost basis')
    if (typeof amount === 'string') {
      setFormError(amount)
      return
    }

    try {
      await createHolding({
        accountId: account.id,
        instrument: {
          name: values.name,
          ...(values.symbol === '' ? {} : { symbol: values.symbol }),
        },
        quantity,
        costBasis: {
          amount,
          currency: account.currency,
        },
      }).unwrap()
      setCreateKey((key) => key + 1)
    } catch (caught) {
      setFormError(apiErrorMessage(caught, 'Unable to create holding.'))
    }
  }

  async function handleUpdate(values: HoldingFormValues) {
    if (editing === null) {
      return
    }

    setFormError(null)

    const quantity = parseNonNegativeNumber(values.quantity, 'Quantity')
    if (typeof quantity === 'string') {
      setFormError(quantity)
      return
    }

    const amount = parseNonNegativeNumber(values.amount, 'Cost basis')
    if (typeof amount === 'string') {
      setFormError(amount)
      return
    }

    const body: UpdateHoldingRequest = { accountId: account.id, id: editing.id }
    const nameChanged = values.name !== editing.instrument.name
    const symbolChanged = values.symbol !== (editing.instrument.symbol ?? '')
    if (nameChanged || symbolChanged) {
      body.instrument = {
        name: values.name,
        ...(values.symbol === '' ? {} : { symbol: values.symbol }),
      }
    }

    if (quantity !== editing.quantity) {
      body.quantity = quantity
    }

    if (amount !== editing.costBasis.amount) {
      body.costBasis = { amount }
    }

    if (body.instrument === undefined && body.quantity === undefined && body.costBasis === undefined) {
      setEditing(null)
      return
    }

    try {
      await updateHolding(body).unwrap()
      setEditing(null)
    } catch (caught) {
      setFormError(apiErrorMessage(caught, 'Unable to update holding.'))
    }
  }

  async function handleDelete() {
    if (pendingDelete === null) {
      return
    }

    setDeleteError(null)
    try {
      await deleteHolding({ accountId: account.id, id: pendingDelete.id }).unwrap()
      setPendingDelete(null)
      if (editing?.id === pendingDelete.id) {
        setEditing(null)
      }
    } catch (caught) {
      setDeleteError(apiErrorMessage(caught, 'Unable to delete holding.'))
    }
  }

  return (
    <section className="mt-8">
      <h2 className="mb-3 text-lg font-semibold">Holdings</h2>

      {holdings.isLoading ? (
        <p className="text-slate-600" aria-busy="true">
          Loading holdings…
        </p>
      ) : null}

      {holdings.isError ? (
        <div>
          <p
            className="mb-3 rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800"
            role="alert"
          >
            Unable to load holdings.
          </p>
          <button
            type="button"
            className="rounded border border-slate-300 bg-white px-3 py-1.5 text-sm hover:bg-slate-50"
            onClick={() => {
              void holdings.refetch()
            }}
          >
            Retry
          </button>
        </div>
      ) : null}

      {holdings.isSuccess && items.length === 0 ? (
        <p className="mb-4 text-slate-600">No holdings yet</p>
      ) : null}

      {holdings.isSuccess && items.length > 0 ? (
        <HoldingsTable
          holdings={items}
          onEdit={
            canWrite
              ? (holding) => {
                  setFormError(null)
                  setEditing(holding)
                }
              : undefined
          }
          onDelete={
            canWrite
              ? (holding) => {
                  setDeleteError(null)
                  setPendingDelete(holding)
                }
              : undefined
          }
        />
      ) : null}

      {canWrite ? (
        <div className="mt-4">
          <h3 className="mb-3 text-base font-semibold">
            {editing ? 'Edit holding' : 'Add holding'}
          </h3>
          <HoldingForm
            key={editing ? `edit-${editing.id}` : `create-${createKey}`}
            initial={editing ? valuesFromHolding(editing) : emptyForm}
            currency={account.currency}
            isSubmitting={editing ? updateState.isLoading : createState.isLoading}
            error={formError}
            submitLabel={editing ? 'Save holding' : 'Add holding'}
            onCancel={
              editing
                ? () => {
                    setEditing(null)
                    setFormError(null)
                  }
                : undefined
            }
            onSubmit={(values) => {
              if (editing) {
                void handleUpdate(values)
              } else {
                void handleCreate(values)
              }
            }}
          />
        </div>
      ) : null}

      <ConfirmDialog
        open={pendingDelete !== null}
        title="Delete holding"
        confirmLabel="Delete"
        busy={deleteState.isLoading}
        error={deleteError}
        onCancel={() => {
          if (!deleteState.isLoading) {
            setPendingDelete(null)
            setDeleteError(null)
          }
        }}
        onConfirm={() => {
          void handleDelete()
        }}
      >
        <p>
          Delete{' '}
          <span className="font-medium text-slate-800">
            {pendingDelete?.instrument.name ?? 'this holding'}
          </span>
          ? This is refused if the holding still has historical transactions.
        </p>
      </ConfirmDialog>
    </section>
  )
}
