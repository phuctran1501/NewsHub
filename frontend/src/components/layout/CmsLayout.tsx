import { Bell, Menu, X } from 'lucide-react'
import { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { Button } from '../common/Button'
import { Logo } from '../common/Logo'
import { ThemeToggle } from '../common/ThemeToggle'
import { Sidebar } from './Sidebar'

export function CmsLayout() {
  const navigate = useNavigate()
  const [isCollapsed, setIsCollapsed] = useState(false)
  const [isMobileDrawerOpen, setIsMobileDrawerOpen] = useState(false)

  return (
    <div className="min-h-screen flex bg-neutral-bg dark:bg-dark-bg text-neutral-text-main dark:text-dark-text-main">
      <Sidebar
        isCollapsed={isCollapsed}
        onToggleCollapse={() => setIsCollapsed((prev) => !prev)}
      />

      {isMobileDrawerOpen && (
        <div
          className="fixed inset-0 z-50 flex md:hidden bg-black/45"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              setIsMobileDrawerOpen(false)
            }
          }}
        >
          <div className="w-64 h-full bg-neutral-surface dark:bg-dark-surface border-r border-neutral-border dark:border-dark-border p-4 space-y-5">
            <div className="flex items-center justify-between">
              <Logo variant="horizontal" size="sm" />
              <button
                type="button"
                onClick={() => setIsMobileDrawerOpen(false)}
                aria-label="Đóng menu"
                className="p-1.5 rounded-[8px] text-neutral-text-muted dark:text-dark-text-muted"
              >
                <X size={16} strokeWidth={1.75} />
              </button>
            </div>

            <nav className="space-y-1 text-xs font-medium">
              {[
                { label: 'Tổng quan', to: '/cms' },
                { label: 'Bài viết của tôi', to: '/cms/articles' },
                { label: 'Soạn bài mới', to: '/cms/editor' },
                { label: 'Duyệt bài Tòa soạn', to: '/cms/review' },
                { label: 'Quản trị Hệ thống', to: '/cms/admin' },
              ].map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.to === '/cms'}
                  onClick={() => setIsMobileDrawerOpen(false)}
                  className={({ isActive }) =>
                    `block px-3 py-2.5 rounded-[10px] ${
                      isActive
                        ? 'bg-sidebar-active dark:bg-dark-border font-semibold text-primary dark:text-secondary'
                        : 'text-neutral-text-muted dark:text-dark-text-muted'
                    }`
                  }
                >
                  {item.label}
                </NavLink>
              ))}
            </nav>
          </div>
        </div>
      )}

      <div className="flex-1 flex flex-col min-w-0">
        <header className="sticky top-0 z-30 h-16 border-b border-neutral-border dark:border-dark-border bg-neutral-surface/95 dark:bg-dark-surface/95 backdrop-blur-md px-6 flex items-center justify-between gap-4">
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => setIsMobileDrawerOpen(true)}
              aria-label="Mở menu điều hướng"
              className="inline-flex md:hidden items-center justify-center w-9 h-9 rounded-[10px] border border-neutral-border dark:border-dark-border text-neutral-text-muted dark:text-dark-text-muted"
            >
              <Menu size={16} strokeWidth={1.75} />
            </button>

            <h1 className="text-sm font-semibold text-neutral-text-main dark:text-dark-text-main">
              Bàn làm việc Tòa soạn
            </h1>
          </div>

          <div className="flex items-center gap-2.5">
            <Button variant="primary" size="sm" onClick={() => navigate('/cms/editor')}>
              Viết bài mới
            </Button>
            <ThemeToggle />
            <button
              type="button"
              onClick={() => navigate('/me/notifications')}
              aria-label="Thông báo"
              className="inline-flex items-center justify-center w-9 h-9 rounded-[10px] border border-neutral-border dark:border-dark-border text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main transition-colors cursor-pointer"
            >
              <Bell size={16} strokeWidth={1.75} />
            </button>
          </div>
        </header>

        <main className="flex-1 p-6 lg:p-8 mx-auto w-full max-w-7xl">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
