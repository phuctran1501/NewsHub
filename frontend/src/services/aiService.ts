import type { ApiResponse } from '@/types/api'
import { apiClient, unwrapData } from './apiClient'

export interface SuggestedCategoryItem {
  id: string
  name: string
  slug: string
}

export const aiService = {
  async suggestCategories(payload: {
    title: string
    content: string
  }): Promise<SuggestedCategoryItem[]> {
    return unwrapData(
      apiClient.post<ApiResponse<SuggestedCategoryItem[]>>(
        '/api/v1/ai/suggest-categories',
        payload,
      ),
    )
  },
}
