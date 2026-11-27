import { useQuery } from '@tanstack/react-query'
import { queryKeys } from '@/lib/queryKeys'
import { categoryService } from '@/services/categoryService'

export function useCategories() {
  return useQuery({
    queryKey: queryKeys.categories.all(),
    queryFn: ({ signal }) => categoryService.getCategories(signal),
    staleTime: 10 * 60 * 1000,
    gcTime: 30 * 60 * 1000,
  })
}

export function useCategoryBySlug(slug: string | undefined) {
  return useQuery({
    queryKey: queryKeys.categories.bySlug(slug ?? ''),
    queryFn: ({ signal }) => categoryService.getCategoryBySlug(slug ?? '', signal),
    enabled: Boolean(slug),
    staleTime: 5 * 60 * 1000,
  })
}
