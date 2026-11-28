import { useEffect, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { EmptyState } from '@/components/common/EmptyState'
import { Skeleton } from '@/components/common/Skeleton'
import { ROUTES } from '@/routes/paths'
import { toAppClientError } from '@/services/apiClient'
import { formatPublishedDate } from '@/utils/date'
import { ArticleAuthorBar } from '../components/ArticleAuthorBar'
import { ArticleRichContent } from '../components/ArticleRichContent'
import { ImageLightboxModal } from '../components/ImageLightboxModal'
import { useDraftPreview } from '../hooks/useArticleDetail'

export function DraftPreviewPage() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const token = searchParams.get('token')
  const [lightboxImage, setLightboxImage] = useState<{ src: string; alt?: string } | null>(null)

  const { data: article, isLoading, isError, error } = useDraftPreview(token)

  // Chặn công cụ tìm kiếm lập chỉ mục & không rò rỉ token qua Referrer
  useEffect(() => {
    const robotsMeta = document.createElement('meta')
    robotsMeta.name = 'robots'
    robotsMeta.content = 'noindex, nofollow'

    const referrerMeta = document.createElement('meta')
    referrerMeta.name = 'referrer'
    referrerMeta.content = 'no-referrer'

    document.head.appendChild(robotsMeta)
    document.head.appendChild(referrerMeta)

    return () => {
      robotsMeta.remove()
      referrerMeta.remove()
    }
  }, [])

  if (!token || !token.trim()) {
    return (
      <div className="w-full px-4 py-16 sm:px-6 lg:px-10 xl:px-14">
        <div className="mx-auto max-w-2xl">
          <EmptyState
            title="Thiếu mã xác thực xem trước bản nháp"
            description="Đường dẫn xem thử không chứa mã token hợp lệ. Vui lòng tạo lại liên kết xem trước từ bàn làm việc nhà báo."
            actionLabel="Quay lại trang chủ"
            onAction={() => navigate(ROUTES.HOME)}
          />
        </div>
      </div>
    )
  }

  if (isLoading) {
    return (
      <div className="w-full bg-neutral-surface dark:bg-dark-surface py-10">
        <div className="mx-auto max-w-3xl px-4 sm:px-6 space-y-6">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-14 w-full rounded-xl" />
          <Skeleton className="aspect-video w-full rounded-xl" />
          <div className="space-y-3">
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-5/6" />
            <Skeleton className="h-4 w-4/5" />
          </div>
        </div>
      </div>
    )
  }

  if (isError || !article) {
    const info = toAppClientError(error)
    return (
      <div className="w-full px-4 py-16 sm:px-6 lg:px-10 xl:px-14">
        <div className="mx-auto max-w-2xl">
          <EmptyState
            title="Liên kết xem trước không hợp lệ hoặc đã hết hạn"
            description={
              info.message ||
              'Liên kết xem thử bản nháp chỉ có hiệu lực tối đa 2 giờ kể từ thời điểm tạo.'
            }
            actionLabel="Quay lại trang chủ"
            onAction={() => navigate(ROUTES.HOME)}
          />
        </div>
      </div>
    )
  }

  return (
    <div className="w-full">
      {/* Internal Draft Preview Warning Banner */}
      <div
        role="status"
        className="w-full border-b border-amber-500/40 bg-amber-50 dark:bg-amber-950/40"
      >
        <div className="flex w-full flex-wrap items-center justify-between gap-3 px-4 py-3 sm:px-6 lg:px-10 xl:px-14 text-xs">
          <div className="flex flex-wrap items-center gap-2.5 text-amber-900 dark:text-amber-200">
            <span className="rounded-full bg-amber-600 px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-wider text-white">
              {article.status}
            </span>
            <span className="font-semibold">
              Chế độ xem trước nội bộ dành cho Tác giả & Ban biên tập — Không lập chỉ mục tìm kiếm
              (noindex).
            </span>
          </div>
          <span className="font-mono text-[11px] text-amber-800 dark:text-amber-300">
            Liên kết bảo mật giới hạn 2 giờ
          </span>
        </div>
      </div>

      <div className="w-full bg-neutral-surface dark:bg-dark-surface py-8 lg:py-12">
        <article className="mx-auto max-w-3xl px-4 sm:px-6 space-y-6">
          <div className="flex flex-wrap items-center gap-2 text-xs text-neutral-text-muted dark:text-dark-text-muted tabular-nums">
            {article.categories.map((cat) => (
              <span
                key={cat.id}
                className="rounded-full bg-secondary-light dark:bg-secondary/15 px-2.5 py-0.5 font-semibold text-primary dark:text-secondary"
              >
                {cat.name}
              </span>
            ))}
            <span>{article.readingTimeMinutes} phút đọc</span>
            <span aria-hidden="true">•</span>
            <span>Tạo lúc {formatPublishedDate(article.createdAt)}</span>
          </div>

          <h1 className="text-3xl sm:text-4xl font-extrabold tracking-[-0.025em] leading-[1.2] text-neutral-text-main dark:text-dark-text-main">
            {article.title}
          </h1>

          <ArticleAuthorBar
            article={article}
            isBookmarked={false}
            isBookmarkPending={false}
            onToggleBookmark={() => {}}
            onOpenReport={() => {}}
            isPreviewMode
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
                className="block w-full overflow-hidden rounded-xl border border-neutral-border dark:border-dark-border cursor-zoom-in"
              >
                <img
                  src={article.coverImage.url}
                  alt={article.title}
                  className="aspect-video w-full object-cover"
                />
              </button>
            </figure>
          )}

          <ArticleRichContent
            html={article.contentHtml}
            onImageClick={(src, alt) => setLightboxImage({ src, alt })}
          />
        </article>
      </div>

      <ImageLightboxModal image={lightboxImage} onClose={() => setLightboxImage(null)} />
    </div>
  )
}
