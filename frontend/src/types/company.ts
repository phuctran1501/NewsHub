export interface PublicCompanyDto {
  id: string
  name: string
  code: string
  ownerName: string | null
  memberCount: number
  publishedArticleCount: number
  followerCount: number
  createdAt: string
}

export interface FollowedCompanyDto {
  companyId: string
  companyName: string
  companyCode: string
  ownerName: string | null
  memberCount: number
  followedAt: string
}
