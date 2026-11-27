import { useState } from 'react'
import { Link } from 'react-router-dom'
import { EmptyState } from '@/components/common/EmptyState'
import { ArticleCardSkeleton } from '@/components/common/Skeleton'
import { buildCategoryPath } from '@/routes/paths'
import type { CategoryResponseDto } from '@/types/category'
import { cn } from '@/utils/cn'
import { usePublishedArticles } from '../hooks/usePublishedArticles'
import { ArticleCard } from './ArticleCard'

interface CategoryStreamSectionProps {
  categories: CategoryResponseDto[]
  companyNameById?: Map<string, string>
}

export function CategoryStreamSection({
  categories,
  companyNameById,
}: CategoryStreamSectionProps) {
  const [selectedSlug, setSelectedSlug] = useState<string>('')

  const { data, isLoading, isFetching, isError, refetch } = usePublishedArticles({
    pageNumber: 1,
    pageSize: 6,
    categorySlug: selectedSlug || undefined,
    sortBy: 'latest',
  })

  const articles = data?.data ?? []
  const activeCategory = categories.find((c) => c.slug === selectedSlug)

  return (
    <section aria-labelledby="category-stream-heading" className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4 border-b border-neutral-border dark:border-dark-border pb-4">
        <div className="space-y-3">
          <h2
            id="category-stream-heading"
            className="text-xl font-bold tracking-[-0.015em] text-neutral-text-main dark:text-dark-text-main"
          >
            Luồng tin theo chuyên mục
          </h2>

          <div className="flex flex-wrap items-center gap-2" role="tablist" aria-label="Lọc theo chuyên mục">
            <button
              type="button"
              role="tab"
              aria-selected={selectedSlug === ''}
              onClick={() => setSelectedSlug('')}
              className={cn(
                'rounded-full border px-3 py-1 text-xs font-medium transition-colors cursor-pointer',
                selectedSlug === ''
                  ? 'border-primary bg-secondary-light text-primary font-semibold dark:border-secondary dark:bg-secondary/15 dark:text-secondary'
                  : 'border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface text-neutral-text-muted dark:text-dark-text-muted hover:border-primary dark:hover:border-secondary hover:text-neutral-text-main dark:hover:text-dark-text-main',
              )}
            >
              Tất cả
            </button>

            {categories.slice(0, 7).map((cat) => {
              const isSelected = selectedSlug === cat.slug
              return (
                <button
                  key={cat.id}
                  type="button"
                  role="tab"
                  aria-selected={isSelected}
                  onClick={() => setSelectedSlug(cat.slug)}
                  className={cn(
                    'rounded-full border px-3 py-1 text-xs font-medium transition-colors cursor-pointer',
                    isSelected
                      ? 'border-primary bg-secondary-light text-primary font-semibold dark:border-secondary dark:bg-secondary/15 dark:text-secondary'
                      : 'border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface text-neutral-text-muted dark:text-dark-text-muted hover:border-primary dark:hover:border-secondary hover:text-neutral-text-main dark:hover:text-dark-text-main',
                  )}
                >
                  {cat.name}
                </button>
              )
            })}
          </div>
        </div>

        {activeCategory ? (
          <Link
            to={buildCategoryPath(activeCategory.slug)}
            className="text-xs sm:text-sm font-semibold text-primary dark:text-secondary hover:underline shrink-0"
          >
            Xem tất cả {activeCategory.name} →
          </Link>
        ) : categories[0] ? (
          <Link
            to={buildCategoryPath(categories[0].slug)}
            className="text-xs sm:text-sm font-semibold text-primary dark:text-secondary hover:underline shrink-0"
          >
            Khám phá chuyên mục →
          </Link>
        ) : null}
      </div>

      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[0, 1, 2, 3, 4, 5].map((idx) => (
            <ArticleCardSkeleton key={idx} />
          ))}
        </div>
      ) : isError ? (
        <EmptyState
          title="Không thể tải danh sách bài viết"
          description="Đã xảy ra lỗi khi kết nối tới máy chủ. Bạn có thể thử tải lại ngay."
          actionLabel="Thử lại"
          onAction={() => void refetch()}
        />
      ) : articles.length === 0 ? (
        <EmptyState
          title="Chưa có bài viết trong chuyên mục này"
          description="Hãy chọn chuyên mục khác hoặc quay lại sau khi tòa soạn xuất bản thêm bài viết mới."
          actionLabel={selectedSlug ? 'Xem tất cả chuyên mục' : undefined}
          onAction={selectedSlug ? () => setSelectedSlug('') : undefined}
        />
      ) : (
        <div
          className={cn(
            'grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 transition-opacity duration-150',
            isFetching && 'opacity-70',
          )}
        >
          {articles.map((article) => (
            <ArticleCard
              key={article.id}
              article={article}
              companyName={article.companyId ? companyNameById?.get(article.companyId) : undefined}
            />
          ))}
        </div>
      )}
    </section>
  )
}
