import { Link } from 'react-router-dom'
import { buttonVariants } from '@/components/ui/variants'
import { useAuth } from '@/hooks/useAuth'
import { ROUTES } from '@/routes/paths'
import { cn } from '@/utils/cn'

export function HomeCtaSection() {
  const { isAuthenticated } = useAuth()

  return (
    <section
      aria-labelledby="home-cta-heading"
      className="rounded-2xl border border-primary/15 dark:border-dark-border bg-primary dark:bg-dark-surface px-6 py-8 sm:px-10 sm:py-10"
    >
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-6">
        <div className="space-y-2 max-w-2xl">
          <h2
            id="home-cta-heading"
            className="text-2xl sm:text-[26px] font-extrabold tracking-[-0.02em] leading-tight text-white"
          >
            {isAuthenticated
              ? 'Sẵn sàng khám phá sâu hơn cùng NewsHub AI?'
              : 'Bắt đầu hành trình đọc & xuất bản cùng NewsHub'}
          </h2>
          <p className="text-sm leading-relaxed text-secondary-light/90 dark:text-dark-text-muted">
            {isAuthenticated
              ? 'Trải nghiệm tìm kiếm ngữ nghĩa theo câu hỏi tự nhiên, lưu bài viết yêu thích và theo dõi các tòa soạn bạn quan tâm.'
              : 'Đăng ký tài khoản miễn phí để theo dõi tòa soạn xác thực, sử dụng tìm kiếm thông minh AI hoặc gửi hồ sơ tác nghiệp nhà báo.'}
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-3 shrink-0">
          {isAuthenticated ? (
            <>
              <Link
                to={ROUTES.SEARCH}
                className={cn(
                  buttonVariants({ variant: 'primary', size: 'lg' }),
                  'bg-white text-primary hover:bg-secondary-light dark:bg-secondary dark:text-dark-bg dark:hover:bg-secondary-dark',
                )}
              >
                Tìm kiếm thông minh →
              </Link>
              <Link
                to={ROUTES.PROFILE}
                className={cn(
                  buttonVariants({ variant: 'outline', size: 'lg' }),
                  'border-white/30 text-white hover:border-white hover:bg-white/10 dark:border-dark-border dark:text-dark-text-main dark:hover:border-secondary',
                )}
              >
                Trang cá nhân
              </Link>
            </>
          ) : (
            <>
              <Link
                to={ROUTES.AUTH.REGISTER}
                className={cn(
                  buttonVariants({ variant: 'primary', size: 'lg' }),
                  'bg-white text-primary hover:bg-secondary-light dark:bg-secondary dark:text-dark-bg dark:hover:bg-secondary-dark',
                )}
              >
                Bắt đầu ngay →
              </Link>
              <Link
                to={ROUTES.AUTH.LOGIN}
                className={cn(
                  buttonVariants({ variant: 'outline', size: 'lg' }),
                  'border-white/30 text-white hover:border-white hover:bg-white/10 dark:border-dark-border dark:text-dark-text-main dark:hover:border-secondary',
                )}
              >
                Đăng nhập
              </Link>
            </>
          )}
        </div>
      </div>
    </section>
  )
}
