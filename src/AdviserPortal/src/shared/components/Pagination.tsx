type PaginationProps = {
  pageNumber: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
  onPageChange: (page: number) => void
}

export function Pagination({
  pageNumber,
  totalPages,
  hasPreviousPage,
  hasNextPage,
  onPageChange,
}: PaginationProps) {
  if (totalPages <= 1) {
    return null
  }

  return (
    <nav aria-label="Pagination" className="mt-4 flex items-center justify-between gap-3 text-sm">
      <p className="text-slate-600">
        Page {pageNumber} of {totalPages}
      </p>
      <div className="flex gap-2">
        <button
          type="button"
          disabled={!hasPreviousPage}
          className="rounded border border-slate-300 bg-white px-3 py-1.5 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
          onClick={() => onPageChange(pageNumber - 1)}
        >
          Previous
        </button>
        <button
          type="button"
          disabled={!hasNextPage}
          className="rounded border border-slate-300 bg-white px-3 py-1.5 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
          onClick={() => onPageChange(pageNumber + 1)}
        >
          Next
        </button>
      </div>
    </nav>
  )
}
