import { Bell, Search } from 'lucide-react'
import { Link, useNavigate } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { Dropdown } from '@/components/common/Dropdown'
import { Logo } from '@/components/common/Logo'
import { ThemeToggle } from '@/components/common/ThemeToggle'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { useAuth } from '@/hooks/useAuth'
import { ROUTES } from '@/routes/paths'
import { NavigationBar } from './NavigationBar'

interface HeaderProps {
  onOpenSearch: () => void
}

function getInitials(fullName: string): string {
  const parts = fullName.trim().split(/\s+/)
  if (parts.length === 0) return 'NH'
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase()
}

export function Header({ onOpenSearch }: HeaderProps) {
  const navigate = useNavigate()
  const { user, roleName, isAuthenticated, logout } = useAuth()

  return (
    <header className="sticky top-0 z-40 border-b border-neutral-border dark:border-dark-border bg-neutral-surface/95 dark:bg-dark-surface/95 backdrop-blur-md">
      <div className="mx-auto flex h-16 w-full max-w-7xl items-center justify-between gap-4 px-6 lg:px-12">
        <div className="flex items-center gap-8">
          <Link
            to={ROUTES.HOME}
            className="focus-visible:outline-2 focus-visible:outline-primary rounded"
          >
            <Logo variant="horizontal" size="md" />
          </Link>

          <NavigationBar />
        </div>

        <div className="flex items-center gap-2.5">
          <button
            type="button"
            onClick={onOpenSearch}
            aria-label="Mở hộp thoại tìm kiếm nhanh"
            className="hidden md:flex items-center justify-between gap-3 rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg px-3 h-9 w-52 xl:w-64 text-xs text-neutral-text-muted dark:text-dark-text-muted hover:border-primary dark:hover:border-secondary transition-colors duration-150 cursor-pointer"
          >
            <span className="inline-flex items-center gap-2">
              <Search size={15} strokeWidth={1.75} className="shrink-0" />
              <span>Tìm kiếm tin tức...</span>
            </span>
            <kbd className="hidden xl:inline-block rounded border border-neutral-border dark:border-dark-border px-1.5 py-0.5 font-mono text-[10px]">
              ⌘K
            </kbd>
          </button>

          <button
            type="button"
            onClick={onOpenSearch}
            aria-label="Tìm kiếm tin tức"
            className="inline-flex md:hidden items-center justify-center w-9 h-9 rounded-[10px] border border-neutral-border dark:border-dark-border text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main hover:border-primary dark:hover:border-secondary transition-colors cursor-pointer"
          >
            <Search size={16} strokeWidth={1.75} />
          </button>

          <ThemeToggle />

          {isAuthenticated && user ? (
            <div className="flex items-center gap-2.5">
              <Tooltip>
                <TooltipTrigger asChild>
                  <button
                    type="button"
                    onClick={() => navigate(ROUTES.NOTIFICATIONS)}
                    aria-label="Thông báo của bạn"
                    className="relative inline-flex items-center justify-center w-9 h-9 rounded-[10px] border border-neutral-border dark:border-dark-border text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main hover:border-primary dark:hover:border-secondary transition-colors cursor-pointer"
                  >
                    <Bell size={16} strokeWidth={1.75} />
                  </button>
                </TooltipTrigger>
                <TooltipContent>Thông báo</TooltipContent>
              </Tooltip>

              <Dropdown
                headerLabel={user.email}
                trigger={
                  <button
                    type="button"
                    className="inline-flex items-center gap-2 rounded-full border border-neutral-border dark:border-dark-border pl-1.5 pr-3 py-1 text-xs font-semibold text-neutral-text-main dark:text-dark-text-main hover:border-primary dark:hover:border-secondary transition-colors cursor-pointer"
                  >
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
                    <span className="hidden sm:inline max-w-30 truncate">{user.fullName}</span>
                  </button>
                }
                items={[
                  {
                    key: 'profile',
                    label: 'Hồ sơ cá nhân',
                    onSelect: () => navigate(ROUTES.PROFILE),
                  },
                  ...(roleName === 'Journalist' || roleName === 'Admin'
                    ? [
                        {
                          key: 'cms',
                          label:
                            roleName === 'Admin'
                              ? 'Cổng Quản trị Hệ thống'
                              : 'Bàn làm việc Nhà báo',
                          onSelect: () => navigate(ROUTES.CMS.ROOT),
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
                    separatorBefore: true,
                    onSelect: () => {
                      void logout()
                    },
                  },
                ]}
              />
            </div>
          ) : (
            <div className="hidden sm:flex items-center gap-2">
              <Button variant="ghost" size="sm" onClick={() => navigate(ROUTES.AUTH.LOGIN)}>
                Đăng nhập
              </Button>
              <Button variant="primary" size="sm" onClick={() => navigate(ROUTES.AUTH.REGISTER)}>
                Đăng ký
              </Button>
            </div>
          )}
        </div>
      </div>
    </header>
  )
}
