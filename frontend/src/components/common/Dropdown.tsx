import { useEffect, useRef, useState, type ReactNode } from 'react'

export interface DropdownItem {
  key: string
  label: string
  onSelect: () => void
  tone?: 'default' | 'danger'
}

interface DropdownProps {
  trigger: ReactNode
  items: DropdownItem[]
  align?: 'left' | 'right'
  className?: string
}

export function Dropdown({
  trigger,
  items,
  align = 'right',
  className = '',
}: DropdownProps) {
  const [isOpen, setIsOpen] = useState(false)
  const containerRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    if (!isOpen) return

    const handleOutsideClick = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setIsOpen(false)
      }
    }

    document.addEventListener('mousedown', handleOutsideClick)
    window.addEventListener('keydown', handleKeyDown)
    return () => {
      document.removeEventListener('mousedown', handleOutsideClick)
      window.removeEventListener('keydown', handleKeyDown)
    }
  }, [isOpen])

  return (
    <div ref={containerRef} className={`relative inline-flex ${className}`}>
      <div
        onClick={() => setIsOpen((prev) => !prev)}
        onKeyDown={(event) => {
          if (event.key === 'Enter' || event.key === ' ') {
            event.preventDefault()
            setIsOpen((prev) => !prev)
          }
        }}
        role="button"
        tabIndex={0}
        aria-expanded={isOpen}
        aria-haspopup="menu"
        className="cursor-pointer"
      >
        {trigger}
      </div>

      {isOpen && (
        <div
          role="menu"
          className={`absolute top-full mt-2 z-40 min-w-50 rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-1.5 shadow-[0_8px_24px_rgba(15,23,42,0.08)] ${
            align === 'right' ? 'right-0' : 'left-0'
          }`}
        >
          {items.map((item) => (
            <button
              key={item.key}
              type="button"
              role="menuitem"
              onClick={() => {
                setIsOpen(false)
                item.onSelect()
              }}
              className={`w-full text-left px-3 py-2 rounded-lg text-xs font-medium transition-colors cursor-pointer ${
                item.tone === 'danger'
                  ? 'text-status-rejected hover:bg-status-rejected/10'
                  : 'text-neutral-text-main dark:text-dark-text-main hover:bg-sidebar-active dark:hover:bg-dark-border hover:text-primary dark:hover:text-secondary'
              }`}
            >
              {item.label}
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
