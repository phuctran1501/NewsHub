import { AnimatePresence, motion, useReducedMotion } from 'framer-motion'
import { Search } from 'lucide-react'
import { useEffect, useState, type FormEvent } from 'react'
import { Badge } from '@/components/common/Badge'
import { Button } from '@/components/common/Button'
import { ArticleCardSkeleton } from '@/components/common/Skeleton'
import { Separator } from '@/components/ui/separator'
import { toast } from '@/components/ui/toastManager'

const BREAKING_TICKER_ITEMS = [
  {
    id: 'tk-1',
    time: '14:32',
    text: 'Chuyển đổi tòa soạn số đa đơn vị tăng tốc với hệ thống tóm tắt AI thời gian thực',
  },
  {
    id: 'tk-2',
    time: '14:15',
    text: 'Thị trường bán dẫn Đông Nam Á ghi nhận mức đầu tư kỷ lục trong quý IV',
  },
  {
    id: 'tk-3',
    time: '13:50',
    text: 'Chuẩn hóa quy trình xuất bản minh bạch cho phóng viên độc lập và tòa soạn',
  },
]

const ROTATING_PLACEHOLDERS = [
  'Diễn biến kinh tế số Việt Nam trong quý này?',
  'Ảnh hưởng của trí tuệ nhân tạo đến thị trường lao động?',
  'Xu hướng chuyển đổi tòa soạn hội tụ năm 2026?',
]

const SUGGESTED_TOPICS = [
  { label: 'AI & Báo chí', query: 'Tác động của AI đến báo chí hiện đại' },
  { label: 'Kinh tế số', query: 'Xu hướng kinh tế số Việt Nam 2026' },
  { label: 'Năng lượng xanh', query: 'Chính sách chuyển đổi năng lượng xanh' },
]

const EDITORIAL_HIGHLIGHTS = [
  {
    id: 'hl-1',
    category: 'Công nghệ',
    readTime: '4 phút đọc',
    title: 'Kiến trúc tìm kiếm ngữ nghĩa pgvector cho kho lưu trữ tin tức đa chiều',
  },
  {
    id: 'hl-2',
    category: 'Thời sự',
    readTime: '3 phút đọc',
    title: 'Chuẩn hóa quy trình xuất bản và mã định danh tòa soạn 8 chữ số',
  },
  {
    id: 'hl-3',
    category: 'Kinh tế',
    readTime: '6 phút đọc',
    title: 'Mô hình cộng tác đa tòa soạn giúp tối ưu vận hành cho phóng viên độc lập',
  },
]

