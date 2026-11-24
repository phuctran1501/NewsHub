import { motion } from 'framer-motion'
import { Badge } from '@/components/common/Badge'
import { ArticleCardSkeleton } from '@/components/common/Skeleton'
import { Separator } from '@/components/ui/separator'

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
    title: 'Chuẩn hóa quy trình xác minh thẻ nhà báo và mã định danh tòa soạn 8 chữ số',
  },
  {
    id: 'hl-3',
    category: 'Kinh tế',
    readTime: '6 phút đọc',
    title: 'Mô hình cộng tác đa tòa soạn giúp tối ưu vận hành cho phóng viên độc lập',
  },
]

export function HomePage() {
  return (
    <div className="mx-auto max-w-7xl px-6 py-8 lg:px-12 lg:py-10 space-y-12">
      <section aria-labelledby="lead-story-heading" className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        <motion.article
          initial={{ opacity: 0, y: 8 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.24, ease: [0.22, 1, 0.36, 1] }}
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

            <h1
              id="lead-story-heading"
              className="text-2xl sm:text-3xl font-bold tracking-[-0.02em] leading-tight text-neutral-text-main dark:text-dark-text-main"
            >
              Chuyển đổi mô hình tòa soạn số đa đơn vị ứng dụng trí tuệ nhân tạo bất đồng bộ
            </h1>

            <p className="text-base leading-[1.7] text-neutral-text-muted dark:text-dark-text-muted max-w-[70ch]">
              Hệ thống nền tảng giao diện NewsHub đã sẵn sàng với kiến trúc phẳng 2D, bộ linh kiện
              shadcn/ui đồng bộ, chuyển động tinh tế và cơ chế tự động làm mới phiên xác thực qua
              HttpOnly Cookie.
            </p>
          </div>
        </motion.article>

        <motion.aside
          aria-labelledby="briefing-heading"
          initial={{ opacity: 0, y: 8 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.24, delay: 0.05, ease: [0.22, 1, 0.36, 1] }}
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
  )
}
