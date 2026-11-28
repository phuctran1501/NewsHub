import type { ApiResponse, PagedApiResponse } from '@/types/api'
import type {
  ArticleAiSummaryDto,
  ArticleDetailDto,
  ArticleSummaryDto,
  CreateArticlePayload,
  DraftPreviewTokenDto,
  MyArticlesQueryParams,
  PublishedArticlesQueryParams,
  ReportArticlePayload,
  SuggestCategoriesResponseDto,
  UpdateArticlePayload,
} from '@/types/article'
import { apiClient, unwrapData, unwrapPaged } from './apiClient'

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

  async getArticleBySlug(slug: string, signal?: AbortSignal): Promise<ArticleDetailDto> {
    return unwrapData(
      apiClient.get<ApiResponse<ArticleDetailDto>>(
        `/api/v1/articles/${encodeURIComponent(slug)}`,
        { signal },
      ),
    )
  },

  async getRelatedArticles(
    articleId: string,
    limit = 4,
    signal?: AbortSignal,
  ): Promise<ArticleSummaryDto[]> {
    return unwrapData(
      apiClient.get<ApiResponse<ArticleSummaryDto[]>>(
        `/api/v1/articles/${encodeURIComponent(articleId)}/related`,
        {
          signal,
          params: { limit },
        },
      ),
    )
  },

  async getDraftPreviewByToken(token: string, signal?: AbortSignal): Promise<ArticleDetailDto> {
    return unwrapData(
      apiClient.get<ApiResponse<ArticleDetailDto>>('/api/v1/articles/preview', {
        signal,
        params: { token },
      }),
    )
  },

  async getArticleAiSummary(
    articleId: string,
    signal?: AbortSignal,
  ): Promise<ArticleAiSummaryDto> {
    return unwrapData(
      apiClient.get<ApiResponse<ArticleAiSummaryDto>>(
        `/api/v1/articles/${encodeURIComponent(articleId)}/ai-summary`,
        { signal },
      ),
    )
  },

  async generateArticleAiSummaryOnDemand(
    articleId: string,
    signal?: AbortSignal,
  ): Promise<ArticleAiSummaryDto> {
    return unwrapData(
      apiClient.post<ApiResponse<ArticleAiSummaryDto>>(
        `/api/v1/articles/${encodeURIComponent(articleId)}/ai-summary/generate`,
        undefined,
        { signal },
      ),
    )
  },

  async regenerateArticleAiSummary(
    articleId: string,
  ): Promise<{ message: string; articleId: string }> {
    return unwrapData(
      apiClient.post<ApiResponse<{ message: string; articleId: string }>>(
        `/api/v1/articles/${encodeURIComponent(articleId)}/ai-summary/regenerate`,
      ),
    )
  },

  async reportArticle(
    articleId: string,
    payload: ReportArticlePayload,
  ): Promise<{ message: string }> {
    return unwrapData(
      apiClient.post<ApiResponse<{ message: string }>>(
        `/api/v1/articles/${encodeURIComponent(articleId)}/report`,
        payload,
      ),
    )
  },

  async getMyArticles(
    params?: MyArticlesQueryParams,
    signal?: AbortSignal,
  ): Promise<PagedApiResponse<ArticleSummaryDto>> {
    return unwrapPaged(
      apiClient.get<PagedApiResponse<ArticleSummaryDto>>('/api/v1/articles/my-articles', {
        signal,
        params: {
          pageNumber: params?.pageNumber ?? 1,
          pageSize: params?.pageSize ?? 10,
          status: params?.status || undefined,
        },
      }),
    )
  },

  async createArticle(payload: CreateArticlePayload): Promise<ArticleDetailDto> {
    return unwrapData(apiClient.post<ApiResponse<ArticleDetailDto>>('/api/v1/articles', payload))
  },

  async updateArticle(
    articleId: string,
    payload: UpdateArticlePayload,
  ): Promise<ArticleDetailDto> {
    return unwrapData(
      apiClient.patch<ApiResponse<ArticleDetailDto>>(
        `/api/v1/articles/${encodeURIComponent(articleId)}`,
        payload,
      ),
    )
  },

  async publishArticle(articleId: string): Promise<ArticleDetailDto> {
    return unwrapData(
      apiClient.post<ApiResponse<ArticleDetailDto>>(
        `/api/v1/articles/${encodeURIComponent(articleId)}/publish`,
      ),
    )
  },

  async submitArticleToCompany(articleId: string): Promise<string> {
    return unwrapData(
      apiClient.post<ApiResponse<string>>(
        `/api/v1/articles/${encodeURIComponent(articleId)}/submit-to-company`,
      ),
    )
  },

  async archiveArticle(articleId: string): Promise<ArticleDetailDto> {
    return unwrapData(
      apiClient.post<ApiResponse<ArticleDetailDto>>(
        `/api/v1/articles/${encodeURIComponent(articleId)}/archive`,
      ),
    )
  },

  async deleteArticle(articleId: string): Promise<{ message: string }> {
    return unwrapData(
      apiClient.delete<ApiResponse<{ message: string }>>(
        `/api/v1/articles/${encodeURIComponent(articleId)}`,
      ),
    )
  },

  async generateDraftPreviewToken(articleId: string): Promise<DraftPreviewTokenDto> {
    return unwrapData(
      apiClient.post<ApiResponse<DraftPreviewTokenDto>>(
        `/api/v1/articles/${encodeURIComponent(articleId)}/preview-token`,
      ),
    )
  },

  async suggestCategories(payload: {
    title: string
    contentSnippet: string
  }): Promise<SuggestCategoriesResponseDto> {
    return unwrapData(
      apiClient.post<ApiResponse<SuggestCategoriesResponseDto>>(
        '/api/v1/ai/categorize',
        payload,
      ),
    )
  },
}
