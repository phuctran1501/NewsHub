import { Bookmark, Home, LayoutGrid, Search, User } from 'lucide-react'
import { NavLink, useLocation, useSearchParams } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth'

export function MobileBottomNav() {
  const { isAuthenticated } = useAuth()
  const location = useLocation()
  const [searchParams] = useSearchParams()
  const section = searchParams.get('section')

  const navItems = [
    {
      label: 'Trang chủ',
      to: '/',
      isActive: location.pathname === '/' && !section && !searchParams.get('category'),
      icon: Home,
    },
    {
      label: 'Chuyên mục',
      to: '/?section=categories',
      isActive: section === 'categories' || Boolean(searchParams.get('category')),
      icon: LayoutGrid,
    },
    {
      label: 'Tìm kiếm',
      to: '/?section=search',
      isActive: section === 'search',
      icon: Search,
    },
    {
      label: 'Đã lưu',
      to: isAuthenticated ? '/?section=bookmarks' : '/auth/login',
      isActive: section === 'bookmarks',
      icon: Bookmark,
    },
    {
      label: 'Tài khoản',
      to: isAuthenticated ? '/cms' : '/auth/login',
      isActive: location.pathname.startsWith('/auth') || location.pathname.startsWith('/cms'),
      icon: User,
    },
  ]

  return (
    <nav
      aria-label="Điều hướng di động"
      className="fixed inset-x-0 bottom-0 z-40 grid grid-cols-5 h-16 border-t border-neutral-border dark:border-dark-border bg-neutral-surface/95 dark:bg-dark-surface/95 backdrop-blur-md md:hidden"
    >
      {navItems.map((item) => {
        const IconComponent = item.icon
        return (
          <NavLink
            key={item.label}
            to={item.to}
            className={`flex flex-col items-center justify-center gap-1 text-[11px] transition-colors ${
              item.isActive
                ? 'font-semibold text-primary dark:text-secondary'
                : 'font-medium text-neutral-text-muted dark:text-dark-text-muted'
            }`}
          >
            <IconComponent size={18} strokeWidth={1.75} />
            <span>{item.label}</span>
          </NavLink>
        )
      })}
    </nav>
  )
}
