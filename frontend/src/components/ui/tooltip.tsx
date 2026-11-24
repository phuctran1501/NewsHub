import * as TooltipPrimitive from '@radix-ui/react-tooltip'
import { forwardRef, type ComponentPropsWithoutRef, type ElementRef } from 'react'
import { cn } from '@/utils/cn'

export const TooltipProvider = TooltipPrimitive.Provider
export const Tooltip = TooltipPrimitive.Root
export const TooltipTrigger = TooltipPrimitive.Trigger

export const TooltipContent = forwardRef<
  ElementRef<typeof TooltipPrimitive.Content>,
  ComponentPropsWithoutRef<typeof TooltipPrimitive.Content>
>(function TooltipContent({ className, sideOffset = 6, ...props }, ref) {
  return (
    <TooltipPrimitive.Portal>
      <TooltipPrimitive.Content
        ref={ref}
        sideOffset={sideOffset}
        className={cn(
          'z-50 rounded-[8px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface px-2.5 py-1 text-xs font-medium text-neutral-text-main dark:text-dark-text-main shadow-[0_8px_24px_rgba(15,23,42,0.08)]',
          className,
        )}
        {...props}
      />
    </TooltipPrimitive.Portal>
  )
})
