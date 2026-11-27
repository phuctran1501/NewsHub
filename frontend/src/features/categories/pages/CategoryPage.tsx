import { Link, useNavigate, useParams, useSearchParams } from 'react-router-dom'
import { Badge } from '@/components/common/Badge'
import { EmptyState } from '@/components/common/EmptyState'
import { Pagination } from '@/components/common/Pagination'
import { ArticleCardSkeleton, Skeleton } from '@/components/common/Skeleton'
import { ArticleCard } from '@/features/articles/components/ArticleCard'
import { usePublishedArticles } from '@/features/articles/hooks/usePublishedArticles'
import { usePublicCompanies } from '@/features/companies/hooks/usePublicCompanies'
import { buildCategoryPath, ROUTES } from '@/routes/paths'
import { AppClientError } from '@/types/api'
import type { ArticleSortBy } from '@/types/article'
import { cn } from '@/utils/cn'
import { useCategories, useCategoryBySlug } from '../hooks/useCategories'

const SORT_OPTIONS: ReadonlyArray<{ value: ArticleSortBy; label: string }> = [
  { value: 'latest', label: 'Mới nhất' },
  { value: 'mostread', label: 'Đọc nhiều nhất' },
  { value: 'oldest', label: 'Cũ nhất' },
]

const PAGE_SIZE = 9

function parseSortBy(raw: string | null): ArticleSortBy {
  if (raw === 'mostread' || raw === 'oldest') return raw
  return 'latest'
}

