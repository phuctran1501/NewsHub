import { useNavigate } from 'react-router-dom'
import { EmptyState } from '@/components/common/EmptyState'
import { ROUTES } from '@/routes/paths'

export function CmsScaffoldPage() {
  const navigate = useNavigate()

  return (
    <EmptyState
      title="Không gian làm việc Tòa soạn & Quản trị"
      description="Khung CmsLayout cùng thanh bên thu gọn mượt mà đã sẵn sàng tích hợp trình soạn thảo bài viết (NH-UI-05), quy trình duyệt bài tòa soạn (NH-UI-06) và cổng Admin (NH-UI-07)."
      actionLabel="Về Cổng Độc giả"
      onAction={() => navigate(ROUTES.HOME)}
    />
  )
}
