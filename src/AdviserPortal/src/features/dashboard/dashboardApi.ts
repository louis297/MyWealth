import { baseApi } from '../../shared/api/baseApi'
import type { AssetAllocationVm, NetWorthVm } from './types'

export const dashboardApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getNetWorth: builder.query<NetWorthVm, void>({
      query: () => '/dashboard/net-worth',
    }),
    getAssetAllocation: builder.query<AssetAllocationVm, void>({
      query: () => '/dashboard/allocation',
    }),
  }),
})

export const { useGetNetWorthQuery, useGetAssetAllocationQuery } = dashboardApi
