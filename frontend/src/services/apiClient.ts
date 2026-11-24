import axios, {
  AxiosError,
  type AxiosInstance,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
} from 'axios'
import { env } from '../config/env'
import {
  AppClientError,
  type ApiErrorResponse,
  type ApiResponse,
  type PagedApiResponse,
} from '../types/api'
import type { RefreshTokenResponseData } from '../types/auth'
import { getAccessToken, notifyAuthFailure, setAccessToken } from './tokenStore'

interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  _retry?: boolean
}

interface QueuedRequest {
  resolve: (token: string) => void
  reject: (error: unknown) => void
}

const AUTH_BYPASS_PATHS = [
  '/api/v1/auth/login',
  '/api/v1/auth/refresh',
  '/api/v1/auth/register',
  '/api/v1/auth/register-journalist',
  '/api/v1/auth/logout',
]

let refreshPromise: Promise<string> | null = null
let failedQueue: QueuedRequest[] = []

function flushQueue(error: unknown, token: string | null = null): void {
  const currentQueue = failedQueue
  failedQueue = []

  currentQueue.forEach(({ resolve, reject }) => {
    if (error || !token) {
      reject(error)
    } else {
      resolve(token)
    }
  })
}

function isBypassedAuthEndpoint(url?: string): boolean {
  if (!url) return false
  return AUTH_BYPASS_PATHS.some((path) => url.includes(path))
}

export function toAppClientError(error: unknown): AppClientError {
  if (error instanceof AppClientError) {
    return error
  }

  if (axios.isAxiosError(error)) {
    const axiosErr = error as AxiosError<ApiErrorResponse>
    const status = axiosErr.response?.status ?? 0
    const payload = axiosErr.response?.data

    if (payload?.error) {
      return new AppClientError({
        status,
        code: payload.error.code || 'API_ERROR',
        message: payload.error.message || 'Yêu cầu không thành công.',
        details: payload.error.details ?? [],
        correlationId: payload.correlationId,
      })
    }

    return new AppClientError({
      status,
      code: status === 0 ? 'NETWORK_ERROR' : `HTTP_${status}`,
      message:
        status === 0
          ? 'Không thể kết nối tới máy chủ. Vui lòng kiểm tra kết nối mạng.'
          : axiosErr.message || 'Đã xảy ra lỗi ngoài dự kiến.',
    })
  }

  return new AppClientError({
    status: 500,
    code: 'UNKNOWN_ERROR',
    message: error instanceof Error ? error.message : 'Đã xảy ra lỗi không xác định.',
  })
}

/**
 * Thực thi xoay vòng Refresh Token đơn luồng (Single-Flight Mutex) qua HttpOnly Cookie.
 * Đảm bảo nhiều lời gọi đồng thời chỉ phát sinh duy nhất 1 HTTP POST /api/v1/auth/refresh.
 */
export async function refreshAccessTokenSingleFlight(): Promise<string> {
  if (refreshPromise) {
    return refreshPromise
  }

  refreshPromise = axios
    .post<ApiResponse<RefreshTokenResponseData>>(
      `${env.apiBaseUrl}/api/v1/auth/refresh`,
      {},
      {
        withCredentials: true,
        headers: { 'Content-Type': 'application/json' },
      },
    )
    .then((response) => {
      const nextToken = response.data.data.accessToken
      setAccessToken(nextToken)
      flushQueue(null, nextToken)
      return nextToken
    })
    .catch((err: unknown) => {
      const normalized = toAppClientError(err)
      flushQueue(normalized, null)
      notifyAuthFailure()
      throw normalized
    })
    .finally(() => {
      refreshPromise = null
    })

  return refreshPromise
}

export const apiClient: AxiosInstance = axios.create({
  baseURL: env.apiBaseUrl,
  withCredentials: true,
  timeout: 20000,
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json',
  },
})

apiClient.interceptors.request.use((config) => {
  const token = getAccessToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiErrorResponse>) => {
    const originalRequest = error.config as RetryableRequestConfig | undefined

    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest._retry &&
      !isBypassedAuthEndpoint(originalRequest.url)
    ) {
      originalRequest._retry = true

      if (refreshPromise) {
        return new Promise<string>((resolve, reject) => {
          failedQueue.push({ resolve, reject })
        }).then((token) => {
          originalRequest.headers.Authorization = `Bearer ${token}`
          return apiClient(originalRequest)
        })
      }

      const freshToken = await refreshAccessTokenSingleFlight()
      originalRequest.headers.Authorization = `Bearer ${freshToken}`
      return apiClient(originalRequest)
    }

    return Promise.reject(toAppClientError(error))
  },
)

export async function unwrapData<T>(request: Promise<AxiosResponse<ApiResponse<T>>>): Promise<T> {
  const response = await request
  return response.data.data
}

export async function unwrapPaged<T>(
  request: Promise<AxiosResponse<PagedApiResponse<T>>>,
): Promise<PagedApiResponse<T>> {
  const response = await request
  return response.data
}
