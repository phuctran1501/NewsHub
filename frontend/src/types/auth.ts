export const UserRole = {
  User: 0,
  Journalist: 1,
  Admin: 2,
} as const

export type UserRoleValue = (typeof UserRole)[keyof typeof UserRole]
export type UserRoleName = 'User' | 'Journalist' | 'Admin'
export type ActiveWorkspaceRole = UserRoleName | 'CompanyOwner'

export function resolveUserRoleName(
  role: UserRoleValue | UserRoleName | number | string,
): UserRoleName {
  if (role === 2 || role === 'Admin') return 'Admin'
  if (role === 1 || role === 'Journalist') return 'Journalist'
  return 'User'
}

export interface UserProfile {
  id: string
  email: string
  fullName: string
  avatarUrl: string | null
  role: UserRoleValue | UserRoleName
  phoneNumber: string | null
  pressCardNumber: string | null
  bio: string | null
  isActive: boolean
  createdAt: string
  updatedAt: string | null
}

export interface LoginRequest {
  email: string
  password: string
}

export interface RegisterReaderRequest {
  fullName: string
  email: string
  password: string
  phoneNumber?: string | null
}

export interface RegisterJournalistRequest {
  fullName: string
  email: string
  password: string
  phoneNumber?: string | null
  pressCardNumber: string
  bio?: string | null
}

export interface AuthUserResponseDto {
  id: string
  email: string
  fullName: string
  role: UserRoleValue | UserRoleName
  createdAt: string
  phoneNumber?: string | null
}

export interface JournalistResponseDto {
  id: string
  email: string
  fullName: string
  phoneNumber: string | null
  pressCardNumber: string | null
  bio: string | null
  role: UserRoleValue | UserRoleName
  createdAt: string
}

export interface LoginResponseData {
  accessToken: string
  tokenType: string
  expiresIn: number
  user: UserProfile
}

export interface RefreshTokenResponseData {
  accessToken: string
  tokenType: string
  expiresIn: number
}

export interface UpdateProfileRequest {
  fullName?: string | null
  avatarUrl?: string | null
  bio?: string | null
  phoneNumber?: string | null
  pressCardNumber?: string | null
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
  confirmPassword: string
}

export const MediaFolder = {
  Avatar: 'Avatar',
  ArticleCover: 'ArticleCover',
  ArticleBody: 'ArticleBody',
} as const

export type MediaFolderValue = (typeof MediaFolder)[keyof typeof MediaFolder]

export interface MediaFileDto {
  id: string
  publicId: string
  url: string
  originalFileName: string
  format: string
  bytes: number
  width: number
  height: number
  folder: MediaFolderValue | number
  uploadedByUserId: string
  createdAt: string
}
