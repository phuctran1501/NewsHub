import { RefreshCw } from 'lucide-react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { Skeleton } from '@/components/common/Skeleton'
import { useAuth } from '@/hooks/useAuth'
import { buildSearchPath, ROUTES } from '@/routes/paths'
import type { ArticleAiSummaryDto } from '@/types/article'
import { cn } from '@/utils/cn'

interface ArticleAiSummaryCardProps {
  hasRequested: boolean
  summary: ArticleAiSummaryDto | undefined
  isLoading: boolean
  isFetching: boolean
  canRegenerate: boolean
  isRegenerating: boolean
  onRequestSummary: () => void
  onRegenerate: () => void
  onRefresh: () => void
}

const SENTIMENT_META: Record<string, { label: string; className: string }> = {
  Positive: {
    label: 'Tích cực',
    className:
      'border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-500/30 dark:bg-emerald-500/15 dark:text-emerald-300',
  },
  Neutral: {
    label: 'Trung lập',
    className:
      'border-neutral-border bg-neutral-surface text-neutral-text-muted dark:border-dark-border dark:bg-dark-surface dark:text-dark-text-muted',
  },
  Mixed: {
    label: 'Nhiều chiều',
    className:
      'border-primary/25 bg-secondary-light text-primary dark:border-secondary/30 dark:bg-secondary/15 dark:text-secondary',
  },
  Negative: {
    label: 'Tiêu cực',
    className:
      'border-red-200 bg-red-50 text-accent-crimson dark:border-red-500/30 dark:bg-red-500/15 dark:text-red-300',
  },
}

