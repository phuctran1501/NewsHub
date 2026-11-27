import { AnimatePresence, motion, useReducedMotion } from 'framer-motion'
import { Search } from 'lucide-react'
import { useEffect, useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { toast } from '@/components/ui/toastManager'
import { buildSearchPath } from '@/routes/paths'

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

export function HeroSearchSection() {
  const navigate = useNavigate()
  const prefersReducedMotion = useReducedMotion()
  const [searchQuery, setSearchQuery] = useState('')
  const [placeholderIndex, setPlaceholderIndex] = useState(0)

  useEffect(() => {
    if (prefersReducedMotion || searchQuery.length > 0) return
    const timer = window.setInterval(() => {
      setPlaceholderIndex((prev) => (prev + 1) % ROTATING_PLACEHOLDERS.length)
    }, 3400)
    return () => window.clearInterval(timer)
  }, [prefersReducedMotion, searchQuery])

  function handleHeroSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const trimmed = searchQuery.trim()
    if (!trimmed) {
      toast.info('Nhập nội dung cần tìm', 'Hãy đặt câu hỏi hoặc nhập từ khóa để tìm kiếm bài viết.')
      return
    }
    navigate(buildSearchPath(trimmed, 'ai'))
  }

  return (
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
          Hỏi trực tiếp bằng ngôn ngữ tự nhiên để tìm kiếm ngữ nghĩa trên toàn bộ kho bài viết đã
          xuất bản, hoặc duyệt luồng tin tiêu điểm được tuyển chọn từ các tòa soạn uy tín.
        </p>
      </div>

      <form onSubmit={handleHeroSearch} className="space-y-3">
        <div className="nh-search-beam rounded-xl">
          <div className="relative z-10 flex items-center gap-2.5 rounded-[10.5px] bg-neutral-bg dark:bg-dark-bg p-1.5 pl-4">
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
                maxLength={200}
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
        </div>

        <div className="flex flex-wrap items-center gap-2 text-xs text-neutral-text-muted dark:text-dark-text-muted">
          <span>Gợi ý:</span>
          {SUGGESTED_TOPICS.map((topic) => (
            <button
              key={topic.label}
              type="button"
              onClick={() => navigate(buildSearchPath(topic.query, 'ai'))}
              className="rounded-full border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-bg px-3 py-1 text-xs font-medium text-neutral-text-main dark:text-dark-text-main hover:border-primary dark:hover:border-secondary hover:text-primary dark:hover:text-secondary transition-colors cursor-pointer"
            >
              {topic.label}
            </button>
          ))}
        </div>
      </form>
    </motion.section>
  )
}
