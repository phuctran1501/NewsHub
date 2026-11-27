import { Link } from 'react-router-dom'
import { Badge } from '@/components/common/Badge'
import { buildArticleDetailPath } from '@/routes/paths'
import type { SemanticSearchResultDto } from '@/types/search'
import { formatPublishedDate } from '@/utils/date'

interface SemanticResultCardProps {
  item: SemanticSearchResultDto
}

function getRelevanceLabel(percent: number): string {
  if (percent >= 85) return 'Khớp ngữ nghĩa cao'
  if (percent >= 72) return 'Khớp ngữ nghĩa'
  return 'Liên quan'
}

export function SemanticResultCard({ item }: SemanticResultCardProps) {
  const clampedScore = Math.max(0, Math.min(1, item.similarityScore))
  const percent = Math.round(clampedScore * 100)
  const href = buildArticleDetailPath(item.slug)

  return (
    <article className="group flex flex-col sm:flex-row sm:items-center justify-between gap-4 rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface px-5 py-5 sm:px-6 hover:border-primary dark:hover:border-secondary-dark transition-colors duration-150">
      <div className="space-y-2 min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-2 text-xs text-neutral-text-muted dark:text-dark-text-muted">
          <Badge variant="category">{getRelevanceLabel(percent)}</Badge>
          <span aria-hidden="true">•</span>
          <time dateTime={item.publishedAt ?? undefined} className="tabular-nums">
            Xuất bản: {formatPublishedDate(item.publishedAt)}
          </time>
        </div>

        <h2 className="text-base sm:text-lg font-bold leading-snug text-neutral-text-main dark:text-dark-text-main group-hover:text-primary dark:group-hover:text-secondary transition-colors">
          <Link to={href} className="focus-visible:outline-2 focus-visible:outline-primary rounded">
            {item.title}
          </Link>
        </h2>

        <div>
          <Link
            to={href}
            className="text-xs font-semibold text-primary dark:text-secondary hover:underline"
          >
            Đọc bài viết →
          </Link>
        </div>
      </div>

      <div className="flex sm:flex-col items-center sm:items-end justify-between sm:justify-center gap-2 shrink-0 sm:min-w-32 pt-3 sm:pt-0 border-t sm:border-t-0 border-neutral-border dark:border-dark-border">
        <span className="text-xs font-bold text-primary dark:text-secondary tabular-nums">
          {percent}% tương đồng
        </span>
        <div
          aria-hidden="true"
          className="h-1.5 w-28 overflow-hidden rounded-full bg-neutral-border dark:bg-dark-border"
        >
          <div
            className="h-full rounded-full bg-primary dark:bg-secondary transition-[width] duration-300"
            style={{ width: `${percent}%` }}
          />
        </div>
      </div>
    </article>
  )
}
