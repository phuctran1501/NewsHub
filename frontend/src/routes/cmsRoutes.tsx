import type { RouteObject } from 'react-router-dom'
import { CmsLayout } from '@/components/layout/CmsLayout'
import { CompanyHubPage, JournalistOverviewPage, MyArticlesPage } from '@/features/journalist'
import { LazyArticleEditorPage } from '@/features/journalist/pages/LazyArticleEditorPage'
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
      element: <JournalistOverviewPage />,
    },
    {
      path: 'articles',
      element: <MyArticlesPage />,
    },
    {
      path: 'editor',
      element: <LazyArticleEditorPage />,
    },
    {
      path: 'editor/:id',
      element: <LazyArticleEditorPage />,
    },
    {
      path: 'companies',
      element: <CompanyHubPage />,
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
