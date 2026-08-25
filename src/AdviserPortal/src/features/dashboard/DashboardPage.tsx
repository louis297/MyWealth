import { PageHeader } from '../../shared/components/PageHeader'
import { useCurrentUser } from '../auth/useCurrentUser'
import { AllocationSection } from './components/AllocationSection'
import { NetWorthSection } from './components/NetWorthSection'
import { useGetAssetAllocationQuery, useGetNetWorthQuery } from './dashboardApi'

export function DashboardPage() {
  const currentUser = useCurrentUser()
  const isSystemAdmin = currentUser?.role === 'SystemAdmin'
  const netWorth = useGetNetWorthQuery(undefined, { skip: isSystemAdmin })
  const allocation = useGetAssetAllocationQuery(undefined, { skip: isSystemAdmin })

  if (isSystemAdmin) {
    return (
      <div>
        <PageHeader title="Dashboard" />
        <p className="text-slate-600">
          Net worth and allocation are available to Tenant Admins and Advisers. System
          administration stays in the API for this demo.
        </p>
      </div>
    )
  }

  if (netWorth.isLoading || allocation.isLoading) {
    return (
      <div>
        <PageHeader title="Dashboard" />
        <p className="text-slate-600" aria-busy="true">
          Loading dashboard…
        </p>
      </div>
    )
  }

  if (netWorth.isError || allocation.isError) {
    return (
      <div>
        <PageHeader title="Dashboard" />
        <p
          className="mb-3 rounded border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800"
          role="alert"
        >
          Unable to load dashboard.
        </p>
        <button
          type="button"
          className="rounded border border-slate-300 bg-white px-3 py-1.5 text-sm hover:bg-slate-50"
          onClick={() => {
            void netWorth.refetch()
            void allocation.refetch()
          }}
        >
          Retry
        </button>
      </div>
    )
  }

  return (
    <div>
      <PageHeader title="Dashboard" />
      <div className="flex flex-col gap-8">
        <NetWorthSection items={netWorth.data?.items ?? []} />
        <AllocationSection items={allocation.data?.items ?? []} />
      </div>
    </div>
  )
}
