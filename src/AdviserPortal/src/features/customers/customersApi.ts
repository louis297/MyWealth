import { baseApi } from '../../shared/api/baseApi'
import type { PaginatedList } from '../../shared/types/pagination'
import { toSearchParams } from '../../shared/utils/searchParams'
import type {
  AccountSummary,
  AdviserLookup,
  CreateCustomerRequest,
  CreatedId,
  Customer,
  GetAccountsArgs,
  GetAdvisersArgs,
  GetCustomersArgs,
  UpdateCustomerRequest,
} from './types'

export const customersApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getCustomers: builder.query<PaginatedList<Customer>, GetCustomersArgs | void>({
      query: (args) => `/customers${toSearchParams(args ?? {})}`,
      providesTags: (result) =>
        result
          ? [
              ...result.items.map((customer) => ({ type: 'Customer' as const, id: customer.id })),
              { type: 'Customer', id: 'LIST' },
            ]
          : [{ type: 'Customer', id: 'LIST' }],
    }),
    getCustomerById: builder.query<Customer, number>({
      query: (id) => `/customers/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Customer', id }],
    }),
    createCustomer: builder.mutation<CreatedId, CreateCustomerRequest>({
      query: (body) => ({
        url: '/customers',
        method: 'POST',
        body,
      }),
      invalidatesTags: [{ type: 'Customer', id: 'LIST' }],
    }),
    updateCustomer: builder.mutation<void, UpdateCustomerRequest>({
      query: ({ id, ...body }) => ({
        url: `/customers/${id}`,
        method: 'PUT',
        body: { id, ...body },
      }),
      invalidatesTags: (_result, _error, { id }) => [
        { type: 'Customer', id },
        { type: 'Customer', id: 'LIST' },
      ],
    }),
    disableCustomer: builder.mutation<void, number>({
      query: (id) => ({
        url: `/customers/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: (_result, _error, id) => [
        { type: 'Customer', id },
        { type: 'Customer', id: 'LIST' },
      ],
    }),
    getAdvisers: builder.query<PaginatedList<AdviserLookup>, GetAdvisersArgs | void>({
      query: (args) => `/advisers${toSearchParams(args ?? {})}`,
      providesTags: [{ type: 'Adviser', id: 'LIST' }],
    }),
    getAccounts: builder.query<PaginatedList<AccountSummary>, GetAccountsArgs | void>({
      query: (args) => `/accounts${toSearchParams(args ?? {})}`,
      providesTags: (_result, _error, args) => [
        {
          type: 'Account',
          id: args?.customerId !== undefined ? `CUSTOMER-${args.customerId}` : 'LIST',
        },
      ],
    }),
  }),
})

export const {
  useGetCustomersQuery,
  useGetCustomerByIdQuery,
  useCreateCustomerMutation,
  useUpdateCustomerMutation,
  useDisableCustomerMutation,
  useGetAdvisersQuery,
  useGetAccountsQuery,
} = customersApi
