import { AnimatePresence, motion, useReducedMotion } from 'framer-motion'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { EmptyState } from '@/components/common/EmptyState'
import { Skeleton } from '@/components/common/Skeleton'
import { UserAvatar } from '@/components/common/UserAvatar'
import { buildArticleDetailPath, buildCategoryPath } from '@/routes/paths'
import type { ArticleSummaryDto } from '@/types/article'
import { formatRelativeTime, formatTickerTime, formatViewCount } from '@/utils/date'

const ROTATE_INTERVAL_MS = 6000
const MAX_FRONT_PAGE_SLIDES = 5

interface HeroSpotlightSectionProps {
  articles: ArticleSummaryDto[]
  isLoading: boolean
  isError: boolean
  onRetry: () => void
}

function selectDiverseFrontPageStories(articles: ArticleSummaryDto[]): ArticleSummaryDto[] {
  if (articles.length <= MAX_FRONT_PAGE_SLIDES) {
    return articles
  }

  const picked: ArticleSummaryDto[] = []
  const usedIds = new Set<string>()
  const usedCategorySlugs = new Set<string>()

  // Pass 1: ưu tiên chọn mỗi chuyên mục 1 bài tiêu điểm mới nhất
  for (const article of articles) {
    if (picked.length >= MAX_FRONT_PAGE_SLIDES) break
    const primarySlug = article.categories[0]?.slug
    if (primarySlug && !usedCategorySlugs.has(primarySlug)) {
      usedCategorySlugs.add(primarySlug)
      usedIds.add(article.id)
      picked.push(article)
    }
  }

  // Pass 2: nếu chưa đủ 5 bài thì lấp đầy bằng các bài mới nhất còn lại
  for (const article of articles) {
    if (picked.length >= MAX_FRONT_PAGE_SLIDES) break
    if (!usedIds.has(article.id)) {
      usedIds.add(article.id)
      picked.push(article)
    }
  }

  return picked
}

