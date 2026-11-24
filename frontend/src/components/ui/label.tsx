import { forwardRef, type LabelHTMLAttributes } from 'react'
import { cn } from '@/utils/cn'

export const Label = forwardRef<HTMLLabelElement, LabelHTMLAttributes<HTMLLabelElement>>(
  function Label({ className, ...props }, ref) {
    return (
      <label
        ref={ref}
        className={cn(
          'block text-xs font-semibold leading-none text-neutral-text-main dark:text-dark-text-main peer-disabled:cursor-not-allowed peer-disabled:opacity-60',
          className,
        )}
        {...props}
      />
    )
  },
)
