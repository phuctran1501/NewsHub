export interface CategoryResponseDto {
  id: string
  name: string
  slug: string
  description: string | null
  articleCount: number
  createdAt: string
}
