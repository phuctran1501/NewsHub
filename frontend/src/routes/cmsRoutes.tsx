import type { RouteObject } from 'react-router-dom'
import { CmsLayout } from '@/components/layout/CmsLayout'
import { CmsScaffoldPage } from '@/pages/CmsScaffoldPage'
import { ROUTES } from './paths'

export const cmsRoutes: RouteObject = {
  path: ROUTES.CMS.ROOT,
  element: <CmsLayout />,
  children: [
    {
      index: true,
      element: <CmsScaffoldPage />,
    },
    {
      path: '*',
      element: <CmsScaffoldPage />,
    },
  ],
}
