import { ChevronLeft, ChevronRight } from 'lucide-react'
import { cn } from '@/utils/cn'

interface PaginationProps {
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
  isFetching?: boolean
  onPageChange: (nextPage: number) => void
  className?: string
}

function getVisiblePages(current: number, total: number): Array<number | 'ellipsis'> {
  if (total <= 7) {
    return Array.from({ length: total }, (_, index) => index + 1)
  }

  const pages: Array<number | 'ellipsis'> = [1]

  if (current > 3) {
    pages.push('ellipsis')
  }

  const start = Math.max(2, current - 1)
  const end = Math.min(total - 1, current + 1)

  for (let page = start; page <= end; page += 1) {
    pages.push(page)
  }

  if (current < total - 2) {
    pages.push('ellipsis')
  }

  pages.push(total)
  return pages
}

export function Pagination({
  pageNumber,
  pageSize,
  totalCount,
  totalPages,
  hasPreviousPage,
  hasNextPage,
  isFetching = false,
  onPageChange,
  className,
}: PaginationProps) {
  if (totalPages <= 1) {
    return null
  }

  const startItem = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1
  const endItem = Math.min(pageNumber * pageSize, totalCount)
  const pages = getVisiblePages(pageNumber, totalPages)

  return (
    <nav
      aria-label="Phân trang bài viết"
      className={cn(
        'flex flex-col sm:flex-row items-center justify-between gap-4 border-t border-neutral-border dark:border-dark-border pt-5',
        isFetching && 'opacity-70 transition-opacity',
        className,
      )}
    >
      <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted tabular-nums">
        Hiển thị <span className="font-semibold text-neutral-text-main dark:text-dark-text-main">{startItem}–{endItem}</span>{' '}
        trong <span className="font-semibold text-neutral-text-main dark:text-dark-text-main">{totalCount}</span> bài viết
      </p>

      <div className="flex items-center gap-1.5">
        <button
          type="button"
          disabled={!hasPreviousPage || isFetching}
          onClick={() => onPageChange(pageNumber - 1)}
          aria-label="Trang trước"
          className="inline-flex items-center gap-1 h-9 px-3 rounded-[9px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface text-xs font-semibold text-neutral-text-main dark:text-dark-text-main hover:border-primary dark:hover:border-secondary disabled:opacity-45 disabled:cursor-not-allowed transition-colors cursor-pointer"
        >
          <ChevronLeft size={14} strokeWidth={1.75} aria-hidden="true" />
          <span>Trước</span>
        </button>

        {pages.map((item, idx) =>
          item === 'ellipsis' ? (
            <span
              key={`ellipsis-${idx}`}
              aria-hidden="true"
              className="px-2 text-xs text-neutral-text-muted dark:text-dark-text-muted select-none"
            >
              …
            </span>
          ) : (
            <button
              key={item}
              type="button"
              disabled={isFetching}
              aria-current={item === pageNumber ? 'page' : undefined}
              onClick={() => onPageChange(item)}
              className={cn(
                'min-w-9 h-9 px-2.5 rounded-[9px] border text-xs font-semibold tabular-nums transition-colors cursor-pointer disabled:cursor-not-allowed',
                item === pageNumber
                  ? 'border-primary bg-primary text-white dark:border-secondary dark:bg-secondary dark:text-dark-bg'
                  : 'border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface text-neutral-text-main dark:text-dark-text-main hover:border-primary dark:hover:border-secondary',
              )}
            >
              {item}
            </button>
          ),
        )}

        <button
          type="button"
          disabled={!hasNextPage || isFetching}
          onClick={() => onPageChange(pageNumber + 1)}
          aria-label="Trang tiếp theo"
          className="inline-flex items-center gap-1 h-9 px-3 rounded-[9px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface text-xs font-semibold text-neutral-text-main dark:text-dark-text-main hover:border-primary dark:hover:border-secondary disabled:opacity-45 disabled:cursor-not-allowed transition-colors cursor-pointer"
        >
          <span>Tiếp</span>
          <ChevronRight size={14} strokeWidth={1.75} aria-hidden="true" />
        </button>
      </div>
    </nav>
  )
}
