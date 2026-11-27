import { Link } from 'react-router-dom'
import { useAuth } from '@/hooks/useAuth'
import { buildCategoryPath, buildSearchPath } from '@/routes/paths'
import type { CategoryResponseDto } from '@/types/category'

interface TrendingTopicsBarProps {
  categories: CategoryResponseDto[]
}

const CURATED_TRENDING_QUERIES = [
  { label: 'Kinh tế số 2026', query: 'Kinh tế số' },
  { label: 'AI & Báo chí', query: 'tóm tắt tin tức' },
  { label: 'Bán dẫn Đông Nam Á', query: 'bán dẫn' },
  { label: 'Giao thông xanh', query: 'xe buýt điện' },
]

export function TrendingTopicsBar({ categories }: TrendingTopicsBarProps) {
  const { isAuthenticated } = useAuth()
  const searchMode = isAuthenticated ? 'ai' : 'keyword'

  const items = [
    ...categories.slice(0, 4).map((cat) => ({
      key: `cat-${cat.id}`,
      label: cat.name,
      to: buildCategoryPath(cat.slug),
    })),
    ...CURATED_TRENDING_QUERIES.map((item) => ({
      key: `q-${item.label}`,
      label: item.label,
      to: buildSearchPath(item.query, searchMode),
    })),
  ]

  return (
    <div
      aria-label="Chủ đề đang quan tâm"
      className="w-full border-b border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface"
    >
      <div className="mx-auto flex max-w-7xl items-center gap-3.5 overflow-x-auto px-6 py-2.5 lg:px-12 whitespace-nowrap">
        <span className="shrink-0 text-[11px] font-extrabold uppercase tracking-wider text-accent-crimson">
          Đang quan tâm
        </span>

        <span aria-hidden="true" className="h-3.5 w-px shrink-0 bg-neutral-border dark:bg-dark-border" />

        <nav aria-label="Liên kết chủ đề nổi bật" className="flex items-center gap-3 text-xs">
          {items.map((item, idx) => (
            <span key={item.key} className="inline-flex items-center gap-3">
              {idx > 0 && (
                <span aria-hidden="true" className="text-neutral-border dark:text-dark-border">
                  /
                </span>
              )}
              <Link
                to={item.to}
                className="font-medium text-neutral-text-main dark:text-dark-text-main hover:text-primary dark:hover:text-secondary hover:underline transition-colors"
              >
                {item.label}
              </Link>
            </span>
          ))}
        </nav>
      </div>
    </div>
  )
}
