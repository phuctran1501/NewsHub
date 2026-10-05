import { useState } from "react";
import { Link } from "react-router-dom";

export default function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    // Placeholder - sẽ được kết nối với authStore & Axios ở Issue #12 / #15
    alert(`Đang phát triển API Auth: Đăng nhập cho ${email}`);
  };

  return (
    <div className="flex min-h-[calc(100vh-12rem)] items-center justify-center px-4 py-12 sm:px-6 lg:px-8">
      <div className="w-full max-w-md space-y-8 rounded-2xl border border-border-default bg-surface-elevated p-8 shadow-card sm:p-10">
        {/* Header */}
        <div className="text-center">
          <Link to="/" className="inline-flex h-12 w-12 items-center justify-center rounded-2xl bg-gradient-to-br from-brand-600 to-accent-600 text-xl font-bold text-white shadow-md">
            N
          </Link>
          <h2 className="mt-4 text-2xl font-bold tracking-tight text-text-primary sm:text-3xl font-sans">
            Chào mừng trở lại
          </h2>
          <p className="mt-2 text-sm text-text-secondary">
            Đăng nhập để truy cập tin tức cá nhân hóa và quản lý nội dung
          </p>
        </div>

        {/* Form */}
        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div className="space-y-4">
            <div>
              <label
                htmlFor="email"
                className="block text-xs font-semibold uppercase tracking-wider text-text-secondary"
              >
                Địa chỉ Email
              </label>
              <div className="mt-1">
                <input
                  id="email"
                  name="email"
                  type="email"
                  autoComplete="email"
                  required
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  placeholder="name@example.com"
                  className="block w-full rounded-xl border border-border-default bg-surface-primary px-4 py-2.5 text-sm text-text-primary placeholder:text-text-muted focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20 transition-colors"
                />
              </div>
            </div>

            <div>
              <div className="flex items-center justify-between">
                <label
                  htmlFor="password"
                  className="block text-xs font-semibold uppercase tracking-wider text-text-secondary"
                >
                  Mật khẩu
                </label>
                <a
                  href="#forgot-password"
                  className="text-xs font-medium text-brand-600 hover:text-brand-700"
                >
                  Quên mật khẩu?
                </a>
              </div>
              <div className="mt-1">
                <input
                  id="password"
                  name="password"
                  type="password"
                  autoComplete="current-password"
                  required
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="••••••••"
                  className="block w-full rounded-xl border border-border-default bg-surface-primary px-4 py-2.5 text-sm text-text-primary placeholder:text-text-muted focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20 transition-colors"
                />
              </div>
            </div>
          </div>

          <div className="flex items-center">
            <input
              id="remember-me"
              name="remember-me"
              type="checkbox"
              className="h-4 w-4 rounded border-border-default text-brand-600 focus:ring-brand-500"
            />
            <label
              htmlFor="remember-me"
              className="ml-2 block text-xs text-text-secondary"
            >
              Ghi nhớ đăng nhập trên thiết bị này
            </label>
          </div>

          <div>
            <button
              type="submit"
              className="flex w-full justify-center rounded-xl bg-brand-600 px-4 py-3 text-sm font-semibold text-white shadow-sm transition-all duration-200 hover:bg-brand-700 hover:shadow-md active:scale-98"
            >
              Đăng nhập
            </button>
          </div>
        </form>

        {/* Footer / Switch link */}
        <div className="border-t border-border-subtle pt-6 text-center text-xs text-text-secondary">
          <span>Chưa có tài khoản? </span>
          <Link
            to="/register"
            className="font-semibold text-brand-600 hover:text-brand-700 hover:underline"
          >
            Đăng ký tài khoản mới
          </Link>
        </div>
      </div>
    </div>
  );
}
