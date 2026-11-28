export interface AuthorSummaryDto {
  id: string
  fullName: string
  avatarUrl: string | null
}

export interface CoverImageDto {
  id: string
  url: string
  width: number
  height: number
  format: string
}

export interface CategorySummaryDto {
  id: string
  name: string
  slug: string
}

export interface ArticleSummaryDto {
  id: string
  title: string
  slug: string
  excerpt: string | null
  status: string
  author: AuthorSummaryDto
  companyId: string | null
  companyName?: string | null
  coverImage: CoverImageDto | null
  readingTimeMinutes: number
  viewCount: number
  categories: CategorySummaryDto[]
  publishedAt: string | null
  createdAt: string
}

export interface ArticleDetailDto {
  id: string
  title: string
  slug: string
  contentHtml: string
  status: string
  author: AuthorSummaryDto
  companyId: string | null
  companyName?: string | null
  sourceName: string | null
  sourceUrl: string | null
  coverImage: CoverImageDto | null
  readingTimeMinutes: number
  viewCount: number
  categories: CategorySummaryDto[]
  publishedAt: string | null
  createdAt: string
  updatedAt: string | null
}

export type ArticleSentiment = 'Neutral' | 'Positive' | 'Negative' | 'Mixed'

export interface ArticleAiSummaryDto {
  status: 'Processing' | 'Completed' | string
  bulletPoints: string[] | null
  sentiment: ArticleSentiment | string | null
  extractedTags: string[] | null
  confidenceScore: number | null
  processedAt: string | null
}

export const REPORT_REASON_VALUES = {
  Spam: 1,
  Misinformation: 2,
  Harassment: 3,
  CopyrightViolation: 4,
  Other: 5,
} as const

export type ReportReasonValue = (typeof REPORT_REASON_VALUES)[keyof typeof REPORT_REASON_VALUES]

export interface ReportArticlePayload {
  reason: ReportReasonValue
  details?: string
}

export interface BookmarkItemDto {
  articleId: string
  title: string
  slug: string
  coverImageUrl: string | null
  savedAt: string
  authorName: string | null
  categoryName: string | null
}

export type ArticleSortBy = 'latest' | 'mostread' | 'oldest'

export interface PublishedArticlesQueryParams {
  pageNumber?: number
  pageSize?: number
  categorySlug?: string
  searchTerm?: string
  authorId?: string
  companyId?: string
  sortBy?: ArticleSortBy
}

export type ArticleStatusValue =
  | 'Draft'
  | 'PendingCompanyReview'
  | 'Published'
  | 'Archived'
  | 'Rejected'

export interface MyArticlesQueryParams {
  pageNumber?: number
  pageSize?: number
  status?: ArticleStatusValue
}

export interface CreateArticlePayload {
  title: string
  contentHtml: string
  coverImageId?: string | null
  categoryIds?: string[]
  companyId?: string | null
  sourceName?: string | null
  sourceUrl?: string | null
}

export interface UpdateArticlePayload {
  title?: string
  contentHtml?: string
  coverImageId?: string | null
  categoryIds?: string[]
  sourceName?: string | null
  sourceUrl?: string | null
}

export interface DraftPreviewTokenDto {
  token: string
  previewUrl: string
  expiresAt: string
}

export interface CategorySuggestionDto {
  categoryId: string
  categoryName: string
  slug: string
  confidence: number
  reason: string
}

export interface SuggestCategoriesResponseDto {
  suggestions: CategorySuggestionDto[]
}

