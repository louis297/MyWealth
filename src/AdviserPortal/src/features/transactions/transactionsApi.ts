import { baseApi } from '../../shared/api/baseApi'
import type { PaginatedList } from '../../shared/types/pagination'
import { toSearchParams } from '../../shared/utils/searchParams'
import type { CreatedId, CreateTransactionRequest, GetTransactionsArgs, Transaction } from './types'

export const transactionsApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getTransactions: builder.query<PaginatedList<Transaction>, GetTransactionsArgs | void>({
      query: (args) => `/transactions${toSearchParams(args ?? {})}`,
      providesTags: (result, _error, args) => [
        { type: 'Transaction', id: 'LIST' },
        ...(args?.accountId !== undefined
          ? [{ type: 'Transaction' as const, id: `ACCOUNT-${args.accountId}` }]
          : []),
        ...(result?.items.map((transaction) => ({
          type: 'Transaction' as const,
          id: transaction.id,
        })) ?? []),
      ],
    }),
    createTransaction: builder.mutation<CreatedId, CreateTransactionRequest>({
      query: (body) => ({
        url: '/transactions',
        method: 'POST',
        body,
      }),
      invalidatesTags: (_result, _error, { accountId }) => [
        { type: 'Transaction', id: 'LIST' },
        { type: 'Transaction', id: `ACCOUNT-${accountId}` },
        { type: 'Holding', id: `ACCOUNT-${accountId}` },
      ],
    }),
  }),
})

export const { useGetTransactionsQuery, useCreateTransactionMutation } = transactionsApi