export function ArticleAiSummaryCard({
  hasRequested,
  summary,
  isLoading,
  isFetching,
  canRegenerate,
  isRegenerating,
  onRequestSummary,
  onRegenerate,
  onRefresh,
}: ArticleAiSummaryCardProps) {
  const { isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const searchMode = isAuthenticated ? 'ai' : 'keyword'

  function handleLoginRedirect() {
    const redirect = encodeURIComponent(`${location.pathname}${location.search}`)
    navigate(`${ROUTES.AUTH.LOGIN}?redirect=${redirect}`)
  }

  const isProcessing = hasRequested && summary?.status === 'Processing'
  const showSkeleton = hasRequested && (isLoading || (isFetching && !summary))
  const sentimentInfo =
    (summary?.sentiment && SENTIMENT_META[summary.sentiment]) || SENTIMENT_META.Neutral
  const confidencePct =
    typeof summary?.confidenceScore === 'number'
      ? Math.round(summary.confidenceScore * 100)
      : null

  return (
    <section
      aria-labelledby="ai-summary-heading"
      className="rounded-xl border border-primary/30 dark:border-secondary/35 bg-neutral-bg dark:bg-dark-bg p-5 space-y-3.5"
    >
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-neutral-border dark:border-dark-border pb-2.5">
        <div className="flex flex-wrap items-center gap-2">
          <h2
            id="ai-summary-heading"
            className="text-sm font-bold text-primary dark:text-secondary"
          >
            Tóm tắt nhanh bằng AI
          </h2>

          {hasRequested && !showSkeleton && !isProcessing && summary && (
            <span
              className={cn(
                'rounded-full border px-2 py-0.5 text-[10px] font-semibold',
                sentimentInfo.className,
              )}
            >
              {sentimentInfo.label}
            </span>
          )}
        </div>

        {hasRequested && isProcessing && (
          <button
            type="button"
            onClick={onRefresh}
            className="inline-flex items-center gap-1 text-[11px] font-semibold text-primary dark:text-secondary hover:underline cursor-pointer"
          >
            <RefreshCw size={12} strokeWidth={1.75} className={cn(isFetching && 'animate-spin')} />
            <span>Kiểm tra lại</span>
          </button>
        )}

        {hasRequested && !showSkeleton && !isProcessing && canRegenerate && (
          <button
            type="button"
            disabled={isRegenerating}
            onClick={onRegenerate}
            className="inline-flex items-center gap-1 text-[11px] font-semibold text-primary dark:text-secondary hover:underline disabled:opacity-50 cursor-pointer"
          >
            <RefreshCw
              size={12}
              strokeWidth={1.75}
              className={cn(isRegenerating && 'animate-spin')}
            />
            <span>{isRegenerating ? 'Đang tạo...' : 'Tạo lại'}</span>
          </button>
        )}
      </div>

      {/* State 1: Chưa đăng nhập */}
      {!isAuthenticated ? (
        <div className="space-y-3">
          <p className="text-xs leading-relaxed text-neutral-text-muted dark:text-dark-text-muted">
            Đăng nhập tài khoản NewsHub để tóm tắt nhanh ý chính, sắc thái cảm xúc và từ khóa bài
            viết bằng AI.
          </p>
          <Button
            type="button"
            variant="primary"
            size="sm"
            onClick={handleLoginRedirect}
            className="w-full"
          >
            Đăng nhập để sử dụng dịch vụ AI
          </Button>
        </div>
      ) : !hasRequested ? (
        /* State 2: Đã đăng nhập, chờ người dùng chủ động bấm tóm tắt */
        <div className="space-y-3">
          <p className="text-xs leading-relaxed text-neutral-text-muted dark:text-dark-text-muted">
            Nhấn nút bên dưới để AI trích xuất 3–4 ý chính, đánh giá sắc thái và gợi ý từ khóa nổi
            bật của bài viết.
          </p>
          <Button
            type="button"
            variant="primary"
            size="sm"
            onClick={onRequestSummary}
            className="w-full"
          >
            Tóm tắt bài viết bằng AI
          </Button>
        </div>
      ) : showSkeleton ? (
        /* State 3: Skeleton khi đang tải */
        <div className="space-y-3 py-1" aria-label="Đang tải bản tóm tắt AI">
          <div className="space-y-2">
            <Skeleton className="h-3.5 w-full" />
            <Skeleton className="h-3.5 w-11/12" />
            <Skeleton className="h-3.5 w-5/6" />
          </div>
          <div className="space-y-2">
            <Skeleton className="h-3.5 w-full" />
            <Skeleton className="h-3.5 w-3/4" />
          </div>
          <div className="flex items-center gap-2 pt-1">
            <Skeleton className="h-5 w-16 rounded-full" />
            <Skeleton className="h-5 w-20 rounded-full" />
            <Skeleton className="h-5 w-14 rounded-full" />
          </div>
        </div>
      ) : isProcessing ? (
        /* State 4: Bài mới xuất bản, AI đang xử lý nền — hiển thị Skeleton kèm thông báo */
        <div className="space-y-3 py-1" aria-label="AI đang phân tích bài viết">
          <p className="text-xs font-medium text-neutral-text-main dark:text-dark-text-main">
            NewsHub AI đang tổng hợp ý chính bài viết này trong nền...
          </p>
          <div className="space-y-2">
            <Skeleton className="h-3 w-full" />
            <Skeleton className="h-3 w-10/12" />
            <Skeleton className="h-3 w-4/5" />
          </div>
          {canRegenerate && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              isLoading={isRegenerating}
              onClick={onRegenerate}
              className="w-full"
            >
              Kích hoạt tóm tắt AI ngay
            </Button>
          )}
        </div>
      ) : summary ? (
        /* State 5: Hoàn tất hiển thị bản tóm tắt */
        <div className="space-y-3.5">
          {summary.bulletPoints && summary.bulletPoints.length > 0 && (
            <ul className="space-y-2 text-xs sm:text-[13px] leading-relaxed text-neutral-text-main dark:text-dark-text-main">
              {summary.bulletPoints.map((point, idx) => (
                <li key={idx} className="flex items-start gap-2">
                  <span
                    aria-hidden="true"
                    className="mt-1.5 h-1.5 w-1.5 shrink-0 rounded-full bg-primary dark:bg-secondary"
                  />
                  <span>{point}</span>
                </li>
              ))}
            </ul>
          )}

          {summary.extractedTags && summary.extractedTags.length > 0 && (
            <div className="flex flex-wrap items-center gap-1.5 border-t border-neutral-border dark:border-dark-border pt-2.5">
              {summary.extractedTags.map((tag) => (
                <Link
                  key={tag}
                  to={buildSearchPath(tag, searchMode)}
                  className="rounded-full border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface px-2.5 py-0.5 text-[11px] font-medium text-neutral-text-muted dark:text-dark-text-muted hover:border-primary dark:hover:border-secondary hover:text-primary dark:hover:text-secondary transition-colors"
                >
                  #{tag}
                </Link>
              ))}
            </div>
          )}

          <div className="text-[11px] text-neutral-text-muted dark:text-dark-text-muted tabular-nums">
            Tổng hợp bởi NewsHub AI
            {confidencePct !== null ? ` • Độ tin cậy ${confidencePct}%` : ''}
          </div>
        </div>
      ) : (
        <div className="space-y-2.5 text-xs text-neutral-text-muted dark:text-dark-text-muted">
          <p>Không thể tải bản tóm tắt AI lúc này.</p>
          <Button type="button" variant="outline" size="sm" onClick={onRefresh} className="w-full">
            Thử lại
          </Button>
        </div>
      )}
    </section>
  )
}
