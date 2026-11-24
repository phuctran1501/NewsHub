export const UserRole = {
  User: 0,
  Journalist: 1,
  Admin: 2,
} as const

export type UserRoleValue = (typeof UserRole)[keyof typeof UserRole]
export type UserRoleName = 'User' | 'Journalist' | 'Admin'
export type ActiveWorkspaceRole = UserRoleName | 'CompanyOwner'

export function resolveUserRoleName(role: UserRoleValue | UserRoleName | number | string): UserRoleName {
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
}

export interface RegisterJournalistRequest {
  fullName: string
  email: string
  password: string
  pressCardNumber: string
  bio?: string
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
