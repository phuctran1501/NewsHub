import { motion } from 'framer-motion'
import { NavLink, useLocation } from 'react-router-dom'
import { useCategories } from '@/features/categories/hooks/useCategories'
import { buildCategoryPath, ROUTES } from '@/routes/paths'
import { cn } from '@/utils/cn'

export interface NavCategoryItem {
  label: string
  to: string
  categorySlug?: string
}

const FALLBACK_NAV_ITEMS: NavCategoryItem[] = [
  { label: 'Trang chủ', to: ROUTES.HOME },
  { label: 'Thời sự', to: buildCategoryPath('thoi-su'), categorySlug: 'thoi-su' },
  { label: 'Kinh tế', to: buildCategoryPath('kinh-te'), categorySlug: 'kinh-te' },
  { label: 'Công nghệ', to: buildCategoryPath('cong-nghe'), categorySlug: 'cong-nghe' },
  { label: 'Thế giới', to: buildCategoryPath('the-gioi'), categorySlug: 'the-gioi' },
]

interface NavigationBarProps {
  items?: NavCategoryItem[]
  className?: string
}

export function NavigationBar({ items, className }: NavigationBarProps) {
  const location = useLocation()
  const { data: categories } = useCategories()

  const navItems: NavCategoryItem[] =
    items ??
    (categories && categories.length > 0
      ? [
          { label: 'Trang chủ', to: ROUTES.HOME },
          ...categories.slice(0, 5).map((cat) => ({
            label: cat.name,
            to: buildCategoryPath(cat.slug),
            categorySlug: cat.slug,
          })),
        ]
      : FALLBACK_NAV_ITEMS)

  return (
    <nav aria-label="Danh mục báo chí" className={cn('hidden lg:flex items-center gap-7', className)}>
      {navItems.map((item) => {
        const isItemActive = item.categorySlug
          ? location.pathname === buildCategoryPath(item.categorySlug)
          : location.pathname === ROUTES.HOME

        return (
          <NavLink
            key={item.to}
            to={item.to}
            className={() =>
              cn(
                'relative py-5 text-sm transition-colors duration-150',
                isItemActive
                  ? 'font-semibold text-primary dark:text-secondary'
                  : 'font-medium text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main',
              )
            }
          >
            {item.label}
            {isItemActive && (
              <motion.span
                layoutId="public-nav-active-indicator"
                transition={{ type: 'spring', stiffness: 450, damping: 38 }}
                aria-hidden="true"
                className="absolute inset-x-0 bottom-0 h-0.5 bg-primary dark:bg-secondary"
              />
            )}
          </NavLink>
        )
      })}
    </nav>
  )
}
