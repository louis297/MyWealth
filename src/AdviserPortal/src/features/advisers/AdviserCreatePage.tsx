import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { PageHeader } from '../../shared/components/PageHeader'
import { apiErrorMessage } from '../../shared/utils/apiError'
import { useCreateAdviserMutation } from './advisersApi'
import { AdviserForm, type AdviserFormValues } from './components/AdviserForm'

export function AdviserCreatePage() {
  const navigate = useNavigate()
  const [error, setError] = useState<string | null>(null)
  const [createAdviser, createState] = useCreateAdviserMutation()

  async function handleSubmit(values: AdviserFormValues) {
    setError(null)

    if (values.password !== values.confirmPassword) {
      setError('Password and confirmation do not match.')
      return
    }

    try {
      const created = await createAdviser({
        name: values.name,
        email: values.email,
        password: values.password,
      }).unwrap()
      navigate(`/advisers/${created.id}`, { replace: true })
    } catch (caught) {
      setError(apiErrorMessage(caught, 'Unable to create adviser.'))
    }
  }

  return (
    <div>
      <PageHeader title="New adviser">
        <Link to="/advisers" className="text-sm text-slate-700 underline hover:no-underline">
          Back to advisers
        </Link>
      </PageHeader>
      <AdviserForm
        mode="create"
        initial={{ name: '', email: '', password: '', confirmPassword: '' }}
        isSubmitting={createState.isLoading}
        error={error}
        submitLabel="Create adviser"
        onSubmit={(values) => {
          void handleSubmit(values)
        }}
      />
    </div>
  )
}
