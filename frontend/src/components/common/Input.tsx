import { forwardRef, useId, type InputHTMLAttributes, type ReactNode } from 'react'

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
  hint?: string
  leadingIcon?: ReactNode
  trailingSlot?: ReactNode
}

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input(
  { label, error, hint, leadingIcon, trailingSlot, id, className = '', ...rest },
  ref,
) {
  const generatedId = useId()
  const inputId = id ?? generatedId
  const messageId = `${inputId}-msg`

  return (
    <div className="w-full space-y-1.5">
      {label && (
        <label
          htmlFor={inputId}
          className="block text-xs font-semibold text-neutral-text-main dark:text-dark-text-main"
        >
          {label}
        </label>
      )}

      <div
        className={`flex items-center gap-2.5 rounded-[10px] border px-3.5 h-10 bg-neutral-surface dark:bg-dark-bg transition-colors duration-150 ${
          error
            ? 'border-status-rejected focus-within:border-status-rejected'
            : 'border-neutral-border dark:border-dark-border focus-within:border-primary dark:focus-within:border-secondary'
        } ${className}`}
      >
        {leadingIcon && (
          <span className="text-neutral-text-muted dark:text-dark-text-muted shrink-0 inline-flex">
            {leadingIcon}
          </span>
        )}

        <input
          ref={ref}
          id={inputId}
          aria-invalid={Boolean(error)}
          aria-describedby={error || hint ? messageId : undefined}
          className="w-full bg-transparent text-sm text-neutral-text-main dark:text-dark-text-main placeholder:text-neutral-text-muted dark:placeholder:text-dark-text-muted outline-none"
          {...rest}
        />

        {trailingSlot && (
          <span className="text-neutral-text-muted dark:text-dark-text-muted shrink-0 inline-flex">
            {trailingSlot}
          </span>
        )}
      </div>

      {error ? (
        <p id={messageId} className="text-xs text-status-rejected">
          {error}
        </p>
      ) : hint ? (
        <p id={messageId} className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
          {hint}
        </p>
      ) : null}
    </div>
  )
})
