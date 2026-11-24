import type { PagedApiResponse } from '../types/api'
import { apiClient, unwrapPaged } from './apiClient'

export interface ArticleSummaryItem {
  id: string
  title: string
  slug: string
  summary: string | null
  thumbnailUrl: string | null
  readingTimeMinutes: number
  publishedAt: string | null
}

export const articleService = {
  async getPublishedFeed(params?: {
    pageNumber?: number
    pageSize?: number
    categorySlug?: string
  }): Promise<PagedApiResponse<ArticleSummaryItem>> {
    return unwrapPaged(
      apiClient.get<PagedApiResponse<ArticleSummaryItem>>('/api/v1/articles', {
        params: {
          pageNumber: params?.pageNumber ?? 1,
          pageSize: params?.pageSize ?? 10,
          categorySlug: params?.categorySlug,
        },
      }),
    )
  },
}
