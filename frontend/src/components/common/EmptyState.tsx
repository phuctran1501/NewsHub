import type { ReactNode } from 'react'
import { Button } from './Button'

interface EmptyStateProps {
  title: string
  description: string
  icon?: ReactNode
  actionLabel?: string
  onAction?: () => void
  className?: string
}

export function EmptyState({
  title,
  description,
  icon,
  actionLabel,
  onAction,
  className = '',
}: EmptyStateProps) {
  return (
    <div
      className={`rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface px-6 py-12 text-center space-y-3 ${className}`}
    >
      {icon && (
        <div className="w-10 h-10 rounded-[10px] border border-neutral-border dark:border-dark-border mx-auto flex items-center justify-center text-neutral-text-muted dark:text-dark-text-muted">
          {icon}
        </div>
      )}

      <h3 className="text-base font-semibold text-neutral-text-main dark:text-dark-text-main">
        {title}
      </h3>

      <p className="text-sm text-neutral-text-muted dark:text-dark-text-muted max-w-md mx-auto leading-relaxed">
        {description}
      </p>

      {actionLabel && onAction && (
        <div className="pt-2">
          <Button variant="outline" size="sm" onClick={onAction}>
            {actionLabel}
          </Button>
        </div>
      )}
    </div>
  )
}
