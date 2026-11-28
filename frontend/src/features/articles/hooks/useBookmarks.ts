import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useMemo } from 'react'
import { toast } from '@/components/ui/toastManager'
import { useAuth } from '@/hooks/useAuth'
import { queryKeys } from '@/lib/queryKeys'
import { toAppClientError } from '@/services/apiClient'
import { bookmarkService } from '@/services/bookmarkService'
import type { PagedApiResponse } from '@/types/api'
import type { BookmarkItemDto } from '@/types/article'

export function useBookmarkedArticleIds() {
  const { isAuthenticated } = useAuth()

  const query = useQuery({
    queryKey: queryKeys.bookmarks.list(1, 50),
    queryFn: ({ signal }) => bookmarkService.getMyBookmarks(1, 50, signal),
    enabled: isAuthenticated,
    staleTime: 2 * 60 * 1000,
  })

  const bookmarkedIds = useMemo(() => {
    const items = query.data?.data ?? []
    return new Set(items.map((item) => item.articleId))
  }, [query.data])

  return {
    bookmarkedIds,
    isLoading: query.isLoading,
  }
}

interface ToggleBookmarkVariables {
  articleId: string
  articleTitle: string
  articleSlug: string
  isCurrentlyBookmarked: boolean
}

export function useToggleBookmark() {
  const queryClient = useQueryClient()
  const listKey = queryKeys.bookmarks.list(1, 50)

  return useMutation({
    mutationFn: async ({ articleId, isCurrentlyBookmarked }: ToggleBookmarkVariables) => {
      if (isCurrentlyBookmarked) {
        return bookmarkService.removeBookmark(articleId)
      }
      return bookmarkService.addBookmark(articleId)
    },
    onMutate: async (variables) => {
      await queryClient.cancelQueries({ queryKey: listKey })
      const previous = queryClient.getQueryData<PagedApiResponse<BookmarkItemDto>>(listKey)

      queryClient.setQueryData<PagedApiResponse<BookmarkItemDto>>(listKey, (current) => {
        if (!current) return current
        if (variables.isCurrentlyBookmarked) {
          return {
            ...current,
            data: current.data.filter((item) => item.articleId !== variables.articleId),
          }
        }
        const alreadyExists = current.data.some((item) => item.articleId === variables.articleId)
        if (alreadyExists) return current
        const optimisticItem: BookmarkItemDto = {
          articleId: variables.articleId,
          title: variables.articleTitle,
          slug: variables.articleSlug,
          coverImageUrl: null,
          savedAt: new Date().toISOString(),
          authorName: null,
          categoryName: null,
        }
        return {
          ...current,
          data: [optimisticItem, ...current.data],
        }
      })

      return { previous }
    },
    onSuccess: (_data, variables) => {
      if (variables.isCurrentlyBookmarked) {
        toast.info('Đã bỏ lưu bài viết', variables.articleTitle)
      } else {
        toast.success('Đã lưu bài viết', 'Bài viết đã được thêm vào danh sách yêu thích của bạn.')
      }
    },
    onError: (error, _variables, context) => {
      if (context?.previous) {
        queryClient.setQueryData(listKey, context.previous)
      }
      const info = toAppClientError(error)
      toast.error('Không thể cập nhật bài đã lưu', info.message)
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.bookmarks.all() })
    },
  })
}
