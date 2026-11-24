import {
  CheckSquare,
  FileText,
  LayoutDashboard,
  PanelLeftClose,
  PanelLeftOpen,
  PenLine,
  Shield,
} from 'lucide-react'
import { NavLink } from 'react-router-dom'
import { useAuth } from '../../hooks/useAuth'
import { Logo } from '../common/Logo'

interface SidebarProps {
  isCollapsed: boolean
  onToggleCollapse: () => void
}

const CMS_NAV_ITEMS = [
  { label: 'Tổng quan', to: '/cms', end: true, icon: LayoutDashboard },
  { label: 'Bài viết của tôi', to: '/cms/articles', end: false, icon: FileText },
  { label: 'Soạn bài mới', to: '/cms/editor', end: false, icon: PenLine },
  { label: 'Duyệt bài Tòa soạn', to: '/cms/review', end: false, icon: CheckSquare },
  { label: 'Quản trị Hệ thống', to: '/cms/admin', end: false, icon: Shield },
]

export function Sidebar({ isCollapsed, onToggleCollapse }: SidebarProps) {
  const { user, roleName } = useAuth()

  return (
    <aside
      aria-label="Thanh điều hướng bàn làm việc"
      className={`hidden md:flex flex-col justify-between border-r border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface shrink-0 transition-[width] duration-150 ${
        isCollapsed ? 'w-[68px]' : 'w-64'
      }`}
    >
      <div className="p-3.5 space-y-5">
        <div
          className={`flex items-center ${
            isCollapsed ? 'justify-center' : 'justify-between px-1.5'
          }`}
        >
          <NavLink to="/" aria-label="Về trang chủ NewsHub">
            <Logo variant={isCollapsed ? 'mark' : 'horizontal'} size="sm" />
          </NavLink>

          {!isCollapsed && (
            <button
              type="button"
              onClick={onToggleCollapse}
              aria-label="Thu gọn thanh bên"
              className="p-1.5 rounded-[8px] text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main hover:bg-neutral-bg dark:hover:bg-dark-border/50 transition-colors cursor-pointer"
            >
              <PanelLeftClose size={16} strokeWidth={1.75} />
            </button>
          )}
        </div>

        {isCollapsed && (
          <div className="flex justify-center">
            <button
              type="button"
              onClick={onToggleCollapse}
              aria-label="Mở rộng thanh bên"
              className="p-1.5 rounded-[8px] text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main hover:bg-neutral-bg dark:hover:bg-dark-border/50 transition-colors cursor-pointer"
            >
              <PanelLeftOpen size={16} strokeWidth={1.75} />
            </button>
          </div>
        )}

        <nav className="space-y-1">
          {CMS_NAV_ITEMS.map((item) => {
            const IconComponent = item.icon
            return (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.end}
                title={isCollapsed ? item.label : undefined}
                className={({ isActive }) =>
                  `flex items-center ${
                    isCollapsed ? 'justify-center px-2' : 'gap-3 px-3'
                  } py-2.5 rounded-[10px] text-xs transition-colors duration-150 ${
                    isActive
                      ? 'bg-sidebar-active dark:bg-dark-border font-semibold text-primary dark:text-secondary'
                      : 'font-medium text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main hover:bg-neutral-bg dark:hover:bg-dark-border/50'
                  }`
                }
              >
                <IconComponent size={16} strokeWidth={1.75} className="shrink-0" />
                {!isCollapsed && <span className="truncate">{item.label}</span>}
              </NavLink>
            )
          })}
        </nav>
      </div>

      <div className="p-3.5 border-t border-neutral-border dark:border-dark-border">
        <div
          className={`flex items-center ${
            isCollapsed ? 'justify-center' : 'gap-2.5 px-1.5'
          } text-xs`}
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
