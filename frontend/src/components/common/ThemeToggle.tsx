import { Moon, Sun } from 'lucide-react'
import { useTheme } from '../../hooks/useTheme'

interface ThemeToggleProps {
  className?: string
}

export function ThemeToggle({ className = '' }: ThemeToggleProps) {
  const { resolvedTheme, toggleTheme } = useTheme()
  const isDark = resolvedTheme === 'dark'

  return (
    <button
      type="button"
      onClick={toggleTheme}
      aria-label={isDark ? 'Chuyển sang giao diện sáng' : 'Chuyển sang giao diện tối'}
      title={isDark ? 'Chuyển sang giao diện sáng' : 'Chuyển sang giao diện tối'}
      className={`inline-flex items-center justify-center w-9 h-9 rounded-[10px] border border-neutral-border dark:border-dark-border bg-transparent text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main hover:border-primary dark:hover:border-secondary transition-colors duration-150 cursor-pointer focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary ${className}`}
    >
      {isDark ? <Sun size={16} strokeWidth={1.75} /> : <Moon size={16} strokeWidth={1.75} />}
    </button>
  )
}
