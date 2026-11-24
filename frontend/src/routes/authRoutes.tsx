import { Navigate, type RouteObject } from 'react-router-dom'
import { AuthLayout } from '@/components/layout/AuthLayout'
import { AuthScaffoldPage } from '@/pages/AuthScaffoldPage'
import { ROUTES } from './paths'

export const authRoutes: RouteObject = {
  path: ROUTES.AUTH.ROOT,
  element: <AuthLayout />,
  children: [
    {
      index: true,
      element: <Navigate to={ROUTES.AUTH.LOGIN} replace />,
    },
    {
      path: '*',
      element: <AuthScaffoldPage />,
    },
  ],
}
