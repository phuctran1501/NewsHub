import type { ReactNode } from 'react'
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
  headerLabel?: string
  items: DropdownItem[]
  align?: 'start' | 'center' | 'end'
}

export function Dropdown({ trigger, headerLabel, items, align = 'end' }: DropdownProps) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>{trigger}</DropdownMenuTrigger>
      <DropdownMenuContent align={align}>
        {headerLabel && (
          <>
            <DropdownMenuLabel>{headerLabel}</DropdownMenuLabel>
            <DropdownMenuSeparator />
          </>
        )}
        {items.map((item) => (
          <div key={item.key}>
            {item.separatorBefore && <DropdownMenuSeparator />}
            <DropdownMenuItem tone={item.tone} onSelect={item.onSelect}>
              {item.label}
            </DropdownMenuItem>
          </div>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
