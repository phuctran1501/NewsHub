import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { useCategories } from '@/features/categories/hooks/useCategories'
import { buildCategoryPath, ROUTES } from '@/routes/paths'

const FALLBACK_CATEGORIES = [
  { name: 'Thời sự', slug: 'thoi-su' },
  { name: 'Kinh tế', slug: 'kinh-te' },
  { name: 'Công nghệ', slug: 'cong-nghe' },
  { name: 'Thế giới', slug: 'the-gioi' },
]

export function NotFoundPage() {
  const location = useLocation()
  const navigate = useNavigate()
  const { data: categories } = useCategories()

  const quickCategories =
    categories && categories.length > 0
      ? categories.slice(0, 4).map((c) => ({ name: c.name, slug: c.slug }))
      : FALLBACK_CATEGORIES

  function handleGoBack() {
    if (window.history.length > 1) {
      navigate(-1)
    } else {
      navigate(ROUTES.HOME)
    }
  }

  return (
    <div className="mx-auto flex min-h-[calc(100vh-14rem)] max-w-2xl flex-col items-center justify-center px-6 py-16 text-center">
      <div className="w-full space-y-5">
        <div
          aria-label="Lỗi 404"
          className="inline-flex items-center justify-center gap-2.5 text-7xl sm:text-[84px] font-extrabold leading-none tracking-[-0.04em] text-primary dark:text-secondary select-none"
        >
          <span>4</span>
          <svg
            aria-hidden="true"
            viewBox="0 0 40 40"
            fill="none"
            className="h-14 w-14 sm:h-[70px] sm:w-[70px] motion-safe:animate-[spin_16s_linear_infinite]"
          >
            <path d="M16.5 5L27 5L35 13V14H16.5V5Z" fill="currentColor" />
            <path d="M35 16.5V27L27 35H26V16.5H35Z" fill="currentColor" />
            <path d="M23.5 35H13L5 27V26H23.5V35Z" fill="currentColor" />
            <path d="M5 23.5V13L13 5H14V23.5H5Z" fill="currentColor" />
          </svg>
          <span>4</span>
        </div>

        <div className="flex justify-center">
          <span className="max-w-full truncate rounded-full border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface px-3.5 py-1 font-mono text-xs text-neutral-text-muted dark:text-dark-text-muted">
            {location.pathname}
          </span>
        </div>

        <div className="space-y-2">
          <h1 className="text-2xl sm:text-[28px] font-extrabold tracking-tight text-neutral-text-main dark:text-dark-text-main">
            Trang bạn tìm không tồn tại
          </h1>
          <p className="mx-auto max-w-md text-sm leading-relaxed text-neutral-text-muted dark:text-dark-text-muted">
            Đường dẫn có thể đã bị thay đổi, bài viết đã được lưu trữ hoặc địa chỉ bạn nhập chưa
            chính xác.
          </p>
        </div>

        <div className="flex flex-wrap items-center justify-center gap-3 pt-1">
          <Button variant="primary" size="md" onClick={() => navigate(ROUTES.HOME)}>
            Về trang chủ
          </Button>
          <Button variant="outline" size="md" onClick={handleGoBack}>
            Quay lại trang trước
          </Button>
        </div>

        <div className="flex flex-wrap items-center justify-center gap-2 border-t border-neutral-border dark:border-dark-border pt-6">
          <span className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
            Chuyên mục phổ biến:
          </span>
          {quickCategories.map((cat) => (
            <Link
              key={cat.slug}
              to={buildCategoryPath(cat.slug)}
              className="rounded-full border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface px-3 py-1 text-xs font-medium text-neutral-text-muted dark:text-dark-text-muted hover:border-primary dark:hover:border-secondary hover:text-primary dark:hover:text-secondary transition-colors"
            >
              {cat.name}
            </Link>
          ))}
        </div>
      </div>
    </div>
  )
}
