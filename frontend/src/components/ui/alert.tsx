import { forwardRef, type HTMLAttributes } from 'react'
import { cn } from '@/utils/cn'
import { alertVariants, type AlertVariantProps } from './variants'

export type AlertProps = HTMLAttributes<HTMLDivElement> & AlertVariantProps

export const Alert = forwardRef<HTMLDivElement, AlertProps>(function Alert(
  { className, variant = 'default', role = 'alert', ...props },
  ref,
) {
  return (
    <div ref={ref} role={role} className={cn(alertVariants({ variant }), className)} {...props} />
  )
})

export const AlertTitle = forwardRef<HTMLParagraphElement, HTMLAttributes<HTMLParagraphElement>>(
  function AlertTitle({ className, ...props }, ref) {
    return <p ref={ref} className={cn('font-semibold leading-snug', className)} {...props} />
  },
)

export const AlertDescription = forwardRef<
  HTMLParagraphElement,
  HTMLAttributes<HTMLParagraphElement>
>(function AlertDescription({ className, ...props }, ref) {
  return <div ref={ref} className={cn('mt-0.5 text-xs leading-relaxed', className)} {...props} />
})
