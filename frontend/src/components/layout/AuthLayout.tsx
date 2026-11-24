import { Link, Outlet, ScrollRestoration } from 'react-router-dom'
import { Logo } from '@/components/common/Logo'
import { PageTransition } from '@/components/common/PageTransition'
import { ThemeToggle } from '@/components/common/ThemeToggle'
import { ROUTES } from '@/routes/paths'

export function AuthLayout() {
  const currentYear = new Date().getFullYear()

  return (
    <div className="min-h-screen flex flex-col justify-between bg-neutral-bg dark:bg-dark-bg text-neutral-text-main dark:text-dark-text-main px-6 py-6">
      <ScrollRestoration />
      <header className="mx-auto flex w-full max-w-7xl items-center justify-between">
        <Link
          to={ROUTES.HOME}
          className="text-xs font-medium text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main transition-colors"
        >
          ← Về trang chủ NewsHub
        </Link>
        <ThemeToggle />
      </header>

      <main className="mx-auto my-10 w-full max-w-md space-y-6">
        <div className="flex justify-center">
          <Link to={ROUTES.HOME} aria-label="Trang chủ NewsHub">
            <Logo variant="horizontal" size="lg" />
          </Link>
        </div>

        <div className="rounded-2xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-6 sm:p-8">
          <PageTransition>
            <Outlet />
          </PageTransition>
        </div>
      </main>

      <footer className="text-center text-xs text-neutral-text-muted dark:text-dark-text-muted">
        Điều khoản sử dụng • Chính sách bảo mật • © {currentYear} NewsHub
      </footer>
    </div>
  )
}
