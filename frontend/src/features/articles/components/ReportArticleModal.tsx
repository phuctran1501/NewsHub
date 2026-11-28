import * as Dialog from '@radix-ui/react-dialog'
import { X } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Button } from '@/components/common/Button'
import { REPORT_REASON_VALUES, type ReportReasonValue } from '@/types/article'
import { cn } from '@/utils/cn'

interface ReportArticleModalProps {
  open: boolean
  onOpenChange: (open: boolean) => void
  isSubmitting: boolean
  onSubmit: (payload: { reason: ReportReasonValue; details?: string }) => Promise<void>
}

const REPORT_REASONS: { value: ReportReasonValue; label: string; hint: string }[] = [
  {
    value: REPORT_REASON_VALUES.Misinformation,
    label: 'Thông tin sai sự thật',
    hint: 'Nội dung bịa đặt, xuyên tạc hoặc thiếu căn cứ kiểm chứng.',
  },
  {
    value: REPORT_REASON_VALUES.Spam,
    label: 'Spam hoặc quảng cáo trá hình',
    hint: 'Chèn liên kết rác, nội dung lặp lại hoặc quảng cáo không liên quan.',
  },
  {
    value: REPORT_REASON_VALUES.CopyrightViolation,
    label: 'Vi phạm bản quyền tác giả',
    hint: 'Sao chép bài viết hoặc hình ảnh mà không ghi nguồn hợp lệ.',
  },
  {
    value: REPORT_REASON_VALUES.Harassment,
    label: 'Quấy rối hoặc ngôn từ thù ghét',
    hint: 'Xúc phạm cá nhân, tổ chức hoặc kích động thù hằn.',
  },
  {
    value: REPORT_REASON_VALUES.Other,
    label: 'Lý do khác',
    hint: 'Các vấn đề vi phạm quy chuẩn biên tập khác.',
  },
]

export function ReportArticleModal({
  open,
  onOpenChange,
  isSubmitting,
  onSubmit,
}: ReportArticleModalProps) {
  const [reason, setReason] = useState<ReportReasonValue>(REPORT_REASON_VALUES.Misinformation)
  const [details, setDetails] = useState('')

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    try {
      await onSubmit({
        reason,
        details: details.trim() ? details.trim() : undefined,
      })
      setDetails('')
      setReason(REPORT_REASON_VALUES.Misinformation)
      onOpenChange(false)
    } catch {
      // Lỗi đã được xử lý và hiển thị Toast trong useReportArticle
    }
  }

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-black/50" />
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 w-[calc(100%-2rem)] max-w-md -translate-x-1/2 -translate-y-1/2 rounded-2xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-6 shadow-[0_8px_24px_rgba(15,23,42,0.08)] focus:outline-none">
          <div className="flex items-center justify-between border-b border-neutral-border dark:border-dark-border pb-3.5">
            <Dialog.Title className="text-base font-bold text-neutral-text-main dark:text-dark-text-main">
              Báo cáo vi phạm nội dung
            </Dialog.Title>
            <Dialog.Close asChild>
              <button
                type="button"
                aria-label="Đóng hộp thoại báo cáo"
                className="inline-flex h-8 w-8 items-center justify-center rounded-lg text-neutral-text-muted hover:bg-neutral-bg dark:hover:bg-dark-bg hover:text-neutral-text-main dark:hover:text-dark-text-main transition-colors cursor-pointer"
              >
                <X size={16} strokeWidth={1.75} />
              </button>
            </Dialog.Close>
          </div>

          <Dialog.Description className="mt-2.5 text-xs leading-relaxed text-neutral-text-muted dark:text-dark-text-muted">
            Chọn lý do phù hợp để Ban biên tập xem xét và xử lý bài viết theo quy chế tòa soạn.
          </Dialog.Description>

          <form onSubmit={(e) => void handleSubmit(e)} className="mt-4 space-y-4">
            <fieldset className="space-y-2">
              <legend className="sr-only">Lý do báo cáo vi phạm</legend>
              {REPORT_REASONS.map((option) => {
                const isSelected = reason === option.value
                return (
                  <label
                    key={option.value}
                    className={cn(
                      'flex items-start gap-2.5 rounded-xl border p-3 text-xs transition-colors cursor-pointer',
                      isSelected
                        ? 'border-primary bg-secondary-light/40 dark:border-secondary dark:bg-secondary/10'
                        : 'border-neutral-border dark:border-dark-border hover:border-primary/60 dark:hover:border-secondary/60',
                    )}
                  >
                    <input
                      type="radio"
                      name="report-reason"
                      checked={isSelected}
                      onChange={() => setReason(option.value)}
                      className="mt-0.5 accent-primary"
                    />
                    <span className="space-y-0.5">
                      <span className="block font-semibold text-neutral-text-main dark:text-dark-text-main">
                        {option.label}
                      </span>
                      <span className="block text-[11px] text-neutral-text-muted dark:text-dark-text-muted">
                        {option.hint}
                      </span>
                    </span>
                  </label>
                )
              })}
            </fieldset>

            <div className="space-y-1.5">
              <label
                htmlFor="report-details"
                className="block text-xs font-semibold text-neutral-text-main dark:text-dark-text-main"
              >
                Mô tả chi tiết (tùy chọn)
              </label>
              <textarea
                id="report-details"
                rows={3}
                maxLength={1000}
                value={details}
                onChange={(event) => setDetails(event.target.value)}
                placeholder="Cung cấp thêm thông tin hoặc dẫn chứng cụ thể (tối đa 1000 ký tự)..."
                className="w-full rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg p-3 text-xs text-neutral-text-main dark:text-dark-text-main placeholder:text-neutral-text-muted focus:border-primary dark:focus:border-secondary focus:outline-none"
              />
              <div className="text-right text-[11px] text-neutral-text-muted dark:text-dark-text-muted tabular-nums">
                {details.length}/1000
              </div>
            </div>

            <div className="flex items-center justify-end gap-2.5 pt-1">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => onOpenChange(false)}
              >
                Hủy
              </Button>
              <Button type="submit" variant="primary" size="sm" isLoading={isSubmitting}>
                Gửi báo cáo
              </Button>
            </div>
          </form>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  )
}
