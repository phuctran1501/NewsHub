import { forwardRef, type ReactNode } from 'react'
import { ShadcnButton, type ShadcnButtonProps } from '@/components/ui/button'
import { cn } from '@/utils/cn'
import { Spinner } from './Spinner'

export interface ButtonProps extends ShadcnButtonProps {
  isLoading?: boolean
  fullWidth?: boolean
  children: ReactNode
}

export const Button = forwardRef<HTMLButtonElement, ButtonProps>(function Button(
  {
    isLoading = false,
    fullWidth = false,
    disabled,
    asChild = false,
    className,
    children,
    ...rest
  },
  ref,
) {
  return (
    <ShadcnButton
      ref={ref}
      asChild={asChild}
      disabled={Boolean(disabled || isLoading)}
      className={cn(fullWidth && 'w-full', className)}
      {...rest}
    >
      {asChild ? (
        children
      ) : (
        <>
          {isLoading && <Spinner size="sm" />}
          <span>{children}</span>
        </>
      )}
    </ShadcnButton>
  )
})
