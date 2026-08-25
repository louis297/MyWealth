import { createSlice, type PayloadAction } from '@reduxjs/toolkit'
import { clearToken, getToken, setToken as persistToken } from './authStorage'
import type { CurrentUser } from './types'

export type AuthStatus = 'unauthenticated' | 'loading' | 'authenticated'

export type AuthState = {
  token: string | null
  currentUser: CurrentUser | null
  status: AuthStatus
}

function loadInitialState(): AuthState {
  const token = getToken()

  if (!token) {
    return {
      token: null,
      currentUser: null,
      status: 'unauthenticated',
    }
  }

  return {
    token,
    currentUser: null,
    status: 'loading',
  }
}

const authSlice = createSlice({
  name: 'auth',
  initialState: loadInitialState(),
  reducers: {
    setToken(state, action: PayloadAction<string>) {
      state.token = action.payload
      state.status = 'loading'
      persistToken(action.payload)
    },
    setCurrentUser(state, action: PayloadAction<CurrentUser>) {
      state.currentUser = action.payload
      state.status = 'authenticated'
    },
    logout(state) {
      state.token = null
      state.currentUser = null
      state.status = 'unauthenticated'
      clearToken()
    },
  },
})

export const { setToken: setAuthToken, setCurrentUser, logout } = authSlice.actions
export const authReducer = authSlice.reducer

type AuthRoot = { auth: AuthState }

export const selectToken = (state: AuthRoot) => state.auth.token
export const selectCurrentUser = (state: AuthRoot) => state.auth.currentUser
export const selectAuthStatus = (state: AuthRoot) => state.auth.status
export const selectIsAuthenticated = (state: AuthRoot) => state.auth.status === 'authenticated'
