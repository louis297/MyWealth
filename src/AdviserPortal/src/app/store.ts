import { configureStore } from '@reduxjs/toolkit'

export const store = configureStore({
  reducer: {
    // add new slice here
    // auth: authReducer,
    // dashboard: dashboardReducer,
  },
})

// predict type from store
export type RootState = ReturnType<typeof store.getState>
export type AppDispatch = typeof store.dispatch
export type AppStore = typeof store