function HeroSpotlightSkeleton() {
  return (
    <section aria-label="Đang tải trang nhất" className="grid grid-cols-1 lg:grid-cols-12 gap-6">
      <div className="lg:col-span-8 rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface overflow-hidden">
        <Skeleton className="h-[420px] sm:h-[490px] w-full rounded-none" />
      </div>

      <div className="lg:col-span-4 rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-6 space-y-5">
        <Skeleton className="h-6 w-40" />
        {[0, 1, 2, 3, 4].map((item) => (
          <div key={item} className="space-y-2 border-t border-neutral-border dark:border-dark-border pt-3.5">
            <Skeleton className="h-3.5 w-24" />
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-3/4" />
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
  const prefersReducedMotion = useReducedMotion()
  const [activeIndex, setActiveIndex] = useState(0)
  const [isPaused, setIsPaused] = useState(false)

  const frontPageSlides = useMemo(() => selectDiverseFrontPageStories(articles), [articles])
  const liveWireArticles = useMemo(() => articles.slice(0, 6), [articles])

  const safeIndex = frontPageSlides.length > 0 ? activeIndex % frontPageSlides.length : 0

  useEffect(() => {
    if (prefersReducedMotion || isPaused || frontPageSlides.length <= 1) {
      return
    }

    const timer = window.setInterval(() => {
      setActiveIndex((prev) => (prev + 1) % frontPageSlides.length)
    }, ROTATE_INTERVAL_MS)

    return () => window.clearInterval(timer)
  }, [prefersReducedMotion, isPaused, frontPageSlides.length])

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

  if (frontPageSlides.length === 0) {
    return (
      <EmptyState
        title="Chưa có bài viết xuất bản"
        description="Các bài báo tiêu điểm mới nhất từ đội ngũ phóng viên và tòa soạn sẽ sớm xuất hiện tại đây."
      />
    )
  }

  const activeStory = frontPageSlides[safeIndex]
  const activeCategory = activeStory.categories[0]

  return (
    <section
      aria-labelledby="lead-story-heading"
      className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-stretch"
    >
      {/* Left 8 cols: Full-bleed Image Overlay Rotating Front Page (5 stories by category) */}
      <article
        onMouseEnter={() => setIsPaused(true)}
        onMouseLeave={() => setIsPaused(false)}
        className="lg:col-span-8 flex flex-col overflow-hidden rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface"
      >
        <div className="relative min-h-[400px] sm:min-h-[470px] w-full flex-1 overflow-hidden bg-primary">
          <AnimatePresence mode="wait">
            <motion.div
              key={activeStory.id}
              initial={prefersReducedMotion ? { opacity: 1 } : { opacity: 0.25 }}
              animate={{ opacity: 1 }}
              exit={prefersReducedMotion ? { opacity: 0 } : { opacity: 0.25 }}
              transition={{ duration: 0.38, ease: [0.16, 1, 0.3, 1] }}
              className="absolute inset-0"
            >
              {activeStory.coverImage?.url ? (
                <motion.img
                  src={activeStory.coverImage.url}
                  alt={activeStory.title}
                  initial={prefersReducedMotion ? false : { scale: 1.03 }}
                  animate={{ scale: 1 }}
                  transition={{ duration: 0.7, ease: [0.16, 1, 0.3, 1] }}
                  className="h-full w-full object-cover"
                />
              ) : (
                <div className="flex h-full w-full items-center justify-center bg-gradient-to-br from-[#0f274a] via-primary to-[#1e40af]">
                  <span className="font-serif text-4xl font-bold tracking-tight text-white/20 select-none">
                    NewsHub
                  </span>
                </div>
              )}

              {/* High-contrast dark editorial scrim for overlaid typography */}
              <div
                aria-hidden="true"
                className="absolute inset-0 bg-gradient-to-t from-[#0b1220]/95 via-[#0b1220]/55 to-[#0b1220]/15"
              />
            </motion.div>
          </AnimatePresence>

          {/* Overlaid content directly on top of the cover photo */}
          <div className="relative z-10 flex h-full min-h-[420px] sm:min-h-[490px] flex-col justify-between p-6 sm:p-8">
            <div className="flex items-center justify-end gap-3">
              {activeCategory && (
                <Link
                  to={buildCategoryPath(activeCategory.slug)}
                  className="inline-flex items-center rounded-full bg-secondary-light px-3 py-1 text-xs font-bold text-primary hover:bg-white transition-colors"
                >
                  {activeCategory.name}
                </Link>
              )}
            </div>

            <div className="space-y-5">
              <AnimatePresence mode="wait">
                <motion.div
                  key={`copy-${activeStory.id}`}
                  initial={prefersReducedMotion ? { opacity: 1 } : { opacity: 0, y: 12 }}
                  animate={{ opacity: 1, y: 0 }}
                  exit={prefersReducedMotion ? { opacity: 0 } : { opacity: 0, y: -8 }}
                  transition={{ duration: 0.32, ease: [0.16, 1, 0.3, 1] }}
                  className="space-y-3.5"
                >
                  <div className="flex flex-wrap items-center gap-2 text-xs text-slate-200 tabular-nums">
                    <span>{activeStory.readingTimeMinutes} phút đọc</span>
                    <span aria-hidden="true">•</span>
                    <span>{formatViewCount(activeStory.viewCount)}</span>
                    <span aria-hidden="true">•</span>
                    <time dateTime={activeStory.publishedAt ?? activeStory.createdAt}>
                      {formatRelativeTime(activeStory.publishedAt ?? activeStory.createdAt)}
                    </time>
                  </div>

                  <h2
                    id="lead-story-heading"
                    className="text-2xl sm:text-3xl lg:text-[34px] font-extrabold tracking-[-0.025em] leading-[1.18] text-white"
                  >
                    <Link
                      to={buildArticleDetailPath(activeStory.slug)}
                      className="hover:text-secondary transition-colors"
                    >
                      {activeStory.title}
                    </Link>
                  </h2>

                  {activeStory.excerpt && (
                    <p className="text-sm sm:text-base leading-relaxed text-slate-200 max-w-[68ch] line-clamp-2">
                      {activeStory.excerpt}
                    </p>
                  )}

                  <div className="flex flex-wrap items-center justify-between gap-3 pt-2 border-t border-white/15 text-xs text-slate-200">
                    <div className="flex flex-wrap items-center gap-2">
                      <UserAvatar
                        fullName={activeStory.author.fullName}
                        avatarUrl={activeStory.author.avatarUrl}
                        size="sm"
                        className="h-7 w-7 text-[11px]"
                      />
                      <span className="font-semibold text-white">{activeStory.author.fullName}</span>
                      {activeStory.companyName && (
                        <span className="rounded-full border border-white/25 bg-white/10 px-2.5 py-0.5 text-[11px] font-semibold text-secondary">
                          {activeStory.companyName}
                        </span>
                      )}
                    </div>

                    <Link
                      to={buildArticleDetailPath(activeStory.slug)}
                      className="font-semibold text-secondary hover:text-white transition-colors"
                    >
                      Đọc chi tiết →
                    </Link>
                  </div>
                </motion.div>
              </AnimatePresence>

              {/* Sleek progress lines directly below the image copy */}
              {frontPageSlides.length > 1 && (
                <div
                  role="tablist"
                  aria-label="Chuyển nhanh bài báo trang nhất"
                  className="flex items-center gap-2 pt-1"
                >
                  {frontPageSlides.map((story, idx) => {
                    const isActive = idx === safeIndex
                    const isCompleted = idx < safeIndex
                    const catLabel = story.categories[0]?.name ?? `Tin ${idx + 1}`

                    return (
                      <button
                        key={story.id}
                        type="button"
                        role="tab"
                        aria-selected={isActive}
                        aria-label={`${catLabel}: ${story.title}`}
                        title={`${catLabel}: ${story.title}`}
                        onClick={() => setActiveIndex(idx)}
                        className="group/seg flex-1 py-2 cursor-pointer focus-visible:outline-2 focus-visible:outline-secondary rounded-full"
                      >
                        <span className="relative block h-[3px] w-full overflow-hidden rounded-full bg-white/25 group-hover/seg:bg-white/40 transition-colors">
                          {isCompleted && (
                            <span className="absolute inset-0 rounded-full bg-white/80" />
                          )}
                          {isActive && (
                            <motion.span
                              key={`progress-${story.id}-${isPaused ? 'paused' : 'run'}`}
                              initial={{ scaleX: 0 }}
                              animate={{ scaleX: 1 }}
                              transition={
                                isPaused || prefersReducedMotion
                                  ? { duration: 0.15 }
                                  : { duration: ROTATE_INTERVAL_MS / 1000, ease: 'linear' }
                              }
                              style={{ transformOrigin: '0% 50%' }}
                              className="absolute inset-0 rounded-full bg-secondary"
                            />
                          )}
                        </span>
                      </button>
                    )
                  })}
                </div>
              )}
            </div>
          </div>
        </div>
      </article>

      {/* Right 4 cols: CNBC-style Tin LIVE mới nhất (Chronological Wire) */}
      <aside
        id="live-wire"
        aria-labelledby="live-wire-heading"
        className="lg:col-span-4 rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-5 sm:p-6 flex flex-col"
      >
        <div className="flex items-center justify-between gap-2 pb-3.5 border-b-2 border-primary dark:border-secondary">
          <h2
            id="live-wire-heading"
            className="text-base font-extrabold uppercase tracking-wide text-neutral-text-main dark:text-dark-text-main"
          >
            Tin mới nhất
          </h2>
          <span className="inline-flex items-center gap-1.5 text-[11px] font-bold uppercase tracking-wider text-accent-crimson">
            <span aria-hidden="true" className="h-1.5 w-1.5 rounded-full bg-accent-crimson animate-pulse" />
            LIVE
          </span>
        </div>

        <ol className="relative mt-4 flex-1 space-y-4 border-l border-neutral-border dark:border-dark-border pl-4">
          {liveWireArticles.map((item) => {
            const cat = item.categories[0]
            return (
              <li
                key={item.id}
                className="group relative pb-3.5 last:pb-0 border-b last:border-b-0 border-neutral-border dark:border-dark-border"
              >
                <span
                  aria-hidden="true"
                  className="absolute -left-[20.5px] top-1.5 h-2 w-2 rounded-full border-2 border-primary dark:border-secondary bg-neutral-surface dark:bg-dark-surface"
                />

                <div className="flex flex-wrap items-center gap-1.5 text-[11px] text-neutral-text-muted dark:text-dark-text-muted">
                  <time
                    dateTime={item.publishedAt ?? item.createdAt}
                    className="font-mono font-bold text-primary dark:text-secondary tabular-nums"
                  >
                    {formatTickerTime(item.publishedAt ?? item.createdAt)}
                  </time>
                  {cat && (
                    <>
                      <span aria-hidden="true">•</span>
                      <Link
                        to={buildCategoryPath(cat.slug)}
                        className="font-semibold hover:text-primary dark:hover:text-secondary transition-colors"
                      >
                        {cat.name}
                      </Link>
                    </>
                  )}
                </div>

                <h3 className="mt-1 text-sm font-semibold leading-snug text-neutral-text-main dark:text-dark-text-main line-clamp-2 group-hover:text-primary dark:group-hover:text-secondary transition-colors">
                  <Link to={buildArticleDetailPath(item.slug)}>{item.title}</Link>
                </h3>
              </li>
            )
          })}
        </ol>
      </aside>
    </section>
  )
}
