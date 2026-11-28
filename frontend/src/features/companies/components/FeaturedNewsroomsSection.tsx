import { useLocation, useNavigate } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { EmptyState } from '@/components/common/EmptyState'
import { Skeleton } from '@/components/common/Skeleton'
import { useAuth } from '@/hooks/useAuth'
import { ROUTES } from '@/routes/paths'
import {
  useFollowedCompanyIds,
  usePublicCompanies,
  useToggleFollowCompany,
} from '../hooks/usePublicCompanies'

function getCompanyMonogram(name: string): string {
  const parts = name
    .trim()
    .split(/\s+/)
    .filter(Boolean)
  if (parts.length === 0) return 'NH'
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase()
}

export function FeaturedNewsroomsSection() {
  const { isAuthenticated } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()

  const { data, isLoading, isError, refetch } = usePublicCompanies({
    pageNumber: 1,
    pageSize: 8,
  })
  const { followedIds } = useFollowedCompanyIds()
  const toggleFollow = useToggleFollowCompany()

  const companies = data?.data ?? []

  function handleFollowClick(companyId: string, companyName: string, isFollowing: boolean) {
    if (!isAuthenticated) {
      const redirect = encodeURIComponent(`${location.pathname}${location.search}`)
      navigate(`${ROUTES.AUTH.LOGIN}?redirect=${redirect}`)
      return
    }
    toggleFollow.mutate({
      companyId,
      companyName,
      isCurrentlyFollowing: isFollowing,
    })
  }

  return (
    <section id="companies" aria-labelledby="featured-newsrooms-heading" className="space-y-5">
      <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-2 border-b border-neutral-border dark:border-dark-border pb-3">
        <h2
          id="featured-newsrooms-heading"
          className="text-xl font-bold tracking-[-0.015em] text-neutral-text-main dark:text-dark-text-main"
        >
          Tòa soạn nổi bật
        </h2>
        <span className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
          Các tòa soạn đã được xác thực pháp lý trên NewsHub
        </span>
      </div>

      {isLoading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-5">
          {[0, 1, 2, 3].map((idx) => (
            <div
              key={idx}
              className="rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-5 space-y-4"
            >
              <div className="flex items-center justify-between gap-3">
                <div className="flex items-center gap-3">
                  <Skeleton className="h-11 w-11 rounded-[10px]" />
                  <div className="space-y-1.5">
                    <Skeleton className="h-4 w-32" />
                    <Skeleton className="h-3 w-20" />
                  </div>
                </div>
                <Skeleton className="h-8 w-24 rounded-lg" />
              </div>
              <Skeleton className="h-4 w-44" />
            </div>
          ))}
        </div>
      ) : isError ? (
        <EmptyState
          title="Không thể tải danh sách tòa soạn"
          description="Đã xảy ra lỗi khi kết nối tới danh bạ tòa soạn. Vui lòng thử lại."
          actionLabel="Thử lại"
          onAction={() => void refetch()}
        />
      ) : companies.length === 0 ? (
        <EmptyState
          title="Chưa có tòa soạn nào được hiển thị"
          description="Các tòa soạn đối tác sau khi được phê duyệt hoạt động sẽ xuất hiện tại đây."
        />
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-5">
          {companies.map((company) => {
            const isFollowing = followedIds.has(company.id)
            const isMutatingThis =
              toggleFollow.isPending && toggleFollow.variables?.companyId === company.id

            return (
              <article
                key={company.id}
                className="flex flex-col justify-between gap-4 rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-5 hover:border-primary dark:hover:border-secondary-dark transition-colors duration-150"
              >
                <div className="flex items-start justify-between gap-3">
                  <div className="flex items-center gap-3 min-w-0">
                    <div
                      aria-hidden="true"
                      className="flex h-11 w-11 shrink-0 items-center justify-center rounded-[10px] border border-neutral-border dark:border-dark-border bg-secondary-light dark:bg-secondary/15 text-sm font-bold text-primary dark:text-secondary"
                    >
                      {getCompanyMonogram(company.name)}
                    </div>

                    <div className="min-w-0">
                      <h3 className="truncate text-sm font-bold text-neutral-text-main dark:text-dark-text-main">
                        {company.name}
                      </h3>
                      <p className="font-mono text-[11px] text-neutral-text-muted dark:text-dark-text-muted">
                        ID: {company.code}
                      </p>
                    </div>
                  </div>

                  <Button
                    type="button"
                    variant={isFollowing ? 'outline' : 'primary'}
                    size="sm"
                    isLoading={isMutatingThis}
                    onClick={() => handleFollowClick(company.id, company.name, isFollowing)}
                    className="shrink-0"
                  >
                    {isFollowing ? 'Đang theo dõi' : '+ Theo dõi'}
                  </Button>
                </div>

                <div className="flex flex-wrap items-center gap-3 border-t border-neutral-border dark:border-dark-border pt-3 text-xs text-neutral-text-muted dark:text-dark-text-muted tabular-nums">
                  <span>
                    <strong className="font-semibold text-neutral-text-main dark:text-dark-text-main">
                      {company.publishedArticleCount}
                    </strong>{' '}
                    bài xuất bản
                  </span>
                  <span aria-hidden="true">•</span>
                  <span>
                    <strong className="font-semibold text-neutral-text-main dark:text-dark-text-main">
                      {company.memberCount}
                    </strong>{' '}
                    phóng viên
                  </span>
                  <span aria-hidden="true">•</span>
                  <span>
                    <strong className="font-semibold text-neutral-text-main dark:text-dark-text-main">
                      {company.followerCount}
                    </strong>{' '}
                    người theo dõi
                  </span>
                </div>
              </article>
            )
          })}
        </div>
      )}
    </section>
  )
}
