import { NavLink, useSearchParams } from 'react-router-dom'

export interface NavCategoryItem {
  label: string
  to: string
  categorySlug?: string
}

const DEFAULT_NAV_ITEMS: NavCategoryItem[] = [
  { label: 'Trang chủ', to: '/' },
  { label: 'Thời sự', to: '/?category=thoi-su', categorySlug: 'thoi-su' },
  { label: 'Kinh tế', to: '/?category=kinh-te', categorySlug: 'kinh-te' },
  { label: 'Công nghệ', to: '/?category=cong-nghe', categorySlug: 'cong-nghe' },
  { label: 'Thế giới', to: '/?category=the-gioi', categorySlug: 'the-gioi' },
  { label: 'Tòa soạn', to: '/?section=companies', categorySlug: 'companies' },
]

interface NavigationBarProps {
  items?: NavCategoryItem[]
  className?: string
}

export function NavigationBar({
  items = DEFAULT_NAV_ITEMS,
  className = '',
}: NavigationBarProps) {
  const [searchParams] = useSearchParams()
  const currentCategory = searchParams.get('category') ?? searchParams.get('section')

  return (
    <nav aria-label="Danh mục báo chí" className={`hidden lg:flex items-center gap-7 ${className}`}>
      {items.map((item) => {
        const isItemActive = item.categorySlug
          ? currentCategory === item.categorySlug
          : !currentCategory

        return (
          <NavLink
            key={item.label}
            to={item.to}
            className={() =>
              `relative py-5 text-sm transition-colors duration-150 ${
                isItemActive
                  ? 'font-semibold text-primary dark:text-secondary'
                  : 'font-medium text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main'
              }`
            }
          >
            {item.label}
            {isItemActive && (
              <span
                aria-hidden="true"
                className="absolute inset-x-0 bottom-0 h-[2px] bg-primary dark:bg-secondary"
              />
            )}
          </NavLink>
        )
      })}
    </nav>
  )
}
