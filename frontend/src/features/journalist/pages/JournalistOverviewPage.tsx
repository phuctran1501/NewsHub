import { useNavigate } from 'react-router-dom'
import { Badge } from '@/components/common/Badge'
import { Button } from '@/components/common/Button'
import { EmptyState } from '@/components/common/EmptyState'
import { useAuth } from '@/hooks/useAuth'
import { buildCmsEditorPath, ROUTES } from '@/routes/paths'
import { isCompanyOwnerRole } from '@/types/company'
import { ArticleStatusBadge } from '../components/ArticleStatusBadge'
import {
  useMyCompanies,
  useMyInvitations,
  useRespondToInvitation,
} from '../hooks/useCompanyHub'
import { useMyArticles } from '../hooks/useMyArticles'

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

export function JournalistOverviewPage() {
  const navigate = useNavigate()
  const { user, roleName } = useAuth()

  const { data: allArticlesData, isLoading: isLoadingAll } = useMyArticles({
    pageNumber: 1,
    pageSize: 6,
  })
  const { data: draftArticlesData } = useMyArticles({
    pageNumber: 1,
    pageSize: 1,
    status: 'Draft',
  })
  const { data: pendingArticlesData } = useMyArticles({
    pageNumber: 1,
    pageSize: 1,
    status: 'PendingCompanyReview',
  })
  const { data: publishedArticlesData } = useMyArticles({
    pageNumber: 1,
    pageSize: 1,
    status: 'Published',
  })

  const { data: myCompanies = [] } = useMyCompanies()
  const { data: invitations = [] } = useMyInvitations()
  const respondInvitationMutation = useRespondToInvitation()

  const recentArticles = allArticlesData?.data ?? []
  const draftCount = draftArticlesData?.meta.totalCount ?? 0
  const pendingCount = pendingArticlesData?.meta.totalCount ?? 0
  const publishedCount = publishedArticlesData?.meta.totalCount ?? 0

  return (
    <div className="space-y-6">
      {/* Greeting & Quick Actions */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="space-y-1">
          <h1 className="text-xl font-bold tracking-tight text-neutral-text-main dark:text-dark-text-main">
            Xin chào, {user?.fullName || 'Nhà báo'}
          </h1>
          <div className="flex flex-wrap items-center gap-2 text-xs text-neutral-text-muted dark:text-dark-text-muted">
            <span>Tổng quan không gian tác nghiệp</span>
            {user?.pressCardNumber && (
              <>
                <span>·</span>
                <span className="font-mono font-semibold text-neutral-text-main dark:text-dark-text-main">
                  Thẻ Nhà Báo: {user.pressCardNumber}
                </span>
              </>
            )}
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {roleName === 'Journalist' && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => navigate(ROUTES.CMS.COMPANIES)}
            >
              Kết nối Tòa soạn
            </Button>
          )}
          <Button
            type="button"
            variant="secondary"
            size="sm"
            onClick={() => navigate(ROUTES.CMS.ARTICLES)}
          >
            Quản lý bài viết
          </Button>
          <Button
            type="button"
            variant="primary"
            size="sm"
            onClick={() => navigate(ROUTES.CMS.EDITOR)}
          >
            Soạn bài mới
          </Button>
        </div>
      </div>

      {/* Summary Metrics */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div className="rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-4 space-y-1">
          <p className="text-xs font-medium text-neutral-text-muted dark:text-dark-text-muted">
            Bản nháp đang viết
          </p>
          <p className="text-2xl font-bold tabular-nums text-primary dark:text-secondary">
            {draftCount}
          </p>
        </div>

        <div className="rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-4 space-y-1">
          <p className="text-xs font-medium text-neutral-text-muted dark:text-dark-text-muted">
            Chờ Tòa soạn duyệt
          </p>
          <p className="text-2xl font-bold tabular-nums text-primary dark:text-secondary">
            {pendingCount}
          </p>
        </div>

        <div className="rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-4 space-y-1">
          <p className="text-xs font-medium text-neutral-text-muted dark:text-dark-text-muted">
            Bài đã xuất bản
          </p>
          <p className="text-2xl font-bold tabular-nums text-primary dark:text-secondary">
            {publishedCount}
          </p>
        </div>

        <div className="rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-4 space-y-1">
          <p className="text-xs font-medium text-neutral-text-muted dark:text-dark-text-muted">
            Tòa soạn tham gia
          </p>
          <p className="text-2xl font-bold tabular-nums text-primary dark:text-secondary">
            {myCompanies.length} / 5
          </p>
        </div>
      </div>

      {/* 2-Column Activity Overview */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-5 items-start">
        {/* Left 7/12: Recent Articles to continue editing */}
        <section className="lg:col-span-7 rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-5 space-y-4">
          <div className="flex items-center justify-between gap-2">
            <div>
              <h2 className="text-sm font-bold text-neutral-text-main dark:text-dark-text-main">
                Bài viết gần đây
              </h2>
              <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
                Mở nhanh bản nháp hoặc theo dõi tiến độ kiểm duyệt mới nhất.
              </p>
            </div>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => navigate(ROUTES.CMS.ARTICLES)}
            >
              Xem tất cả
            </Button>
          </div>

          {isLoadingAll ? (
            <div className="space-y-3">
              {Array.from({ length: 4 }).map((_, idx) => (
                <div
                  key={idx}
                  className="h-16 rounded-[10px] bg-neutral-bg dark:bg-dark-bg animate-pulse"
                />
              ))}
            </div>
          ) : recentArticles.length === 0 ? (
            <EmptyState
              title="Chưa có bài viết nào"
              description="Bắt đầu viết bài báo đầu tiên của bạn để xuất bản tự do hoặc gửi tới Tòa soạn."
              actionLabel="Soạn bài ngay"
              onAction={() => navigate(ROUTES.CMS.EDITOR)}
            />
          ) : (
            <div className="space-y-2.5">
              {recentArticles.map((article) => (
                <div
                  key={article.id}
                  className="rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-bg/40 dark:bg-dark-bg/50 px-3.5 py-3 flex flex-wrap items-center justify-between gap-3"
                >
                  <div className="min-w-0 flex-1 space-y-1">
                    <p className="text-sm font-semibold text-neutral-text-main dark:text-dark-text-main line-clamp-1">
                      {article.title}
                    </p>
                    <div className="flex flex-wrap items-center gap-2 text-xs text-neutral-text-muted dark:text-dark-text-muted">
                      <ArticleStatusBadge status={article.status} />
                      <span>{formatShortDate(article.publishedAt ?? article.createdAt)}</span>
                      <span>· {article.readingTimeMinutes} phút đọc</span>
                    </div>
                  </div>
                  <Button
                    type="button"
                    variant="secondary"
                    size="sm"
                    onClick={() => navigate(buildCmsEditorPath(article.id))}
                  >
                    Mở trình soạn
                  </Button>
                </div>
              ))}
            </div>
          )}
        </section>

        {/* Right 5/12: Companies & Invitations */}
        <section className="lg:col-span-5 rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-5 space-y-4">
          <div className="flex items-center justify-between gap-2">
            <div>
              <h2 className="text-sm font-bold text-neutral-text-main dark:text-dark-text-main">
                Tòa soạn & Lời mời
              </h2>
              <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
                Trạng thái cộng tác cùng các cơ quan báo chí.
              </p>
            </div>
            {invitations.length > 0 && (
              <Badge variant="category">{invitations.length} lời mời mới</Badge>
            )}
          </div>

          {invitations.length > 0 && (
            <div className="space-y-2.5">
              {invitations.slice(0, 2).map((invite) => (
                <div
                  key={invite.id}
                  className="rounded-[10px] border border-primary/30 dark:border-secondary/35 bg-neutral-bg dark:bg-dark-bg p-3 space-y-2.5"
                >
                  <div className="space-y-0.5">
                    <p className="text-xs font-bold text-neutral-text-main dark:text-dark-text-main">
                      Lời mời từ {invite.companyName}
                    </p>
                    <p className="text-[11px] text-neutral-text-muted dark:text-dark-text-muted font-mono">
                      Mã {invite.companyCode}
                    </p>
                  </div>
                  <div className="flex items-center gap-2">
                    <Button
                      type="button"
                      variant="primary"
                      size="sm"
                      disabled={respondInvitationMutation.isPending || myCompanies.length >= 5}
                      onClick={() =>
                        respondInvitationMutation.mutate({ requestId: invite.id, accept: true })
                      }
                    >
                      Chấp nhận
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      disabled={respondInvitationMutation.isPending}
                      onClick={() =>
                        respondInvitationMutation.mutate({ requestId: invite.id, accept: false })
                      }
                    >
                      Từ chối
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}

          {myCompanies.length === 0 ? (
            <div className="rounded-[10px] border border-dashed border-neutral-border dark:border-dark-border p-4 text-center space-y-2">
              <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
                Bạn đang tác nghiệp ở chế độ Tự do cá nhân (Freelance).
              </p>
              {roleName === 'Journalist' && (
                <Button
                  type="button"
                  variant="secondary"
                  size="sm"
                  onClick={() => navigate(ROUTES.CMS.COMPANIES)}
                >
                  Tìm kiếm Tòa soạn
                </Button>
              )}
            </div>
          ) : (
            <div className="space-y-2">
              {myCompanies.map((company) => (
                <div
                  key={company.id}
                  className="rounded-[10px] border border-neutral-border dark:border-dark-border px-3.5 py-2.5 flex items-center justify-between gap-2"
                >
                  <div className="min-w-0">
                    <p className="text-xs font-semibold text-neutral-text-main dark:text-dark-text-main truncate">
                      {company.name}
                    </p>
                    <p className="text-[11px] font-mono text-neutral-text-muted dark:text-dark-text-muted">
                      Mã {company.code}
                    </p>
                  </div>
                  <Badge variant={isCompanyOwnerRole(company.role) ? 'published' : 'category'}>
                    {isCompanyOwnerRole(company.role) ? 'Chủ sở hữu' : 'Thành viên'}
                  </Badge>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  )
}
