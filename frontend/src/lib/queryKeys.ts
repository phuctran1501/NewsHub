import type { MyArticlesQueryParams, PublishedArticlesQueryParams } from '@/types/article'
import type { SearchMode } from '@/types/search'

export const queryKeys = {
  account: {
    me: () => ['account', 'me'] as const,
  },
  categories: {
    all: () => ['categories', 'all'] as const,
    bySlug: (slug: string) => ['categories', 'slug', slug] as const,
  },
  articles: {
    all: () => ['articles'] as const,
    published: (params: PublishedArticlesQueryParams) =>
      ['articles', 'published', params] as const,
    myArticles: (params: MyArticlesQueryParams) => ['articles', 'my-articles', params] as const,
    detail: (slug: string) => ['articles', 'detail', slug] as const,
    editDetail: (id: string) => ['articles', 'edit-detail', id] as const,
    aiSummary: (articleId: string) => ['articles', 'ai-summary', articleId] as const,
    related: (articleId: string, limit = 4) => ['articles', 'related', articleId, limit] as const,
    preview: (token: string) => ['articles', 'preview', token] as const,
  },
  bookmarks: {
    all: () => ['bookmarks'] as const,
    list: (pageNumber = 1, pageSize = 50) => ['bookmarks', 'list', { pageNumber, pageSize }] as const,
  },
  search: {
    all: () => ['search'] as const,
    ai: (q: string, limit: number) => ['search', 'ai', { q, limit }] as const,
    keyword: (params: {
      q: string
      pageNumber: number
      pageSize: number
      sortBy: string
      mode: SearchMode
    }) => ['search', 'keyword', params] as const,
  },
  companies: {
    all: () => ['companies'] as const,
    publicList: (params: { pageNumber?: number; pageSize?: number; searchTerm?: string }) =>
      ['companies', 'public', params] as const,
    following: (pageNumber = 1, pageSize = 50) =>
      ['companies', 'following', { pageNumber, pageSize }] as const,
    myCompanies: () => ['companies', 'my-companies'] as const,
    search: (params: { searchTerm?: string; pageNumber?: number; pageSize?: number }) =>
      ['companies', 'search', params] as const,
    myInvitations: () => ['companies', 'my-invitations'] as const,
    internalArticles: (companyId: string, params: { pageNumber?: number; pageSize?: number }) =>
      ['companies', 'internal-articles', companyId, params] as const,
  },
  media: {
    myMedia: (params?: { pageNumber?: number; pageSize?: number; folder?: string }) =>
      ['media', 'my-media', params ?? {}] as const,
  },
  utility: {
    weather: () => ['utility', 'weather', 'vn-8-cities'] as const,
    marketTicker: () => ['utility', 'market-ticker'] as const,
  },
} as const
