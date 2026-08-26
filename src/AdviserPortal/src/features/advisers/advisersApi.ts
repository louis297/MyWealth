import { baseApi } from '../../shared/api/baseApi'
import type { PaginatedList } from '../../shared/types/pagination'
import { toSearchParams } from '../../shared/utils/searchParams'
import type {
  Adviser,
  CreateAdviserRequest,
  CreatedId,
  GetAdvisersArgs,
  UpdateAdviserRequest,
} from './types'

export const advisersApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getAdvisers: builder.query<PaginatedList<Adviser>, GetAdvisersArgs | void>({
      query: (args) => `/advisers${toSearchParams(args ?? {})}`,
      providesTags: (result) =>
        result
          ? [
              ...result.items.map((adviser) => ({ type: 'Adviser' as const, id: adviser.id })),
              { type: 'Adviser', id: 'LIST' },
            ]
          : [{ type: 'Adviser', id: 'LIST' }],
    }),
    getAdviserById: builder.query<Adviser, number>({
      query: (id) => `/advisers/${id}`,
      providesTags: (_result, _error, id) => [{ type: 'Adviser', id }],
    }),
    createAdviser: builder.mutation<CreatedId, CreateAdviserRequest>({
      query: (body) => ({
        url: '/advisers',
        method: 'POST',
        body,
      }),
      invalidatesTags: [{ type: 'Adviser', id: 'LIST' }],
    }),
    updateAdviser: builder.mutation<void, UpdateAdviserRequest>({
      query: ({ id, ...body }) => ({
        url: `/advisers/${id}`,
        method: 'PUT',
        body: { id, ...body },
      }),
      invalidatesTags: (_result, _error, { id }) => [
        { type: 'Adviser', id },
        { type: 'Adviser', id: 'LIST' },
      ],
    }),
    disableAdviser: builder.mutation<void, number>({
      query: (id) => ({
        url: `/advisers/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: (_result, _error, id) => [
        { type: 'Adviser', id },
        { type: 'Adviser', id: 'LIST' },
      ],
    }),
  }),
})

export const {
  useGetAdvisersQuery,
  useGetAdviserByIdQuery,
  useCreateAdviserMutation,
  useUpdateAdviserMutation,
  useDisableAdviserMutation,
} = advisersApi
