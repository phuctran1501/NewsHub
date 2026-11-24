import type { RouteObject } from 'react-router-dom'
import { CmsLayout } from '@/components/layout/CmsLayout'
import { CmsScaffoldPage } from '@/pages/CmsScaffoldPage'
import { ROUTES } from './paths'
import { ProtectedRoute } from './ProtectedRoute'

export const cmsRoutes: RouteObject = {
  path: ROUTES.CMS.ROOT,
  element: (
    <ProtectedRoute allowedRoles={['Journalist', 'Admin']}>
      <CmsLayout />
    </ProtectedRoute>
  ),
  children: [
    {
      index: true,
      element: <CmsScaffoldPage />,
    },
    {
      path: 'admin',
      element: (
        <ProtectedRoute allowedRoles={['Admin']}>
          <CmsScaffoldPage />
        </ProtectedRoute>
      ),
    },
    {
      path: 'admin/*',
      element: (
        <ProtectedRoute allowedRoles={['Admin']}>
          <CmsScaffoldPage />
        </ProtectedRoute>
      ),
    },
    {
      path: '*',
      element: <CmsScaffoldPage />,
    },
  ],
}
