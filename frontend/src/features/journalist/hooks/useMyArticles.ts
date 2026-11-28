import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from '@/components/ui/toastManager'
import { queryKeys } from '@/lib/queryKeys'
import { toAppClientError } from '@/services/apiClient'
import { articleService } from '@/services/articleService'
import type {
  CreateArticlePayload,
  MyArticlesQueryParams,
  UpdateArticlePayload,
} from '@/types/article'

export function useMyArticles(params: MyArticlesQueryParams) {
  return useQuery({
    queryKey: queryKeys.articles.myArticles(params),
    queryFn: ({ signal }) => articleService.getMyArticles(params, signal),
    staleTime: 1000 * 30,
  })
}

export function useArticleForEdit(articleId: string | undefined) {
  return useQuery({
    queryKey: queryKeys.articles.editDetail(articleId ?? ''),
    queryFn: ({ signal }) => articleService.getArticleBySlug(articleId ?? '', signal),
    enabled: Boolean(articleId),
    staleTime: 0,
  })
}

export function useCreateArticle() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (payload: CreateArticlePayload) => articleService.createArticle(payload),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.articles.all() })
    },
    onError: (error) => {
      toast.error('Không thể tạo bài viết', toAppClientError(error).message)
    },
  })
}

export function useUpdateArticle() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ articleId, payload }: { articleId: string; payload: UpdateArticlePayload }) =>
      articleService.updateArticle(articleId, payload),
    onSuccess: (updated) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.articles.all() })
      queryClient.setQueryData(queryKeys.articles.editDetail(updated.id), updated)
    },
    onError: (error) => {
      toast.error('Không thể cập nhật bài viết', toAppClientError(error).message)
    },
  })
}

export function usePublishArticle() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (articleId: string) => articleService.publishArticle(articleId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.articles.all() })
      toast.success('Đã xuất bản bài viết thành công')
    },
    onError: (error) => {
      toast.error('Không thể xuất bản bài viết', toAppClientError(error).message)
    },
  })
}

export function useSubmitArticleToCompany() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (articleId: string) => articleService.submitArticleToCompany(articleId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.articles.all() })
      toast.success('Đã nộp bài viết lên Tòa soạn chờ duyệt')
    },
    onError: (error) => {
      toast.error('Không thể nộp bài lên Tòa soạn', toAppClientError(error).message)
    },
  })
}

export function useArchiveArticle() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (articleId: string) => articleService.archiveArticle(articleId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.articles.all() })
      toast.success('Đã chuyển bài viết vào mục lưu trữ')
    },
    onError: (error) => {
      toast.error('Không thể lưu trữ bài viết', toAppClientError(error).message)
    },
  })
}

export function useDeleteArticle() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (articleId: string) => articleService.deleteArticle(articleId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.articles.all() })
      toast.success('Đã xóa bài viết')
    },
    onError: (error) => {
      toast.error('Không thể xóa bài viết', toAppClientError(error).message)
    },
  })
}

export function useGenerateDraftPreviewToken() {
  return useMutation({
    mutationFn: (articleId: string) => articleService.generateDraftPreviewToken(articleId),
    onError: (error) => {
      toast.error('Không thể tạo liên kết xem trước', toAppClientError(error).message)
    },
  })
}

export function useSuggestCategories() {
  return useMutation({
    mutationFn: (payload: { title: string; contentSnippet: string }) =>
      articleService.suggestCategories(payload),
    onError: (error) => {
      toast.error('Không thể gợi ý chuyên mục bằng AI', toAppClientError(error).message)
    },
  })
}
