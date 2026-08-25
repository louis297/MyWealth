import { Link } from 'react-router-dom'
import { PageHeader } from './PageHeader'

export function NotFoundPage() {
  return (
    <div>
      <PageHeader title="Page not found" />
      <p className="text-slate-600">
        That page does not exist.{' '}
        <Link to="/" className="text-slate-800 underline hover:no-underline">
          Back to dashboard
        </Link>
      </p>
    </div>
  )
}
