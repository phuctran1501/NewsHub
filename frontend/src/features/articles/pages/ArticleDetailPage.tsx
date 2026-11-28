import { useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { EmptyState } from '@/components/common/EmptyState'
import { Skeleton } from '@/components/common/Skeleton'
import {
  useFollowedCompanyIds,
  useToggleFollowCompany,
} from '@/features/companies/hooks/usePublicCompanies'
import { useAuth } from '@/hooks/useAuth'
import { buildCategoryPath, ROUTES } from '@/routes/paths'
import { toAppClientError } from '@/services/apiClient'
import { formatPublishedDate, formatViewCount } from '@/utils/date'
import { ArticleAiSummaryCard } from '../components/ArticleAiSummaryCard'
import { ArticleAuthorBar } from '../components/ArticleAuthorBar'
import { ArticleRichContent } from '../components/ArticleRichContent'
import { ImageLightboxModal } from '../components/ImageLightboxModal'
import { RelatedArticlesSection } from '../components/RelatedArticlesSection'
import { ReportArticleModal } from '../components/ReportArticleModal'
import {
  useArticleAiSummary,
  useArticleBySlug,
  useRegenerateAiSummary,
  useRelatedArticles,
  useReportArticle,
} from '../hooks/useArticleDetail'
import { useBookmarkedArticleIds, useToggleBookmark } from '../hooks/useBookmarks'

function ArticleDetailSkeleton() {
  return (
    <div className="w-full bg-neutral-surface dark:bg-dark-surface py-8 lg:py-12">
      <div className="w-full px-4 sm:px-6 lg:px-10 xl:px-14">
        <div className="mx-auto max-w-290 grid grid-cols-1 lg:grid-cols-12 gap-10">
          <div className="lg:col-span-8 space-y-6">
            <Skeleton className="h-4 w-56" />
            <div className="space-y-2.5">
              <Skeleton className="h-9 w-full" />
              <Skeleton className="h-9 w-4/5" />
            </div>
            <Skeleton className="h-16 w-full rounded-xl" />
            <Skeleton className="h-44 w-full rounded-xl" />
            <Skeleton className="aspect-video w-full rounded-xl" />
            <div className="space-y-3">
              <Skeleton className="h-4 w-full" />
              <Skeleton className="h-4 w-11/12" />
              <Skeleton className="h-4 w-full" />
              <Skeleton className="h-4 w-3/4" />
            </div>
          </div>
          <div className="lg:col-span-4 space-y-6">
            <Skeleton className="h-40 w-full rounded-xl" />
            <Skeleton className="h-28 w-full rounded-xl" />
          </div>
        </div>
      </div>
    </div>
  )
}

export function ArticleDetailPage() {
  const { slug } = useParams<{ slug: string }>()
  const navigate = useNavigate()
  const location = useLocation()
  const { user, roleName, isAuthenticated } = useAuth()

  const [lightboxImage, setLightboxImage] = useState<{ src: string; alt?: string } | null>(null)
  const [isReportOpen, setIsReportOpen] = useState(false)
  const [requestedSummaryArticleId, setRequestedSummaryArticleId] = useState<string | null>(null)

  const {
    data: article,
    isLoading,
    isError,
    error,
    refetch,
  } = useArticleBySlug(slug)

  const isPublished = article?.status === 'Published'
  const hasRequestedSummary =
    isAuthenticated && Boolean(article?.id) && requestedSummaryArticleId === article?.id

  const {
    data: aiSummary,
    isLoading: isLoadingSummary,
    isFetching: isFetchingSummary,
    refetch: refetchSummary,
  } = useArticleAiSummary(article?.id, Boolean(isPublished) && hasRequestedSummary)

  const { data: relatedArticles = [], isLoading: isLoadingRelated } = useRelatedArticles(
    article?.id,
    4,
  )

  const { bookmarkedIds } = useBookmarkedArticleIds()
  const toggleBookmark = useToggleBookmark()
  const regenerateAi = useRegenerateAiSummary(article?.id ?? '')
  const reportArticle = useReportArticle(article?.id ?? '')

  const { followedIds } = useFollowedCompanyIds()
  const toggleFollow = useToggleFollowCompany()

  if (isLoading) {
    return <ArticleDetailSkeleton />
  }

  if (isError || !article) {
    const errorInfo = toAppClientError(error)
    const isNotFound = errorInfo.status === 404
    return (
      <div className="w-full px-4 py-16 sm:px-6 lg:px-10 xl:px-14">
        <div className="mx-auto max-w-2xl">
          <EmptyState
            title={isNotFound ? 'Không tìm thấy bài viết' : 'Không thể tải nội dung bài viết'}
            description={
              isNotFound
                ? 'Bài viết có thể đã bị gỡ bỏ, chưa xuất bản hoặc đường dẫn không còn tồn tại.'
                : errorInfo.message
            }
            actionLabel={isNotFound ? 'Quay lại trang chủ' : 'Thử lại'}
            onAction={() => {
              if (isNotFound) {
                navigate(ROUTES.HOME)
              } else {
                void refetch()
              }
            }}
          />
        </div>
      </div>
    )
  }

  const primaryCategory = article.categories[0]
  const isBookmarked = bookmarkedIds.has(article.id)
  const canRegenerateAi =
    isAuthenticated && (user?.id === article.author.id || roleName === 'Admin')
  const isFollowingCompany = article.companyId ? followedIds.has(article.companyId) : false
  const companyDisplayName = article.companyName ?? article.sourceName

  function handleFollowCompany() {
    if (!article?.companyId) return
    if (!isAuthenticated) {
      const redirect = encodeURIComponent(`${location.pathname}${location.search}`)
      navigate(`${ROUTES.AUTH.LOGIN}?redirect=${redirect}`)
      return
    }
    toggleFollow.mutate({
      companyId: article.companyId,
      companyName: companyDisplayName ?? 'Tòa soạn',
      isCurrentlyFollowing: isFollowingCompany,
    })
  }

  return (
    <div className="w-full">
      <div className="w-full border-b border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface py-8 lg:py-12">
        <div className="w-full px-4 sm:px-6 lg:px-10 xl:px-14">
          <div className="mx-auto max-w-290 grid grid-cols-1 lg:grid-cols-12 gap-10 items-start">
            {/* Primary Reading Column (8/12) */}
            <article className="lg:col-span-8 space-y-6">
              <nav
                aria-label="Đường dẫn bài viết"
                className="flex flex-wrap items-center gap-2 text-xs text-neutral-text-muted dark:text-dark-text-muted tabular-nums"
              >
                <Link to={ROUTES.HOME} className="hover:text-primary dark:hover:text-secondary">
                  Trang chủ
                </Link>
                {primaryCategory && (
                  <>
                    <span aria-hidden="true">/</span>
                    <Link
                      to={buildCategoryPath(primaryCategory.slug)}
                      className="font-semibold text-primary dark:text-secondary hover:underline"
                    >
                      {primaryCategory.name}
                    </Link>
                  </>
                )}
                <span aria-hidden="true">•</span>
                <span>{article.readingTimeMinutes} phút đọc</span>
                <span aria-hidden="true">•</span>
                <span>{formatViewCount(article.viewCount)}</span>
                <span aria-hidden="true">•</span>
                <time dateTime={article.publishedAt ?? article.createdAt}>
                  {formatPublishedDate(article.publishedAt ?? article.createdAt)}
                </time>
              </nav>

              <h1 className="text-3xl sm:text-4xl font-extrabold tracking-tight leading-[1.2] text-neutral-text-main dark:text-dark-text-main">
                {article.title}
              </h1>

              <ArticleAuthorBar
                article={article}
                isBookmarked={isBookmarked}
                isBookmarkPending={toggleBookmark.isPending}
                onToggleBookmark={() =>
                  toggleBookmark.mutate({
                    articleId: article.id,
                    articleTitle: article.title,
                    articleSlug: article.slug,
                    isCurrentlyBookmarked: isBookmarked,
                  })
                }
                onOpenReport={() => setIsReportOpen(true)}
              />

              {article.coverImage?.url && (
                <figure className="space-y-2">
                  <button
                    type="button"
                    onClick={() =>
                      setLightboxImage({
                        src: article.coverImage!.url,
                        alt: article.title,
                      })
                    }
                    className="group block w-full overflow-hidden rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg cursor-zoom-in focus-visible:outline-2 focus-visible:outline-primary"
                  >
                    <img
                      src={article.coverImage.url}
                      alt={article.title}
                      className="aspect-video w-full object-cover transition-transform duration-300 group-hover:scale-[1.015]"
                    />
                  </button>
                  <figcaption className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
                    {article.title}
                    {companyDisplayName ? ` — Ảnh: ${companyDisplayName}` : ''}
                  </figcaption>
                </figure>
              )}

              <ArticleRichContent
                html={article.contentHtml}
                onImageClick={(src, alt) => setLightboxImage({ src, alt })}
              />

              {/* Source & Last Updated Footer */}
              <div className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg px-4 py-3 text-xs text-neutral-text-muted dark:text-dark-text-muted">
                <span>
                  Nguồn tin:{' '}
                  {article.sourceUrl ? (
                    <a
                      href={article.sourceUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="font-semibold text-primary dark:text-secondary hover:underline"
                    >
                      {companyDisplayName ?? article.author.fullName}
                    </a>
                  ) : (
                    <strong className="font-semibold text-neutral-text-main dark:text-dark-text-main">
                      {companyDisplayName ?? article.author.fullName}
                    </strong>
                  )}
                </span>
                {article.updatedAt && (
                  <span className="tabular-nums">
                    Cập nhật lần cuối: {formatPublishedDate(article.updatedAt)}
                  </span>
                )}
              </div>
            </article>

            {/* Right Sticky Editorial Sidebar (4/12) */}
            <aside className="lg:col-span-4 lg:sticky lg:top-32 space-y-6">
              {article.companyId && companyDisplayName && (
                <div className="rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg p-5 space-y-4">
                  <h2 className="border-b border-neutral-border dark:border-dark-border pb-2.5 text-xs font-bold uppercase tracking-wider text-primary dark:text-secondary">
                    Thông tin tòa soạn
                  </h2>

                  <div className="flex items-center justify-between gap-3">
                    <div className="min-w-0">
                      <div className="truncate text-sm font-bold text-neutral-text-main dark:text-dark-text-main">
                        {companyDisplayName}
                      </div>
                      <div className="text-[11px] text-neutral-text-muted dark:text-dark-text-muted">
                        Tòa soạn đã xác thực trên NewsHub
                      </div>
                    </div>

                    <Button
                      type="button"
                      variant={isFollowingCompany ? 'outline' : 'primary'}
                      size="sm"
                      isLoading={toggleFollow.isPending}
                      onClick={handleFollowCompany}
                      className="shrink-0"
                    >
                      {isFollowingCompany ? 'Đang theo dõi' : '+ Theo dõi'}
                    </Button>
                  </div>

                  <p className="text-xs leading-relaxed text-neutral-text-muted dark:text-dark-text-muted">
                    Theo dõi tòa soạn để nhận thông báo mỗi khi có bài viết mới được kiểm duyệt và
                    xuất bản.
                  </p>
                </div>
              )}

              {article.categories.length > 0 && (
                <div className="rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg p-5 space-y-3">
                  <h2 className="border-b border-neutral-border dark:border-dark-border pb-2.5 text-sm font-bold text-neutral-text-main dark:text-dark-text-main">
                    Chuyên mục bài viết
                  </h2>
                  <div className="flex flex-wrap gap-2">
                    {article.categories.map((cat) => (
                      <Link
                        key={cat.id}
                        to={buildCategoryPath(cat.slug)}
                        className="rounded-full bg-secondary-light dark:bg-secondary/15 px-3 py-1 text-xs font-semibold text-primary dark:text-secondary hover:opacity-85 transition-opacity"
                      >
                        {cat.name}
                      </Link>
                    ))}
                  </div>
                </div>
              )}

              {isPublished && (
                <ArticleAiSummaryCard
                  hasRequested={hasRequestedSummary}
                  summary={aiSummary}
                  isLoading={isLoadingSummary}
                  isFetching={isFetchingSummary}
                  canRegenerate={canRegenerateAi}
                  isRegenerating={regenerateAi.isPending}
                  onRequestSummary={() => setRequestedSummaryArticleId(article.id)}
                  onRegenerate={() => regenerateAi.mutate()}
                  onRefresh={() => void refetchSummary()}
                />
              )}
            </aside>
          </div>
        </div>
      </div>

      <RelatedArticlesSection articles={relatedArticles} isLoading={isLoadingRelated} />

      <ImageLightboxModal image={lightboxImage} onClose={() => setLightboxImage(null)} />

      <ReportArticleModal
        open={isReportOpen}
        onOpenChange={setIsReportOpen}
        isSubmitting={reportArticle.isPending}
        onSubmit={async (payload) => {
          await reportArticle.mutateAsync(payload)
        }}
      />
    </div>
  )
}
