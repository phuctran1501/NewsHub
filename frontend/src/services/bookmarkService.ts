import type { ApiResponse, PagedApiResponse } from '@/types/api'
import type { BookmarkItemDto } from '@/types/article'
import { apiClient, unwrapData, unwrapPaged } from './apiClient'

export const bookmarkService = {
  async getMyBookmarks(
    pageNumber = 1,
    pageSize = 50,
    signal?: AbortSignal,
  ): Promise<PagedApiResponse<BookmarkItemDto>> {
    return unwrapPaged(
      apiClient.get<PagedApiResponse<BookmarkItemDto>>('/api/v1/me/bookmarks', {
        signal,
        params: { pageNumber, pageSize },
      }),
    )
  },

  async addBookmark(articleId: string): Promise<{ message: string }> {
    return unwrapData(
      apiClient.post<ApiResponse<{ message: string }>>(
        `/api/v1/me/bookmarks/${encodeURIComponent(articleId)}`,
      ),
    )
  },

  async removeBookmark(articleId: string): Promise<{ message: string }> {
    return unwrapData(
      apiClient.delete<ApiResponse<{ message: string }>>(
        `/api/v1/me/bookmarks/${encodeURIComponent(articleId)}`,
      ),
    )
  },
}
