import { AnimatePresence, motion, useReducedMotion } from 'framer-motion'
import { Search } from 'lucide-react'
import { useEffect, useMemo, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { toast } from '@/components/ui/toastManager'
import { CategoryStreamSection } from '@/features/articles/components/CategoryStreamSection'
import { HeroSpotlightSection } from '@/features/articles/components/HeroSpotlightSection'
import { usePublishedArticles } from '@/features/articles/hooks/usePublishedArticles'
import { useCategories } from '@/features/categories/hooks/useCategories'
import { FeaturedNewsroomsSection } from '@/features/companies/components/FeaturedNewsroomsSection'
import { usePublicCompanies } from '@/features/companies/hooks/usePublicCompanies'
import { useAuth } from '@/hooks/useAuth'
import { buildArticleDetailPath, buildSearchPath } from '@/routes/paths'
import { formatTickerTime } from '@/utils/date'

const FALLBACK_TICKER_ITEMS = [
  {
    id: 'tk-1',
    slug: null as string | null,
    time: '14:32',
    text: 'Chuyển đổi tòa soạn số đa đơn vị tăng tốc với hệ thống tóm tắt AI thời gian thực',
  },
  {
    id: 'tk-2',
    slug: null as string | null,
    time: '14:15',
    text: 'Thị trường bán dẫn Đông Nam Á ghi nhận mức đầu tư kỷ lục trong quý IV',
  },
  {
    id: 'tk-3',
    slug: null as string | null,
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

export function HomePage() {
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const prefersReducedMotion = useReducedMotion()
  const [searchQuery, setSearchQuery] = useState('')
  const [placeholderIndex, setPlaceholderIndex] = useState(0)

  const {
    data: spotlightResponse,
    isLoading: isLoadingSpotlight,
    isError: isSpotlightError,
    refetch: refetchSpotlight,
  } = usePublishedArticles({ pageNumber: 1, pageSize: 5, sortBy: 'latest' })

  const { data: categories = [] } = useCategories()
  const { data: companiesResponse } = usePublicCompanies({ pageNumber: 1, pageSize: 50 })

  const companyNameById = useMemo(
    () => new Map((companiesResponse?.data ?? []).map((company) => [company.id, company.name])),
    [companiesResponse?.data],
  )

  const spotlightArticles = useMemo(
    () => spotlightResponse?.data ?? [],
    [spotlightResponse?.data],
  )

  const tickerItems = useMemo(() => {
    if (spotlightArticles.length === 0) {
      return FALLBACK_TICKER_ITEMS
    }
    return spotlightArticles.slice(0, 5).map((article) => ({
      id: article.id,
      slug: article.slug,
      time: formatTickerTime(article.publishedAt ?? article.createdAt),
      text: article.title,
    }))
  }, [spotlightArticles])

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
    navigate(buildSearchPath(trimmed, 'ai'))
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
              {[...tickerItems, ...tickerItems].map((item, idx) =>
                item.slug ? (
                  <Link
                    key={`${item.id}-${idx}`}
                    to={buildArticleDetailPath(item.slug)}
                    className="inline-flex items-center gap-2.5 hover:text-white transition-colors"
                  >
                    <span className="font-mono text-[11px] text-secondary/90">{item.time}</span>
                    <span>{item.text}</span>
                  </Link>
                ) : (
                  <span
                    key={`${item.id}-${idx}`}
                    className="inline-flex items-center gap-2.5 hover:text-white transition-colors"
                  >
                    <span className="font-mono text-[11px] text-secondary/90">{item.time}</span>
                    <span>{item.text}</span>
                  </span>
                ),
              )}
            </div>
          </div>
        </div>
      </div>

      <div className="mx-auto max-w-7xl px-6 py-8 lg:px-12 lg:py-10 space-y-12">
        {isAuthenticated && (
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
                    onClick={() => navigate(buildSearchPath(topic.query, 'ai'))}
                    className="rounded-full border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-bg px-3 py-1 text-xs font-medium text-neutral-text-main dark:text-dark-text-main hover:border-primary dark:hover:border-secondary hover:text-primary dark:hover:text-secondary transition-colors cursor-pointer"
                  >
                    {topic.label}
                  </button>
                ))}
              </div>
            </form>
          </motion.section>
        )}

        <HeroSpotlightSection
          articles={spotlightArticles}
          companyNameById={companyNameById}
          isLoading={isLoadingSpotlight}
          isError={isSpotlightError}
          onRetry={() => void refetchSpotlight()}
        />

        <CategoryStreamSection
          categories={categories}
          companyNameById={companyNameById}
        />

        <FeaturedNewsroomsSection />
      </div>
    </div>
  )
}
