import { RefreshCw } from 'lucide-react'
import { Link } from 'react-router-dom'
import { Skeleton } from '@/components/common/Skeleton'
import { useAuth } from '@/hooks/useAuth'
import { buildSearchPath } from '@/routes/paths'
import type { ArticleAiSummaryDto } from '@/types/article'
import { cn } from '@/utils/cn'

interface ArticleAiSummaryCardProps {
  summary: ArticleAiSummaryDto | undefined
  isLoading: boolean
  canRegenerate: boolean
  isRegenerating: boolean
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
  summary,
  isLoading,
  canRegenerate,
  isRegenerating,
  onRegenerate,
  onRefresh,
}: ArticleAiSummaryCardProps) {
  const { isAuthenticated } = useAuth()
  const searchMode = isAuthenticated ? 'ai' : 'keyword'

  if (isLoading) {
    return (
      <section
        aria-label="Đang tải tóm tắt AI"
        className="rounded-xl border border-primary/25 dark:border-secondary/30 bg-neutral-bg dark:bg-dark-bg p-5 sm:p-6 space-y-4"
      >
        <div className="flex items-center justify-between">
          <Skeleton className="h-5 w-44" />
          <Skeleton className="h-5 w-20 rounded-full" />
        </div>
        <div className="space-y-2.5">
          <Skeleton className="h-4 w-full" />
          <Skeleton className="h-4 w-11/12" />
          <Skeleton className="h-4 w-4/5" />
        </div>
      </section>
    )
  }

  if (!summary) {
    return null
  }

  const isProcessing = summary.status === 'Processing'
  const sentimentInfo =
    (summary.sentiment && SENTIMENT_META[summary.sentiment]) || SENTIMENT_META.Neutral
  const confidencePct =
    typeof summary.confidenceScore === 'number'
      ? Math.round(summary.confidenceScore * 100)
      : null

  return (
    <section
      aria-labelledby="ai-summary-heading"
      className="rounded-xl border border-primary/30 dark:border-secondary/35 bg-neutral-bg dark:bg-dark-bg p-5 sm:p-6 space-y-4"
    >
      <div className="flex flex-wrap items-center justify-between gap-2.5 border-b border-neutral-border dark:border-dark-border pb-3">
        <div className="flex flex-wrap items-center gap-2.5">
          <h2
            id="ai-summary-heading"
            className="text-sm font-bold text-primary dark:text-secondary"
          >
            Tóm tắt nhanh bằng AI
          </h2>

          {!isProcessing && (
            <span
              className={cn(
                'rounded-full border px-2.5 py-0.5 text-[11px] font-semibold',
                sentimentInfo.className,
              )}
            >
              {sentimentInfo.label}
            </span>
          )}
        </div>

        <div className="flex items-center gap-3">
          {isProcessing ? (
            <button
              type="button"
              onClick={onRefresh}
              className="inline-flex items-center gap-1.5 text-xs font-semibold text-primary dark:text-secondary hover:underline cursor-pointer"
            >
              <RefreshCw size={13} strokeWidth={1.75} />
              <span>Kiểm tra lại</span>
            </button>
          ) : canRegenerate ? (
            <button
              type="button"
              disabled={isRegenerating}
              onClick={onRegenerate}
              className="inline-flex items-center gap-1.5 text-xs font-semibold text-primary dark:text-secondary hover:underline disabled:opacity-50 cursor-pointer"
            >
              <RefreshCw
                size={13}
                strokeWidth={1.75}
                className={cn(isRegenerating && 'animate-spin')}
              />
              <span>{isRegenerating ? 'Đang yêu cầu...' : 'Tạo lại tóm tắt AI'}</span>
            </button>
          ) : null}
        </div>
      </div>

      {isProcessing ? (
        <div className="space-y-2 py-1 text-xs text-neutral-text-muted dark:text-dark-text-muted">
          <p className="font-medium text-neutral-text-main dark:text-dark-text-main">
            Hệ thống NewsHub AI đang phân tích và tổng hợp ý chính bài viết này trong nền.
          </p>
          <p>Bản tóm tắt sẽ tự động hiển thị ngay khi tác vụ hoàn tất.</p>
        </div>
      ) : (
        <>
          {summary.bulletPoints && summary.bulletPoints.length > 0 && (
            <ul className="space-y-2.5 text-sm leading-relaxed text-neutral-text-main dark:text-dark-text-main">
              {summary.bulletPoints.map((point, idx) => (
                <li key={idx} className="flex items-start gap-2.5">
                  <span
                    aria-hidden="true"
                    className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-primary dark:bg-secondary"
                  />
                  <span>{point}</span>
                </li>
              ))}
            </ul>
          )}

          <div className="flex flex-wrap items-center justify-between gap-3 border-t border-neutral-border dark:border-dark-border pt-3">
            <div className="flex flex-wrap items-center gap-1.5">
              {summary.extractedTags?.map((tag) => (
                <Link
                  key={tag}
                  to={buildSearchPath(tag, searchMode)}
                  className="rounded-full border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface px-2.5 py-0.5 text-[11px] font-medium text-neutral-text-muted dark:text-dark-text-muted hover:border-primary dark:hover:border-secondary hover:text-primary dark:hover:text-secondary transition-colors"
                >
                  #{tag}
                </Link>
              ))}
            </div>

            <span className="text-[11px] text-neutral-text-muted dark:text-dark-text-muted tabular-nums">
              Tổng hợp bởi NewsHub AI
              {confidencePct !== null ? ` • Độ tin cậy ${confidencePct}%` : ''}
            </span>
          </div>
        </>
      )}
    </section>
  )
}
