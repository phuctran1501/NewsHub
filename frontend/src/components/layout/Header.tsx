import { Bell, Search } from 'lucide-react'
import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth'
import { useDebounce } from '../../hooks/useDebounce'
import { Button } from '../common/Button'
import { Dropdown } from '../common/Dropdown'
import { Logo } from '../common/Logo'
import { ThemeToggle } from '../common/ThemeToggle'
import { NavigationBar } from './NavigationBar'

function getInitials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/)
  if (parts.length === 0) return 'NH'
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase()
}

export function Header() {
  const navigate = useNavigate()
  const { user, roleName, isAuthenticated, logout } = useAuth()
  const [query, setQuery] = useState('')
  const debouncedQuery = useDebounce(query, 300)
  const searchInputRef = useRef<HTMLInputElement | null>(null)

  useEffect(() => {
    const handleGlobalShortcut = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault()
        searchInputRef.current?.focus()
      }
    }

    window.addEventListener('keydown', handleGlobalShortcut)
    return () => window.removeEventListener('keydown', handleGlobalShortcut)
  }, [])

  const handleSearchSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const trimmed = (debouncedQuery || query).trim()
    if (!trimmed) return
    navigate(`/?q=${encodeURIComponent(trimmed)}`)
  }

  return (
    <header className="sticky top-0 z-40 border-b border-neutral-border dark:border-dark-border bg-neutral-surface/95 dark:bg-dark-surface/95 backdrop-blur-md">
      <div className="mx-auto flex h-16 w-full max-w-7xl items-center justify-between gap-4 px-6 lg:px-12">
        <div className="flex items-center gap-8">
          <Link to="/" className="focus-visible:outline-2 focus-visible:outline-primary rounded">
            <Logo variant="horizontal" size="md" />
          </Link>

          <NavigationBar />
        </div>

        <div className="flex items-center gap-2.5">
          <form
            onSubmit={handleSearchSubmit}
            role="search"
            className="hidden md:flex items-center gap-2 rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg px-3 h-9 w-56 xl:w-64 focus-within:border-primary dark:focus-within:border-secondary transition-colors duration-150"
          >
            <Search
              size={15}
              strokeWidth={1.75}
              className="text-neutral-text-muted dark:text-dark-text-muted shrink-0"
            />
            <input
              ref={searchInputRef}
              type="search"
              value={query}
              onChange={(event) => setQuery(event.target.value)}
              placeholder="Tìm kiếm tin tức..."
              aria-label="Tìm kiếm tin tức"
              className="w-full bg-transparent text-xs text-neutral-text-main dark:text-dark-text-main placeholder:text-neutral-text-muted dark:placeholder:text-dark-text-muted outline-none"
            />
            <kbd className="hidden xl:inline-block rounded border border-neutral-border dark:border-dark-border px-1.5 py-0.5 font-mono text-[10px] text-neutral-text-muted dark:text-dark-text-muted">
              ⌘K
            </kbd>
          </form>

          <ThemeToggle />

          {isAuthenticated && user ? (
            <div className="flex items-center gap-2.5">
              <button
                type="button"
                onClick={() => navigate('/me/notifications')}
                aria-label="Thông báo của bạn"
                className="relative inline-flex items-center justify-center w-9 h-9 rounded-[10px] border border-neutral-border dark:border-dark-border text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main hover:border-primary dark:hover:border-secondary transition-colors cursor-pointer"
              >
                <Bell size={16} strokeWidth={1.75} />
              </button>

              <Dropdown
                trigger={
                  <span className="inline-flex items-center gap-2 rounded-full border border-neutral-border dark:border-dark-border pl-1.5 pr-3 py-1 text-xs font-semibold text-neutral-text-main dark:text-dark-text-main hover:border-primary dark:hover:border-secondary transition-colors">
                    {user.avatarUrl ? (
                      <img
                        src={user.avatarUrl}
                        alt={user.fullName}
                        className="w-6 h-6 rounded-full object-cover"
                      />
                    ) : (
                      <span className="w-6 h-6 rounded-full border border-neutral-border dark:border-dark-border flex items-center justify-center text-[10px] font-bold text-primary dark:text-secondary">
                        {getInitials(user.fullName)}
                      </span>
                    )}
                    <span className="hidden sm:inline max-w-[120px] truncate">{user.fullName}</span>
                  </span>
                }
                items={[
                  ...(roleName === 'Journalist' || roleName === 'Admin'
                    ? [
                        {
                          key: 'cms',
                          label:
                            roleName === 'Admin' ? 'Cổng Quản trị Hệ thống' : 'Bàn làm việc Nhà báo',
                          onSelect: () => navigate('/cms'),
                        },
                      ]
                    : []),
                  {
                    key: 'bookmarks',
                    label: 'Bài viết đã lưu',
                    onSelect: () => navigate('/?section=bookmarks'),
                  },
                  {
                    key: 'logout',
                    label: 'Đăng xuất',
                    tone: 'danger',
                    onSelect: () => {
                      void logout()
                    },
                  },
                ]}
              />
            </div>
          ) : (
            <div className="hidden sm:flex items-center gap-2">
              <Button variant="ghost" size="sm" onClick={() => navigate('/auth/login')}>
                Đăng nhập
              </Button>
              <Button variant="primary" size="sm" onClick={() => navigate('/auth/register')}>
                Đăng ký
              </Button>
            </div>
          )}
        </div>
      </div>
    </header>
  )
}
