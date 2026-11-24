import { Link } from 'react-router-dom'
import { Logo } from '@/components/common/Logo'
import { Separator } from '@/components/ui/separator'
import { ROUTES } from '@/routes/paths'

export function Footer() {
  const currentYear = new Date().getFullYear()

  return (
    <footer className="border-t border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface pb-20 md:pb-0">
      <div className="mx-auto max-w-7xl px-6 py-12 lg:px-12">
        <div className="grid grid-cols-1 gap-8 sm:grid-cols-2 lg:grid-cols-4">
          <div className="space-y-3">
            <Logo variant="horizontal" size="sm" />
            <p className="text-xs leading-relaxed text-neutral-text-muted dark:text-dark-text-muted">
              Nền tảng báo điện tử và hệ điều hành tòa soạn số đa đơn vị tích hợp trí tuệ nhân tạo,
              đề cao chuẩn mực báo chí chính thống và trải nghiệm đọc chuyên sâu.
            </p>
          </div>

          <div className="space-y-2.5 text-xs">
            <h2 className="font-semibold text-neutral-text-main dark:text-dark-text-main">
              Chuyên mục chính
            </h2>
            <ul className="space-y-2 text-neutral-text-muted dark:text-dark-text-muted">
              <li>
                <Link to="/?category=thoi-su" className="hover:text-primary dark:hover:text-secondary">
                  Thời sự trong nước
                </Link>
              </li>
              <li>
                <Link to="/?category=kinh-te" className="hover:text-primary dark:hover:text-secondary">
                  Kinh tế & Tài chính
                </Link>
              </li>
              <li>
                <Link to="/?category=cong-nghe" className="hover:text-primary dark:hover:text-secondary">
                  Công nghệ & Dữ liệu
                </Link>
              </li>
              <li>
                <Link to="/?category=the-gioi" className="hover:text-primary dark:hover:text-secondary">
                  Toàn cảnh Thế giới
                </Link>
              </li>
            </ul>
          </div>

          <div className="space-y-2.5 text-xs">
            <h2 className="font-semibold text-neutral-text-main dark:text-dark-text-main">
              Hệ sinh thái Tòa soạn
            </h2>
            <ul className="space-y-2 text-neutral-text-muted dark:text-dark-text-muted">
              <li>
                <Link
                  to={ROUTES.AUTH.REGISTER}
                  className="hover:text-primary dark:hover:text-secondary"
                >
                  Đăng ký Thẻ Nhà báo
                </Link>
              </li>
              <li>
                <Link to={ROUTES.CMS.ROOT} className="hover:text-primary dark:hover:text-secondary">
                  Bàn làm việc Phóng viên
                </Link>
              </li>
              <li>
                <Link
                  to="/?section=companies"
                  className="hover:text-primary dark:hover:text-secondary"
                >
                  Danh bạ Tòa soạn số
                </Link>
              </li>
            </ul>
          </div>

          <div className="space-y-2.5 text-xs">
            <h2 className="font-semibold text-neutral-text-main dark:text-dark-text-main">
              Quy chuẩn & Pháp lý
            </h2>
            <ul className="space-y-2 text-neutral-text-muted dark:text-dark-text-muted">
              <li>Quy chế hoạt động tòa soạn đa đơn vị</li>
              <li>Minh bạch nội dung hỗ trợ bởi AI</li>
              <li>Chính sách bảo mật dữ liệu độc giả</li>
            </ul>
          </div>
        </div>

        <Separator className="my-8" />

        <div className="flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-neutral-text-muted dark:text-dark-text-muted">
          <p>© {currentYear} Thời Báo Số NewsHub. Bảo lưu mọi quyền.</p>
          <p>Kiến trúc Multi-Tenant Digital Newsroom</p>
        </div>
      </div>
    </footer>
  )
}
