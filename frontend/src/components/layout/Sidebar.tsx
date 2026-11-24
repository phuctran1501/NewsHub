import { motion } from 'framer-motion'
import { PanelLeftClose, PanelLeftOpen } from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { Logo } from '@/components/common/Logo'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { useAuth } from '@/hooks/useAuth'
import { ROUTES } from '@/routes/paths'
import { cn } from '@/utils/cn'
import { CMS_NAV_ITEMS } from './cmsNavItems'

interface SidebarProps {
  isCollapsed: boolean
  onToggleCollapse: () => void
}

export function Sidebar({ isCollapsed, onToggleCollapse }: SidebarProps) {
  const { user, roleName } = useAuth()

  return (
    <aside
      aria-label="Thanh điều hướng bàn làm việc"
      className={cn(
        'hidden md:flex flex-col justify-between border-r border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface shrink-0 transition-[width] duration-200 ease-out',
        isCollapsed ? 'w-17' : 'w-64',
      )}
    >
      <div className="p-3.5 space-y-5">
        <div
          className={cn(
            'flex items-center',
            isCollapsed ? 'justify-center' : 'justify-between px-1.5',
          )}
        >
          <NavLink to={ROUTES.HOME} aria-label="Về trang chủ NewsHub">
            <Logo variant={isCollapsed ? 'mark' : 'horizontal'} size="sm" />
          </NavLink>

          {!isCollapsed && (
            <button
              type="button"
              onClick={onToggleCollapse}
              aria-label="Thu gọn thanh bên"
              className="p-1.5 rounded-lg text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main hover:bg-neutral-bg dark:hover:bg-dark-border/50 transition-colors cursor-pointer"
            >
              <PanelLeftClose size={16} strokeWidth={1.75} />
            </button>
          )}
        </div>

        {isCollapsed && (
          <div className="flex justify-center">
            <Tooltip>
              <TooltipTrigger asChild>
                <button
                  type="button"
                  onClick={onToggleCollapse}
                  aria-label="Mở rộng thanh bên"
                  className="p-1.5 rounded-lg text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main hover:bg-neutral-bg dark:hover:bg-dark-border/50 transition-colors cursor-pointer"
                >
                  <PanelLeftOpen size={16} strokeWidth={1.75} />
                </button>
              </TooltipTrigger>
              <TooltipContent side="right">Mở rộng thanh bên</TooltipContent>
            </Tooltip>
          </div>
        )}

        <nav className="space-y-1">
          {CMS_NAV_ITEMS.map((item) => {
            const IconComponent = item.icon

            const linkNode = (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                className={({ isActive }) =>
                  cn(
                    'relative flex items-center py-2.5 rounded-[10px] text-xs transition-colors duration-150',
                    isCollapsed ? 'justify-center px-2' : 'gap-3 px-3',
                    isActive
                      ? 'font-semibold text-primary dark:text-secondary'
                      : 'font-medium text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main hover:bg-neutral-bg dark:hover:bg-dark-border/50',
                  )
                }
              >
                {({ isActive }) => (
                  <>
                    {isActive && (
                      <motion.span
                        layoutId="cms-sidebar-active-pill"
                        transition={{ type: 'spring', stiffness: 450, damping: 38 }}
                        aria-hidden="true"
                        className="absolute inset-0 rounded-[10px] bg-sidebar-active dark:bg-dark-border"
                      />
                    )}
                    <IconComponent size={16} strokeWidth={1.75} className="relative z-10 shrink-0" />
                    {!isCollapsed && <span className="relative z-10 truncate">{item.label}</span>}
                  </>
                )}
              </NavLink>
            )

            if (!isCollapsed) {
              return linkNode
            }

            return (
              <Tooltip key={item.to}>
                <TooltipTrigger asChild>{linkNode}</TooltipTrigger>
                <TooltipContent side="right">{item.label}</TooltipContent>
              </Tooltip>
            )
          })}
        </nav>
      </div>

      <div className="p-3.5 border-t border-neutral-border dark:border-dark-border">
        <div
          className={cn(
            'flex items-center text-xs',
            isCollapsed ? 'justify-center' : 'gap-2.5 px-1.5',
          )}
        >
          <div className="w-8 h-8 rounded-full border border-neutral-border dark:border-dark-border flex items-center justify-center font-semibold text-primary dark:text-secondary shrink-0">
            {user ? user.fullName.slice(0, 2).toUpperCase() : 'NH'}
          </div>
          {!isCollapsed && (
            <div className="min-w-0">
              <p className="font-semibold text-neutral-text-main dark:text-dark-text-main truncate">
                {user ? user.fullName : 'Chế độ xem khung CMS'}
              </p>
              <p className="text-[11px] text-neutral-text-muted dark:text-dark-text-muted truncate">
                {roleName ?? 'Chưa đăng nhập'}
              </p>
            </div>
          )}
        </div>
      </div>
    </aside>
  )
}
