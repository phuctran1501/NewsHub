import { useMemo } from 'react'
import { Link } from 'react-router-dom'
import { UserAvatar } from '@/components/common/UserAvatar'
import { buildArticleDetailPath, buildSearchPath } from '@/routes/paths'
import type { ArticleSummaryDto } from '@/types/article'

interface OpinionAndAiBriefColumnProps {
  articles: ArticleSummaryDto[]
}

const FALLBACK_BRIEF_POINTS = [
  'Kiến trúc tìm kiếm ngữ nghĩa pgvector giúp độc giả tra cứu báo chí theo ngữ cảnh chính xác hơn.',
  'Thị trường bán dẫn Đông Nam Á ghi nhận làn sóng đầu tư mở rộng nhà máy kiểm thử và đóng gói.',
  'Chuẩn hóa quy trình kiểm duyệt đa tầng giúp minh bạch nguồn tin giữa các tòa soạn số.',
]

export function OpinionAndAiBriefColumn({ articles }: OpinionAndAiBriefColumnProps) {
  const opinionArticles = useMemo(() => {
    if (articles.length === 0) return []
    const picked: ArticleSummaryDto[] = []
    const seenAuthors = new Set<string>()

    for (const article of articles) {
      if (picked.length >= 3) break
      if (!seenAuthors.has(article.author.id)) {
        seenAuthors.add(article.author.id)
        picked.push(article)
      }
    }

    for (const article of articles) {
      if (picked.length >= 3) break
      if (!picked.some((item) => item.id === article.id)) {
        picked.push(article)
      }
    }

    return picked
  }, [articles])

  const briefPoints = useMemo(() => {
    if (articles.length === 0) {
      return FALLBACK_BRIEF_POINTS.map((text, idx) => ({
        id: `brief-fb-${idx}`,
        text,
        slug: null as string | null,
      }))
    }

    return articles.slice(0, 3).map((article) => ({
      id: article.id,
      text: article.title,
      slug: article.slug,
    }))
  }, [articles])

  return (
    <aside
      id="opinion-col"
      aria-label="Góc nhìn và Điểm tin nhanh AI"
      className="lg:col-span-4 lg:sticky lg:top-32 space-y-6"
    >
      {/* Góc nhìn & Bình luận */}
      <div className="rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-5 space-y-4">
        <div className="flex items-center justify-between gap-2 border-b border-neutral-border dark:border-dark-border pb-3">
          <h2 className="text-base font-bold text-neutral-text-main dark:text-dark-text-main">
            Góc nhìn & Bình luận
          </h2>
          <Link
            to={buildSearchPath('phân tích', 'keyword')}
            className="text-xs font-semibold text-primary dark:text-secondary hover:underline"
          >
            Xem thêm
          </Link>
        </div>

        {opinionArticles.length > 0 ? (
          <div className="divide-y divide-neutral-border dark:divide-dark-border">
            {opinionArticles.map((article) => (
              <article key={article.id} className="py-3.5 first:pt-0 last:pb-0 space-y-2">
                <h3 className="text-sm font-bold leading-snug text-neutral-text-main dark:text-dark-text-main hover:text-primary dark:hover:text-secondary transition-colors line-clamp-2">
                  <Link to={buildArticleDetailPath(article.slug)}>{article.title}</Link>
                </h3>

                {article.excerpt && (
                  <p className="text-xs leading-relaxed text-neutral-text-muted dark:text-dark-text-muted line-clamp-2">
                    {article.excerpt}
                  </p>
                )}

                <div className="flex items-center gap-2.5 pt-1">
                  <UserAvatar
                    fullName={article.author.fullName}
                    avatarUrl={article.author.avatarUrl}
                    size="sm"
                    className="h-7 w-7 text-[11px]"
                  />
                  <div className="min-w-0 text-[11px]">
                    <div className="truncate font-semibold text-neutral-text-main dark:text-dark-text-main">
                      {article.author.fullName}
                    </div>
                    <div className="truncate text-neutral-text-muted dark:text-dark-text-muted">
                      {article.companyName ?? 'Phóng viên độc lập'}
                    </div>
                  </div>
                </div>
              </article>
            ))}
          </div>
        ) : (
          <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
            Các bài viết phân tích và bình luận chuyên sâu sẽ xuất hiện tại đây.
          </p>
        )}
      </div>

      {/* Điểm tin nhanh AI hôm nay */}
      <div className="rounded-xl border border-primary/30 dark:border-secondary/30 bg-neutral-surface dark:bg-dark-surface p-5 space-y-3.5">
        <div className="flex items-center justify-between gap-2 border-b border-neutral-border dark:border-dark-border pb-2.5">
          <h2 className="text-sm font-bold text-primary dark:text-secondary">
            Điểm tin nhanh AI hôm nay
          </h2>
          <span className="rounded-full bg-secondary-light dark:bg-secondary/15 px-2.5 py-0.5 text-[10px] font-semibold text-primary dark:text-secondary">
            60 giây
          </span>
        </div>

        <ol className="space-y-3 text-xs leading-relaxed text-neutral-text-muted dark:text-dark-text-muted">
          {briefPoints.map((point, idx) => (
            <li key={point.id} className="flex items-start gap-2.5">
              <span className="font-mono font-bold text-primary dark:text-secondary tabular-nums">
                0{idx + 1}.
              </span>
              {point.slug ? (
                <Link
                  to={buildArticleDetailPath(point.slug)}
                  className="hover:text-primary dark:hover:text-secondary transition-colors line-clamp-2"
                >
                  {point.text}
                </Link>
              ) : (
                <span className="line-clamp-2">{point.text}</span>
              )}
            </li>
          ))}
        </ol>
      </div>
    </aside>
  )
}
