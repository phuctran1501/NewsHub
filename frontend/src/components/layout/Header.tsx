import { Bell, Search } from 'lucide-react'
import { Link, useNavigate } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { Dropdown } from '@/components/common/Dropdown'
import { Logo } from '@/components/common/Logo'
import { ThemeToggle } from '@/components/common/ThemeToggle'
import { UserAvatar } from '@/components/common/UserAvatar'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { useAuth } from '@/hooks/useAuth'
import { ROUTES } from '@/routes/paths'
import { CategoryMegaMenuBar } from './CategoryMegaMenuBar'

interface HeaderProps {
  onOpenSearch: () => void
}

export function Header({ onOpenSearch }: HeaderProps) {
  const navigate = useNavigate()
  const { user, roleName, isAuthenticated, isInitializing, logout } = useAuth()

  return (
    <header className="sticky top-0 z-40 w-full border-b border-neutral-border dark:border-dark-border bg-neutral-surface/95 dark:bg-dark-surface/95 backdrop-blur-md">
      <div className="flex h-16 w-full items-center justify-between gap-4 px-4 sm:px-6 lg:px-10 xl:px-14">
        <div className="flex items-center gap-4 shrink-0">
          <Link
            to={ROUTES.HOME}
            className="focus-visible:outline-2 focus-visible:outline-primary rounded"
          >
            <Logo variant="horizontal" size="md" />
          </Link>

          <span className="hidden xl:inline-block border-l border-neutral-border dark:border-dark-border pl-4 text-xs text-neutral-text-muted dark:text-dark-text-muted">
            Cổng Tin Tức & Hệ Sinh Thái Đa Tòa Soạn
          </span>
        </div>

        <div className="hidden md:block flex-1 max-w-xl">
          <div className="nh-search-beam rounded-xl">
            <button
              type="button"
              onClick={onOpenSearch}
              aria-label="Mở hộp thoại tìm kiếm nhanh"
              className="relative z-10 flex h-10 w-full items-center justify-between gap-3 rounded-[10.5px] bg-neutral-surface dark:bg-dark-bg px-3.5 text-xs text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main transition-colors duration-150 cursor-pointer"
            >
              <span className="inline-flex items-center gap-2 truncate">
                <Search size={15} strokeWidth={1.75} className="shrink-0" />
                <span className="truncate">
                  {isAuthenticated
                    ? 'Tìm kiếm ngữ nghĩa AI hoặc từ khóa tin tức...'
                    : 'Tìm kiếm bài viết, chuyên mục, tòa soạn...'}
                </span>
              </span>
              <span className="flex items-center gap-2 shrink-0">
                {isAuthenticated && (
                  <span className="rounded-full bg-secondary-light dark:bg-secondary/15 px-2 py-0.5 text-[10px] font-semibold text-primary dark:text-secondary">
                    AI
                  </span>
                )}
                <kbd className="hidden lg:inline-block rounded border border-neutral-border dark:border-dark-border px-1.5 py-0.5 font-mono text-[10px]">
                  ⌘K
                </kbd>
              </span>
            </button>
          </div>
        </div>

        <div className="flex items-center gap-2.5 shrink-0">
          <button
            type="button"
            onClick={onOpenSearch}
            aria-label="Tìm kiếm tin tức"
            className="inline-flex md:hidden items-center justify-center w-9 h-9 rounded-[10px] border border-neutral-border dark:border-dark-border text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main hover:border-primary dark:hover:border-secondary transition-colors cursor-pointer"
          >
            <Search size={16} strokeWidth={1.75} />
          </button>

          <ThemeToggle />

          {isInitializing ? (
            <div className="flex items-center gap-2.5" aria-hidden="true">
              <div className="w-9 h-9 rounded-[10px] bg-neutral-border/60 dark:bg-dark-border/60 animate-pulse" />
              <div className="w-9 h-9 rounded-full bg-neutral-border/60 dark:bg-dark-border/60 animate-pulse" />
            </div>
          ) : isAuthenticated && user ? (
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
                headerTitle={user.fullName}
                headerLabel={user.email}
                trigger={
                  <button
                    type="button"
                    aria-label="Mở menu tài khoản cá nhân"
                    className="inline-flex items-center justify-center rounded-full focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary dark:focus-visible:outline-secondary cursor-pointer"
                  >
                    <UserAvatar
                      fullName={user.fullName}
                      avatarUrl={user.avatarUrl}
                      size="md"
                      className="hover:border-primary dark:hover:border-secondary transition-colors"
                    />
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

      <CategoryMegaMenuBar />
    </header>
  )
}
