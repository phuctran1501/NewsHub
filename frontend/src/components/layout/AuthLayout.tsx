import { AnimatePresence, motion, useReducedMotion } from 'framer-motion'
import { Link, Outlet, ScrollRestoration, useLocation, useSearchParams } from 'react-router-dom'
import { Logo } from '@/components/common/Logo'
import { PageTransition } from '@/components/common/PageTransition'
import { ThemeToggle } from '@/components/common/ThemeToggle'
import { ROUTES } from '@/routes/paths'

const VISUAL_SCENES = {
  reader: {
    key: 'reader-scene',
    imageSrc: '/images/auth-editorial-dawn.svg',
    imageAlt: 'Bình minh trên thành phố thông tin NewsHub',
    badgeText: 'Báo điện tử đa chiều & Tóm tắt AI tức thì',
    headline: 'Đón đọc dòng chảy tin tức chính xác mỗi buổi sáng',
    description:
      'Trải nghiệm đọc báo hiện đại với tóm tắt thông minh, tìm kiếm ngữ nghĩa tự nhiên và theo dõi trực tiếp các tòa soạn uy tín.',
  },
  journalist: {
    key: 'journalist-scene',
    imageSrc: '/images/auth-editorial-press.svg',
    imageAlt: 'Tháp truyền thông và không gian tòa soạn số NewsHub',
    badgeText: 'Hệ điều hành Tòa soạn số hội tụ',
    headline: 'Không gian tác nghiệp & xuất bản chuyên nghiệp cho Nhà báo',
    description:
      'Soạn thảo chuẩn báo chí, gợi ý chuyên mục bằng AI, xuất bản độc lập hoặc cộng tác biên tập cùng nhiều cơ quan báo chí.',
  },
} as const

export function AuthLayout() {
  const location = useLocation()
  const [searchParams] = useSearchParams()
  const prefersReducedMotion = useReducedMotion()

  const isJournalistMode =
    location.pathname.endsWith('/register') && searchParams.get('tab') === 'journalist'

  const activeScene = isJournalistMode ? VISUAL_SCENES.journalist : VISUAL_SCENES.reader

  return (
    <div className="mx-auto flex min-h-screen w-full max-w-[1440px] flex-col gap-5 bg-neutral-surface dark:bg-dark-bg text-neutral-text-main dark:text-dark-text-main p-4 sm:p-6 lg:flex-row">
      <ScrollRestoration />

      <div className="relative flex w-full flex-1 flex-col items-center justify-center overflow-hidden rounded-[28px] border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-surface px-6 py-10 sm:px-12 lg:w-1/2 lg:px-16">
        <div className="absolute top-5 right-5 z-10">
          <ThemeToggle />
        </div>

        <div className="mb-7 flex justify-center">
          <Link
            to={ROUTES.HOME}
            aria-label="Trang chủ NewsHub"
            className="focus-visible:outline-2 focus-visible:outline-primary rounded"
          >
            <Logo variant="horizontal" size="lg" />
          </Link>
        </div>

        <main className="w-full max-w-[420px]">
          <PageTransition>
            <Outlet />
          </PageTransition>
        </main>
      </div>

      <aside
        aria-label="Không gian thương hiệu NewsHub"
        className="relative hidden lg:flex lg:w-1/2 flex-col justify-between overflow-hidden rounded-[28px] bg-[#0b1930] p-8 xl:p-10 text-white"
      >
        <AnimatePresence mode="wait">
          <motion.img
            key={activeScene.key}
            src={activeScene.imageSrc}
            alt={activeScene.imageAlt}
            initial={
              prefersReducedMotion
                ? { opacity: 1 }
                : { opacity: 0, scale: 1.03, filter: 'blur(8px)' }
            }
            animate={{ opacity: 1, scale: 1, filter: 'blur(0px)' }}
            exit={prefersReducedMotion ? { opacity: 0 } : { opacity: 0, filter: 'blur(6px)' }}
            transition={{ duration: 0.35, ease: [0.22, 1, 0.36, 1] }}
            className="absolute inset-0 h-full w-full object-cover object-center select-none pointer-events-none"
          />
        </AnimatePresence>

        <div className="relative z-10 flex items-center">
          <div className="inline-flex items-center gap-2.5 rounded-full border border-white/20 bg-[#0b1930]/65 px-4 py-2 text-xs font-medium text-[#e0f4fe] backdrop-blur-md">
            <span aria-hidden="true" className="h-2 w-2 rounded-full bg-[#0d9488]" />
            <span>{activeScene.badgeText}</span>
          </div>
        </div>

        <div className="relative z-10 rounded-3xl border border-white/20 bg-[#0b1930]/72 p-7 backdrop-blur-md">
          <AnimatePresence mode="wait">
            <motion.div
              key={activeScene.key}
              initial={prefersReducedMotion ? { opacity: 1 } : { opacity: 0, y: 6 }}
              animate={{ opacity: 1, y: 0 }}
              exit={prefersReducedMotion ? { opacity: 0 } : { opacity: 0, y: -4 }}
              transition={{ duration: 0.22, ease: 'easeOut' }}
              className="space-y-2"
            >
              <h2 className="text-xl xl:text-2xl font-bold leading-snug tracking-tight text-white">
                {activeScene.headline}
              </h2>
              <p className="text-xs sm:text-sm leading-relaxed text-[#d6f1fc]/85">
                {activeScene.description}
              </p>
            </motion.div>
          </AnimatePresence>
        </div>
      </aside>
    </div>
  )
}
