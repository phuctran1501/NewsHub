import { BrowserRouter, Navigate, Route, Routes, useNavigate } from 'react-router-dom'
import { EmptyState } from './components/common/EmptyState'
import { AuthLayout } from './components/layout/AuthLayout'
import { CmsLayout } from './components/layout/CmsLayout'
import { RootLayout } from './components/layout/RootLayout'
import { HomePage } from './pages/HomePage'
import { AuthProvider } from './store/AuthContext'
import { ThemeProvider } from './store/ThemeContext'

function AuthRoutePlaceholder() {
  const navigate = useNavigate()
  return (
    <div className="space-y-3 text-center">
      <h1 className="text-xl font-bold text-neutral-text-main dark:text-dark-text-main">
        Xác thực tài khoản
      </h1>
      <p className="text-sm text-neutral-text-muted dark:text-dark-text-muted leading-relaxed">
        Khung AuthLayout đã sẵn sàng. Biểu mẫu Đăng nhập và Đăng ký phân luồng Độc giả / Nhà báo sẽ
        được triển khai tại NH-UI-02.
      </p>
      <button
        type="button"
        onClick={() => navigate('/')}
        className="mt-2 inline-flex text-xs font-semibold text-primary dark:text-secondary hover:underline cursor-pointer"
      >
        Quay về trang chủ
      </button>
    </div>
  )
}

function CmsRoutePlaceholder() {
  const navigate = useNavigate()
  return (
    <EmptyState
      title="Không gian làm việc Tòa soạn & Quản trị"
      description="Khung CmsLayout cùng thanh bên thu gọn đã hoàn tất và sẵn sàng tích hợp trình soạn thảo bài viết, quy trình duyệt bài và cổng Admin trong các task tiếp theo."
      actionLabel="Về Cổng Độc giả"
      onAction={() => navigate('/')}
    />
  )
}

export default function App() {
  return (
    <ThemeProvider>
      <AuthProvider>
        <BrowserRouter>
          <Routes>
            <Route element={<RootLayout />}>
              <Route path="/" element={<HomePage />} />
              <Route path="/me/notifications" element={<HomePage />} />
            </Route>

            <Route path="/auth" element={<AuthLayout />}>
              <Route index element={<Navigate to="/auth/login" replace />} />
              <Route path="*" element={<AuthRoutePlaceholder />} />
            </Route>

            <Route path="/cms" element={<CmsLayout />}>
              <Route index element={<CmsRoutePlaceholder />} />
              <Route path="*" element={<CmsRoutePlaceholder />} />
            </Route>

            <Route path="*" element={<Navigate to="/" replace />} />
          </Routes>
        </BrowserRouter>
      </AuthProvider>
    </ThemeProvider>
  )
}
