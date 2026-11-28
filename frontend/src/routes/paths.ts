import type { SearchMode } from '@/types/search'

export const ROUTES = {
  HOME: '/',
  CATEGORY: '/categories/:slug',
  SEARCH: '/search',
  ARTICLE_PREVIEW: '/articles/preview',
  ARTICLE_DETAIL: '/articles/:slug',
  PROFILE: '/me/profile',
  NOTIFICATIONS: '/me/notifications',
  AUTH: {
    ROOT: '/auth',
    LOGIN: '/auth/login',
    REGISTER: '/auth/register',
  },
  CMS: {
    ROOT: '/cms',
    ARTICLES: '/cms/articles',
    EDITOR: '/cms/editor',
    EDITOR_EDIT: '/cms/editor/:id',
    COMPANIES: '/cms/companies',
    REVIEW: '/cms/review',
    ADMIN: '/cms/admin',
  },
} as const

export function buildCmsEditorPath(articleId?: string): string {
  return articleId ? `/cms/editor/${encodeURIComponent(articleId)}` : ROUTES.CMS.EDITOR
}

export function buildCategoryPath(slug: string): string {
  return `/categories/${encodeURIComponent(slug)}`
}

export function buildArticleDetailPath(slug: string): string {
  return `/articles/${encodeURIComponent(slug)}`
}

export function buildDraftPreviewPath(token: string): string {
  return `${ROUTES.ARTICLE_PREVIEW}?token=${encodeURIComponent(token)}`
}

export function buildSearchPath(query?: string, mode: SearchMode = 'ai'): string {
  const params = new URLSearchParams()
  const clean = query?.trim()
  if (clean) {
    params.set('q', clean)
  }
  if (mode !== 'ai') {
    params.set('mode', mode)
  }
  const qs = params.toString()
  return qs ? `${ROUTES.SEARCH}?${qs}` : ROUTES.SEARCH
}
