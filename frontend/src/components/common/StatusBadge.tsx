import type { ReactNode } from 'react'

interface StatusBadgeProps {
  status: 'online' | 'testing' | 'offline' | 'warning'
  children: ReactNode
}

export function StatusBadge({ status, children }: StatusBadgeProps) {
  const statusStyles = {
    online: 'bg-emerald-50 text-emerald-800 border-emerald-300 dark:bg-emerald-950/40 dark:text-emerald-300 dark:border-emerald-800',
    testing: 'bg-indigo-50 text-indigo-800 border-indigo-300 dark:bg-indigo-950/40 dark:text-indigo-300 dark:border-indigo-800',
    offline: 'bg-rose-50 text-rose-800 border-rose-300 dark:bg-rose-950/40 dark:text-rose-300 dark:border-rose-800',
    warning: 'bg-amber-50 text-amber-800 border-amber-300 dark:bg-amber-950/40 dark:text-amber-300 dark:border-amber-800',
  }

  const dotColors = {
    online: 'bg-emerald-500',
    testing: 'bg-indigo-500 animate-pulse',
    offline: 'bg-rose-500',
    warning: 'bg-amber-500',
  }

  return (
    <span
      className={`inline-flex items-center gap-1.5 px-2.5 py-1 text-xs font-semibold uppercase tracking-wider border rounded-[4px] ${statusStyles[status]}`}
    >
      <span className={`w-2 h-2 rounded-full ${dotColors[status]}`} />
      {children}
    </span>
  )
}
