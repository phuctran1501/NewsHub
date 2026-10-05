import { Link, NavLink, Outlet, ScrollRestoration } from "react-router-dom";

export default function RootLayout() {
  return (
    <div className="flex min-h-screen flex-col bg-surface-primary text-text-primary antialiased selection:bg-brand-500 selection:text-white">
      {/* Scroll restoration on route change */}
      <ScrollRestoration />

      {/* ── Top Announcement / Sub-bar ───────────────────────────────── */}
      <div className="border-b border-border-subtle bg-surface-secondary/60 px-4 py-1.5 text-center text-xs text-text-muted">
        <span>🚀 NewsHub v0.1 — Nền tảng tổng hợp & phân tích tin tức thông minh hỗ trợ bởi AI</span>
      </div>

      {/* ── Global Header / Navigation ───────────────────────────────── */}
      <header className="sticky top-0 z-50 border-b border-border-default bg-surface-primary/85 shadow-nav backdrop-blur-md">
        <div className="mx-auto flex max-w-7xl items-center justify-between px-4 py-3.5 sm:px-6 lg:px-8">
          {/* Brand Logo */}
          <Link to="/" className="group flex items-center gap-2">
            <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-gradient-to-br from-brand-600 to-accent-600 text-base font-extrabold text-white shadow-md transition-transform duration-200 group-hover:scale-105">
              N
            </span>
            <span className="text-xl font-bold tracking-tight text-text-primary">
              News<span className="text-brand-600">Hub</span>
            </span>
          </Link>

          {/* Desktop Navigation Links */}
          <nav className="hidden items-center gap-1 md:flex">
            <NavLink
              to="/"
              end
              className={({ isActive }) =>
                `rounded-lg px-3.5 py-2 text-sm font-medium transition-colors duration-150 ${
                  isActive
                    ? "bg-brand-50 font-semibold text-brand-700"
                    : "text-text-secondary hover:bg-surface-secondary hover:text-text-primary"
                }`
              }
            >
              Trang chủ
            </NavLink>
            <span className="rounded-lg px-3.5 py-2 text-sm font-medium text-text-muted cursor-not-allowed opacity-60">
              Xu hướng (M2)
            </span>
            <span className="rounded-lg px-3.5 py-2 text-sm font-medium text-text-muted cursor-not-allowed opacity-60">
              Chuyên mục (M2)
            </span>
          </nav>

          {/* User Auth Action Buttons */}
          <div className="flex items-center gap-3">
            <NavLink
              to="/login"
              className={({ isActive }) =>
                `rounded-lg px-4 py-2 text-sm font-semibold transition-all duration-200 ${
                  isActive
                    ? "bg-surface-secondary text-brand-600 shadow-inner"
                    : "text-text-secondary hover:bg-surface-secondary hover:text-text-primary"
                }`
              }
            >
              Đăng nhập
            </NavLink>
            <NavLink
              to="/register"
              className={({ isActive }) =>
                `rounded-lg px-4 py-2 text-sm font-semibold transition-all duration-200 ${
                  isActive
                    ? "bg-brand-700 text-white shadow-md ring-2 ring-brand-400"
                    : "bg-brand-600 text-white shadow-sm hover:bg-brand-700 hover:shadow-md active:scale-95"
                }`
              }
            >
              Đăng ký
            </NavLink>
          </div>
        </div>
      </header>

      {/* ── Main Routed Page Content ─────────────────────────────────── */}
      <main className="flex-1">
        <Outlet />
      </main>

      {/* ── Global Footer ────────────────────────────────────────────── */}
      <footer className="mt-auto border-t border-border-default bg-surface-primary">
        <div className="mx-auto max-w-7xl px-4 py-12 sm:px-6 lg:px-8">
          <div className="grid grid-cols-1 gap-8 md:grid-cols-4">
            {/* Col 1: Brand & Mission */}
            <div className="md:col-span-2">
              <div className="flex items-center gap-2">
                <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-brand-600 text-xs font-bold text-white">
                  N
                </span>
                <span className="text-lg font-bold tracking-tight text-text-primary">
                  News<span className="text-brand-600">Hub</span>
                </span>
              </div>
              <p className="mt-3 max-w-md text-sm leading-relaxed text-text-secondary">
                Nền tảng báo chí điện tử thế hệ mới, tối ưu hóa trải nghiệm đọc tin với kiến trúc Clean Architecture, ASP.NET Core & phân tích thông minh bởi Google Gemini AI.
              </p>
            </div>

            {/* Col 2: Navigation */}
            <div>
              <h4 className="text-xs font-semibold uppercase tracking-wider text-text-muted">
                Điều hướng
              </h4>
              <ul className="mt-3 space-y-2 text-sm text-text-secondary">
                <li>
                  <Link to="/" className="hover:text-brand-600 transition-colors">
                    Trang chủ
                  </Link>
                </li>
                <li>
                  <Link to="/login" className="hover:text-brand-600 transition-colors">
                    Đăng nhập tài khoản
                  </Link>
                </li>
                <li>
                  <Link to="/register" className="hover:text-brand-600 transition-colors">
                    Đăng ký mới
                  </Link>
                </li>
              </ul>
            </div>

            {/* Col 3: Tech Stack */}
            <div>
              <h4 className="text-xs font-semibold uppercase tracking-wider text-text-muted">
                Hạ tầng công nghệ
              </h4>
              <ul className="mt-3 space-y-1.5 text-xs text-text-muted">
                <li>• ASP.NET Core (.NET 10 / C# 13)</li>
                <li>• Clean Architecture & DDD</li>
                <li>• React 19 + TailwindCSS v4</li>
                <li>• Supabase PostgreSQL & EF Core</li>
                <li>• In-memory JWT + HttpOnly Cookie</li>
              </ul>
            </div>
          </div>

          <div className="mt-8 border-t border-border-subtle pt-6 text-center text-xs text-text-muted">
            <p>© 2026 NewsHub Project. Được xây dựng theo tiêu chuẩn Production-Grade & Clean Architecture.</p>
          </div>
        </div>
      </footer>
    </div>
  );
}
