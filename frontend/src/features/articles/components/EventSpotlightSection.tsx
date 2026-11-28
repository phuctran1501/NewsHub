import { motion, useReducedMotion } from 'framer-motion'
import { useMemo } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '@/hooks/useAuth'
import { buildSearchPath } from '@/routes/paths'
import type { ArticleSummaryDto } from '@/types/article'

interface EventSpotlightSectionProps {
  articles: ArticleSummaryDto[]
}

interface EventThreadDefinition {
  id: string
  title: string
  query: string
  keywords: string[]
  fallbackLatest: string
  fallbackCount: number
}

const EVENT_THREADS: EventThreadDefinition[] = [
  {
    id: 'semiconductor-ai',
    title: 'Làn sóng đầu tư bán dẫn & Trung tâm dữ liệu AI tại Đông Nam Á',
    query: 'bán dẫn',
    keywords: ['bán dẫn', 'chip', 'ai', 'pgvector', 'công nghệ'],
    fallbackLatest: 'Các tập đoàn công nghệ mở rộng năng lực kiểm thử và đóng gói vi mạch khu vực',
    fallbackCount: 18,
  },
  {
    id: 'digital-newsroom',
    title: 'Chuyển đổi số tòa soạn & Chuẩn mực báo chí minh bạch tác quyền',
    query: 'tòa soạn',
    keywords: ['tòa soạn', 'báo chí', 'kiểm duyệt', 'tóm tắt', 'eu'],
    fallbackLatest: 'Chuẩn hóa quy trình kiểm duyệt đa tầng và minh bạch nguồn dữ liệu AI',
    fallbackCount: 12,
  },
  {
    id: 'green-economy',
    title: 'Đô thị xanh & Hạ tầng giao thông công cộng thế hệ mới',
    query: 'xanh',
    keywords: ['xanh', 'đô thị', 'buýt', 'kinh tế', 'năng lượng'],
    fallbackLatest: 'Mở rộng kết nối mạng lưới vận tải công cộng thân thiện môi trường',
    fallbackCount: 15,
  },
]

export function EventSpotlightSection({ articles }: EventSpotlightSectionProps) {
  const prefersReducedMotion = useReducedMotion()
  const { isAuthenticated } = useAuth()
  const searchMode = isAuthenticated ? 'ai' : 'keyword'

  const threads = useMemo(() => {
    return EVENT_THREADS.map((thread) => {
      const matched = articles.filter((article) => {
        const haystack = `${article.title} ${article.excerpt ?? ''} ${article.categories.map((c) => c.name).join(' ')}`.toLowerCase()
        return thread.keywords.some((kw) => haystack.includes(kw))
      })

      return {
        ...thread,
        count: matched.length > 0 ? matched.length : thread.fallbackCount,
        latestHeadline: matched[0]?.title ?? thread.fallbackLatest,
      }
    })
  }, [articles])

  return (
    <section
      aria-labelledby="event-spotlight-heading"
      className="w-full border-b border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface py-7"
    >
      <div className="w-full px-4 sm:px-6 lg:px-10 xl:px-14 space-y-4">
        <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-2 border-b border-neutral-border dark:border-dark-border pb-3">
          <h2
            id="event-spotlight-heading"
            className="text-xl font-bold tracking-[-0.015em] text-neutral-text-main dark:text-dark-text-main"
          >
            Dòng sự kiện đang theo dõi
          </h2>
          <span className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
            Cập nhật liên tục từ các tòa soạn thành viên
          </span>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
          {threads.map((thread, idx) => (
            <motion.div
              key={thread.id}
              initial={prefersReducedMotion ? false : { opacity: 0, y: 10 }}
              whileInView={{ opacity: 1, y: 0 }}
              viewport={{ once: true, amount: 0.25 }}
              transition={{ duration: 0.28, delay: idx * 0.05, ease: [0.16, 1, 0.3, 1] }}
            >
              <Link
                to={buildSearchPath(thread.query, searchMode)}
                className="group flex h-full items-start justify-between gap-4 rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg p-4 hover:border-primary dark:hover:border-secondary transition-colors duration-150"
              >
                <div className="min-w-0 space-y-1.5">
                  <div className="text-[11px] font-bold uppercase tracking-wider text-primary dark:text-secondary tabular-nums">
                    Chuyên đề • {thread.count} bài viết
                  </div>
                  <h3 className="text-sm font-bold leading-snug text-neutral-text-main dark:text-dark-text-main group-hover:text-primary dark:group-hover:text-secondary transition-colors">
                    {thread.title}
                  </h3>
                  <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted line-clamp-1">
                    Mới nhất: {thread.latestHeadline}
                  </p>
                </div>

                <span
                  aria-hidden="true"
                  className="shrink-0 text-base font-bold text-primary dark:text-secondary transition-transform duration-150 group-hover:translate-x-0.5"
                >
                  →
                </span>
              </Link>
            </motion.div>
          ))}
        </div>
      </div>
    </section>
  )
}
