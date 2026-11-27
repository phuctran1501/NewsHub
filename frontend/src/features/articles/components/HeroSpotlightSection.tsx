import { Link } from 'react-router-dom'
import { Badge } from '@/components/common/Badge'
import { EmptyState } from '@/components/common/EmptyState'
import { Skeleton } from '@/components/common/Skeleton'
import { UserAvatar } from '@/components/common/UserAvatar'
import { buildArticleDetailPath, buildCategoryPath } from '@/routes/paths'
import type { ArticleSummaryDto } from '@/types/article'
import { formatRelativeTime, formatViewCount } from '@/utils/date'

interface HeroSpotlightSectionProps {
  articles: ArticleSummaryDto[]
  isLoading: boolean
  isError: boolean
  onRetry: () => void
}

function HeroSpotlightSkeleton() {
  return (
    <section aria-label="Đang tải tin tiêu điểm" className="grid grid-cols-1 lg:grid-cols-12 gap-6">
      <div className="lg:col-span-8 rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface overflow-hidden">
        <Skeleton className="aspect-video w-full rounded-none" />
        <div className="p-6 lg:p-8 space-y-3.5">
          <div className="flex items-center gap-2">
            <Skeleton className="h-5 w-24 rounded-full" />
            <Skeleton className="h-4 w-20" />
          </div>
          <Skeleton className="h-8 w-full" />
          <Skeleton className="h-8 w-4/5" />
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-2/3" />
        </div>
      </div>

      <div className="lg:col-span-4 rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-6 space-y-5">
        <Skeleton className="h-6 w-40" />
        {[0, 1, 2].map((item) => (
          <div key={item} className="space-y-2 border-t border-neutral-border dark:border-dark-border pt-4">
            <Skeleton className="h-4 w-24 rounded-full" />
            <Skeleton className="h-5 w-full" />
            <Skeleton className="h-4 w-1/2" />
          </div>
        ))}
      </div>
    </section>
  )
}

