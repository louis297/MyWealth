import { createSlice, type PayloadAction } from '@reduxjs/toolkit'
import { clearAuthStorage, getCurrentUser, getToken, setCurrentUser, setToken } from './authStorage'
import type { CurrentUser } from './types'

export type AuthStatus = 'unauthenticated' | 'authenticated'

export type AuthState = {
  token: string | null
  currentUser: CurrentUser | null
  status: AuthStatus
}

type SetCredentialsPayload = {
  token: string
  currentUser: CurrentUser
}

function loadInitialState(): AuthState {
  const token = getToken()
  const currentUser = getCurrentUser()

  if (!token) {
    return {
      token: null,
      currentUser: null,
      status: 'unauthenticated',
    }
  }

  return {
    token,
    currentUser,
    status: 'authenticated',
  }
}

const authSlice = createSlice({
  name: 'auth',
  initialState: loadInitialState(),
  reducers: {
    setCredentials(state, action: PayloadAction<SetCredentialsPayload>) {
      state.token = action.payload.token
      state.currentUser = action.payload.currentUser
      state.status = 'authenticated'
      setToken(action.payload.token)
      setCurrentUser(action.payload.currentUser)
    },
    logout(state) {
      state.token = null
      state.currentUser = null
      state.status = 'unauthenticated'
      clearAuthStorage()
    },
  },
})

export const { setCredentials, logout } = authSlice.actions
export const authReducer = authSlice.reducer

type AuthRoot = { auth: AuthState }

export const selectToken = (state: AuthRoot) => state.auth.token
export const selectCurrentUser = (state: AuthRoot) => state.auth.currentUser
export const selectAuthStatus = (state: AuthRoot) => state.auth.status
export const selectIsAuthenticated = (state: AuthRoot) => Boolean(state.auth.token)
