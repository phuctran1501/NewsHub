import { Fragment, type ReactNode } from 'react'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'

export interface DropdownItem {
  key: string
  label: string
  onSelect: () => void
  tone?: 'default' | 'danger'
  separatorBefore?: boolean
}

interface DropdownProps {
  trigger: ReactNode
  headerTitle?: string
  headerLabel?: string
  items: DropdownItem[]
  align?: 'start' | 'center' | 'end'
}

export function Dropdown({
  trigger,
  headerTitle,
  headerLabel,
  items,
  align = 'end',
}: DropdownProps) {
  const hasHeader = Boolean(headerTitle || headerLabel)

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>{trigger}</DropdownMenuTrigger>
      <DropdownMenuContent align={align}>
        {hasHeader && (
          <>
            <DropdownMenuLabel className="space-y-0.5">
              {headerTitle && (
                <p className="truncate text-xs font-semibold text-neutral-text-main dark:text-dark-text-main">
                  {headerTitle}
                </p>
              )}
              {headerLabel && (
                <p className="truncate text-[11px] font-normal text-neutral-text-muted dark:text-dark-text-muted">
                  {headerLabel}
                </p>
              )}
            </DropdownMenuLabel>
            <DropdownMenuSeparator />
          </>
        )}
        {items.map((item) => (
          <Fragment key={item.key}>
            {item.separatorBefore && <DropdownMenuSeparator />}
            <DropdownMenuItem tone={item.tone} onSelect={item.onSelect}>
              {item.label}
            </DropdownMenuItem>
          </Fragment>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
