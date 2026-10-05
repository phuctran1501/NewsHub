import { Link } from "react-router-dom";

export default function HomePage() {
  return (
    <div className="flex flex-col">
      {/* ── Hero Section ─────────────────────────────────────────────── */}
      <section className="relative overflow-hidden bg-linear-to-b from-brand-50/40 via-surface-primary to-surface-primary px-6 py-20 text-center sm:py-28 animate-fade-in">
        <div className="mx-auto max-w-4xl">
          <span className="mb-4 inline-flex items-center gap-1.5 rounded-full bg-brand-100 px-4 py-1.5 text-xs font-semibold tracking-wide text-brand-700 uppercase">
            <span>✨</span> Powered by Google Gemini AI
          </span>
          <h1 className="mt-4 text-4xl font-extrabold tracking-tight text-text-primary sm:text-5xl lg:text-6xl font-sans">
            Tin tức thông minh,
            <br />
            <span className="bg-linear-to-r from-brand-600 via-brand-500 to-accent-600 bg-clip-text text-transparent">
              được cá nhân hóa cho bạn
            </span>
          </h1>
          <p className="mx-auto mt-6 max-w-2xl text-lg leading-relaxed text-text-secondary font-sans">
            NewsHub ứng dụng trí tuệ nhân tạo để phân loại chuyên sâu, tóm tắt nhanh và đề xuất tin tức phù hợp nhất.
            Đọc ít hơn, hiểu sâu hơn, bảo vệ quyền riêng tư tuyệt đối.
          </p>

          <div className="mt-10 flex flex-wrap items-center justify-center gap-4">
            <Link
              to="/register"
              className="inline-flex items-center justify-center rounded-xl bg-brand-600 px-8 py-3.5 text-base font-semibold text-white shadow-lg transition-all duration-200 hover:bg-brand-700 hover:shadow-xl hover:-translate-y-0.5 active:scale-95"
            >
              Bắt đầu trải nghiệm ngay
            </Link>
            <Link
              to="/login"
              className="inline-flex items-center justify-center rounded-xl border border-border-default bg-surface-elevated px-8 py-3.5 text-base font-semibold text-text-primary transition-all duration-200 hover:border-brand-300 hover:bg-brand-50/60 hover:-translate-y-0.5"
            >
              Đăng nhập tài khoản
            </Link>
          </div>
        </div>
      </section>

      {/* ── Feature Cards (Grid demo) ────────────────────────────────── */}
      <section className="mx-auto max-w-7xl px-4 pb-20 sm:px-6 lg:px-8">
        <div className="text-center mb-12">
          <h2 className="text-2xl font-bold tracking-tight text-text-primary sm:text-3xl">
            Điểm nổi bật của NewsHub
          </h2>
          <p className="mt-2 text-sm text-text-secondary">
            Thiết kế theo chuẩn báo chí số hiện đại kết hợp hạ tầng kỹ thuật Production-Grade
          </p>
        </div>

        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {[
            {
              icon: "🤖",
              title: "AI-Powered Summaries",
              desc: "Google Gemini tự động phân tích và tóm tắt bài viết, giúp bạn nắm trọn ý chính chỉ trong 30 giây.",
              badge: "Gemini 2.5",
            },
            {
              icon: "📂",
              title: "Smart Categorization",
              desc: "Bài viết được tự động gắn nhãn và phân loại ngữ nghĩa chính xác, hỗ trợ lọc tin nhanh chóng.",
              badge: "Semantic Tagging",
            },
            {
              icon: "🔒",
              title: "Bảo Mật Cấp Ngân Hàng",
              desc: "In-memory Access Token kết hợp HttpOnly Cookie Refresh Token, ngăn chặn triệt để lỗ hổng XSS & CSRF.",
              badge: "RFC 7519",
            },
            {
              icon: "⚡",
              title: "Tốc Độ & Hiệu Năng",
              desc: "Giao diện React 19 siêu nhẹ, TailwindCSS v4 zero-runtime CSS, tối ưu hóa First Contentful Paint.",
              badge: "React 19 + Vite",
            },
            {
              icon: "🏛️",
              title: "Clean Architecture",
              desc: "Backend ASP.NET Core phân tách 4 tầng rõ ràng (Domain, Application, Infrastructure, API).",
              badge: "DDD Ready",
            },
            {
              icon: "📱",
              title: "Typography Báo Chí",
              desc: "Phối hợp hài hòa giữa Inter (Sans) cho tiêu đề hiện đại và Merriweather (Serif) cho trải nghiệm đọc báo sâu lắng.",
              badge: "News Typography",
            },
          ].map((feature) => (
            <div
              key={feature.title}
              className="group relative rounded-2xl border border-border-subtle bg-surface-elevated p-6 shadow-card transition-all duration-300 hover:shadow-card-hover hover:-translate-y-1"
            >
              <div className="flex items-center justify-between">
                <span className="inline-flex h-12 w-12 items-center justify-center rounded-xl bg-brand-50 text-2xl transition-transform duration-300 group-hover:scale-110">
                  {feature.icon}
                </span>
                <span className="rounded-full bg-surface-secondary px-2.5 py-0.5 text-[11px] font-medium text-text-muted">
                  {feature.badge}
                </span>
              </div>
              <h3 className="mt-4 text-lg font-semibold text-text-primary">
                {feature.title}
              </h3>
              <p className="mt-2 text-sm leading-relaxed text-text-secondary">
                {feature.desc}
              </p>
            </div>
          ))}
        </div>
      </section>

      {/* ── Sample Article Preview (Typography Verification) ─────────── */}
      <section className="border-t border-border-default bg-surface-secondary/40 py-16">
        <div className="mx-auto max-w-4xl px-4 sm:px-6">
          <div className="rounded-2xl border border-border-default bg-surface-elevated p-8 shadow-sm">
            <span className="inline-block rounded-md bg-accent-100 px-2.5 py-1 text-xs font-bold text-accent-700 uppercase tracking-wide">
              Công nghệ & Trí tuệ nhân tạo
            </span>
            <h2 className="mt-3 font-sans text-2xl font-bold tracking-tight text-text-primary sm:text-3xl">
              Tương lai của báo chí điện tử trong kỷ nguyên Trí tuệ nhân tạo
            </h2>
            <div className="mt-3 flex items-center gap-3 text-xs text-text-muted">
              <span>Tác giả: Ban Biên Tập NewsHub</span>
              <span>•</span>
              <span>5 phút đọc</span>
              <span>•</span>
              <span>Hôm nay</span>
            </div>

            <div className="prose-article mt-6">
              <p>
                Kỷ nguyên số hóa đang chứng kiến bước chuyển mình mạnh mẽ của các tòa soạn điện tử. 
                Không chỉ đơn thuần là việc đăng tải thông tin nhanh nhất, mà việc phân loại ngữ nghĩa 
                và trích xuất dữ liệu giá trị đóng vai trò sống còn trong việc giữ chân độc giả.
              </p>
              <p className="mt-4">
                Với việc ứng dụng các mô hình ngôn ngữ lớn (LLMs), độc giả NewsHub có thể ngay lập tức nắm bắt 
                bức tranh toàn cảnh của bất kỳ chủ đề nóng nào mà không bị ngập chìm trong biển thông tin hỗn tạp.
              </p>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
