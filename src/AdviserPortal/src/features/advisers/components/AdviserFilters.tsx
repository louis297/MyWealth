import { useState, type FormEvent } from 'react'

export type EnabledFilter = 'all' | 'enabled' | 'disabled'

type AdviserFiltersProps = {
  search: string
  enabled: EnabledFilter
  onApply: (next: { search: string; enabled: EnabledFilter }) => void
}

export function AdviserFilters({ search, enabled, onApply }: AdviserFiltersProps) {
  const [draftSearch, setDraftSearch] = useState(search)
  const [draftEnabled, setDraftEnabled] = useState(enabled)

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    onApply({ search: draftSearch.trim(), enabled: draftEnabled })
  }

  return (
    <form
      onSubmit={handleSubmit}
      className="mb-4 flex flex-col gap-3 rounded border border-slate-200 bg-white p-4 md:flex-row md:items-end"
    >
      <label className="flex min-w-0 flex-1 flex-col gap-1 text-sm" htmlFor="adviser-search">
        Search
        <input
          id="adviser-search"
          type="search"
          name="search"
          value={draftSearch}
          placeholder="Name, email or id"
          onChange={(event) => setDraftSearch(event.target.value)}
          className="rounded border border-slate-300 px-3 py-2"
        />
      </label>
      <label className="flex flex-col gap-1 text-sm" htmlFor="adviser-enabled">
        Status
        <select
          id="adviser-enabled"
          name="isEnabled"
          value={draftEnabled}
          onChange={(event) => setDraftEnabled(event.target.value as EnabledFilter)}
          className="rounded border border-slate-300 bg-white px-3 py-2"
        >
          <option value="all">All</option>
          <option value="enabled">Enabled</option>
          <option value="disabled">Disabled</option>
        </select>
      </label>
      <button
        type="submit"
        className="rounded border border-slate-300 bg-white px-3 py-2 text-sm hover:bg-slate-50"
      >
        Apply
      </button>
    </form>
  )
}