export function HeroSpotlightSection({
  articles,
  isLoading,
  isError,
  onRetry,
}: HeroSpotlightSectionProps) {
  if (isLoading) {
    return <HeroSpotlightSkeleton />
  }

  if (isError) {
    return (
      <EmptyState
        title="Không thể tải tin tiêu điểm"
        description="Kết nối tới máy chủ bài viết đang gặp gián đoạn. Vui lòng thử tải lại."
        actionLabel="Thử lại"
        onAction={onRetry}
      />
    )
  }

  if (articles.length === 0) {
    return (
      <EmptyState
        title="Chưa có bài viết xuất bản"
        description="Các bài báo tiêu điểm mới nhất từ đội ngũ phóng viên và tòa soạn sẽ sớm xuất hiện tại đây."
      />
    )
  }

  const leadArticle = articles[0]
  const secondaryArticles = articles.slice(1, 4)
  const leadCategory = leadArticle.categories[0]
  const leadCompanyName = leadArticle.companyName

  return (
    <section
      aria-labelledby="lead-story-heading"
      className="grid grid-cols-1 lg:grid-cols-12 gap-6"
    >
      <article className="group lg:col-span-8 flex flex-col overflow-hidden rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface hover:border-primary dark:hover:border-secondary-dark transition-colors duration-150">
        <Link
          to={buildArticleDetailPath(leadArticle.slug)}
          className="block aspect-video w-full overflow-hidden border-b border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg"
        >
          {leadArticle.coverImage?.url ? (
            <img
              src={leadArticle.coverImage.url}
              alt={leadArticle.title}
              className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-[1.015]"
            />
          ) : (
            <div className="flex h-full w-full items-center justify-center px-6 text-center bg-neutral-bg dark:bg-dark-bg">
              <span className="font-serif text-2xl font-bold tracking-tight text-primary/35 dark:text-secondary/35 select-none">
                NewsHub
              </span>
            </div>
          )}
        </Link>

        <div className="flex flex-1 flex-col justify-between gap-4 p-6 lg:p-8">
          <div className="space-y-3">
            <div className="flex flex-wrap items-center gap-2 text-xs text-neutral-text-muted dark:text-dark-text-muted">
              {leadCategory && (
                <Link to={buildCategoryPath(leadCategory.slug)}>
                  <Badge variant="category">{leadCategory.name}</Badge>
                </Link>
              )}
              <span className="tabular-nums">{leadArticle.readingTimeMinutes} phút đọc</span>
              <span aria-hidden="true">•</span>
              <span className="tabular-nums">{formatViewCount(leadArticle.viewCount)}</span>
            </div>

            <h2
              id="lead-story-heading"
              className="text-2xl sm:text-3xl font-bold tracking-[-0.02em] leading-tight text-neutral-text-main dark:text-dark-text-main group-hover:text-primary dark:group-hover:text-secondary transition-colors"
            >
              <Link to={buildArticleDetailPath(leadArticle.slug)}>{leadArticle.title}</Link>
            </h2>

            {leadArticle.excerpt && (
              <p className="text-base leading-[1.7] text-neutral-text-muted dark:text-dark-text-muted max-w-[70ch] line-clamp-2">
                {leadArticle.excerpt}
              </p>
            )}
          </div>

          <div className="flex flex-wrap items-center justify-between gap-3 border-t border-neutral-border dark:border-dark-border pt-4 text-xs text-neutral-text-muted dark:text-dark-text-muted">
            <div className="flex flex-wrap items-center gap-2">
              <UserAvatar
                fullName={leadArticle.author.fullName}
                avatarUrl={leadArticle.author.avatarUrl}
                size="sm"
                className="h-7 w-7 text-[11px]"
              />
              <span className="font-semibold text-neutral-text-main dark:text-dark-text-main">
                {leadArticle.author.fullName}
              </span>
              {leadCompanyName && (
                <span className="rounded-full border border-neutral-border dark:border-dark-border px-2.5 py-0.5 text-[11px] font-semibold text-primary dark:text-secondary">
                  {leadCompanyName}
                </span>
              )}
            </div>

            <time
              dateTime={leadArticle.publishedAt ?? leadArticle.createdAt}
              className="tabular-nums"
            >
              {formatRelativeTime(leadArticle.publishedAt ?? leadArticle.createdAt)}
            </time>
          </div>
        </div>
      </article>

      <aside
        aria-labelledby="briefing-heading"
        className="lg:col-span-4 rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-6 flex flex-col"
      >
        <h2
          id="briefing-heading"
          className="text-lg font-bold tracking-[-0.015em] text-neutral-text-main dark:text-dark-text-main pb-4 border-b border-neutral-border dark:border-dark-border"
        >
          Tiêu điểm biên tập
        </h2>

        {secondaryArticles.length > 0 ? (
          <div className="divide-y divide-neutral-border dark:divide-dark-border flex-1">
            {secondaryArticles.map((item) => {
              const cat = item.categories[0]
              return (
                <article key={item.id} className="group py-4 last:pb-0 space-y-1.5">
                  <div className="flex flex-wrap items-center gap-2 text-xs text-neutral-text-muted dark:text-dark-text-muted">
                    {cat && (
                      <Link
                        to={buildCategoryPath(cat.slug)}
                        className="font-semibold text-primary dark:text-secondary hover:underline"
                      >
                        {cat.name}
                      </Link>
                    )}
                    {cat && <span aria-hidden="true">•</span>}
                    <span className="tabular-nums">{item.readingTimeMinutes} phút đọc</span>
                  </div>

                  <h3 className="text-sm font-semibold leading-snug text-neutral-text-main dark:text-dark-text-main line-clamp-2 group-hover:text-primary dark:group-hover:text-secondary transition-colors">
                    <Link to={buildArticleDetailPath(item.slug)}>{item.title}</Link>
                  </h3>

                  <div className="flex items-center gap-1.5 text-xs text-neutral-text-muted dark:text-dark-text-muted">
                    <span className="truncate">{item.author.fullName}</span>
                    <span aria-hidden="true">•</span>
                    <time
                      dateTime={item.publishedAt ?? item.createdAt}
                      className="shrink-0 tabular-nums"
                    >
                      {formatRelativeTime(item.publishedAt ?? item.createdAt)}
                    </time>
                  </div>
                </article>
              )
            })}
          </div>
        ) : (
          <p className="pt-4 text-xs leading-relaxed text-neutral-text-muted dark:text-dark-text-muted">
            Các bài tiêu điểm tiếp theo sẽ tự động hiển thị ngay khi tòa soạn xuất bản tin mới.
          </p>
        )}
      </aside>
    </section>
  )
}
