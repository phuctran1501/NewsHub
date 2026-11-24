import type { ApiResponse } from '../types/api'
import { apiClient, unwrapData } from './apiClient'

export interface SystemAnalyticsOverview {
  totalUsers: number
  totalArticles: number
  totalCompanies: number
  pendingReportsCount: number
}

export const adminService = {
  async getAnalyticsOverview(): Promise<SystemAnalyticsOverview> {
    return unwrapData(
      apiClient.get<ApiResponse<SystemAnalyticsOverview>>('/api/v1/admin/analytics/overview'),
    )
  },
}
