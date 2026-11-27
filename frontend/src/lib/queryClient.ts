import { QueryClient } from '@tanstack/react-query'
import { AppClientError } from '@/types/api'

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 60 * 1000, // 1 phút mặc định cho các feed tin tức
      gcTime: 10 * 60 * 1000, // Giữ cache 10 phút trong bộ nhớ
      refetchOnWindowFocus: false,
      retry(failureCount, error) {
        if (error instanceof AppClientError) {
          // Không retry các lỗi phía client (4xx) như 400, 401, 403, 404, 422, 429
          if (error.status >= 400 && error.status < 500) {
            return false
          }
        }
        return failureCount < 1
      },
    },
    mutations: {
      retry: false,
    },
  },
})
