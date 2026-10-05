import { useState } from "react";
import { Link } from "react-router-dom";

export default function RegisterPage() {
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (password !== confirmPassword) {
      alert("Mật khẩu xác nhận không khớp!");
      return;
    }
    // Placeholder - sẽ được kết nối với authStore & Axios ở Issue #11 / #14
    alert(`Đang phát triển API Auth: Đăng ký cho ${fullName} (${email})`);
  };

  return (
    <div className="flex min-h-[calc(100vh-12rem)] items-center justify-center px-4 py-12 sm:px-6 lg:px-8">
      <div className="w-full max-w-md space-y-8 rounded-2xl border border-border-default bg-surface-elevated p-8 shadow-card sm:p-10">
        {/* Header */}
        <div className="text-center">
          <Link to="/" className="inline-flex h-12 w-12 items-center justify-center rounded-2xl bg-linear-to-br from-brand-600 to-accent-600 text-xl font-bold text-white shadow-md">
            N
          </Link>
          <h2 className="mt-4 text-2xl font-bold tracking-tight text-text-primary sm:text-3xl font-sans">
            Tạo tài khoản mới
          </h2>
          <p className="mt-2 text-sm text-text-secondary">
            Gia nhập cộng đồng độc giả thông minh tại NewsHub
          </p>
        </div>

        {/* Form */}
        <form className="mt-8 space-y-5" onSubmit={handleSubmit}>
          <div>
            <label
              htmlFor="fullName"
              className="block text-xs font-semibold uppercase tracking-wider text-text-secondary"
            >
              Họ và tên
            </label>
            <div className="mt-1">
              <input
                id="fullName"
                name="fullName"
                type="text"
                autoComplete="name"
                required
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                placeholder="Nguyễn Văn A"
                className="block w-full rounded-xl border border-border-default bg-surface-primary px-4 py-2.5 text-sm text-text-primary placeholder:text-text-muted focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20 transition-colors"
              />
            </div>
          </div>

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
            <label
              htmlFor="password"
              className="block text-xs font-semibold uppercase tracking-wider text-text-secondary"
            >
              Mật khẩu
            </label>
            <div className="mt-1">
              <input
                id="password"
                name="password"
                type="password"
                autoComplete="new-password"
                required
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="Tối thiểu 8 ký tự, có số & chữ"
                className="block w-full rounded-xl border border-border-default bg-surface-primary px-4 py-2.5 text-sm text-text-primary placeholder:text-text-muted focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20 transition-colors"
              />
            </div>
          </div>

          <div>
            <label
              htmlFor="confirmPassword"
              className="block text-xs font-semibold uppercase tracking-wider text-text-secondary"
            >
              Xác nhận mật khẩu
            </label>
            <div className="mt-1">
              <input
                id="confirmPassword"
                name="confirmPassword"
                type="password"
                autoComplete="new-password"
                required
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="Nhập lại mật khẩu"
                className="block w-full rounded-xl border border-border-default bg-surface-primary px-4 py-2.5 text-sm text-text-primary placeholder:text-text-muted focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-500/20 transition-colors"
              />
            </div>
          </div>

          <div className="flex items-start">
            <input
              id="terms"
              name="terms"
              type="checkbox"
              required
              className="mt-0.5 h-4 w-4 rounded border-border-default text-brand-600 focus:ring-brand-500"
            />
            <label
              htmlFor="terms"
              className="ml-2 block text-xs leading-relaxed text-text-secondary"
            >
              Tôi đồng ý với{" "}
              <a href="#terms" className="text-brand-600 underline">
                Điều khoản dịch vụ
              </a>{" "}
              và{" "}
              <a href="#privacy" className="text-brand-600 underline">
                Chính sách bảo mật
              </a>{" "}
              của NewsHub.
            </label>
          </div>

          <div>
            <button
              type="submit"
              className="flex w-full justify-center rounded-xl bg-brand-600 px-4 py-3 text-sm font-semibold text-white shadow-sm transition-all duration-200 hover:bg-brand-700 hover:shadow-md active:scale-98"
            >
              Tạo tài khoản
            </button>
          </div>
        </form>

        {/* Footer / Switch link */}
        <div className="border-t border-border-subtle pt-6 text-center text-xs text-text-secondary">
          <span>Đã có tài khoản? </span>
          <Link
            to="/login"
            className="font-semibold text-brand-600 hover:text-brand-700 hover:underline"
          >
            Đăng nhập ngay
          </Link>
        </div>
      </div>
    </div>
  );
}
