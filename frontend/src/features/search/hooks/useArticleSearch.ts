import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { queryKeys } from '@/lib/queryKeys'
import { aiService } from '@/services/aiService'
import { articleService } from '@/services/articleService'
import type { ArticleSortBy } from '@/types/article'

export function useAiSemanticSearch(query: string, limit = 10, enabled = true) {
  const trimmed = query.trim()

  return useQuery({
    queryKey: queryKeys.search.ai(trimmed, limit),
    queryFn: ({ signal }) => aiService.searchByNlp({ q: trimmed, limit }, signal),
    enabled: enabled && trimmed.length > 0,
    staleTime: 2 * 60 * 1000,
  })
}

export function useKeywordSearch(
  params: {
    query: string
    pageNumber: number
    pageSize: number
    sortBy: ArticleSortBy
  },
  enabled = true,
) {
  const trimmed = params.query.trim()

  return useQuery({
    queryKey: queryKeys.search.keyword({
      q: trimmed,
      pageNumber: params.pageNumber,
      pageSize: params.pageSize,
      sortBy: params.sortBy,
      mode: 'keyword',
    }),
    queryFn: ({ signal }) =>
      articleService.getPublishedArticles(
        {
          searchTerm: trimmed,
          pageNumber: params.pageNumber,
          pageSize: params.pageSize,
          sortBy: params.sortBy,
        },
        signal,
      ),
    placeholderData: keepPreviousData,
    enabled: enabled && trimmed.length > 0,
    staleTime: 60 * 1000,
  })
}
