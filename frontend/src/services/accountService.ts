import type { ApiResponse } from '@/types/api'
import type { ChangePasswordRequest, UpdateProfileRequest, UserProfile } from '@/types/auth'
import { apiClient, unwrapData } from './apiClient'

export const accountService = {
  async getMyProfile(): Promise<UserProfile> {
    return unwrapData(apiClient.get<ApiResponse<UserProfile>>('/api/v1/account/me'))
  },

  async updateProfile(payload: UpdateProfileRequest): Promise<UserProfile> {
    return unwrapData(apiClient.patch<ApiResponse<UserProfile>>('/api/v1/account/me', payload))
  },

  async changePassword(payload: ChangePasswordRequest): Promise<void> {
    await apiClient.post<ApiResponse<unknown>>('/api/v1/account/change-password', payload)
  },
}
