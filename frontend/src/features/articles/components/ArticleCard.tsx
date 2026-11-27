import { Link } from 'react-router-dom'
import { Badge } from '@/components/common/Badge'
import { UserAvatar } from '@/components/common/UserAvatar'
import { buildArticleDetailPath, buildCategoryPath } from '@/routes/paths'
import type { ArticleSummaryDto } from '@/types/article'
import { cn } from '@/utils/cn'
import { formatRelativeTime } from '@/utils/date'

interface ArticleCardProps {
  article: ArticleSummaryDto
  className?: string
}

export function ArticleCard({ article, className }: ArticleCardProps) {
  const primaryCategory = article.categories[0]
  const articleHref = buildArticleDetailPath(article.slug)
  const companyName = article.companyName

  return (
    <article
      className={cn(
        'group flex flex-col overflow-hidden rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface hover:border-primary dark:hover:border-secondary-dark transition-colors duration-150',
        className,
      )}
    >
      <Link
        to={articleHref}
        className="block aspect-video w-full overflow-hidden border-b border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg"
      >
        {article.coverImage?.url ? (
          <img
            src={article.coverImage.url}
            alt={article.title}
            loading="lazy"
            className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-[1.02]"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center px-5 text-center bg-neutral-bg dark:bg-dark-bg">
            <span className="font-serif text-lg font-bold tracking-tight text-primary/35 dark:text-secondary/35 select-none">
              NewsHub
            </span>
          </div>
        )}
      </Link>

      <div className="flex flex-1 flex-col justify-between gap-3.5 p-5">
        <div className="space-y-2.5">
          <div className="flex flex-wrap items-center gap-2 text-xs text-neutral-text-muted dark:text-dark-text-muted">
            {primaryCategory && (
              <Link
                to={buildCategoryPath(primaryCategory.slug)}
                className="focus-visible:outline-2 focus-visible:outline-primary rounded-full"
              >
                <Badge variant="category">{primaryCategory.name}</Badge>
              </Link>
            )}
            <span className="tabular-nums">{article.readingTimeMinutes} phút đọc</span>
          </div>

          <h3 className="text-base font-semibold leading-snug tracking-[-0.01em] text-neutral-text-main dark:text-dark-text-main line-clamp-2 group-hover:text-primary dark:group-hover:text-secondary transition-colors">
            <Link
              to={articleHref}
              className="focus-visible:outline-2 focus-visible:outline-primary rounded"
            >
              {article.title}
            </Link>
          </h3>

          {article.excerpt && (
            <p className="text-[13px] leading-relaxed text-neutral-text-muted dark:text-dark-text-muted line-clamp-2">
              {article.excerpt}
            </p>
          )}
        </div>

        <div className="flex items-center justify-between gap-3 border-t border-neutral-border dark:border-dark-border pt-3 text-xs text-neutral-text-muted dark:text-dark-text-muted">
          <div className="flex items-center gap-2 min-w-0">
            <UserAvatar
              fullName={article.author.fullName}
              avatarUrl={article.author.avatarUrl}
              size="sm"
              className="h-6 w-6 text-[10px] shrink-0"
            />
            <span className="truncate font-medium text-neutral-text-main dark:text-dark-text-main">
              {article.author.fullName}
            </span>
            {companyName && (
              <span className="hidden sm:inline-block truncate rounded-full border border-neutral-border dark:border-dark-border px-2 py-0.5 text-[10px] font-semibold text-primary dark:text-secondary">
                {companyName}
              </span>
            )}
          </div>

          <time
            dateTime={article.publishedAt ?? article.createdAt}
            className="shrink-0 tabular-nums"
          >
            {formatRelativeTime(article.publishedAt ?? article.createdAt)}
          </time>
        </div>
      </div>
    </article>
  )
}
