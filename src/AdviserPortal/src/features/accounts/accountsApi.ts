import { baseApi } from '../../shared/api/baseApi'
import type { PaginatedList } from '../../shared/types/pagination'
import { toSearchParams } from '../../shared/utils/searchParams'
import type {
  Account,
  CreateAccountRequest,
  CreateHoldingRequest,
  CreatedId,
  DeleteHoldingRequest,
  GetAccountsArgs,
  Holding,
  UpdateAccountRequest,
  UpdateHoldingRequest,
} from './types'

export const accountsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getAccounts: builder.query<PaginatedList<Account>, GetAccountsArgs | void>({
      query: (args) => `/accounts${toSearchParams(args ?? {})}`,
      providesTags: (result, _error, args) => [
        { type: 'Account', id: 'LIST' },
        ...(args?.customerId !== undefined
          ? [{ type: 'Account' as const, id: `CUSTOMER-${args.customerId}` }]
          : []),
        ...(result?.items.map((account) => ({ type: 'Account' as const, id: account.id })) ?? []),
      ],
    }),
    getAccountById: builder.query<Account, number>({
      query: (id) => `/accounts/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Account', id }],
    }),
    createAccount: builder.mutation<CreatedId, CreateAccountRequest>({
      query: (body) => ({
        url: '/accounts',
        method: 'POST',
        body,
      }),
      invalidatesTags: (_result, _error, { customerId }) => [
        { type: 'Account', id: 'LIST' },
        { type: 'Account', id: `CUSTOMER-${customerId}` },
      ],
    }),
    updateAccount: builder.mutation<void, UpdateAccountRequest>({
      query: ({ id, ...body }) => ({
        url: `/accounts/${id}`,
        method: 'PUT',
        body: { id, ...body },
      }),
      invalidatesTags: (_result, _error, { id }) => [
        { type: 'Account', id },
        { type: 'Account', id: 'LIST' },
      ],
    }),
    closeAccount: builder.mutation<void, number>({
      query: (id) => ({
        url: `/accounts/${id}/close`,
        method: 'POST',
      }),
      invalidatesTags: (_result, _error, id) => [
        { type: 'Account', id },
        { type: 'Account', id: 'LIST' },
      ],
    }),
    getHoldingsByAccount: builder.query<Holding[], number>({
      query: (accountId) => `/accounts/${accountId}/holdings`,
      providesTags: (result, _error, accountId) => [
        { type: 'Holding', id: `ACCOUNT-${accountId}` },
        ...(result?.map((holding) => ({ type: 'Holding' as const, id: holding.id })) ?? []),
      ],
    }),
    createHolding: builder.mutation<CreatedId, CreateHoldingRequest>({
      query: ({ accountId, ...body }) => ({
        url: `/accounts/${accountId}/holdings`,
        method: 'POST',
        body,
      }),
      invalidatesTags: (_result, _error, { accountId }) => [
        { type: 'Holding', id: `ACCOUNT-${accountId}` },
      ],
    }),
    updateHolding: builder.mutation<void, UpdateHoldingRequest>({
      query: ({ accountId, id, ...body }) => ({
        url: `/accounts/${accountId}/holdings/${id}`,
        method: 'PUT',
        body,
      }),
      invalidatesTags: (_result, _error, { accountId, id }) => [
        { type: 'Holding', id },
        { type: 'Holding', id: `ACCOUNT-${accountId}` },
      ],
    }),
    deleteHolding: builder.mutation<void, DeleteHoldingRequest>({
      query: ({ accountId, id }) => ({
        url: `/accounts/${accountId}/holdings/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: (_result, _error, { accountId, id }) => [
        { type: 'Holding', id },
        { type: 'Holding', id: `ACCOUNT-${accountId}` },
      ],
    }),
  }),
})

export const {
  useGetAccountsQuery,
  useGetAccountByIdQuery,
  useCreateAccountMutation,
  useUpdateAccountMutation,
  useCloseAccountMutation,
  useGetHoldingsByAccountQuery,
  useCreateHoldingMutation,
  useUpdateHoldingMutation,
  useDeleteHoldingMutation,
} = accountsApi
