import type { ApiResponse } from '@/types/api'
import type {
  AuthUserResponseDto,
  JournalistResponseDto,
  LoginRequest,
  LoginResponseData,
  RefreshTokenResponseData,
  RegisterJournalistRequest,
  RegisterReaderRequest,
} from '@/types/auth'
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

  async registerReader(payload: RegisterReaderRequest): Promise<AuthUserResponseDto> {
    return unwrapData(
      apiClient.post<ApiResponse<AuthUserResponseDto>>('/api/v1/auth/register', payload),
    )
  },

  async registerJournalist(payload: RegisterJournalistRequest): Promise<JournalistResponseDto> {
    return unwrapData(
      apiClient.post<ApiResponse<JournalistResponseDto>>(
        '/api/v1/auth/register-journalist',
        payload,
      ),
    )
  },

  async refreshToken(): Promise<RefreshTokenResponseData> {
    return refreshAccessTokenSingleFlight()
  },

  async logout(): Promise<void> {
    try {
      await apiClient.post('/api/v1/auth/logout')
    } finally {
      clearAccessToken()
    }
  },
}
