import { AnimatePresence, motion, useReducedMotion } from 'framer-motion'
import { Menu, X } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import { Link, NavLink, useLocation } from 'react-router-dom'
import { useCategories } from '@/features/categories/hooks/useCategories'
import { useAuth } from '@/hooks/useAuth'
import { buildCategoryPath, buildSearchPath, ROUTES } from '@/routes/paths'
import { cn } from '@/utils/cn'

interface NavCategoryItem {
  label: string
  to: string
  categorySlug?: string
}

const FEATURED_CATEGORY_SLUGS = [
  'thoi-su',
  'the-gioi',
  'phap-luat',
  'kinh-doanh',
  'kinh-te',
  'cong-nghe',
  'giao-duc',
  'doi-song',
] as const

const MAX_FEATURED_CATEGORIES = 7

const FALLBACK_NAV_ITEMS: NavCategoryItem[] = [
  { label: 'Trang chủ', to: ROUTES.HOME },
  { label: 'Thời sự', to: buildCategoryPath('thoi-su'), categorySlug: 'thoi-su' },
  { label: 'Thế giới', to: buildCategoryPath('the-gioi'), categorySlug: 'the-gioi' },
  { label: 'Pháp luật', to: buildCategoryPath('phap-luat'), categorySlug: 'phap-luat' },
  { label: 'Kinh doanh', to: buildCategoryPath('kinh-doanh'), categorySlug: 'kinh-doanh' },
  { label: 'Công nghệ', to: buildCategoryPath('cong-nghe'), categorySlug: 'cong-nghe' },
  { label: 'Giáo dục', to: buildCategoryPath('giao-duc'), categorySlug: 'giao-duc' },
  { label: 'Đời sống', to: buildCategoryPath('doi-song'), categorySlug: 'doi-song' },
]

interface MegaCategoryGroup {
  title: string
  slugs: string[]
}

const MEGA_CATEGORY_GROUPS: MegaCategoryGroup[] = [
  { title: 'Thời sự & Xã hội', slugs: ['thoi-su', 'the-gioi', 'phap-luat', 'moi-truong', 'ban-doc'] },
  {
    title: 'Kinh tế & Công nghệ',
    slugs: ['kinh-te', 'kinh-doanh', 'nha-dat', 'khoi-nghiep', 'nong-nghiep', 'cong-nghe', 'khoa-hoc', 'xe'],
  },
  { title: 'Đời sống & Tri thức', slugs: ['giao-duc', 'suc-khoe', 'doi-song', 'nhip-song-tre', 'du-lich'] },
  { title: 'Văn hóa & Thể thao', slugs: ['van-hoa', 'sach-tri-thuc', 'giai-tri', 'the-thao'] },
]

