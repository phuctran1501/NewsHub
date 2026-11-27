import { Toast } from '@base-ui/react/toast'
import { AlertCircle, AlertTriangle, CheckCircle2, Info, Loader2, X } from 'lucide-react'
import { toastManager } from './toastManager'

function ToastStatusIcon({ type }: { type?: string }) {
  const iconClass = 'h-4 w-4 shrink-0 mt-0.5 text-primary dark:text-secondary'

  switch (type) {
    case 'success':
      return <CheckCircle2 strokeWidth={1.75} className={iconClass} aria-hidden="true" />
    case 'error':
      return <AlertCircle strokeWidth={1.75} className={iconClass} aria-hidden="true" />
    case 'warning':
      return <AlertTriangle strokeWidth={1.75} className={iconClass} aria-hidden="true" />
    case 'loading':
      return <Loader2 strokeWidth={1.75} className={`${iconClass} animate-spin`} aria-hidden="true" />
    default:
      return <Info strokeWidth={1.75} className={iconClass} aria-hidden="true" />
  }
}

function StackedToastList() {
  const { toasts } = Toast.useToastManager()

  return (
    <>
      {toasts.map((item) => (
        <Toast.Root
          key={item.id}
          toast={item}
          className="nh-toast-root rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface text-neutral-text-main dark:text-dark-text-main shadow-[0_8px_24px_rgba(15,23,42,0.08)] dark:shadow-[0_10px_28px_rgba(0,0,0,0.45)]"
        >
          <Toast.Content className="nh-toast-content flex items-start gap-3 px-4 py-3.5">
            <ToastStatusIcon type={item.type} />

            <div className="min-w-0 flex-1 space-y-0.5">
              {item.title && (
                <Toast.Title className="text-[13px] font-semibold leading-snug text-neutral-text-main dark:text-dark-text-main" />
              )}
              {item.description && (
                <Toast.Description className="text-xs leading-relaxed text-neutral-text-muted dark:text-dark-text-muted" />
              )}
            </div>

            <div className="flex shrink-0 items-center gap-1.5">
              {item.actionProps && (
                <Toast.Action className="inline-flex h-7 items-center justify-center rounded-lg bg-primary px-2.5 text-[11px] font-semibold text-white hover:bg-primary-hover dark:bg-secondary dark:text-dark-bg dark:hover:bg-secondary-dark transition-colors cursor-pointer" />
              )}

              <Toast.Close
                aria-label="Đóng thông báo"
                className="inline-flex h-6 w-6 items-center justify-center rounded-md text-neutral-text-muted dark:text-dark-text-muted hover:bg-neutral-bg dark:hover:bg-dark-border/60 hover:text-neutral-text-main dark:hover:text-dark-text-main transition-colors cursor-pointer"
              >
                <X size={14} strokeWidth={1.75} />
              </Toast.Close>
            </div>
          </Toast.Content>
        </Toast.Root>
      ))}
    </>
  )
}

export function Toaster() {
  return (
    <Toast.Provider toastManager={toastManager} limit={3} timeout={3800}>
      <Toast.Portal>
        <Toast.Viewport
          aria-label="Thông báo hệ thống"
          className="fixed right-4 bottom-20 sm:right-6 sm:bottom-6 z-60 w-90 max-w-[calc(100vw-2rem)] outline-none"
        >
          <StackedToastList />
        </Toast.Viewport>
      </Toast.Portal>
    </Toast.Provider>
  )
}
