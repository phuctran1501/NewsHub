import { createBrowserRouter } from 'react-router-dom'
import { authRoutes } from './authRoutes'
import { cmsRoutes } from './cmsRoutes'
import { publicRoutes } from './publicRoutes'

export const appRouter = createBrowserRouter([
  authRoutes,
  cmsRoutes,
  publicRoutes,
])
