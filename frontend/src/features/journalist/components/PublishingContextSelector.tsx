import { Badge } from '@/components/common/Badge'
import { isCompanyOwnerRole, type MyCompanyDto } from '@/types/company'
import { cn } from '@/utils/cn'

interface PublishingContextSelectorProps {
  companies: MyCompanyDto[]
  selectedCompanyId: string | null
  onChange: (companyId: string | null) => void
  locked?: boolean
  lockedCompanyName?: string | null
  isLoading?: boolean
}

export function PublishingContextSelector({
  companies,
  selectedCompanyId,
  onChange,
  locked = false,
  lockedCompanyName,
  isLoading = false,
}: PublishingContextSelectorProps) {
  return (
    <section className="rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-4 space-y-3">
      <div className="space-y-0.5">
        <h2 className="text-sm font-semibold text-neutral-text-main dark:text-dark-text-main">
          Ngữ cảnh xuất bản
        </h2>
        <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
          {locked
            ? 'Ngữ cảnh xuất bản được cố định theo bài viết hiện tại.'
            : 'Chọn xuất bản tự do cá nhân hoặc nộp bài dưới danh nghĩa Tòa soạn.'}
        </p>
      </div>

      {isLoading ? (
        <div className="space-y-2">
          <div className="h-14 rounded-[10px] bg-neutral-bg dark:bg-dark-bg animate-pulse" />
          <div className="h-14 rounded-[10px] bg-neutral-bg dark:bg-dark-bg animate-pulse" />
        </div>
      ) : locked ? (
        <div className="rounded-[10px] border border-primary/40 dark:border-secondary/40 bg-sidebar-active/70 dark:bg-dark-bg p-3 flex items-center justify-between gap-2">
          <div className="min-w-0">
            <p className="text-xs font-semibold text-neutral-text-main dark:text-dark-text-main truncate">
              {selectedCompanyId
                ? lockedCompanyName ||
                  companies.find((c) => c.id === selectedCompanyId)?.name ||
                  'Tòa soạn trực thuộc'
                : 'Tự do cá nhân (Freelance)'}
            </p>
            <p className="text-[11px] text-neutral-text-muted dark:text-dark-text-muted">
              {selectedCompanyId
                ? 'Bài viết thuộc quyền quản lý biên tập của Tòa soạn'
                : 'Xuất bản trực tiếp bằng Thẻ Nhà Báo'}
            </p>
          </div>
          <Badge variant={selectedCompanyId ? 'category' : 'neutral'}>
            {selectedCompanyId ? 'Tòa soạn' : 'Tự do'}
          </Badge>
        </div>
      ) : (
        <div className="space-y-2" role="radiogroup" aria-label="Chọn ngữ cảnh xuất bản">
          <button
            type="button"
            role="radio"
            aria-checked={selectedCompanyId === null}
            onClick={() => onChange(null)}
            className={cn(
              'w-full text-left rounded-[10px] border px-3 py-2.5 flex items-center justify-between gap-2 transition-colors duration-150 cursor-pointer',
              selectedCompanyId === null
                ? 'border-primary dark:border-secondary bg-secondary-light/50 dark:bg-secondary/12'
                : 'border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface hover:border-primary/50 dark:hover:border-secondary/50',
            )}
          >
            <div className="min-w-0">
              <p className="text-xs font-semibold text-neutral-text-main dark:text-dark-text-main">
                Tự do cá nhân (Freelance)
              </p>
              <p className="text-[11px] text-neutral-text-muted dark:text-dark-text-muted">
                Xuất bản ngay bằng Thẻ Nhà Báo
              </p>
            </div>
            <Badge variant="neutral">Tự do</Badge>
          </button>

          {companies.map((company) => {
            const isSelected = selectedCompanyId === company.id
            const isOwner = isCompanyOwnerRole(company.role)

            return (
              <button
                key={company.id}
                type="button"
                role="radio"
                aria-checked={isSelected}
                onClick={() => onChange(company.id)}
                className={cn(
                  'w-full text-left rounded-[10px] border px-3 py-2.5 flex items-center justify-between gap-2 transition-colors duration-150 cursor-pointer',
                  isSelected
                    ? 'border-primary dark:border-secondary bg-secondary-light/50 dark:bg-secondary/12'
                    : 'border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface hover:border-primary/50 dark:hover:border-secondary/50',
                )}
              >
                <div className="min-w-0">
                  <p className="text-xs font-semibold text-neutral-text-main dark:text-dark-text-main truncate">
                    {company.name}
                  </p>
                  <p className="text-[11px] text-neutral-text-muted dark:text-dark-text-muted font-mono">
                    Mã {company.code} · {isOwner ? 'Chủ sở hữu' : 'Thành viên'}
                  </p>
                </div>
                <Badge variant="category">Tòa soạn</Badge>
              </button>
            )
          })}
        </div>
      )}
    </section>
  )
}
