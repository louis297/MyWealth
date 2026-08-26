import { baseApi } from '../../shared/api/baseApi'
import type {
  ChangePasswordRequest,
  CurrentUser,
  LoginRequest,
  LoginResult,
  UpdateCurrentUserRequest,
} from './types'

export const authApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    login: builder.mutation<LoginResult, LoginRequest>({
      query: (body) => ({
        url: '/auth/login',
        method: 'POST',
        body,
      }),
    }),
    logout: builder.mutation<void, void>({
      query: () => ({
        url: '/auth/logout',
        method: 'POST',
      }),
    }),
    getCurrentUser: builder.query<CurrentUser, void>({
      query: () => '/users/me',
      providesTags: ['CurrentUser'],
    }),
    updateCurrentUser: builder.mutation<void, UpdateCurrentUserRequest>({
      query: (body) => ({
        url: '/users/me',
        method: 'PUT',
        body,
      }),
      invalidatesTags: ['CurrentUser'],
    }),
    changePassword: builder.mutation<void, ChangePasswordRequest>({
      query: (body) => ({
        url: '/users/me/password',
        method: 'PUT',
        body,
      }),
    }),
  }),
})

export const {
  useLoginMutation,
  useLogoutMutation,
  useGetCurrentUserQuery,
  useLazyGetCurrentUserQuery,
  useUpdateCurrentUserMutation,
  useChangePasswordMutation,
} = authApi
