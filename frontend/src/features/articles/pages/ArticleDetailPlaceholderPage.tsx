import { useNavigate, useParams } from 'react-router-dom'
import { EmptyState } from '@/components/common/EmptyState'
import { ROUTES } from '@/routes/paths'

export function ArticleDetailPlaceholderPage() {
  const { slug } = useParams<{ slug: string }>()
  const navigate = useNavigate()

  return (
    <div className="mx-auto max-w-4xl px-6 py-12 lg:px-12">
      <EmptyState
        title="Trang đọc bài chi tiết đang được hoàn thiện ở NH-UI-04"
        description={`Bạn đang truy cập bài viết "${slug ?? ''}". Giao diện đọc chuyên sâu kèm Khối Tóm tắt AI và bài liên quan sẽ được kết nối ngay ở bước tiếp theo.`}
        actionLabel="Quay lại trang chủ"
        onAction={() => navigate(ROUTES.HOME)}
      />
    </div>
  )
}
