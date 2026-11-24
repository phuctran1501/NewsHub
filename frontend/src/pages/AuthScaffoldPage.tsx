import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { ROUTES } from '@/routes/paths'

export function AuthScaffoldPage() {
  const navigate = useNavigate()

  return (
    <div className="space-y-4 text-center">
      <h1 className="text-xl font-bold text-neutral-text-main dark:text-dark-text-main">
        Xác thực tài khoản
      </h1>
      <p className="text-sm text-neutral-text-muted dark:text-dark-text-muted leading-relaxed">
        Khung AuthLayout đã hoàn tất. Biểu mẫu Đăng nhập và Đăng ký phân luồng Độc giả / Nhà báo sẽ
        được triển khai tại task NH-UI-02.
      </p>
      <div className="pt-1">
        <Button variant="outline" size="sm" onClick={() => navigate(ROUTES.HOME)}>
          Về trang chủ
        </Button>
      </div>
    </div>
  )
}
