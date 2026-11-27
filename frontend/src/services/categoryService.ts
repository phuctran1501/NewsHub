import type { ApiResponse } from '@/types/api'
import type { CategoryResponseDto } from '@/types/category'
import { apiClient, unwrapData } from './apiClient'

export const categoryService = {
  async getCategories(signal?: AbortSignal): Promise<CategoryResponseDto[]> {
    return unwrapData(
      apiClient.get<ApiResponse<CategoryResponseDto[]>>('/api/v1/categories', { signal }),
    )
  },

  async getCategoryBySlug(slug: string, signal?: AbortSignal): Promise<CategoryResponseDto> {
    return unwrapData(
      apiClient.get<ApiResponse<CategoryResponseDto>>(
        `/api/v1/categories/${encodeURIComponent(slug)}`,
        { signal },
      ),
    )
  },
}
