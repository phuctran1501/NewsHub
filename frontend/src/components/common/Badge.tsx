import type { HTMLAttributes } from 'react'
import { badgeVariants, type BadgeVariantProps } from '@/components/ui/variants'
import { cn } from '@/utils/cn'

export type BadgeProps = HTMLAttributes<HTMLSpanElement> & BadgeVariantProps

export function Badge({ variant = 'category', className, ...props }: BadgeProps) {
  return <span className={cn(badgeVariants({ variant }), className)} {...props} />
}
