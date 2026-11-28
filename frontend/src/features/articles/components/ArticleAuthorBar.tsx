import { Bookmark, Check, Flag, Share2 } from 'lucide-react'
import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { UserAvatar } from '@/components/common/UserAvatar'
import { toast } from '@/components/ui/toastManager'
import { useAuth } from '@/hooks/useAuth'
import { ROUTES } from '@/routes/paths'
import type { ArticleDetailDto } from '@/types/article'
import { cn } from '@/utils/cn'

interface ArticleAuthorBarProps {
  article: ArticleDetailDto
  isBookmarked: boolean
  isBookmarkPending: boolean
  onToggleBookmark: () => void
  onOpenReport: () => void
  isPreviewMode?: boolean
}

export function ArticleAuthorBar({
  article,
  isBookmarked,
  isBookmarkPending,
  onToggleBookmark,
  onOpenReport,
  isPreviewMode = false,
}: ArticleAuthorBarProps) {
  const { isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [copied, setCopied] = useState(false)

  const displayNewsroom = article.companyName ?? article.sourceName

  function redirectToLogin() {
    const redirect = encodeURIComponent(`${location.pathname}${location.search}`)
    navigate(`${ROUTES.AUTH.LOGIN}?redirect=${redirect}`)
  }

  function handleBookmarkClick() {
    if (!isAuthenticated) {
      redirectToLogin()
      return
    }
    onToggleBookmark()
  }

  function handleReportClick() {
    if (!isAuthenticated) {
      redirectToLogin()
      return
    }
    onOpenReport()
  }

  async function handleShareClick() {
    const shareUrl = window.location.href
    try {
      if (navigator.share) {
        await navigator.share({
          title: article.title,
          url: shareUrl,
        })
        return
      }
      await navigator.clipboard.writeText(shareUrl)
      setCopied(true)
      toast.success('Đã sao chép liên kết', 'Bạn có thể dán liên kết bài viết để chia sẻ ngay.')
      window.setTimeout(() => setCopied(false), 2000)
    } catch {
      // Người dùng đóng hộp thoại chia sẻ hoặc trình duyệt chặn clipboard
    }
  }

  return (
    <div className="flex flex-wrap items-center justify-between gap-4 border-y border-neutral-border dark:border-dark-border py-4">
      <div className="flex items-center gap-3 min-w-0">
        <UserAvatar
          fullName={article.author.fullName}
          avatarUrl={article.author.avatarUrl}
          size="md"
        />

        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-sm font-bold text-neutral-text-main dark:text-dark-text-main truncate">
              {article.author.fullName}
            </span>
            {displayNewsroom && (
              <a
                href="/#companies"
                className="rounded-full bg-secondary-light dark:bg-secondary/15 px-2.5 py-0.5 text-[11px] font-semibold text-primary dark:text-secondary hover:underline truncate max-w-55"
              >
                {displayNewsroom}
              </a>
            )}
          </div>
          <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
            {article.companyId ? 'Phóng viên thuộc Tòa soạn xác thực' : 'Phóng viên độc lập NewsHub'}
          </p>
        </div>
      </div>

      {!isPreviewMode && (
        <div className="flex items-center gap-2 shrink-0">
          <button
            type="button"
            disabled={isBookmarkPending}
            onClick={handleBookmarkClick}
            aria-pressed={isBookmarked}
            className={cn(
              'inline-flex h-9 items-center gap-1.5 rounded-[10px] border px-3.5 text-xs font-semibold transition-colors duration-150 cursor-pointer disabled:opacity-60',
              isBookmarked
                ? 'border-primary bg-secondary-light text-primary dark:border-secondary dark:bg-secondary/15 dark:text-secondary'
                : 'border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface text-neutral-text-main dark:text-dark-text-main hover:border-primary dark:hover:border-secondary',
            )}
          >
            <Bookmark
              size={15}
              strokeWidth={1.75}
              className={cn(isBookmarked && 'fill-current')}
            />
            <span>{isBookmarked ? 'Đã lưu' : 'Lưu bài'}</span>
          </button>

          <button
            type="button"
            onClick={() => void handleShareClick()}
            className="inline-flex h-9 items-center gap-1.5 rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface px-3 text-xs font-semibold text-neutral-text-main dark:text-dark-text-main hover:border-primary dark:hover:border-secondary transition-colors duration-150 cursor-pointer"
          >
            {copied ? <Check size={15} strokeWidth={1.75} /> : <Share2 size={15} strokeWidth={1.75} />}
            <span>{copied ? 'Đã chép' : 'Chia sẻ'}</span>
          </button>

          <button
            type="button"
            onClick={handleReportClick}
            aria-label="Báo cáo vi phạm bài viết"
            title="Báo cáo vi phạm nội dung"
            className="inline-flex h-9 w-9 items-center justify-center rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface text-neutral-text-muted dark:text-dark-text-muted hover:border-accent-crimson hover:text-accent-crimson transition-colors duration-150 cursor-pointer"
          >
            <Flag size={15} strokeWidth={1.75} />
          </button>
        </div>
      )}
    </div>
  )
}
