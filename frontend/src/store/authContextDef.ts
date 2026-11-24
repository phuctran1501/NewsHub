import { createContext } from 'react'
import type {
  ActiveWorkspaceRole,
  LoginRequest,
  UserProfile,
  UserRoleName,
} from '../types/auth'

export interface AuthContextValue {
  user: UserProfile | null
  accessToken: string | null
  roleName: UserRoleName | null
  activeWorkspaceRole: ActiveWorkspaceRole | null
  isAuthenticated: boolean
  isInitializing: boolean
  login: (credentials: LoginRequest) => Promise<UserProfile>
  logout: () => Promise<void>
  setActiveWorkspaceRole: (role: ActiveWorkspaceRole) => void
  updateCurrentUser: (user: UserProfile | null) => void
}

export const AuthContext = createContext<AuthContextValue | null>(null)
