import { Search } from 'lucide-react'
import { useMemo, useState, type FormEvent } from 'react'
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
import { isCompanyOwnerRole, type MyCompanyDto } from '@/types/company'
import { cn } from '@/utils/cn'
import { ArticleStatusBadge } from '../components/ArticleStatusBadge'
import {
  useCompanyInternalArticles,
  useLeaveCompany,
  useMyCompanies,
  useMyInvitations,
  useRequestJoinCompany,
  useRespondToInvitation,
  useSearchCompanies,
} from '../hooks/useCompanyHub'

type HubTabKey = 'mine' | 'discover' | 'invitations' | 'internal'

const MAX_COMPANIES = 5

function formatDate(iso: string | null): string {
  if (!iso) return '—'
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return '—'
  return date.toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  })
}

export function CompanyHubPage() {
  const { roleName } = useAuth()
  const [activeTab, setActiveTab] = useState<HubTabKey>('mine')
  const [searchInput, setSearchInput] = useState('')
  const [submittedSearch, setSubmittedSearch] = useState('')
  const [searchPage, setSearchPage] = useState(1)
  const [selectedInternalCompanyId, setSelectedInternalCompanyId] = useState<string>('')
  const [internalPage, setInternalPage] = useState(1)
  const [companyToLeave, setCompanyToLeave] = useState<MyCompanyDto | null>(null)

  const { data: myCompanies = [], isLoading: isLoadingMine } = useMyCompanies()
  const { data: invitations = [], isLoading: isLoadingInvites } = useMyInvitations()
  const { data: searchResult, isLoading: isLoadingSearch } = useSearchCompanies({
    searchTerm: submittedSearch,
    pageNumber: searchPage,
    pageSize: 8,
  })

  const effectiveInternalCompanyId = selectedInternalCompanyId || myCompanies[0]?.id || ''
  const { data: internalArticlesResult, isLoading: isLoadingInternal } = useCompanyInternalArticles(
    activeTab === 'internal' ? effectiveInternalCompanyId : undefined,
    { pageNumber: internalPage, pageSize: 10 },
  )

  const requestJoinMutation = useRequestJoinCompany()
  const respondInvitationMutation = useRespondToInvitation()
  const leaveCompanyMutation = useLeaveCompany()

  const myCompanyIds = useMemo(() => new Set(myCompanies.map((c) => c.id)), [myCompanies])
  const hasReachedCompanyLimit = myCompanies.length >= MAX_COMPANIES

  if (roleName !== 'Journalist') {
    return (
      <EmptyState
        title="Không gian dành riêng cho Nhà báo"
        description="Tính năng kết nối Tòa soạn, xin gia nhập và xem kho bài nội bộ chỉ áp dụng cho tài khoản Nhà báo."
      />
    )
  }

  const handleSearchSubmit = (event: FormEvent) => {
    event.preventDefault()
    setSubmittedSearch(searchInput.trim())
    setSearchPage(1)
  }

  const tabs: ReadonlyArray<{ key: HubTabKey; label: string }> = [
    { key: 'mine', label: `Tòa soạn của tôi (${myCompanies.length}/${MAX_COMPANIES})` },
    { key: 'discover', label: 'Tìm kiếm & Gia nhập' },
    { key: 'invitations', label: `Lời mời (${invitations.length})` },
    { key: 'internal', label: 'Kho bài nội bộ' },
  ]

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="space-y-0.5">
          <h1 className="text-xl font-bold tracking-tight text-neutral-text-main dark:text-dark-text-main">
            Không gian Kết nối Tòa soạn
          </h1>
          <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
            Tìm tòa soạn theo tên hoặc mã 8 số, quản lý lời mời gia nhập và theo dõi kho bài nội bộ.
          </p>
        </div>

        <Badge variant="category">
          Đang tham gia: {myCompanies.length} / {MAX_COMPANIES} Tòa soạn
        </Badge>
      </div>

      <div className="rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface overflow-hidden">
        <div
          role="tablist"
          aria-label="Phân mục kết nối tòa soạn"
          className="flex flex-wrap items-center gap-1.5 px-4 py-3 border-b border-neutral-border dark:border-dark-border bg-neutral-bg/60 dark:bg-dark-bg/60"
        >
          {tabs.map((tab) => {
            const isActive = activeTab === tab.key
            return (
              <button
                key={tab.key}
                type="button"
                role="tab"
                aria-selected={isActive}
                onClick={() => setActiveTab(tab.key)}
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

        <div className="p-5">
          {/* TAB 1: MY COMPANIES */}
          {activeTab === 'mine' && (
            <>
              {isLoadingMine ? (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div className="h-32 rounded-[12px] bg-neutral-bg dark:bg-dark-bg animate-pulse" />
                  <div className="h-32 rounded-[12px] bg-neutral-bg dark:bg-dark-bg animate-pulse" />
                </div>
              ) : myCompanies.length === 0 ? (
                <EmptyState
                  title="Bạn chưa tham gia Tòa soạn nào"
                  description="Bạn vẫn có thể viết và xuất bản bài tự do, hoặc tìm kiếm Tòa soạn uy tín để gửi đơn gia nhập."
                  actionLabel="Tìm kiếm Tòa soạn"
                  onAction={() => setActiveTab('discover')}
                />
              ) : (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {myCompanies.map((company) => {
                    const isOwner = isCompanyOwnerRole(company.role)
                    return (
                      <article
                        key={company.id}
                        className="rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-bg/40 dark:bg-dark-bg/50 p-4 flex flex-col justify-between gap-3"
                      >
                        <div className="flex items-start justify-between gap-3">
                          <div className="space-y-1 min-w-0">
                            <h2 className="text-sm font-bold text-neutral-text-main dark:text-dark-text-main truncate">
                              {company.name}
                            </h2>
                            <div className="flex flex-wrap items-center gap-2 text-xs">
                              <span className="font-mono px-2 py-0.5 rounded-[6px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface text-neutral-text-main dark:text-dark-text-main">
                                {company.code}
                              </span>
                              <Badge variant={isOwner ? 'published' : 'category'}>
                                {isOwner ? 'Chủ sở hữu' : 'Thành viên'}
                              </Badge>
                            </div>
                          </div>

                          {!isOwner && (
                            <Button
                              type="button"
                              variant="outline"
                              size="sm"
                              onClick={() => setCompanyToLeave(company)}
                            >
                              Rời Tòa soạn
                            </Button>
                          )}
                        </div>

                        <div className="flex flex-wrap items-center justify-between gap-2 pt-2 border-t border-neutral-border dark:border-dark-border text-xs text-neutral-text-muted dark:text-dark-text-muted">
                          <span>Gia nhập ngày {formatDate(company.joinedAt)}</span>
                          <Button
                            type="button"
                            variant="secondary"
                            size="sm"
                            onClick={() => {
                              setSelectedInternalCompanyId(company.id)
                              setInternalPage(1)
                              setActiveTab('internal')
                            }}
                          >
                            Xem kho bài nội bộ
                          </Button>
                        </div>
                      </article>
                    )
                  })}
                </div>
              )}
            </>
          )}

          {/* TAB 2: DISCOVER & JOIN */}
          {activeTab === 'discover' && (
            <div className="space-y-4">
              <form onSubmit={handleSearchSubmit} className="flex flex-col sm:flex-row gap-2.5">
                <div className="relative flex-1">
                  <Search
                    size={16}
                    strokeWidth={1.75}
                    className="absolute left-3 top-1/2 -translate-y-1/2 text-neutral-text-muted dark:text-dark-text-muted pointer-events-none"
                  />
                  <input
                    type="text"
                    value={searchInput}
                    onChange={(event) => setSearchInput(event.target.value)}
                    placeholder="Nhập tên Tòa soạn hoặc Mã định danh 8 số..."
                    className="w-full rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-bg pl-9 pr-3.5 py-2 text-xs text-neutral-text-main dark:text-dark-text-main focus:outline-none focus:border-primary dark:focus:border-secondary"
                  />
                </div>
                <Button type="submit" variant="primary" size="sm">
                  Tìm kiếm
                </Button>
              </form>

              {hasReachedCompanyLimit && (
                <div className="rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg px-3.5 py-2.5 text-xs text-neutral-text-muted dark:text-dark-text-muted">
                  Bạn đã đạt giới hạn tham gia tối đa {MAX_COMPANIES} Tòa soạn cùng lúc. Hãy rời một
                  tòa soạn trước khi gửi đơn gia nhập mới.
                </div>
              )}

              {isLoadingSearch ? (
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {Array.from({ length: 4 }).map((_, idx) => (
                    <div
                      key={idx}
                      className="h-32 rounded-[12px] bg-neutral-bg dark:bg-dark-bg animate-pulse"
                    />
                  ))}
                </div>
              ) : (searchResult?.data.length ?? 0) === 0 ? (
                <EmptyState
                  title="Không tìm thấy Tòa soạn phù hợp"
                  description="Kiểm tra lại tên tòa soạn hoặc mã định danh 8 số và thử tìm kiếm lại."
                />
              ) : (
                <>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    {searchResult?.data.map((company) => {
                      const alreadyMember = myCompanyIds.has(company.id)
                      return (
                        <article
                          key={company.id}
                          className="rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-4 flex flex-col justify-between gap-3"
                        >
                          <div className="flex items-start justify-between gap-3">
                            <div className="space-y-1 min-w-0">
                              <h3 className="text-sm font-bold text-neutral-text-main dark:text-dark-text-main truncate">
                                {company.name}
                              </h3>
                              <div className="flex flex-wrap items-center gap-2 text-xs text-neutral-text-muted dark:text-dark-text-muted">
                                <span className="font-mono px-2 py-0.5 rounded-[6px] border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg text-neutral-text-main dark:text-dark-text-main">
                                  {company.code}
                                </span>
                                {company.ownerName && <span>Sáng lập: {company.ownerName}</span>}
                              </div>
                            </div>

                            {alreadyMember ? (
                              <Badge variant="published">Đã tham gia</Badge>
                            ) : (
                              <Button
                                type="button"
                                variant="primary"
                                size="sm"
                                disabled={hasReachedCompanyLimit}
                                isLoading={
                                  requestJoinMutation.isPending &&
                                  requestJoinMutation.variables === company.id
                                }
                                onClick={() => requestJoinMutation.mutate(company.id)}
                              >
                                Yêu cầu gia nhập
                              </Button>
                            )}
                          </div>

                          <div className="text-xs text-neutral-text-muted dark:text-dark-text-muted pt-2 border-t border-neutral-border dark:border-dark-border">
                            {company.memberCount} thành viên · {company.publishedArticlesCount} bài đã
                            xuất bản
                          </div>
                        </article>
                      )
                    })}
                  </div>

                  {searchResult?.meta && searchResult.meta.totalPages > 1 && (
                    <div className="flex items-center justify-between pt-2 text-xs">
                      <span className="text-neutral-text-muted dark:text-dark-text-muted">
                        Trang {searchResult.meta.pageNumber} / {searchResult.meta.totalPages}
                      </span>
                      <div className="flex items-center gap-2">
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          disabled={!searchResult.meta.hasPreviousPage}
                          onClick={() => setSearchPage((p) => Math.max(1, p - 1))}
                        >
                          Trang trước
                        </Button>
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          disabled={!searchResult.meta.hasNextPage}
                          onClick={() => setSearchPage((p) => p + 1)}
                        >
                          Trang sau
                        </Button>
                      </div>
                    </div>
                  )}
                </>
              )}
            </div>
          )}

          {/* TAB 3: INVITATIONS */}
          {activeTab === 'invitations' && (
            <>
              {isLoadingInvites ? (
                <div className="space-y-3">
                  <div className="h-20 rounded-[12px] bg-neutral-bg dark:bg-dark-bg animate-pulse" />
                  <div className="h-20 rounded-[12px] bg-neutral-bg dark:bg-dark-bg animate-pulse" />
                </div>
              ) : invitations.length === 0 ? (
                <EmptyState
                  title="Hiện không có lời mời gia nhập nào"
                  description="Khi Chủ tòa soạn mời bạn cộng tác qua email, lời mời sẽ xuất hiện tại đây."
                />
              ) : (
                <div className="space-y-3">
                  {invitations.map((invite) => (
                    <div
                      key={invite.id}
                      className="rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-bg/40 dark:bg-dark-bg/50 p-4 flex flex-wrap items-center justify-between gap-3"
                    >
                      <div className="space-y-1">
                        <p className="text-sm font-bold text-neutral-text-main dark:text-dark-text-main">
                          {invite.companyName}
                        </p>
                        <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
                          Mã tòa soạn:{' '}
                          <span className="font-mono font-semibold text-neutral-text-main dark:text-dark-text-main">
                            {invite.companyCode}
                          </span>
                          {' · '}Ngày mời: {formatDate(invite.createdAt)}
                        </p>
                      </div>

                      <div className="flex items-center gap-2">
                        <Button
                          type="button"
                          variant="outline"
                          size="sm"
                          disabled={respondInvitationMutation.isPending}
                          onClick={() =>
                            respondInvitationMutation.mutate({
                              requestId: invite.id,
                              accept: false,
                            })
                          }
                        >
                          Từ chối
                        </Button>
                        <Button
                          type="button"
                          variant="primary"
                          size="sm"
                          disabled={hasReachedCompanyLimit || respondInvitationMutation.isPending}
                          onClick={() =>
                            respondInvitationMutation.mutate({
                              requestId: invite.id,
                              accept: true,
                            })
                          }
                        >
                          Chấp nhận
                        </Button>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </>
          )}

          {/* TAB 4: INTERNAL ARTICLES (READ-ONLY) */}
          {activeTab === 'internal' && (
            <div className="space-y-4">
              {myCompanies.length === 0 ? (
                <EmptyState
                  title="Chưa có kho bài nội bộ"
                  description="Bạn cần tham gia ít nhất một Tòa soạn để xem kho bài viết nội bộ của đồng nghiệp."
                  actionLabel="Tìm kiếm Tòa soạn"
                  onAction={() => setActiveTab('discover')}
                />
              ) : (
                <>
                  <div className="flex flex-wrap items-center justify-between gap-3">
                    <div className="flex items-center gap-2.5">
                      <label
                        htmlFor="internal-company-select"
                        className="text-xs font-semibold text-neutral-text-main dark:text-dark-text-main"
                      >
                        Tòa soạn:
                      </label>
                      <select
                        id="internal-company-select"
                        value={effectiveInternalCompanyId}
                        onChange={(event) => {
                          setSelectedInternalCompanyId(event.target.value)
                          setInternalPage(1)
                        }}
                        className="rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-bg px-3 py-1.5 text-xs font-medium text-neutral-text-main dark:text-dark-text-main focus:outline-none focus:border-primary dark:focus:border-secondary"
                      >
                        {myCompanies.map((company) => (
                          <option key={company.id} value={company.id}>
                            {company.name} ({company.code})
                          </option>
                        ))}
                      </select>
                    </div>
                    <Badge variant="neutral">Chế độ Chỉ đọc (Read-only)</Badge>
                  </div>

                  {isLoadingInternal ? (
                    <div className="space-y-3">
                      {Array.from({ length: 4 }).map((_, idx) => (
                        <div
                          key={idx}
                          className="h-12 rounded-[10px] bg-neutral-bg dark:bg-dark-bg animate-pulse"
                        />
                      ))}
                    </div>
                  ) : (internalArticlesResult?.data.length ?? 0) === 0 ? (
                    <EmptyState
                      title="Kho bài nội bộ đang trống"
                      description="Các bài viết của đồng nghiệp trong tòa soạn này sẽ hiển thị tại đây để tránh trùng lặp đề tài."
                    />
                  ) : (
                    <>
                      <div className="overflow-x-auto rounded-[10px] border border-neutral-border dark:border-dark-border">
                        <table className="w-full text-left border-collapse">
                          <thead>
                            <tr className="border-b border-neutral-border dark:border-dark-border bg-neutral-bg/60 dark:bg-dark-bg/60 text-[11px] font-semibold uppercase tracking-wider text-neutral-text-muted dark:text-dark-text-muted">
                              <th className="py-2.5 px-4">Tiêu đề bài viết</th>
                              <th className="py-2.5 px-4">Phóng viên</th>
                              <th className="py-2.5 px-4">Trạng thái</th>
                              <th className="py-2.5 px-4">Cập nhật</th>
                            </tr>
                          </thead>
                          <tbody className="divide-y divide-neutral-border dark:divide-dark-border text-sm">
                            {internalArticlesResult?.data.map((article) => (
                              <tr key={article.id}>
                                <td className="py-3 px-4 font-medium text-neutral-text-main dark:text-dark-text-main">
                                  {article.title}
                                  <span className="block text-[11px] text-neutral-text-muted dark:text-dark-text-muted font-normal">
                                    {article.readingTimeMinutes} phút đọc
                                  </span>
                                </td>
                                <td className="py-3 px-4 text-xs text-neutral-text-main dark:text-dark-text-main whitespace-nowrap">
                                  {article.authorName}
                                </td>
                                <td className="py-3 px-4 whitespace-nowrap">
                                  <ArticleStatusBadge status={article.status} />
                                </td>
                                <td className="py-3 px-4 text-xs text-neutral-text-muted dark:text-dark-text-muted whitespace-nowrap">
                                  {formatDate(article.updatedAt ?? article.createdAt)}
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>

                      {internalArticlesResult?.meta && internalArticlesResult.meta.totalPages > 1 && (
                        <div className="flex items-center justify-between text-xs">
                          <span className="text-neutral-text-muted dark:text-dark-text-muted">
                            Trang {internalArticlesResult.meta.pageNumber} /{' '}
                            {internalArticlesResult.meta.totalPages}
                          </span>
                          <div className="flex items-center gap-2">
                            <Button
                              type="button"
                              variant="outline"
                              size="sm"
                              disabled={!internalArticlesResult.meta.hasPreviousPage}
                              onClick={() => setInternalPage((p) => Math.max(1, p - 1))}
                            >
                              Trang trước
                            </Button>
                            <Button
                              type="button"
                              variant="outline"
                              size="sm"
                              disabled={!internalArticlesResult.meta.hasNextPage}
                              onClick={() => setInternalPage((p) => p + 1)}
                            >
                              Trang sau
                            </Button>
                          </div>
                        </div>
                      )}
                    </>
                  )}
                </>
              )}
            </div>
          )}
        </div>
      </div>

      <Dialog
        open={Boolean(companyToLeave)}
        onOpenChange={(open) => {
          if (!open) setCompanyToLeave(null)
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Xác nhận rời Tòa soạn</DialogTitle>
            <DialogDescription>
              Khi rời khỏi{' '}
              <span className="font-semibold text-neutral-text-main dark:text-dark-text-main">
                {companyToLeave?.name}
              </span>
              , toàn bộ bài viết đang ở trạng thái Bản nháp và Chờ duyệt của bạn tại tòa soạn này sẽ
              tự động được giải phóng về tài khoản cá nhân (Tự do). Các bài đã xuất bản vẫn thuộc
              quyền lưu trữ của Tòa soạn.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setCompanyToLeave(null)}
            >
              Hủy bỏ
            </Button>
            <Button
              type="button"
              variant="primary"
              size="sm"
              isLoading={leaveCompanyMutation.isPending}
              onClick={() => {
                if (!companyToLeave) return
                leaveCompanyMutation.mutate(companyToLeave.id, {
                  onSuccess: () => setCompanyToLeave(null),
                })
              }}
            >
              Xác nhận rời Tòa soạn
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
