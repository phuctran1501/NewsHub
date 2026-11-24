import type { ReactNode } from 'react'
import { Navigate, useSearchParams } from 'react-router-dom'
import { Spinner } from '@/components/common/Spinner'
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
    return (
      <div className="min-h-screen flex items-center justify-center bg-neutral-bg dark:bg-dark-bg">
        <Spinner size="lg" label="Đang kiểm tra phiên đăng nhập" />
      </div>
    )
  }

  if (isAuthenticated) {
    const safeRedirect = getSafeRedirectPath(searchParams.get('redirect'), ROUTES.HOME)
    return <Navigate to={safeRedirect} replace />
  }

  return <>{children}</>
}
