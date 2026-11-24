import {
  CheckSquare,
  FileText,
  LayoutDashboard,
  PenLine,
  Shield,
} from 'lucide-react'
import { ROUTES } from '@/routes/paths'

export const CMS_NAV_ITEMS = [
  { label: 'Tổng quan', to: ROUTES.CMS.ROOT, end: true, icon: LayoutDashboard },
  { label: 'Bài viết của tôi', to: ROUTES.CMS.ARTICLES, end: false, icon: FileText },
  { label: 'Soạn bài mới', to: ROUTES.CMS.EDITOR, end: false, icon: PenLine },
  { label: 'Duyệt bài Tòa soạn', to: ROUTES.CMS.REVIEW, end: false, icon: CheckSquare },
  { label: 'Quản trị Hệ thống', to: ROUTES.CMS.ADMIN, end: false, icon: Shield },
]
