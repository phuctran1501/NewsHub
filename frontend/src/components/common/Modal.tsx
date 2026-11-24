import { X } from 'lucide-react'
import { useEffect, useId, type ReactNode } from 'react'

interface ModalProps {
  isOpen: boolean
  onClose: () => void
  title: string
  children: ReactNode
  footer?: ReactNode
  maxWidthClass?: string
}

export function Modal({
  isOpen,
  onClose,
  title,
  children,
  footer,
  maxWidthClass = 'max-w-md',
}: ModalProps) {
  const titleId = useId()

  useEffect(() => {
    if (!isOpen) return

    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        onClose()
      }
    }

    window.addEventListener('keydown', handleKeyDown)
    return () => {
      document.body.style.overflow = previousOverflow
      window.removeEventListener('keydown', handleKeyDown)
    }
  }, [isOpen, onClose])

  if (!isOpen) return null

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/45"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          onClose()
        }
      }}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        className={`w-full ${maxWidthClass} rounded-2xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-6 space-y-4 shadow-[0_8px_24px_rgba(15,23,42,0.08)]`}
      >
        <div className="flex items-center justify-between gap-4">
          <h2
            id={titleId}
            className="text-lg font-semibold text-neutral-text-main dark:text-dark-text-main"
          >
            {title}
          </h2>
          <button
            type="button"
            onClick={onClose}
            aria-label="Đóng hộp thoại"
            className="inline-flex items-center justify-center w-8 h-8 rounded-lg text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main hover:bg-neutral-bg dark:hover:bg-dark-border/50 transition-colors cursor-pointer"
          >
            <X size={16} strokeWidth={1.75} />
          </button>
        </div>

        <div className="text-sm text-neutral-text-muted dark:text-dark-text-muted leading-relaxed">
          {children}
        </div>

        {footer && (
          <div className="flex items-center justify-end gap-2.5 pt-2 border-t border-neutral-border dark:border-dark-border">
            {footer}
          </div>
        )}
      </div>
    </div>
  )
}
