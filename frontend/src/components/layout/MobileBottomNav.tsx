import { Bookmark, Home, LayoutGrid, Search, User } from 'lucide-react'
import { NavLink, useLocation, useSearchParams } from 'react-router-dom'
import { useAuth } from '@/hooks/useAuth'
import { ROUTES } from '@/routes/paths'
import { cn } from '@/utils/cn'

interface MobileBottomNavProps {
  isSearchOpen: boolean
  onOpenSearch: () => void
}

export function MobileBottomNav({ isSearchOpen, onOpenSearch }: MobileBottomNavProps) {
  const { isAuthenticated } = useAuth()
  const location = useLocation()
  const [searchParams] = useSearchParams()
  const section = searchParams.get('section')

  const isHomeActive =
    !isSearchOpen &&
    location.pathname === ROUTES.HOME &&
    !section &&
    !searchParams.get('category')

  const isCategoriesActive =
    !isSearchOpen && (section === 'categories' || Boolean(searchParams.get('category')))

  const isBookmarksActive = !isSearchOpen && section === 'bookmarks'

  const isAccountActive =
    !isSearchOpen &&
    (location.pathname.startsWith(ROUTES.AUTH.ROOT) ||
      location.pathname === ROUTES.PROFILE ||
      location.pathname.startsWith(ROUTES.CMS.ROOT))

  return (
    <nav
      aria-label="Điều hướng di động"
      className="fixed inset-x-0 bottom-0 z-40 grid grid-cols-5 h-16 border-t border-neutral-border dark:border-dark-border bg-neutral-surface/95 dark:bg-dark-surface/95 backdrop-blur-md md:hidden"
    >
      <NavLink
        to={ROUTES.HOME}
        className={cn(
          'flex flex-col items-center justify-center gap-1 text-[11px] transition-colors',
          isHomeActive
            ? 'font-semibold text-primary dark:text-secondary'
            : 'font-medium text-neutral-text-muted dark:text-dark-text-muted',
        )}
      >
        <Home size={18} strokeWidth={1.75} />
        <span>Trang chủ</span>
      </NavLink>

      <NavLink
        to="/?section=categories"
        className={cn(
          'flex flex-col items-center justify-center gap-1 text-[11px] transition-colors',
          isCategoriesActive
            ? 'font-semibold text-primary dark:text-secondary'
            : 'font-medium text-neutral-text-muted dark:text-dark-text-muted',
        )}
      >
        <LayoutGrid size={18} strokeWidth={1.75} />
        <span>Chuyên mục</span>
      </NavLink>

      <button
        type="button"
        onClick={onOpenSearch}
        className={cn(
          'flex flex-col items-center justify-center gap-1 text-[11px] transition-colors cursor-pointer',
          isSearchOpen
            ? 'font-semibold text-primary dark:text-secondary'
            : 'font-medium text-neutral-text-muted dark:text-dark-text-muted',
        )}
      >
        <Search size={18} strokeWidth={1.75} />
        <span>Tìm kiếm</span>
      </button>

      <NavLink
        to={isAuthenticated ? '/?section=bookmarks' : ROUTES.AUTH.LOGIN}
        className={cn(
          'flex flex-col items-center justify-center gap-1 text-[11px] transition-colors',
          isBookmarksActive
            ? 'font-semibold text-primary dark:text-secondary'
            : 'font-medium text-neutral-text-muted dark:text-dark-text-muted',
        )}
      >
        <Bookmark size={18} strokeWidth={1.75} />
        <span>Đã lưu</span>
      </NavLink>

      <NavLink
        to={isAuthenticated ? ROUTES.PROFILE : ROUTES.AUTH.LOGIN}
        className={cn(
          'flex flex-col items-center justify-center gap-1 text-[11px] transition-colors',
          isAccountActive
            ? 'font-semibold text-primary dark:text-secondary'
            : 'font-medium text-neutral-text-muted dark:text-dark-text-muted',
        )}
      >
        <User size={18} strokeWidth={1.75} />
        <span>Tài khoản</span>
      </NavLink>
    </nav>
  )
}
