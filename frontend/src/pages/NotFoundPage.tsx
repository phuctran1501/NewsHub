import { Link } from "react-router-dom";

export default function NotFoundPage() {
  return (
    <div className="flex min-h-[calc(100vh-14rem)] flex-col items-center justify-center px-4 py-16 text-center sm:px-6 lg:px-8">
      <div className="mx-auto max-w-md">
        <span className="inline-block rounded-2xl bg-brand-100 px-4 py-2 font-mono text-4xl font-extrabold text-brand-700 shadow-inner sm:text-5xl">
          404
        </span>
        <h1 className="mt-6 text-3xl font-extrabold tracking-tight text-text-primary sm:text-4xl font-sans">
          Không tìm thấy trang
        </h1>
        <p className="mt-3 text-sm leading-relaxed text-text-secondary">
          Bài viết hoặc đường dẫn bạn đang truy cập có thể đã được gỡ bỏ, đổi tên, hoặc tạm thời không khả dụng.
        </p>

        <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
          <Link
            to="/"
            className="inline-flex items-center justify-center rounded-xl bg-brand-600 px-6 py-2.5 text-sm font-semibold text-white shadow-sm transition-all duration-200 hover:bg-brand-700 hover:shadow-md active:scale-95"
          >
            Quay về Trang chủ
          </Link>
          <button
            type="button"
            onClick={() => window.history.back()}
            className="inline-flex items-center justify-center rounded-xl border border-border-default bg-surface-elevated px-6 py-2.5 text-sm font-semibold text-text-secondary transition-all duration-200 hover:bg-surface-secondary hover:text-text-primary"
          >
            Quay lại trang trước
          </button>
        </div>
      </div>
    </div>
  );
}
