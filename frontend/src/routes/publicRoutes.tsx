import type { RouteObject } from 'react-router-dom'
import { RootLayout } from '@/components/layout/RootLayout'
import { ProfilePage } from '@/features/auth'
import { HomePage } from '@/pages/HomePage'
import { ROUTES } from './paths'
import { ProtectedRoute } from './ProtectedRoute'

export const publicRoutes: RouteObject = {
  element: <RootLayout />,
  children: [
    {
      path: ROUTES.HOME,
      element: <HomePage />,
    },
    {
      path: ROUTES.PROFILE,
      element: (
        <ProtectedRoute>
          <ProfilePage />
        </ProtectedRoute>
      ),
    },
    {
      path: ROUTES.NOTIFICATIONS,
      element: (
        <ProtectedRoute>
          <HomePage />
        </ProtectedRoute>
      ),
    },
  ],
}
