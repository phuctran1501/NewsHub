/**
 * App.tsx — Trang demo tạm để verify TailwindCSS v4 hoạt động.
 * Sẽ được thay thế bởi RouterProvider ở Issue #06.
 */
function App() {
  return (
    <div className="min-h-screen bg-surface-primary text-text-primary">
      {/* ── Navbar Demo ──────────────────────────────────────────────── */}
      <nav className="sticky top-0 z-50 border-b border-border-default bg-surface-primary/80 shadow-nav backdrop-blur-md">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
          <h1 className="text-2xl font-bold tracking-tight text-brand-600">
            News<span className="text-accent-500">Hub</span>
          </h1>
          <div className="flex items-center gap-4">
            <span className="hidden text-sm text-text-secondary sm:inline">
              Intelligent News Aggregator
            </span>
            <button
              type="button"
              className="rounded-lg bg-brand-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition-all duration-200 hover:bg-brand-700 hover:shadow-md active:scale-95"
            >
              Đăng nhập
            </button>
          </div>
        </div>
      </nav>

      {/* ── Hero Section ─────────────────────────────────────────────── */}
      <section className="mx-auto max-w-6xl px-6 py-20 text-center animate-fade-in">
        <span className="mb-4 inline-block rounded-full bg-brand-100 px-4 py-1.5 text-xs font-semibold tracking-wide text-brand-700 uppercase">
          ✨ Powered by AI
        </span>
        <h2 className="mt-4 text-4xl font-bold tracking-tight text-text-primary sm:text-5xl lg:text-6xl">
          Tin tức thông minh,
          <br />
          <span className="bg-gradient-to-r from-brand-500 to-accent-500 bg-clip-text text-transparent">
            được cá nhân hóa cho bạn
          </span>
        </h2>
        <p className="mx-auto mt-6 max-w-2xl text-lg leading-relaxed text-text-secondary">
          NewsHub sử dụng Google Gemini AI để phân loại, tóm tắt và đề xuất tin tức phù hợp nhất.
          Đọc ít hơn, hiểu nhiều hơn.
        </p>
        <div className="mt-10 flex flex-wrap items-center justify-center gap-4">
          <button
            type="button"
            className="rounded-xl bg-brand-600 px-8 py-3 text-base font-semibold text-white shadow-lg transition-all duration-200 hover:bg-brand-700 hover:shadow-xl hover:-translate-y-0.5 active:scale-95"
          >
            Bắt đầu đọc
          </button>
          <button
            type="button"
            className="rounded-xl border border-border-default px-8 py-3 text-base font-semibold text-text-primary transition-all duration-200 hover:border-brand-300 hover:bg-brand-50 hover:-translate-y-0.5"
          >
            Tìm hiểu thêm
          </button>
        </div>
      </section>

      {/* ── Feature Cards (Grid demo) ────────────────────────────────── */}
      <section className="mx-auto max-w-6xl px-6 pb-20">
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {[
            {
              icon: "🤖",
              title: "AI-Powered Summaries",
              desc: "Google Gemini tự động tóm tắt bài viết, giúp bạn nắm ý chính trong 30 giây.",
            },
            {
              icon: "📂",
              title: "Smart Categorization",
              desc: "Bài viết được tự động phân loại vào danh mục phù hợp nhờ phân tích ngữ nghĩa.",
            },
            {
              icon: "🔒",
              title: "Secure by Design",
              desc: "Xác thực HttpOnly Cookie, refresh token rotation, Clean Architecture.",
            },
          ].map((feature) => (
            <div
              key={feature.title}
              className="group rounded-2xl border border-border-subtle bg-surface-elevated p-6 shadow-card transition-all duration-300 hover:shadow-card-hover hover:-translate-y-1"
            >
              <span className="mb-4 inline-flex h-12 w-12 items-center justify-center rounded-xl bg-brand-100 text-2xl transition-transform duration-300 group-hover:scale-110">
                {feature.icon}
              </span>
              <h3 className="mt-2 text-lg font-semibold text-text-primary">
                {feature.title}
              </h3>
              <p className="mt-2 text-sm leading-relaxed text-text-secondary">
                {feature.desc}
              </p>
            </div>
          ))}
        </div>
      </section>

      {/* ── Typography Demo (Font verification) ──────────────────────── */}
      <section className="border-t border-border-default bg-surface-secondary py-16">
        <div className="mx-auto max-w-4xl px-6">
          <h3 className="mb-8 text-center text-2xl font-bold text-text-primary">
            Typography Preview
          </h3>

          <div className="grid gap-8 sm:grid-cols-2">
            {/* Sans-serif (Inter) */}
            <div className="rounded-xl border border-border-subtle bg-surface-elevated p-6">
              <span className="text-xs font-medium tracking-widest text-text-muted uppercase">
                Font Sans — Inter
              </span>
              <p className="mt-3 font-sans text-base leading-relaxed text-text-primary">
                The quick brown fox jumps over the lazy dog. Tin tức công nghệ & kinh doanh hôm nay.
              </p>
              <p className="mt-2 font-sans text-sm font-semibold text-brand-600">
                Semibold 600 — Headings & Labels
              </p>
            </div>

            {/* Serif (Merriweather) */}
            <div className="rounded-xl border border-border-subtle bg-surface-elevated p-6">
              <span className="text-xs font-medium tracking-widest text-text-muted uppercase">
                Font Serif — Merriweather
              </span>
              <p className="prose-article mt-3">
                The quick brown fox jumps over the lazy dog. Đây là đoạn văn mẫu sử dụng font serif cho nội dung bài báo.
              </p>
            </div>
          </div>

          {/* Dark Mode indicator */}
          <div className="mt-8 rounded-xl border border-border-subtle bg-surface-elevated p-4 text-center">
            <p className="text-sm text-text-muted">
              🌓 Dark Mode: Thử chuyển hệ thống sang Dark Mode để xem giao diện tự động thích ứng
            </p>
          </div>
        </div>
      </section>

      {/* ── Footer ───────────────────────────────────────────────────── */}
      <footer className="border-t border-border-default bg-surface-primary py-8">
        <p className="text-center text-sm text-text-muted">
          © 2026 NewsHub. Built with React, TailwindCSS v4 & ASP.NET 10.
        </p>
      </footer>
    </div>
  );
}

export default App;
