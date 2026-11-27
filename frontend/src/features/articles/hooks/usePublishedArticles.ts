import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { queryKeys } from '@/lib/queryKeys'
import { articleService } from '@/services/articleService'
import type { PublishedArticlesQueryParams } from '@/types/article'

export function usePublishedArticles(
  params: PublishedArticlesQueryParams,
  options?: { enabled?: boolean; staleTime?: number },
) {
  return useQuery({
    queryKey: queryKeys.articles.published(params),
    queryFn: ({ signal }) => articleService.getPublishedArticles(params, signal),
    placeholderData: keepPreviousData,
    enabled: options?.enabled ?? true,
    staleTime: options?.staleTime ?? 60 * 1000,
  })
}
