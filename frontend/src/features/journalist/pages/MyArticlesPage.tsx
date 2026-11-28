import { useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Badge } from '@/components/common/Badge'
import { Button } from '@/components/common/Button'
import { EmptyState } from '@/components/common/EmptyState'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { useAuth } from '@/hooks/useAuth'
import {
  buildArticleDetailPath,
  buildCmsEditorPath,
  buildDraftPreviewPath,
  ROUTES,
} from '@/routes/paths'
import type { ArticleStatusValue, ArticleSummaryDto } from '@/types/article'
import { cn } from '@/utils/cn'
import { ArticleStatusBadge } from '../components/ArticleStatusBadge'
import { normalizeArticleStatus } from '../utils/articleStatus'
import { useMyCompanies } from '../hooks/useCompanyHub'
import {
  useArchiveArticle,
  useDeleteArticle,
  useGenerateDraftPreviewToken,
  useMyArticles,
} from '../hooks/useMyArticles'

const STATUS_TABS: ReadonlyArray<{ label: string; value?: ArticleStatusValue }> = [
  { label: 'Tất cả', value: undefined },
  { label: 'Bản nháp', value: 'Draft' },
  { label: 'Chờ duyệt', value: 'PendingCompanyReview' },
  { label: 'Đã xuất bản', value: 'Published' },
  { label: 'Đã lưu trữ', value: 'Archived' },
  { label: 'Bị từ chối', value: 'Rejected' },
]

const PAGE_SIZE = 10

function formatShortDate(iso: string | null): string {
  if (!iso) return '—'
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })
}

