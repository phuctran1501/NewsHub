import { Link } from 'react-router-dom'

export function NotFoundPage() {
  return (
    <div className="min-h-screen bg-neutral-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100 flex items-center justify-center p-4">
      <div className="max-w-md w-full border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-8 text-center">
        <span className="text-xs uppercase tracking-widest font-semibold text-rose-600 dark:text-rose-400">
          Mã Lỗi 404
        </span>
        <h1 className="text-3xl font-serif font-bold text-slate-900 dark:text-white mt-2 mb-3">
          Không Tìm Thấy Trang
        </h1>
        <p className="text-sm text-slate-600 dark:text-slate-400 mb-6 leading-relaxed">
          Đường dẫn bạn yêu cầu không tồn tại hoặc đã được chuyển dời. Cơ chế Single Page Application (SPA) Routing của NewsHub đang hoạt động chính xác.
        </p>
        <Link
          to="/"
          className="inline-flex items-center justify-center px-4 py-2 bg-slate-900 hover:bg-slate-800 text-white dark:bg-white dark:hover:bg-slate-100 dark:text-slate-900 text-xs font-semibold rounded-[4px] transition-colors"
        >
          ← Quay Lại Bảng Trạng Thái
        </Link>
      </div>
    </div>
  )
}
