import type { ApiResponse } from '../types/api'
import type {
  LoginRequest,
  LoginResponseData,
  RefreshTokenResponseData,
  RegisterJournalistRequest,
  RegisterReaderRequest,
  UserProfile,
} from '../types/auth'
import { apiClient, refreshAccessTokenSingleFlight, unwrapData } from './apiClient'
import { clearAccessToken, setAccessToken } from './tokenStore'

export const authService = {
  async login(payload: LoginRequest): Promise<LoginResponseData> {
    const data = await unwrapData(
      apiClient.post<ApiResponse<LoginResponseData>>('/api/v1/auth/login', payload),
    )
    setAccessToken(data.accessToken)
    return data
  },

  async registerReader(payload: RegisterReaderRequest): Promise<UserProfile> {
    return unwrapData(apiClient.post<ApiResponse<UserProfile>>('/api/v1/auth/register', payload))
  },

  async registerJournalist(payload: RegisterJournalistRequest): Promise<UserProfile> {
    return unwrapData(
      apiClient.post<ApiResponse<UserProfile>>('/api/v1/auth/register-journalist', payload),
    )
  },

  async refreshToken(): Promise<RefreshTokenResponseData> {
    const accessToken = await refreshAccessTokenSingleFlight()
    return { accessToken, tokenType: 'Bearer', expiresIn: 900 }
  },

  async getMyProfile(): Promise<UserProfile> {
    return unwrapData(apiClient.get<ApiResponse<UserProfile>>('/api/v1/account/me'))
  },

  async logout(): Promise<void> {
    try {
      await apiClient.post('/api/v1/auth/logout')
    } finally {
      clearAccessToken()
    }
  },
}
