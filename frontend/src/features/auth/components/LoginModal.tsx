import { Link } from 'react-router-dom'
import { Modal } from '@/components/common/Modal'
import { ROUTES } from '@/routes/paths'
import type { UserProfile } from '@/types/auth'
import { LoginForm } from './LoginForm'

interface LoginModalProps {
  isOpen: boolean
  onClose: () => void
  onLoginSuccess?: (user: UserProfile) => void
  description?: string
}

export function LoginModal({
  isOpen,
  onClose,
  onLoginSuccess,
  description = 'Đăng nhập tài khoản NewsHub để lưu bài viết, theo dõi tòa soạn và đồng bộ trải nghiệm đọc.',
}: LoginModalProps) {
  return (
    <Modal isOpen={isOpen} onClose={onClose} title="Đăng nhập NewsHub" description={description}>
      <div className="space-y-4 text-neutral-text-main dark:text-dark-text-main">
        <LoginForm
          onSuccess={(user) => {
            onLoginSuccess?.(user)
            onClose()
          }}
        />
        <p className="text-center text-xs text-neutral-text-muted dark:text-dark-text-muted">
          Chưa có tài khoản?{' '}
          <Link
            to={ROUTES.AUTH.REGISTER}
            onClick={onClose}
            className="font-semibold text-primary dark:text-secondary hover:underline"
          >
            Tạo tài khoản Độc giả hoặc Nhà báo
          </Link>
        </p>
      </div>
    </Modal>
  )
}
