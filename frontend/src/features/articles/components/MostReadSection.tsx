import { Link } from 'react-router-dom'
import { Skeleton } from '@/components/common/Skeleton'
import { buildArticleDetailPath, buildCategoryPath } from '@/routes/paths'
import { formatViewCount } from '@/utils/date'
import { usePublishedArticles } from '../hooks/usePublishedArticles'

export function MostReadSection() {
  const { data, isLoading, isError } = usePublishedArticles({
    pageNumber: 1,
    pageSize: 5,
    sortBy: 'mostread',
  })

  const articles = data?.data ?? []

  if (isError || (!isLoading && articles.length === 0)) {
    return null
  }

  return (
    <section
      aria-labelledby="most-read-heading"
      className="border-t-2 border-neutral-text-main dark:border-dark-text-main border-b border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface px-6 py-5"
    >
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 pb-4">
        <h2
          id="most-read-heading"
          className="text-sm font-extrabold uppercase tracking-wider text-neutral-text-main dark:text-dark-text-main"
        >
          Đọc nhiều nhất hôm nay
        </h2>
        <span className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
          Xếp hạng theo lượt đọc thực tế
        </span>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-5">
          {[0, 1, 2, 3, 4].map((idx) => (
            <div key={idx} className="space-y-2.5">
              <Skeleton className="h-6 w-10" />
              <Skeleton className="h-4 w-full" />
              <Skeleton className="h-4 w-4/5" />
              <Skeleton className="h-3 w-24" />
            </div>
          ))}
        </div>
      ) : (
        <ol className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 divide-y sm:divide-y-0 lg:divide-x divide-neutral-border dark:divide-dark-border gap-y-4 sm:gap-y-5 lg:gap-y-0">
          {articles.map((article, idx) => {
            const rank = String(idx + 1).padStart(2, '0')
            const primaryCategory = article.categories[0]

            return (
              <li
                key={article.id}
                className="group flex items-start gap-3 pt-4 first:pt-0 sm:pt-0 lg:px-4 lg:first:pl-0 lg:last:pr-0"
              >
                <span
                  aria-hidden="true"
                  className="shrink-0 text-2xl font-extrabold leading-none text-primary/35 dark:text-secondary/40 tabular-nums group-hover:text-primary dark:group-hover:text-secondary transition-colors"
                >
                  {rank}
                </span>

                <div className="min-w-0 space-y-1.5">
                  <h3 className="text-[13.5px] font-bold leading-snug text-neutral-text-main dark:text-dark-text-main line-clamp-3 group-hover:text-primary dark:group-hover:text-secondary transition-colors">
                    <Link to={buildArticleDetailPath(article.slug)}>{article.title}</Link>
                  </h3>

                  <div className="flex flex-wrap items-center gap-1.5 text-[11px] text-neutral-text-muted dark:text-dark-text-muted tabular-nums">
                    {primaryCategory && (
                      <>
                        <Link
                          to={buildCategoryPath(primaryCategory.slug)}
                          className="font-semibold text-primary dark:text-secondary hover:underline"
                        >
                          {primaryCategory.name}
                        </Link>
                        <span aria-hidden="true">•</span>
                      </>
                    )}
                    <span>{formatViewCount(article.viewCount)}</span>
                  </div>
                </div>
              </li>
            )
          })}
        </ol>
      )}
    </section>
  )
}
