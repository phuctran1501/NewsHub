export const ROUTES = {
  HOME: '/',
  PROFILE: '/me/profile',
  NOTIFICATIONS: '/me/notifications',
  AUTH: {
    ROOT: '/auth',
    LOGIN: '/auth/login',
    REGISTER: '/auth/register',
  },
  CMS: {
    ROOT: '/cms',
    ARTICLES: '/cms/articles',
    EDITOR: '/cms/editor',
    REVIEW: '/cms/review',
    ADMIN: '/cms/admin',
  },
} as const