export function MyArticlesPage() {
  const navigate = useNavigate()
  const { roleName } = useAuth()
  const [selectedStatus, setSelectedStatus] = useState<ArticleStatusValue | undefined>(undefined)
  const [pageNumber, setPageNumber] = useState(1)
  const [articleToDelete, setArticleToDelete] = useState<ArticleSummaryDto | null>(null)

  const { data: companies = [] } = useMyCompanies()
  const { data, isLoading, isError, refetch } = useMyArticles({
    pageNumber,
    pageSize: PAGE_SIZE,
    status: selectedStatus,
  })

  const deleteMutation = useDeleteArticle()
  const archiveMutation = useArchiveArticle()
  const previewMutation = useGenerateDraftPreviewToken()

  const companyNameMap = useMemo(() => {
    const map = new Map<string, string>()
    companies.forEach((company) => map.set(company.id, company.name))
    return map
  }, [companies])

  const articles = data?.data ?? []
  const meta = data?.meta

  const canDeleteArticle = (article: ArticleSummaryDto): boolean => {
    const status = normalizeArticleStatus(article.status)
    const isCompanyLocked =
      Boolean(article.companyId) &&
      (status === 'Published' || status === 'Archived' || Boolean(article.publishedAt))
    if (isCompanyLocked && roleName !== 'Admin') {
      return false
    }
    return true
  }

  const handleOpenPreview = async (articleId: string) => {
    const tokenResult = await previewMutation.mutateAsync(articleId)
    window.open(buildDraftPreviewPath(tokenResult.token), '_blank', 'noopener,noreferrer')
  }

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="space-y-0.5">
          <h1 className="text-xl font-bold tracking-tight text-neutral-text-main dark:text-dark-text-main">
            Bài viết của tôi
          </h1>
          <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
            Quản lý toàn bộ bản nháp, bài chờ Tòa soạn kiểm duyệt và kho bài đã xuất bản.
          </p>
        </div>

        <Button type="button" variant="primary" size="sm" onClick={() => navigate(ROUTES.CMS.EDITOR)}>
          Viết bài mới
        </Button>
      </div>

      <div className="rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface overflow-hidden">
        <div
          role="tablist"
          aria-label="Lọc bài viết theo trạng thái"
          className="flex flex-wrap items-center gap-1.5 px-4 py-3 border-b border-neutral-border dark:border-dark-border bg-neutral-bg/60 dark:bg-dark-bg/60"
        >
          {STATUS_TABS.map((tab) => {
            const isActive = selectedStatus === tab.value
            return (
              <button
                key={tab.label}
                type="button"
                role="tab"
                aria-selected={isActive}
                onClick={() => {
                  setSelectedStatus(tab.value)
                  setPageNumber(1)
                }}
                className={cn(
                  'px-3 py-1.5 rounded-[8px] text-xs font-semibold border transition-colors duration-150 cursor-pointer',
                  isActive
                    ? 'bg-primary text-white border-primary dark:bg-secondary dark:text-dark-bg dark:border-secondary'
                    : 'bg-neutral-surface dark:bg-dark-surface text-neutral-text-muted dark:text-dark-text-muted border-neutral-border dark:border-dark-border hover:text-neutral-text-main dark:hover:text-dark-text-main',
                )}
              >
                {tab.label}
              </button>
            )
          })}
        </div>

        {isLoading ? (
          <div className="p-5 space-y-3">
            {Array.from({ length: 5 }).map((_, idx) => (
              <div
                key={idx}
                className="h-14 rounded-[10px] bg-neutral-bg dark:bg-dark-bg animate-pulse"
              />
            ))}
          </div>
        ) : isError ? (
          <div className="p-6">
            <EmptyState
              title="Không tải được danh sách bài viết"
              description="Kết nối máy chủ gặp gián đoạn. Vui lòng thử tải lại."
              actionLabel="Thử lại"
              onAction={() => void refetch()}
            />
          </div>
        ) : articles.length === 0 ? (
          <div className="p-6">
            <EmptyState
              title="Chưa có bài viết nào trong mục này"
              description="Bắt đầu soạn thảo bài báo mới để lưu nháp, xuất bản tự do hoặc nộp lên Tòa soạn."
              actionLabel="Soạn bài viết mới"
              onAction={() => navigate(ROUTES.CMS.EDITOR)}
            />
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="border-b border-neutral-border dark:border-dark-border text-[11px] font-semibold uppercase tracking-wider text-neutral-text-muted dark:text-dark-text-muted">
                    <th className="py-3 px-4">Bài viết</th>
                    <th className="py-3 px-4">Ngữ cảnh</th>
                    <th className="py-3 px-4">Trạng thái</th>
                    <th className="py-3 px-4">Ngày tạo / Xuất bản</th>
                    <th className="py-3 px-4 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-neutral-border dark:divide-dark-border text-sm">
                  {articles.map((article) => {
                    const status = normalizeArticleStatus(article.status)
                    const allowDelete = canDeleteArticle(article)
                    const companyLabel = article.companyId
                      ? article.companyName || companyNameMap.get(article.companyId) || 'Tòa soạn'
                      : null

                    return (
                      <tr
                        key={article.id}
                        className="hover:bg-neutral-bg/50 dark:hover:bg-dark-bg/40 transition-colors"
                      >
                        <td className="py-3.5 px-4 max-w-md">
                          <div className="space-y-1">
                            {status === 'Published' ? (
                              <Link
                                to={buildArticleDetailPath(article.slug)}
                                className="font-semibold text-neutral-text-main dark:text-dark-text-main hover:text-primary dark:hover:text-secondary line-clamp-2"
                              >
                                {article.title}
                              </Link>
                            ) : (
                              <p className="font-semibold text-neutral-text-main dark:text-dark-text-main line-clamp-2">
                                {article.title}
                              </p>
                            )}
                            <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
                              {article.categories.map((c) => c.name).join(', ') || 'Chưa gắn chuyên mục'}
                              {' · '}
                              {article.readingTimeMinutes} phút đọc
                              {status === 'Published' ? ` · ${article.viewCount.toLocaleString('vi-VN')} lượt xem` : ''}
                            </p>
                          </div>
                        </td>

                        <td className="py-3.5 px-4 whitespace-nowrap">
                          {companyLabel ? (
                            <Badge variant="category">{companyLabel}</Badge>
                          ) : (
                            <Badge variant="neutral">Tự do</Badge>
                          )}
                        </td>

                        <td className="py-3.5 px-4 whitespace-nowrap">
                          <ArticleStatusBadge status={article.status} />
                        </td>

                        <td className="py-3.5 px-4 whitespace-nowrap text-xs text-neutral-text-muted dark:text-dark-text-muted">
                          {formatShortDate(article.publishedAt ?? article.createdAt)}
                        </td>

                        <td className="py-3.5 px-4 whitespace-nowrap text-right">
                          <div className="inline-flex items-center gap-1.5">
                            <Button
                              type="button"
                              variant="secondary"
                              size="sm"
                              onClick={() => navigate(buildCmsEditorPath(article.id))}
                            >
                              Sửa
                            </Button>

                            {status !== 'Published' && (
                              <Button
                                type="button"
                                variant="outline"
                                size="sm"
                                isLoading={previewMutation.isPending}
                                onClick={() => void handleOpenPreview(article.id)}
                              >
                                Xem trước
                              </Button>
                            )}

                            {status === 'Published' && !article.companyId && (
                              <Button
                                type="button"
                                variant="outline"
                                size="sm"
                                isLoading={archiveMutation.isPending}
                                onClick={() => archiveMutation.mutate(article.id)}
                              >
                                Lưu trữ
                              </Button>
                            )}

                            {allowDelete && (
                              <Button
                                type="button"
                                variant="outline"
                                size="sm"
                                onClick={() => setArticleToDelete(article)}
                              >
                                Xóa
                              </Button>
                            )}
                          </div>
                        </td>
                      </tr>
                    )
                  })}
                </tbody>
              </table>
            </div>

            {meta && meta.totalPages > 1 && (
              <div className="px-4 py-3 border-t border-neutral-border dark:border-dark-border flex items-center justify-between gap-3 text-xs">
                <span className="text-neutral-text-muted dark:text-dark-text-muted">
                  Trang {meta.pageNumber} / {meta.totalPages} ({meta.totalCount} bài viết)
                </span>
                <div className="flex items-center gap-2">
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={!meta.hasPreviousPage}
                    onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
                  >
                    Trang trước
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    disabled={!meta.hasNextPage}
                    onClick={() => setPageNumber((p) => p + 1)}
                  >
                    Trang sau
                  </Button>
                </div>
              </div>
            )}
          </>
        )}
      </div>

      <Dialog
        open={Boolean(articleToDelete)}
        onOpenChange={(open) => {
          if (!open) setArticleToDelete(null)
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Xác nhận xóa bài viết</DialogTitle>
            <DialogDescription>
              Bạn có chắc chắn muốn xóa vĩnh viễn bài viết{' '}
              <span className="font-semibold text-neutral-text-main dark:text-dark-text-main">
                “{articleToDelete?.title}”
              </span>
              ? Hành động này không thể hoàn tác.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setArticleToDelete(null)}
            >
              Hủy bỏ
            </Button>
            <Button
              type="button"
              variant="primary"
              size="sm"
              isLoading={deleteMutation.isPending}
              onClick={() => {
                if (!articleToDelete) return
                deleteMutation.mutate(articleToDelete.id, {
                  onSuccess: () => setArticleToDelete(null),
                })
              }}
            >
              Xác nhận xóa
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
