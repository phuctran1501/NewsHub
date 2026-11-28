import { motion, useReducedMotion } from 'framer-motion'
import { Link } from 'react-router-dom'
import { Skeleton } from '@/components/common/Skeleton'
import { buildArticleDetailPath, buildCategoryPath } from '@/routes/paths'
import type { ArticleSummaryDto } from '@/types/article'
import { formatRelativeTime } from '@/utils/date'

interface SubHeroStripProps {
  articles: ArticleSummaryDto[]
  isLoading: boolean
}

export function SubHeroStrip({ articles, isLoading }: SubHeroStripProps) {
  const prefersReducedMotion = useReducedMotion()

  if (isLoading) {
    return (
      <div className="space-y-3" aria-label="Đang tải tiêu điểm trong ngày">
        <Skeleton className="h-4 w-44" />
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
          {[0, 1, 2, 3].map((idx) => (
            <div
              key={idx}
              className="overflow-hidden rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface"
            >
              <Skeleton className="h-36 w-full rounded-none" />
              <div className="p-4 space-y-2.5">
                <Skeleton className="h-4 w-full" />
                <Skeleton className="h-4 w-3/4" />
                <Skeleton className="h-3 w-32" />
              </div>
            </div>
          ))}
        </div>
      </div>
    )
  }

  const items = articles.length >= 5 ? articles.slice(1, 5) : articles.slice(0, 4)

  if (items.length === 0) {
    return null
  }

  return (
    <section aria-labelledby="sub-hero-heading" className="space-y-3.5">
      <div className="flex items-center justify-between gap-2">
        <h2
          id="sub-hero-heading"
          className="text-sm font-bold uppercase tracking-wider text-primary dark:text-secondary"
        >
          Tiêu điểm trong ngày
        </h2>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
        {items.map((article, idx) => {
          const category = article.categories[0]
          return (
            <motion.article
              key={article.id}
              initial={prefersReducedMotion ? false : { opacity: 0, y: 12 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true, amount: 0.2 }}
              transition={{ duration: 0.3, delay: idx * 0.05, ease: [0.16, 1, 0.3, 1] }}
              className="group flex flex-col overflow-hidden rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface hover:border-primary dark:hover:border-secondary transition-colors duration-150"
            >
              <Link
                to={buildArticleDetailPath(article.slug)}
                className="relative block aspect-[16/10] w-full overflow-hidden bg-primary"
              >
                {article.coverImage?.url ? (
                  <img
                    src={article.coverImage.url}
                    alt={article.title}
                    loading="lazy"
                    className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-[1.03]"
                  />
                ) : (
                  <div className="flex h-full w-full items-center justify-center bg-gradient-to-br from-[#0f274a] via-primary to-[#1e3a5f]">
                    <span className="font-serif text-xl font-bold text-white/20 select-none">
                      NewsHub
                    </span>
                  </div>
                )}

                {category && (
                  <span className="absolute bottom-2.5 left-3 rounded-full bg-neutral-surface/95 dark:bg-dark-surface/95 px-2.5 py-0.5 text-[10px] font-bold text-primary dark:text-secondary">
                    {category.name}
                  </span>
                )}
              </Link>

              <div className="flex flex-1 flex-col justify-between gap-2.5 p-4">
                <h3 className="text-sm font-semibold leading-snug text-neutral-text-main dark:text-dark-text-main line-clamp-2 group-hover:text-primary dark:group-hover:text-secondary transition-colors">
                  <Link to={buildArticleDetailPath(article.slug)}>{article.title}</Link>
                </h3>

                <div className="flex items-center justify-between gap-2 text-[11px] text-neutral-text-muted dark:text-dark-text-muted">
                  <span className="truncate font-medium">
                    {article.companyName ?? article.author.fullName}
                  </span>
                  <time
                    dateTime={article.publishedAt ?? article.createdAt}
                    className="shrink-0 tabular-nums"
                  >
                    {formatRelativeTime(article.publishedAt ?? article.createdAt)}
                  </time>
                </div>

                {category && (
                  <Link
                    to={buildCategoryPath(category.slug)}
                    className="sr-only"
                  >
                    Xem chuyên mục {category.name}
                  </Link>
                )}
              </div>
            </motion.article>
          )
        })}
      </div>
    </section>
  )
}
