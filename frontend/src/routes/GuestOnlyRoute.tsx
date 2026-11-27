import type { ReactNode } from 'react'
import { Navigate, useSearchParams } from 'react-router-dom'
import { Loading } from '@/components/common/Loading'
import { useAuth } from '@/hooks/useAuth'
import { getSafeRedirectPath } from '@/utils/validation'
import { ROUTES } from './paths'

interface GuestOnlyRouteProps {
  children: ReactNode
}

export function GuestOnlyRoute({ children }: GuestOnlyRouteProps) {
  const { isAuthenticated, isInitializing } = useAuth()
  const [searchParams] = useSearchParams()

  if (isInitializing) {
    return <Loading mode="fullscreen" size="lg" label="Đang kiểm tra phiên đăng nhập..." />
  }

  if (isAuthenticated) {
    const safeRedirect = getSafeRedirectPath(searchParams.get('redirect'), ROUTES.HOME)
    return <Navigate to={safeRedirect} replace />
  }

  return <>{children}</>
}
