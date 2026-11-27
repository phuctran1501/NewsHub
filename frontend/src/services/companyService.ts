import type { ApiResponse, PagedApiResponse } from '@/types/api'
import type { FollowedCompanyDto, PublicCompanyDto } from '@/types/company'
import { apiClient, unwrapData, unwrapPaged } from './apiClient'

export const companyService = {
  async getPublicCompanies(
    params?: {
      pageNumber?: number
      pageSize?: number
      searchTerm?: string
    },
    signal?: AbortSignal,
  ): Promise<PagedApiResponse<PublicCompanyDto>> {
    return unwrapPaged(
      apiClient.get<PagedApiResponse<PublicCompanyDto>>('/api/v1/companies/public', {
        signal,
        params: {
          pageNumber: params?.pageNumber ?? 1,
          pageSize: params?.pageSize ?? 6,
          searchTerm: params?.searchTerm || undefined,
        },
      }),
    )
  },

  async getFollowedCompanies(
    params?: { pageNumber?: number; pageSize?: number },
    signal?: AbortSignal,
  ): Promise<PagedApiResponse<FollowedCompanyDto>> {
    return unwrapPaged(
      apiClient.get<PagedApiResponse<FollowedCompanyDto>>('/api/v1/companies/following', {
        signal,
        params: {
          pageNumber: params?.pageNumber ?? 1,
          pageSize: params?.pageSize ?? 50,
        },
      }),
    )
  },

  async followCompany(companyId: string): Promise<string> {
    return unwrapData(
      apiClient.post<ApiResponse<string>>(`/api/v1/companies/${companyId}/follow`),
    )
  },

  async unfollowCompany(companyId: string): Promise<string> {
    return unwrapData(
      apiClient.delete<ApiResponse<string>>(`/api/v1/companies/${companyId}/follow`),
    )
  },
}
