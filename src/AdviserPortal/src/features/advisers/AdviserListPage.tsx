import { useState } from 'react'
import { Link } from 'react-router-dom'
import { PageHeader } from '../../shared/components/PageHeader'
import { Pagination } from '../../shared/components/Pagination'
import { useGetAdvisersQuery } from './advisersApi'
import { AdviserFilters, type EnabledFilter } from './components/AdviserFilters'
import { AdviserTable } from './components/AdviserTable'

const PAGE_SIZE = 20

function isEnabledArg(enabled: EnabledFilter): boolean | undefined {
  if (enabled === 'enabled') {
    return true
  }

  if (enabled === 'disabled') {
    return false
  }

  return undefined
}

export function AdviserListPage() {
  const [page, setPage] = useState(1)
  const [search, setSearch] = useState('')
  const [enabled, setEnabled] = useState<EnabledFilter>('all')
  const list = useGetAdvisersQuery({
    page,
    pageSize: PAGE_SIZE,
    search: search || undefined,
    isEnabled: isEnabledArg(enabled),
  })
  const items = list.data?.items ?? []
  const hasActiveFilters = search !== '' || enabled !== 'all'

  return (
    <div>
      <PageHeader title="Advisers">
        <Link
          to="/advisers/new"
          className="rounded bg-slate-800 px-3 py-2 text-sm text-white hover:bg-slate-700"
        >
          New adviser
        </Link>
      </PageHeader>

      <AdviserFilters
        search={search}
        enabled={enabled}
        onApply={(next) => {
          setSearch(next.search)
          setEnabled(next.enabled)
          setPage(1)
        }}
      />

      {list.isLoading ? (
        <p className="text-slate-600" aria-busy="true">
          Loading advisers…
        </p>
      ) : null}

      {list.isError ? (
        <div>
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
              void list.refetch()
            }}
          >
            Retry
          </button>
        </div>
      ) : null}

      {list.isSuccess && items.length === 0 ? (
        <div className="rounded border border-slate-200 bg-white p-6 text-slate-600">
          <p>{hasActiveFilters ? 'No advisers match these filters.' : 'No advisers yet'}</p>
          {!hasActiveFilters ? (
            <Link
              to="/advisers/new"
              className="mt-3 inline-block text-sm text-slate-800 underline hover:no-underline"
            >
              Create an adviser
            </Link>
          ) : null}
        </div>
      ) : null}

      {list.isSuccess && items.length > 0 ? (
        <>
          <AdviserTable advisers={items} />
          <Pagination
            pageNumber={list.data.pageNumber}
            totalPages={list.data.totalPages}
            hasPreviousPage={list.data.hasPreviousPage}
            hasNextPage={list.data.hasNextPage}
            onPageChange={setPage}
          />
        </>
      ) : null}
    </div>
  )
}
