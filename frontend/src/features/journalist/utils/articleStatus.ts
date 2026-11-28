import type { ArticleStatusValue } from '@/types/article'

export function normalizeArticleStatus(raw: string | number): ArticleStatusValue {
  switch (String(raw)) {
    case '2':
    case 'PendingCompanyReview':
      return 'PendingCompanyReview'
    case '3':
    case 'Published':
      return 'Published'
    case '4':
    case 'Archived':
      return 'Archived'
    case '5':
    case 'Rejected':
      return 'Rejected'
    default:
      return 'Draft'
  }
}
