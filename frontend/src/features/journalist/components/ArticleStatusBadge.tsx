import { Badge } from '@/components/common/Badge'

const STATUS_MAP: Record<
  string,
  { label: string; variant: 'neutral' | 'pending' | 'published' | 'rejected' }
> = {
  '1': { label: 'Bản nháp', variant: 'neutral' },
  Draft: { label: 'Bản nháp', variant: 'neutral' },
  '2': { label: 'Chờ duyệt', variant: 'pending' },
  PendingCompanyReview: { label: 'Chờ duyệt', variant: 'pending' },
  '3': { label: 'Đã xuất bản', variant: 'published' },
  Published: { label: 'Đã xuất bản', variant: 'published' },
  '4': { label: 'Đã lưu trữ', variant: 'neutral' },
  Archived: { label: 'Đã lưu trữ', variant: 'neutral' },
  '5': { label: 'Bị từ chối', variant: 'rejected' },
  Rejected: { label: 'Bị từ chối', variant: 'rejected' },
}

interface ArticleStatusBadgeProps {
  status: string | number
}

export function ArticleStatusBadge({ status }: ArticleStatusBadgeProps) {
  const config = STATUS_MAP[String(status)] ?? {
    label: String(status),
    variant: 'neutral' as const,
  }
  return <Badge variant={config.variant}>{config.label}</Badge>
}
