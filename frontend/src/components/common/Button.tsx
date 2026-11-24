import type { ButtonHTMLAttributes, ReactNode } from 'react'
import { Spinner } from './Spinner'

export type ButtonVariant = 'primary' | 'action' | 'outline' | 'ghost' | 'danger'
export type ButtonSize = 'sm' | 'md' | 'lg'

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant
  size?: ButtonSize
  isLoading?: boolean
  fullWidth?: boolean
  children: ReactNode
}

const VARIANT_CLASSES: Record<ButtonVariant, string> = {
  primary:
    'bg-primary text-white hover:bg-primary-hover dark:bg-secondary dark:text-dark-bg dark:hover:bg-secondary-dark',
  action: 'bg-accent-action text-white hover:bg-accent-action-hover',
  outline:
    'border border-neutral-border dark:border-dark-border bg-transparent text-neutral-text-main dark:text-dark-text-main hover:border-primary dark:hover:border-secondary',
  ghost:
    'bg-transparent text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main hover:bg-neutral-bg dark:hover:bg-dark-border/50',
  danger:
    'border border-status-rejected/45 bg-transparent text-status-rejected hover:bg-status-rejected/10',
}

const SIZE_CLASSES: Record<ButtonSize, string> = {
  sm: 'h-[34px] px-3.5 text-xs rounded-[8px]',
  md: 'h-[40px] px-5 text-sm rounded-[10px]',
  lg: 'h-[46px] px-6 text-sm rounded-[12px]',
}

export function Button({
  variant = 'primary',
  size = 'md',
  isLoading = false,
  fullWidth = false,
  disabled,
  className = '',
  type = 'button',
  children,
  ...rest
}: ButtonProps) {
  const isDisabled = Boolean(disabled || isLoading)

  return (
    <button
      type={type}
      disabled={isDisabled}
      className={`inline-flex items-center justify-center gap-2 font-semibold transition-colors duration-150 cursor-pointer focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary dark:focus-visible:outline-secondary disabled:opacity-50 disabled:pointer-events-none ${VARIANT_CLASSES[variant]} ${SIZE_CLASSES[size]} ${fullWidth ? 'w-full' : ''} ${className}`}
      {...rest}
    >
      {isLoading && <Spinner size="sm" />}
      <span>{children}</span>
    </button>
  )
}
