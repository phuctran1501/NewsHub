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

export type CompanyMemberRoleValue = 0 | 1 | 'Member' | 'Owner'

export function isCompanyOwnerRole(role: CompanyMemberRoleValue | number | string): boolean {
  return role === 1 || role === 'Owner'
}

export interface MyCompanyDto {
  id: string
  name: string
  code: string
  taxCode: string
  role: CompanyMemberRoleValue
  joinedAt: string
}

export interface CompanySummaryDto {
  id: string
  name: string
  code: string
  taxCode: string
  ownerName: string | null
  memberCount: number
  publishedArticlesCount: number
  createdAt: string
}

export interface CompanyRequestDto {
  id: string
  companyId: string
  companyName: string
  companyCode: string
  journalistId: string
  journalistName: string
  journalistEmail: string
  pressCardNumber: string | null
  type: number | string
  status: number | string
  createdAt: string
  respondedAt: string | null
}

export interface EditorialArticleDto {
  id: string
  title: string
  slug: string
  authorId: string
  authorName: string
  authorAvatarUrl: string | null
  status: number | string
  readingTimeMinutes: number
  createdAt: string
  updatedAt: string | null
  publishedAt: string | null
}

