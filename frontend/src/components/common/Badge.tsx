import type { ReactNode } from 'react'

export type BadgeVariant =
  | 'category'
  | 'ai'
  | 'breaking'
  | 'published'
  | 'pending'
  | 'rejected'
  | 'neutral'

interface BadgeProps {
  variant?: BadgeVariant
  children: ReactNode
  className?: string
}

const BADGE_VARIANTS: Record<BadgeVariant, string> = {
  category:
    'bg-secondary-light text-primary dark:bg-secondary/12 dark:text-secondary border border-transparent',
  ai: 'bg-[#f5f3ff] text-accent-ai border border-[#ddd6fe] dark:bg-accent-ai/12 dark:border-accent-ai/35 dark:text-indigo-300',
  breaking: 'bg-accent-crimson text-white border border-transparent uppercase tracking-wider',
  published:
    'bg-status-published/10 text-emerald-700 border border-status-published/30 dark:text-emerald-400',
  pending:
    'bg-status-pending/10 text-amber-700 border border-status-pending/30 dark:text-amber-400',
  rejected:
    'bg-status-rejected/10 text-red-600 border border-status-rejected/30 dark:text-red-400',
  neutral:
    'bg-neutral-bg text-neutral-text-muted border border-neutral-border dark:bg-dark-bg dark:text-dark-text-muted dark:border-dark-border',
}

export function Badge({ variant = 'category', children, className = '' }: BadgeProps) {
  return (
    <span
      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold leading-tight ${BADGE_VARIANTS[variant]} ${className}`}
    >
      {children}
    </span>
  )
}
