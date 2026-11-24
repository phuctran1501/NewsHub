import type { PagedApiResponse } from '@/types/api'
import { apiClient, unwrapPaged } from './apiClient'

export interface CompanyDirectoryItem {
  id: string
  code: string
  name: string
  logoUrl: string | null
  description: string | null
}

export const companyService = {
  async getPublicCompanies(params?: {
    pageNumber?: number
    pageSize?: number
    search?: string
  }): Promise<PagedApiResponse<CompanyDirectoryItem>> {
    return unwrapPaged(
      apiClient.get<PagedApiResponse<CompanyDirectoryItem>>('/api/v1/companies', {
        params: {
          pageNumber: params?.pageNumber ?? 1,
          pageSize: params?.pageSize ?? 10,
          search: params?.search,
        },
      }),
    )
  },
}
