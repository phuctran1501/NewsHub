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
  coverImage: CoverImageDto | null
  readingTimeMinutes: number
  viewCount: number
  categories: CategorySummaryDto[]
  publishedAt: string | null
  createdAt: string
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
