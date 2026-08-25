import { Outlet } from 'react-router-dom'

export function AuthLayout() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-slate-100 p-6 text-slate-900">
      <div className="w-full max-w-sm">
        <p className="mb-4 text-center text-lg font-semibold">MyWealth</p>
        <Outlet />
      </div>
    </div>
  )
}
