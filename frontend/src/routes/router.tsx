import { createBrowserRouter, Navigate } from 'react-router-dom'
import { authRoutes } from './authRoutes'
import { cmsRoutes } from './cmsRoutes'
import { ROUTES } from './paths'
import { publicRoutes } from './publicRoutes'

export const appRouter = createBrowserRouter([
  publicRoutes,
  authRoutes,
  cmsRoutes,
  {
    path: '*',
    element: <Navigate to={ROUTES.HOME} replace />,
  },
])
