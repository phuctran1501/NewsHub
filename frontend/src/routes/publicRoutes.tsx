import type { RouteObject } from 'react-router-dom'
import { RootLayout } from '@/components/layout/RootLayout'
import { HomePage } from '@/pages/HomePage'
import { ROUTES } from './paths'

export const publicRoutes: RouteObject = {
  element: <RootLayout />,
  children: [
    {
      path: ROUTES.HOME,
      element: <HomePage />,
    },
    {
      path: ROUTES.NOTIFICATIONS,
      element: <HomePage />,
    },
  ],
}
