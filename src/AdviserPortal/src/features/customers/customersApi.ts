import { baseApi } from '../../shared/api/baseApi'
import type { PaginatedList } from '../../shared/types/pagination'
import { toSearchParams } from '../../shared/utils/searchParams'
import type {
  CreateCustomerRequest,
  CreatedId,
  Customer,
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
  }),
})

export const {
  useGetCustomersQuery,
  useGetCustomerByIdQuery,
  useCreateCustomerMutation,
  useUpdateCustomerMutation,
  useDisableCustomerMutation,
} = customersApi
