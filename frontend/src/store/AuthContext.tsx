import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { authService } from '../services/authService'
import { clearAccessToken, getAccessToken, onAuthFailure } from '../services/tokenStore'
import {
  resolveUserRoleName,
  type ActiveWorkspaceRole,
  type LoginRequest,
  type UserProfile,
} from '../types/auth'
import { AuthContext } from './authContextDef'

const SESSION_FLAG_KEY = 'newshub-has-session'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(null)
  const [accessToken, setTokenState] = useState<string | null>(() => getAccessToken())
  const [activeWorkspaceRole, setActiveWorkspaceRole] = useState<ActiveWorkspaceRole | null>(null)
  const [isInitializing, setIsInitializing] = useState<boolean>(() => {
    if (typeof window === 'undefined') return false
    return window.localStorage.getItem(SESSION_FLAG_KEY) === '1'
  })

  const clearSessionState = useCallback(() => {
    clearAccessToken()
    setTokenState(null)
    setUser(null)
    setActiveWorkspaceRole(null)
    window.localStorage.removeItem(SESSION_FLAG_KEY)
  }, [])

  useEffect(() => {
    return onAuthFailure(() => {
      clearSessionState()
    })
  }, [clearSessionState])

  useEffect(() => {
    if (window.localStorage.getItem(SESSION_FLAG_KEY) !== '1') {
      return
    }

    let cancelled = false

    async function bootstrapSession() {
      try {
        const refreshed = await authService.refreshToken()
        const profile = await authService.getMyProfile()
        if (!cancelled) {
          setTokenState(refreshed.accessToken)
          setUser(profile)
          setActiveWorkspaceRole(resolveUserRoleName(profile.role))
        }
      } catch {
        if (!cancelled) {
          clearSessionState()
        }
      } finally {
        if (!cancelled) {
          setIsInitializing(false)
        }
      }
    }

    void bootstrapSession()

    return () => {
      cancelled = true
    }
  }, [clearSessionState])

  const login = useCallback(async (credentials: LoginRequest): Promise<UserProfile> => {
    const result = await authService.login(credentials)
    window.localStorage.setItem(SESSION_FLAG_KEY, '1')
    setTokenState(result.accessToken)
    setUser(result.user)
    setActiveWorkspaceRole(resolveUserRoleName(result.user.role))
    return result.user
  }, [])

  const logout = useCallback(async (): Promise<void> => {
    try {
      await authService.logout()
    } finally {
      clearSessionState()
    }
  }, [clearSessionState])

  const roleName = user ? resolveUserRoleName(user.role) : null

  const value = useMemo(
    () => ({
      user,
      accessToken,
      roleName,
      activeWorkspaceRole,
      isAuthenticated: Boolean(user && accessToken),
      isInitializing,
      login,
      logout,
      setActiveWorkspaceRole,
      updateCurrentUser: setUser,
    }),
    [user, accessToken, roleName, activeWorkspaceRole, isInitializing, login, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
