export type SearchMode = 'ai' | 'keyword'

export interface SemanticSearchResultDto {
  articleId: string
  title: string
  slug: string
  similarityScore: number
  publishedAt: string | null
}
