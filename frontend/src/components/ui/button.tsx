import { Slot } from '@radix-ui/react-slot'
import { forwardRef, type ButtonHTMLAttributes } from 'react'
import { cn } from '@/utils/cn'
import { buttonVariants, type ButtonVariantProps } from './variants'

export interface ShadcnButtonProps
  extends ButtonHTMLAttributes<HTMLButtonElement>,
    ButtonVariantProps {
  asChild?: boolean
}

export const ShadcnButton = forwardRef<HTMLButtonElement, ShadcnButtonProps>(
  function ShadcnButton({ className, variant, size, asChild = false, type = 'button', ...props }, ref) {
    const Comp = asChild ? Slot : 'button'
    return (
      <Comp
        ref={ref}
        type={asChild ? undefined : type}
        className={cn(buttonVariants({ variant, size }), className)}
        {...props}
      />
    )
  },
)