export function HomePage() {
  const prefersReducedMotion = useReducedMotion()
  const [searchQuery, setSearchQuery] = useState('')
  const [placeholderIndex, setPlaceholderIndex] = useState(0)

  useEffect(() => {
    if (prefersReducedMotion) return
    const timer = window.setInterval(() => {
      setPlaceholderIndex((prev) => (prev + 1) % ROTATING_PLACEHOLDERS.length)
    }, 3400)
    return () => window.clearInterval(timer)
  }, [prefersReducedMotion])

  function handleHeroSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const trimmed = searchQuery.trim()
    if (!trimmed) {
      toast.info('Nhập nội dung cần tìm', 'Hãy đặt câu hỏi hoặc nhập từ khóa để tìm kiếm bài viết.')
      return
    }
    toast.info(
      'Tìm kiếm ngữ nghĩa AI',
      `Đã ghi nhận truy vấn "${trimmed}". Luồng kết quả bài viết sẽ kết nối ở NH-UI-03.`,
    )
  }

  return (
    <div>
      <div
        aria-label="Tin mới cập nhật"
        title="Rê chuột để tạm dừng dòng tin"
        className="nh-ticker-bar h-10 w-full overflow-hidden border-b border-[#275296] bg-primary text-neutral-bg flex items-center"
      >
        <div className="mx-auto flex w-full max-w-7xl items-center gap-3.5 px-6 lg:px-12 overflow-hidden">
          <span className="inline-flex shrink-0 items-center gap-1.5 rounded-full bg-accent-crimson px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-wider text-white">
            <span
              aria-hidden="true"
              className="h-1.5 w-1.5 rounded-full bg-white animate-pulse"
            />
            Tin mới
          </span>

          <div className="flex-1 overflow-hidden mask-[linear-gradient(to_right,transparent,#000_3%,#000_97%,transparent)]">
            <div className="nh-ticker-track text-xs text-secondary-light">
              {[...BREAKING_TICKER_ITEMS, ...BREAKING_TICKER_ITEMS].map((item, idx) => (
                <span
                  key={`${item.id}-${idx}`}
                  className="inline-flex items-center gap-2.5 hover:text-white transition-colors"
                >
                  <span className="font-mono text-[11px] text-secondary/90">{item.time}</span>
                  <span>{item.text}</span>
                </span>
              ))}
            </div>
          </div>
        </div>
      </div>

      <div className="mx-auto max-w-7xl px-6 py-8 lg:px-12 lg:py-10 space-y-10">
        <motion.section
          aria-labelledby="hero-search-heading"
          initial={prefersReducedMotion ? false : { opacity: 0, y: 8 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.24, ease: [0.22, 1, 0.36, 1] }}
          className="rounded-2xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-6 sm:p-8 space-y-5"
        >
          <div className="space-y-2">
            <h1
              id="hero-search-heading"
              className="text-2xl sm:text-3xl lg:text-[32px] font-extrabold tracking-tight leading-tight text-neutral-text-main dark:text-dark-text-main"
            >
              Đọc sâu hơn, nắm bắt nhanh hơn cùng{' '}
              <span className="text-primary dark:text-secondary">NewsHub AI</span>
            </h1>
            <p className="text-sm leading-relaxed text-neutral-text-muted dark:text-dark-text-muted max-w-2xl">
              Hỏi trực tiếp bằng ngôn ngữ tự nhiên để tìm kiếm ngữ nghĩa trên toàn bộ kho bài viết
              đã xuất bản, hoặc duyệt luồng tin tiêu điểm được tuyển chọn từ các tòa soạn uy tín.
            </p>
          </div>

          <form onSubmit={handleHeroSearch} className="space-y-3">
            <div className="flex items-center gap-2.5 rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg p-1.5 pl-4 focus-within:border-primary dark:focus-within:border-secondary transition-colors duration-150">
              <Search
                size={16}
                strokeWidth={1.75}
                className="shrink-0 text-neutral-text-muted dark:text-dark-text-muted"
                aria-hidden="true"
              />

              <div className="relative flex-1 min-w-0">
                <input
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  aria-label="Tìm kiếm bài viết với NewsHub AI"
                  className="w-full bg-transparent py-2 text-sm text-neutral-text-main dark:text-dark-text-main outline-none"
                />
                {!searchQuery && (
                  <div className="pointer-events-none absolute inset-0 flex items-center overflow-hidden">
                    <AnimatePresence mode="wait">
                      <motion.span
                        key={placeholderIndex}
                        initial={prefersReducedMotion ? { opacity: 1 } : { opacity: 0, y: 6 }}
                        animate={{ opacity: 1, y: 0 }}
                        exit={prefersReducedMotion ? { opacity: 0 } : { opacity: 0, y: -6 }}
                        transition={{ duration: 0.22, ease: 'easeOut' }}
                        className="truncate text-sm text-neutral-text-muted dark:text-dark-text-muted"
                      >
                        {ROTATING_PLACEHOLDERS[placeholderIndex]}
                      </motion.span>
                    </AnimatePresence>
                  </div>
                )}
              </div>

              <Button type="submit" variant="primary" size="sm" className="h-9 px-4 shrink-0">
                Tìm kiếm
              </Button>
            </div>

            <div className="flex flex-wrap items-center gap-2 text-xs text-neutral-text-muted dark:text-dark-text-muted">
              <span>Gợi ý:</span>
              {SUGGESTED_TOPICS.map((topic) => (
                <button
                  key={topic.label}
                  type="button"
                  onClick={() => setSearchQuery(topic.query)}
                  className="rounded-full border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-bg px-3 py-1 text-xs font-medium text-neutral-text-main dark:text-dark-text-main hover:border-primary dark:hover:border-secondary hover:text-primary dark:hover:text-secondary transition-colors cursor-pointer"
                >
                  {topic.label}
                </button>
              ))}
            </div>
          </form>
        </motion.section>

        <section
          aria-labelledby="lead-story-heading"
          className="grid grid-cols-1 lg:grid-cols-12 gap-6"
        >
          <motion.article
            initial={prefersReducedMotion ? false : { opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.24, delay: 0.04, ease: [0.22, 1, 0.36, 1] }}
            className="lg:col-span-8 rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface overflow-hidden hover:border-primary dark:hover:border-secondary transition-colors duration-150"
          >
            <div className="aspect-video w-full border-b border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg flex items-center justify-center px-6 text-center">
              <span className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
                Khung ảnh tiêu điểm tỷ lệ 16:9 — dữ liệu bài xuất bản kết nối tại NH-UI-03
              </span>
            </div>

            <div className="p-6 lg:p-8 space-y-3.5">
              <div className="flex flex-wrap items-center gap-2">
                <Badge variant="category">Kinh tế số</Badge>
                <Badge variant="ai">Tóm tắt AI</Badge>
                <span className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
                  5 phút đọc
                </span>
              </div>

              <h2
                id="lead-story-heading"
                className="text-2xl sm:text-3xl font-bold tracking-[-0.02em] leading-tight text-neutral-text-main dark:text-dark-text-main"
              >
                Chuyển đổi mô hình tòa soạn số đa đơn vị ứng dụng trí tuệ nhân tạo bất đồng bộ
              </h2>

              <p className="text-base leading-[1.7] text-neutral-text-muted dark:text-dark-text-muted max-w-[70ch]">
                Hệ thống nền tảng giao diện NewsHub đã sẵn sàng với kiến trúc phẳng 2D, bộ linh kiện
                shadcn/ui đồng bộ, chuyển động tinh tế và cơ chế tự động làm mới phiên xác thực qua
                HttpOnly Cookie.
              </p>
            </div>
          </motion.article>

          <motion.aside
            aria-labelledby="briefing-heading"
            initial={prefersReducedMotion ? false : { opacity: 0, y: 8 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.24, delay: 0.08, ease: [0.22, 1, 0.36, 1] }}
            className="lg:col-span-4 rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-6 flex flex-col justify-between"
          >
            <div className="space-y-5">
              <h2
                id="briefing-heading"
                className="text-lg font-bold tracking-[-0.015em] text-neutral-text-main dark:text-dark-text-main"
              >
                Tiêu điểm biên tập
              </h2>

              <div className="divide-y divide-neutral-border dark:divide-dark-border">
                {EDITORIAL_HIGHLIGHTS.map((item) => (
                  <article key={item.id} className="py-4 first:pt-0 last:pb-0 space-y-1.5">
                    <div className="flex items-center gap-2 text-xs text-neutral-text-muted dark:text-dark-text-muted">
                      <span className="font-semibold text-primary dark:text-secondary">
                        {item.category}
                      </span>
                      <span>•</span>
                      <span>{item.readTime}</span>
                    </div>
                    <h3 className="text-sm font-semibold leading-snug text-neutral-text-main dark:text-dark-text-main">
                      {item.title}
                    </h3>
                  </article>
                ))}
              </div>
            </div>

            <div className="pt-5">
              <Separator className="mb-4" />
              <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
                Luồng tin trực tiếp từ API sẽ được tích hợp trong bước tiếp theo.
              </p>
            </div>
          </motion.aside>
        </section>

        <section aria-labelledby="latest-feed-heading" className="space-y-5">
          <div className="flex items-baseline justify-between border-b border-neutral-border dark:border-dark-border pb-3">
            <h2
              id="latest-feed-heading"
              className="text-xl font-bold tracking-[-0.015em] text-neutral-text-main dark:text-dark-text-main"
            >
              Mới cập nhật theo chuyên mục
            </h2>
            <span className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
              Khung tải Skeleton chuẩn
            </span>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <ArticleCardSkeleton />
            <ArticleCardSkeleton />
            <ArticleCardSkeleton />
          </div>
        </section>
      </div>
    </div>
  )
}
