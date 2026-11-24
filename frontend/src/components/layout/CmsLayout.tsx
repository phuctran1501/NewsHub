import { Bell, Menu } from 'lucide-react'
import { useState } from 'react'
import { NavLink, Outlet, ScrollRestoration, useNavigate } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { Logo } from '@/components/common/Logo'
import { PageTransition } from '@/components/common/PageTransition'
import { ThemeToggle } from '@/components/common/ThemeToggle'
import { Sheet, SheetContent, SheetDescription, SheetTitle } from '@/components/ui/sheet'
import { ROUTES } from '@/routes/paths'
import { cn } from '@/utils/cn'
import { CMS_NAV_ITEMS } from './cmsNavItems'
import { Sidebar } from './Sidebar'

export function CmsLayout() {
  const navigate = useNavigate()
  const [isCollapsed, setIsCollapsed] = useState(false)
  const [isMobileDrawerOpen, setIsMobileDrawerOpen] = useState(false)

  return (
    <div className="min-h-screen flex bg-neutral-bg dark:bg-dark-bg text-neutral-text-main dark:text-dark-text-main">
      <ScrollRestoration />
      <Sidebar
        isCollapsed={isCollapsed}
        onToggleCollapse={() => setIsCollapsed((prev) => !prev)}
      />

      <Sheet open={isMobileDrawerOpen} onOpenChange={setIsMobileDrawerOpen}>
        <SheetContent side="left" className="space-y-6">
          <SheetTitle className="sr-only">Điều hướng Bàn làm việc Tòa soạn</SheetTitle>
          <SheetDescription className="sr-only">
            Chọn phân mục quản trị bài viết hoặc hệ thống.
          </SheetDescription>

          <Logo variant="horizontal" size="sm" />

          <nav className="space-y-1 text-xs font-medium">
            {CMS_NAV_ITEMS.map((item) => {
              const IconComponent = item.icon
              return (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.end}
                  onClick={() => setIsMobileDrawerOpen(false)}
                  className={({ isActive }) =>
                    cn(
                      'flex items-center gap-3 px-3 py-2.5 rounded-[10px] transition-colors',
                      isActive
                        ? 'bg-sidebar-active dark:bg-dark-border font-semibold text-primary dark:text-secondary'
                        : 'text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main',
                    )
                  }
                >
                  <IconComponent size={16} strokeWidth={1.75} className="shrink-0" />
                  <span>{item.label}</span>
                </NavLink>
              )
            })}
          </nav>
        </SheetContent>
      </Sheet>

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
            <Button variant="primary" size="sm" onClick={() => navigate(ROUTES.CMS.EDITOR)}>
              Viết bài mới
            </Button>
            <ThemeToggle />
            <button
              type="button"
              onClick={() => navigate(ROUTES.NOTIFICATIONS)}
              aria-label="Thông báo"
              className="inline-flex items-center justify-center w-9 h-9 rounded-[10px] border border-neutral-border dark:border-dark-border text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main transition-colors cursor-pointer"
            >
              <Bell size={16} strokeWidth={1.75} />
            </button>
          </div>
        </header>

        <main className="flex-1 p-6 lg:p-8 mx-auto w-full max-w-7xl">
          <PageTransition>
            <Outlet />
          </PageTransition>
        </main>
      </div>
    </div>
  )
}