function parsePageNumber(raw: string | null): number {
  const parsed = Number.parseInt(raw ?? '1', 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : 1
}

export function CategoryPage() {
  const { slug } = useParams<{ slug: string }>()
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()

  const sortBy = parseSortBy(searchParams.get('sortBy'))
  const pageNumber = parsePageNumber(searchParams.get('page'))

  const { data: categories = [] } = useCategories()
  const {
    data: categoryDetail,
    isLoading: isLoadingCategory,
    isError: isCategoryError,
    error: categoryError,
  } = useCategoryBySlug(slug)

  const {
    data: articlesResponse,
    isLoading: isLoadingArticles,
    isFetching: isFetchingArticles,
    isError: isArticlesError,
    refetch: refetchArticles,
  } = usePublishedArticles(
    {
      categorySlug: slug,
      pageNumber,
      pageSize: PAGE_SIZE,
      sortBy,
    },
    { enabled: Boolean(slug) && !isCategoryError },
  )

  const { data: companiesResponse } = usePublicCompanies({ pageNumber: 1, pageSize: 50 })
  const companyNameById = new Map(
    (companiesResponse?.data ?? []).map((company) => [company.id, company.name]),
  )

  const matchedFromList = categories.find((c) => c.slug === slug)
  const displayCategory = categoryDetail ?? matchedFromList
  const articles = articlesResponse?.data ?? []
  const meta = articlesResponse?.meta
  const totalArticleCount = meta?.totalCount ?? matchedFromList?.articleCount ?? 0

  function updateQueryParams(next: { page?: number; sortBy?: ArticleSortBy }) {
    const nextParams = new URLSearchParams(searchParams)
    if (next.sortBy) {
      if (next.sortBy === 'latest') {
        nextParams.delete('sortBy')
      } else {
        nextParams.set('sortBy', next.sortBy)
      }
      nextParams.delete('page')
    }
    if (next.page !== undefined) {
      if (next.page <= 1) {
        nextParams.delete('page')
      } else {
        nextParams.set('page', String(next.page))
      }
    }
    setSearchParams(nextParams)
    window.scrollTo({ top: 0, behavior: 'smooth' })
  }

  const isNotFound = isCategoryError && categoryError instanceof AppClientError && categoryError.status === 404

  if (isNotFound) {
    return (
      <div className="mx-auto max-w-7xl px-6 py-12 lg:px-12">
        <EmptyState
          title="Không tìm thấy chuyên mục"
          description={`Chuyên mục "${slug ?? ''}" không tồn tại hoặc đã được thay đổi đường dẫn.`}
          actionLabel="Về trang chủ"
          onAction={() => navigate(ROUTES.HOME)}
        />
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-7xl px-6 py-8 lg:px-12 lg:py-10 space-y-8">
      <section
        aria-labelledby="category-page-heading"
        className="rounded-2xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-6 sm:p-8 space-y-3"
      >
        <nav aria-label="Đường dẫn" className="flex items-center gap-2 text-xs text-neutral-text-muted dark:text-dark-text-muted">
          <Link to={ROUTES.HOME} className="hover:text-primary dark:hover:text-secondary transition-colors">
            Trang chủ
          </Link>
          <span aria-hidden="true">/</span>
          <span className="font-semibold text-neutral-text-main dark:text-dark-text-main">
            {displayCategory?.name ?? slug}
          </span>
        </nav>

        {isLoadingCategory && !displayCategory ? (
          <div className="space-y-2.5">
            <Skeleton className="h-8 w-56" />
            <Skeleton className="h-4 w-full max-w-xl" />
          </div>
        ) : (
          <>
            <div className="flex flex-wrap items-center justify-between gap-3">
              <h1
                id="category-page-heading"
                className="text-2xl sm:text-3xl font-extrabold tracking-tight text-neutral-text-main dark:text-dark-text-main"
              >
                {displayCategory?.name ?? slug}
              </h1>
              <Badge variant="category" className="tabular-nums">
                {totalArticleCount} bài viết đã xuất bản
              </Badge>
            </div>

            {displayCategory?.description && (
              <p className="text-sm leading-relaxed text-neutral-text-muted dark:text-dark-text-muted max-w-2xl">
                {displayCategory.description}
              </p>
            )}
          </>
        )}
      </section>

      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div className="flex flex-wrap items-center gap-2">
          <Link
            to={ROUTES.HOME}
            className="rounded-full border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface px-3 py-1 text-xs font-medium text-neutral-text-muted dark:text-dark-text-muted hover:border-primary dark:hover:border-secondary hover:text-neutral-text-main dark:hover:text-dark-text-main transition-colors"
          >
            Tất cả
          </Link>
          {categories.map((cat) => {
            const isCurrent = cat.slug === slug
            return (
              <Link
                key={cat.id}
                to={buildCategoryPath(cat.slug)}
                aria-current={isCurrent ? 'page' : undefined}
                className={cn(
                  'rounded-full border px-3 py-1 text-xs font-medium transition-colors',
                  isCurrent
                    ? 'border-primary bg-secondary-light text-primary font-semibold dark:border-secondary dark:bg-secondary/15 dark:text-secondary'
                    : 'border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface text-neutral-text-muted dark:text-dark-text-muted hover:border-primary dark:hover:border-secondary hover:text-neutral-text-main dark:hover:text-dark-text-main',
                )}
              >
                {cat.name}
              </Link>
            )
          })}
        </div>

        <div
          role="group"
          aria-label="Sắp xếp bài viết"
          className="inline-flex shrink-0 rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-1 gap-1 self-start md:self-auto"
        >
          {SORT_OPTIONS.map((option) => {
            const isActive = sortBy === option.value
            return (
              <button
                key={option.value}
                type="button"
                onClick={() => updateQueryParams({ sortBy: option.value })}
                className={cn(
                  'rounded-[7px] px-3 py-1.5 text-xs font-semibold transition-colors cursor-pointer',
                  isActive
                    ? 'bg-primary text-white dark:bg-secondary dark:text-dark-bg'
                    : 'text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main',
                )}
              >
                {option.label}
              </button>
            )
          })}
        </div>
      </div>

      {isLoadingArticles ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[0, 1, 2, 3, 4, 5].map((idx) => (
            <ArticleCardSkeleton key={idx} />
          ))}
        </div>
      ) : isArticlesError ? (
        <EmptyState
          title="Không thể tải bài viết của chuyên mục"
          description="Kết nối máy chủ đang bị gián đoạn. Vui lòng thử tải lại."
          actionLabel="Thử lại"
          onAction={() => void refetchArticles()}
        />
      ) : articles.length === 0 ? (
        <EmptyState
          title="Chuyên mục chưa có bài viết nào"
          description="Hiện chưa có bài viết đã xuất bản trong chuyên mục này. Bạn có thể quay lại trang chủ để đọc các tin mới nhất."
          actionLabel="Về trang chủ"
          onAction={() => navigate(ROUTES.HOME)}
        />
      ) : (
        <div className="space-y-6">
          <div
            className={cn(
              'grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 transition-opacity duration-150',
              isFetchingArticles && 'opacity-70',
            )}
          >
            {articles.map((article) => (
              <ArticleCard
                key={article.id}
                article={article}
                companyName={article.companyId ? companyNameById.get(article.companyId) : undefined}
              />
            ))}
          </div>

          {meta && (
            <Pagination
              pageNumber={meta.pageNumber}
              pageSize={meta.pageSize}
              totalCount={meta.totalCount}
              totalPages={meta.totalPages}
              hasPreviousPage={meta.hasPreviousPage}
              hasNextPage={meta.hasNextPage}
              isFetching={isFetchingArticles}
              onPageChange={(nextPage) => updateQueryParams({ page: nextPage })}
            />
          )}
        </div>
      )}
    </div>
  )
}
