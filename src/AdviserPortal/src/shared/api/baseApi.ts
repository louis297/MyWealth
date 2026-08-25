import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react'
import type { BaseQueryFn, FetchArgs, FetchBaseQueryError } from '@reduxjs/toolkit/query'
import { logout } from '../../features/auth/authSlice'
import { getApiBaseUrl } from './baseUrl'

const rawBaseQuery = fetchBaseQuery({
  baseUrl: getApiBaseUrl(),
  prepareHeaders: (headers, { getState }) => {
    const token = (getState() as { auth?: { token?: string | null } }).auth?.token
    if (token) {
      headers.set('Authorization', `Bearer ${token}`)
    }
    return headers
  },
})

function isLoginRequest(args: string | FetchArgs): boolean {
  const url = typeof args === 'string' ? args : args.url
  const method = typeof args === 'string' ? 'GET' : (args.method ?? 'GET')
  return method.toUpperCase() === 'POST' && url.replace(/^\//, '') === 'auth/login'
}

const baseQueryWithAuth: BaseQueryFn<string | FetchArgs, unknown, FetchBaseQueryError> = async (
  args,
  api,
  extraOptions,
) => {
  const result = await rawBaseQuery(args, api, extraOptions)

  if (result.error?.status === 401 && !isLoginRequest(args)) {
    api.dispatch(logout())
    api.dispatch(baseApi.util.resetApiState())
    if (window.location.pathname !== '/login') {
      window.location.assign('/login')
    }
  }

  return result
}

export const baseApi = createApi({
  reducerPath: 'api',
  baseQuery: baseQueryWithAuth,
  tagTypes: ['Customer', 'Adviser', 'Account'],
  endpoints: () => ({}),
})
