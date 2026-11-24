import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { Spinner } from '@/components/common/Spinner'
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
      <div className="min-h-[50vh] flex items-center justify-center">
        <Spinner size="lg" label="Đang kiểm tra phiên đăng nhập" />
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to={ROUTES.AUTH.LOGIN} state={{ from: location }} replace />
  }

  if (allowedRoles && (!roleName || !allowedRoles.includes(roleName))) {
    return <Navigate to={ROUTES.HOME} replace />
  }

  return <>{children}</>
}
