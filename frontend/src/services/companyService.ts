import type { ApiResponse, PagedApiResponse } from '@/types/api'
import type {
  CompanyRequestDto,
  CompanySummaryDto,
  EditorialArticleDto,
  FollowedCompanyDto,
  MyCompanyDto,
  PublicCompanyDto,
} from '@/types/company'
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

  async getMyCompanies(signal?: AbortSignal): Promise<MyCompanyDto[]> {
    return unwrapData(
      apiClient.get<ApiResponse<MyCompanyDto[]>>('/api/v1/companies/my-companies', { signal }),
    )
  },

  async searchCompanies(
    params?: {
      searchTerm?: string
      pageNumber?: number
      pageSize?: number
    },
    signal?: AbortSignal,
  ): Promise<PagedApiResponse<CompanySummaryDto>> {
    return unwrapPaged(
      apiClient.get<PagedApiResponse<CompanySummaryDto>>('/api/v1/companies/search', {
        signal,
        params: {
          searchTerm: params?.searchTerm?.trim() || undefined,
          pageNumber: params?.pageNumber ?? 1,
          pageSize: params?.pageSize ?? 10,
        },
      }),
    )
  },

  async requestJoinCompany(companyId: string): Promise<CompanyRequestDto> {
    return unwrapData(
      apiClient.post<ApiResponse<CompanyRequestDto>>(
        `/api/v1/companies/${encodeURIComponent(companyId)}/join-requests`,
      ),
    )
  },

  async getMyInvitations(signal?: AbortSignal): Promise<CompanyRequestDto[]> {
    return unwrapData(
      apiClient.get<ApiResponse<CompanyRequestDto[]>>('/api/v1/companies/my-invitations', {
        signal,
      }),
    )
  },

  async respondToInvitation(requestId: string, accept: boolean): Promise<string> {
    return unwrapData(
      apiClient.post<ApiResponse<string>>(
        `/api/v1/companies/invitations/${encodeURIComponent(requestId)}/respond`,
        { accept },
      ),
    )
  },

  async leaveCompany(companyId: string): Promise<string> {
    return unwrapData(
      apiClient.post<ApiResponse<string>>(
        `/api/v1/companies/${encodeURIComponent(companyId)}/leave`,
      ),
    )
  },

  async getCompanyInternalArticles(
    companyId: string,
    params?: { pageNumber?: number; pageSize?: number },
    signal?: AbortSignal,
  ): Promise<PagedApiResponse<EditorialArticleDto>> {
    return unwrapPaged(
      apiClient.get<PagedApiResponse<EditorialArticleDto>>(
        `/api/v1/companies/${encodeURIComponent(companyId)}/articles`,
        {
          signal,
          params: {
            pageNumber: params?.pageNumber ?? 1,
            pageSize: params?.pageSize ?? 10,
          },
        },
      ),
    )
  },
}
