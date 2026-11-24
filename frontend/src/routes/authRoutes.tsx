import { Navigate, type RouteObject } from 'react-router-dom'
import { AuthLayout } from '@/components/layout/AuthLayout'
import { LoginPage, RegisterPage } from '@/features/auth'
import { GuestOnlyRoute } from './GuestOnlyRoute'
import { ROUTES } from './paths'

export const authRoutes: RouteObject = {
  path: ROUTES.AUTH.ROOT,
  element: (
    <GuestOnlyRoute>
      <AuthLayout />
    </GuestOnlyRoute>
  ),
  children: [
    {
      index: true,
      element: <Navigate to={ROUTES.AUTH.LOGIN} replace />,
    },
    {
      path: 'login',
      element: <LoginPage />,
    },
    {
      path: 'register',
      element: <RegisterPage />,
    },
    {
      path: '*',
      element: <Navigate to={ROUTES.AUTH.LOGIN} replace />,
    },
  ],
}
