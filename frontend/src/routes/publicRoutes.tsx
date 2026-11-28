import type { RouteObject } from 'react-router-dom'
import { RootLayout } from '@/components/layout/RootLayout'
import { ArticleDetailPage, DraftPreviewPage } from '@/features/articles'
import { ProfilePage } from '@/features/auth'
import { CategoryPage } from '@/features/categories'
import { SearchPage } from '@/features/search'
import { HomePage } from '@/pages/HomePage'
import { NotFoundPage } from '@/pages/NotFoundPage'
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
      path: ROUTES.CATEGORY,
      element: <CategoryPage />,
    },
    {
      path: ROUTES.SEARCH,
      element: <SearchPage />,
    },
    {
      path: ROUTES.ARTICLE_PREVIEW,
      element: <DraftPreviewPage />,
    },
    {
      path: ROUTES.ARTICLE_DETAIL,
      element: <ArticleDetailPage />,
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
    {
      path: '*',
      element: <NotFoundPage />,
    },
  ],
}
