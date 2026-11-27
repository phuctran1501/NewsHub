import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { Loading } from '@/components/common/Loading'
import { useAuth } from '@/hooks/useAuth'
import type { UserRoleName } from '@/types/auth'
import { ROUTES } from './paths'

interface ProtectedRouteProps {
  children: ReactNode
  allowedRoles?: UserRoleName[]
}

export function ProtectedRoute({ children, allowedRoles }: ProtectedRouteProps) {
  const { isAuthenticated, isInitializing, roleName } = useAuth()
  const location = useLocation()

  if (isInitializing) {
    return (
      <Loading
        mode="section"
        size="lg"
        label="Đang kiểm tra phiên đăng nhập..."
        className="min-h-[50vh]"
      />
    )
  }

  if (!isAuthenticated) {
    const requestedPath = `${location.pathname}${location.search}`
    const loginTarget =
      requestedPath && requestedPath !== ROUTES.HOME
        ? `${ROUTES.AUTH.LOGIN}?redirect=${encodeURIComponent(requestedPath)}`
        : ROUTES.AUTH.LOGIN

    return <Navigate to={loginTarget} state={{ from: location }} replace />
  }

  if (allowedRoles && (!roleName || !allowedRoles.includes(roleName))) {
    const fallbackPath =
      roleName === 'Journalist' && location.pathname.startsWith(ROUTES.CMS.ADMIN)
        ? ROUTES.CMS.ROOT
        : ROUTES.HOME
    return <Navigate to={fallbackPath} replace />
  }

  return <>{children}</>
}
