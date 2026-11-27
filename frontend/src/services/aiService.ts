import type { ApiResponse } from '@/types/api'
import type { SemanticSearchResultDto } from '@/types/search'
import { apiClient, unwrapData } from './apiClient'

export interface SuggestedCategoryItem {
  id: string
  name: string
  slug: string
}

export const aiService = {
  async searchByNlp(
    params: { q: string; limit?: number },
    signal?: AbortSignal,
  ): Promise<SemanticSearchResultDto[]> {
    return unwrapData(
      apiClient.get<ApiResponse<SemanticSearchResultDto[]>>('/api/v1/ai/search', {
        signal,
        params: {
          q: params.q,
          limit: params.limit ?? 10,
        },
      }),
    )
  },

  async suggestCategories(payload: {
    title: string
    contentSnippet: string
  }): Promise<SuggestedCategoryItem[]> {
    return unwrapData(
      apiClient.post<ApiResponse<SuggestedCategoryItem[]>>('/api/v1/ai/categorize', payload),
    )
  },
}
