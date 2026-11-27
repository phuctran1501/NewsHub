import type { PagedApiResponse } from '@/types/api'
import type { ArticleSummaryDto, PublishedArticlesQueryParams } from '@/types/article'
import { apiClient, unwrapPaged } from './apiClient'

export const articleService = {
  async getPublishedArticles(
    params?: PublishedArticlesQueryParams,
    signal?: AbortSignal,
  ): Promise<PagedApiResponse<ArticleSummaryDto>> {
    return unwrapPaged(
      apiClient.get<PagedApiResponse<ArticleSummaryDto>>('/api/v1/articles', {
        signal,
        params: {
          pageNumber: params?.pageNumber ?? 1,
          pageSize: params?.pageSize ?? 10,
          categorySlug: params?.categorySlug || undefined,
          searchTerm: params?.searchTerm || undefined,
          authorId: params?.authorId || undefined,
          companyId: params?.companyId || undefined,
          sortBy: params?.sortBy ?? 'latest',
        },
      }),
    )
  },
}
