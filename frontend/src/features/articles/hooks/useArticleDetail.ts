import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from '@/components/ui/toastManager'
import { queryKeys } from '@/lib/queryKeys'
import { toAppClientError } from '@/services/apiClient'
import { articleService } from '@/services/articleService'
import type { ReportArticlePayload } from '@/types/article'

export function useArticleBySlug(slug: string | undefined) {
  return useQuery({
    queryKey: queryKeys.articles.detail(slug ?? ''),
    queryFn: ({ signal }) => articleService.getArticleBySlug(slug ?? '', signal),
    enabled: Boolean(slug),
    staleTime: 2 * 60 * 1000,
    retry: (failureCount, error) => {
      const info = toAppClientError(error)
      if (info.status === 404 || info.status === 403 || info.status === 401) {
        return false
      }
      return failureCount < 1
    },
  })
}

export function useArticleAiSummary(articleId: string | undefined, enabled = true) {
  return useQuery({
    queryKey: queryKeys.articles.aiSummary(articleId ?? ''),
    queryFn: ({ signal }) => articleService.generateArticleAiSummaryOnDemand(articleId ?? '', signal),
    enabled: Boolean(articleId) && enabled,
    staleTime: 5 * 60 * 1000,
    refetchInterval: (query) => {
      return query.state.data?.status === 'Processing' ? 6000 : false
    },
    retry: false,
  })
}

export function useRelatedArticles(articleId: string | undefined, limit = 4) {
  return useQuery({
    queryKey: queryKeys.articles.related(articleId ?? '', limit),
    queryFn: ({ signal }) => articleService.getRelatedArticles(articleId ?? '', limit, signal),
    enabled: Boolean(articleId),
    staleTime: 5 * 60 * 1000,
    retry: 1,
  })
}

export function useDraftPreview(token: string | null) {
  return useQuery({
    queryKey: queryKeys.articles.preview(token ?? ''),
    queryFn: ({ signal }) => articleService.getDraftPreviewByToken(token ?? '', signal),
    enabled: Boolean(token && token.trim().length > 0),
    staleTime: 0,
    gcTime: 0,
    retry: false,
  })
}

export function useRegenerateAiSummary(articleId: string) {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: () => articleService.regenerateArticleAiSummary(articleId),
    onSuccess: (res) => {
      toast.success(
        'Đã tiếp nhận yêu cầu AI',
        res.message || 'Hệ thống đang tạo lại bản tóm tắt AI trong nền.',
      )
      queryClient.setQueryData(queryKeys.articles.aiSummary(articleId), {
        status: 'Processing',
        bulletPoints: null,
        sentiment: null,
        extractedTags: null,
        confidenceScore: null,
        processedAt: null,
      })
      void queryClient.invalidateQueries({ queryKey: queryKeys.articles.aiSummary(articleId) })
    },
    onError: (error) => {
      const info = toAppClientError(error)
      if (info.status === 429) {
        toast.warning('Giới hạn tần suất AI', 'Bạn chỉ có thể tạo lại tối đa 5 lần mỗi phút.')
        return
      }
      toast.error('Không thể tạo lại tóm tắt AI', info.message)
    },
  })
}

export function useReportArticle(articleId: string) {
  return useMutation({
    mutationFn: (payload: ReportArticlePayload) => articleService.reportArticle(articleId, payload),
    onSuccess: (res) => {
      toast.success(
        'Đã gửi báo cáo vi phạm',
        res.message || 'Ban biên tập sẽ xem xét và kiểm duyệt nội dung này.',
      )
    },
    onError: (error) => {
      const info = toAppClientError(error)
      toast.error('Không thể gửi báo cáo', info.message)
    },
  })
}
