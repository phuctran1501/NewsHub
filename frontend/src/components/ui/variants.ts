import { cva, type VariantProps } from 'class-variance-authority'

export const buttonVariants = cva(
  'inline-flex items-center justify-center gap-2 font-semibold transition-colors duration-150 cursor-pointer select-none focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary dark:focus-visible:outline-secondary disabled:opacity-50 disabled:pointer-events-none',
  {
    variants: {
      variant: {
        primary:
          'bg-primary text-white hover:bg-primary-hover dark:bg-secondary dark:text-dark-bg dark:hover:bg-secondary-dark',
        secondary:
          'bg-secondary-light text-primary hover:bg-secondary/70 dark:bg-secondary/15 dark:text-secondary dark:hover:bg-secondary/25',
        action: 'bg-accent-action text-white hover:bg-accent-action-hover',
        outline:
          'border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface text-neutral-text-main dark:text-dark-text-main hover:border-primary dark:hover:border-secondary',
        ghost:
          'bg-transparent text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main hover:bg-neutral-bg dark:hover:bg-dark-border/50',
        danger:
          'border border-status-rejected/45 bg-transparent text-status-rejected hover:bg-status-rejected/10',
      },
      size: {
        sm: 'h-[34px] px-3.5 text-xs rounded-[8px]',
        md: 'h-[40px] px-5 text-sm rounded-[10px]',
        lg: 'h-[46px] px-6 text-sm rounded-[12px]',
        icon: 'h-9 w-9 rounded-[10px]',
      },
    },
    defaultVariants: {
      variant: 'primary',
      size: 'md',
    },
  },
)

export type ButtonVariantProps = VariantProps<typeof buttonVariants>

export const badgeVariants = cva(
  'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold leading-tight border',
  {
    variants: {
      variant: {
        category:
          'bg-secondary-light text-primary border-transparent dark:bg-secondary/12 dark:text-secondary',
        ai: 'bg-[#f5f3ff] text-accent-ai border-[#ddd6fe] dark:bg-accent-ai/12 dark:border-accent-ai/35 dark:text-indigo-300',
        breaking: 'bg-accent-crimson text-white border-transparent uppercase tracking-wider',
        published:
          'bg-status-published/10 text-emerald-700 border-status-published/30 dark:text-emerald-400',
        pending:
          'bg-status-pending/10 text-amber-700 border-status-pending/30 dark:text-amber-400',
        rejected:
          'bg-status-rejected/10 text-red-600 border-status-rejected/30 dark:text-red-400',
        neutral:
          'bg-neutral-bg text-neutral-text-muted border-neutral-border dark:bg-dark-bg dark:text-dark-text-muted dark:border-dark-border',
      },
    },
    defaultVariants: {
      variant: 'category',
    },
  },
)

export type BadgeVariantProps = VariantProps<typeof badgeVariants>

export const alertVariants = cva(
  'relative w-full rounded-[12px] border px-3.5 py-2.5 text-xs leading-relaxed',
  {
    variants: {
      variant: {
        default:
          'border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg text-neutral-text-main dark:text-dark-text-main',
        success:
          'border-status-published/35 bg-status-published/10 text-emerald-800 dark:text-emerald-300',
        warning:
          'border-status-pending/45 bg-status-pending/10 text-amber-800 dark:text-amber-300',
        destructive:
          'border-status-rejected/40 bg-status-rejected/10 text-status-rejected',
      },
    },
    defaultVariants: {
      variant: 'default',
    },
  },
)

export type AlertVariantProps = VariantProps<typeof alertVariants>