export function CategoryMegaMenuBar() {
  const location = useLocation()
  const prefersReducedMotion = useReducedMotion()
  const { isAuthenticated } = useAuth()
  const { data: categories } = useCategories()
  const [openedForRoute, setOpenedForRoute] = useState<string | null>(null)
  const containerRef = useRef<HTMLDivElement>(null)

  const searchMode = isAuthenticated ? 'ai' : 'keyword'
  const normalizedPathname = location.pathname.replace(/\/+$/, '') || '/'
  const currentRouteKey = `${location.pathname}${location.search}`
  const isMegaOpen = openedForRoute === currentRouteKey

  const allCategoryItems = useMemo<NavCategoryItem[]>(() => {
    if (!categories || categories.length === 0) {
      return FALLBACK_NAV_ITEMS.filter((item) => item.categorySlug)
    }
    return categories.map((cat) => ({
      label: cat.name,
      to: buildCategoryPath(cat.slug),
      categorySlug: cat.slug,
    }))
  }, [categories])

  const featuredNavItems = useMemo<NavCategoryItem[]>(() => {
    if (!categories || categories.length === 0) {
      return FALLBACK_NAV_ITEMS
    }

    const bySlug = new Map(categories.map((cat) => [cat.slug.toLowerCase(), cat]))
    const picked: NavCategoryItem[] = []
    const pickedSlugs = new Set<string>()

    for (const slug of FEATURED_CATEGORY_SLUGS) {
      if (picked.length >= MAX_FEATURED_CATEGORIES) break
      const match = bySlug.get(slug)
      if (match) {
        picked.push({
          label: match.name,
          to: buildCategoryPath(match.slug),
          categorySlug: match.slug,
        })
        pickedSlugs.add(match.slug.toLowerCase())
      }
    }

    for (const cat of categories) {
      if (picked.length >= MAX_FEATURED_CATEGORIES) break
      if (!pickedSlugs.has(cat.slug.toLowerCase())) {
        picked.push({
          label: cat.name,
          to: buildCategoryPath(cat.slug),
          categorySlug: cat.slug,
        })
        pickedSlugs.add(cat.slug.toLowerCase())
      }
    }

    return [{ label: 'Trang chủ', to: ROUTES.HOME }, ...picked]
  }, [categories])

  const groupedMegaCategories = useMemo(() => {
    const bySlug = new Map(
      allCategoryItems.map((item) => [(item.categorySlug ?? '').toLowerCase(), item]),
    )
    const assigned = new Set<string>()

    const groups = MEGA_CATEGORY_GROUPS.map((group) => {
      const items: NavCategoryItem[] = []
      for (const slug of group.slugs) {
        const match = bySlug.get(slug)
        if (match) {
          items.push(match)
          assigned.add(slug)
        }
      }
      return { title: group.title, items }
    }).filter((group) => group.items.length > 0)

    const remaining = allCategoryItems.filter(
      (item) => item.categorySlug && !assigned.has(item.categorySlug.toLowerCase()),
    )
    if (remaining.length > 0) {
      groups.push({ title: 'Chuyên mục khác', items: remaining })
    }

    return groups
  }, [allCategoryItems])

  useEffect(() => {
    if (!isMegaOpen) return

    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        setOpenedForRoute(null)
      }
    }

    function handlePointerDown(event: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setOpenedForRoute(null)
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    document.addEventListener('mousedown', handlePointerDown)
    return () => {
      window.removeEventListener('keydown', handleKeyDown)
      document.removeEventListener('mousedown', handlePointerDown)
    }
  }, [isMegaOpen])

  return (
    <div
      ref={containerRef}
      className="relative w-full border-t border-neutral-border/80 dark:border-dark-border/80 bg-neutral-surface dark:bg-dark-surface"
    >
      <div className="flex h-11 w-full items-center justify-between gap-4 px-4 sm:px-6 lg:px-10 xl:px-14">
        <nav
          aria-label="Danh mục báo chí tiêu biểu"
          className="nh-no-scrollbar flex h-full items-center gap-1 overflow-x-auto"
        >
          {featuredNavItems.map((item) => {
            const isItemActive = item.categorySlug
              ? normalizedPathname === buildCategoryPath(item.categorySlug)
              : normalizedPathname === ROUTES.HOME

            return (
              <NavLink
                key={item.to}
                to={item.to}
                className={() =>
                  cn(
                    'relative inline-flex h-full items-center whitespace-nowrap px-3 text-xs sm:text-[13px] transition-colors duration-150',
                    isItemActive
                      ? 'font-bold text-primary dark:text-secondary'
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
                    className="absolute inset-x-2 bottom-0 h-0.5 bg-primary dark:bg-secondary"
                  />
                )}
              </NavLink>
            )
          })}

          <a
            href="/#opinion-col"
            className="hidden sm:inline-flex h-full items-center whitespace-nowrap px-3 text-xs sm:text-[13px] font-medium text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main transition-colors"
          >
            Góc nhìn
          </a>
          <a
            href="/#multimedia-band"
            className="hidden sm:inline-flex h-full items-center whitespace-nowrap px-3 text-xs sm:text-[13px] font-semibold text-primary dark:text-secondary hover:underline transition-colors"
          >
            Media
          </a>
        </nav>

        <button
          type="button"
          aria-expanded={isMegaOpen}
          aria-controls="newshub-mega-menu"
          onClick={() =>
            setOpenedForRoute((prev) => (prev === currentRouteKey ? null : currentRouteKey))
          }
          className={cn(
            'inline-flex h-7 shrink-0 items-center gap-1.5 rounded-md border px-2.5 text-xs font-semibold transition-colors duration-150 cursor-pointer',
            isMegaOpen
              ? 'border-primary bg-primary text-white dark:border-secondary dark:bg-secondary dark:text-dark-bg'
              : 'border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface text-neutral-text-main dark:text-dark-text-main hover:border-primary dark:hover:border-secondary',
          )}
        >
          {isMegaOpen ? <X size={14} strokeWidth={1.75} /> : <Menu size={14} strokeWidth={1.75} />}
          <span>Tất cả ({allCategoryItems.length})</span>
        </button>
      </div>

      <AnimatePresence>
        {isMegaOpen && (
          <motion.div
            id="newshub-mega-menu"
            role="region"
            aria-label="Toàn bộ chuyên mục NewsHub"
            initial={prefersReducedMotion ? { opacity: 1 } : { opacity: 0, y: -6 }}
            animate={{ opacity: 1, y: 0 }}
            exit={prefersReducedMotion ? { opacity: 0 } : { opacity: 0, y: -6 }}
            transition={{ duration: 0.18, ease: [0.16, 1, 0.3, 1] }}
            className="absolute left-0 right-0 top-full z-50 w-full border-b border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface shadow-[0_8px_24px_rgba(15,23,42,0.08)]"
          >
            <div className="grid w-full grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-6 px-4 py-7 sm:px-6 lg:px-10 xl:px-14 text-xs">
              {groupedMegaCategories.map((group) => (
                <div key={group.title} className="space-y-2.5">
                  <h3 className="border-b border-neutral-border dark:border-dark-border pb-1.5 font-bold uppercase tracking-wider text-primary dark:text-secondary">
                    {group.title}
                  </h3>
                  <ul className="space-y-1.5 text-neutral-text-muted dark:text-dark-text-muted">
                    {group.items.map((item) => (
                      <li key={item.to}>
                        <Link
                          to={item.to}
                          onClick={() => setOpenedForRoute(null)}
                          className="hover:text-primary dark:hover:text-secondary transition-colors"
                        >
                          {item.label}
                        </Link>
                      </li>
                    ))}
                  </ul>
                </div>
              ))}

              <div className="rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg p-4 flex flex-col justify-between gap-3">
                <div className="space-y-2">
                  <h3 className="font-bold text-neutral-text-main dark:text-dark-text-main">
                    Hệ sinh thái & Tiện ích
                  </h3>
                  <ul className="space-y-1.5 text-neutral-text-muted dark:text-dark-text-muted">
                    <li>
                      <Link
                        to={buildSearchPath('', searchMode)}
                        onClick={() => setOpenedForRoute(null)}
                        className="hover:text-primary dark:hover:text-secondary transition-colors"
                      >
                        Tìm kiếm tin tức
                      </Link>
                    </li>
                    <li>
                      <a
                        href="/#opinion-col"
                        onClick={() => setOpenedForRoute(null)}
                        className="hover:text-primary dark:hover:text-secondary transition-colors"
                      >
                        Góc nhìn & Bình luận
                      </a>
                    </li>
                    <li>
                      <a
                        href="/#multimedia-band"
                        onClick={() => setOpenedForRoute(null)}
                        className="hover:text-primary dark:hover:text-secondary transition-colors"
                      >
                        Đa phương tiện (Media)
                      </a>
                    </li>
                  </ul>
                </div>
                <a
                  href="/#companies"
                  onClick={() => setOpenedForRoute(null)}
                  className="text-[11px] font-semibold text-primary dark:text-secondary hover:underline"
                >
                  Khám phá danh bạ tòa soạn →
                </a>
              </div>
            </div>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
