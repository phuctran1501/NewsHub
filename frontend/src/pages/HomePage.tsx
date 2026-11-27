import { useMemo } from 'react'
import { Link } from 'react-router-dom'
import { CategoryStreamSection } from '@/features/articles/components/CategoryStreamSection'
import { HeroSpotlightSection } from '@/features/articles/components/HeroSpotlightSection'
import { usePublishedArticles } from '@/features/articles/hooks/usePublishedArticles'
import { useCategories } from '@/features/categories/hooks/useCategories'
import { FeaturedNewsroomsSection } from '@/features/companies/components/FeaturedNewsroomsSection'
import { HeroSearchSection } from '@/features/search/components/HeroSearchSection'
import { useAuth } from '@/hooks/useAuth'
import { buildArticleDetailPath } from '@/routes/paths'
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

export function HomePage() {
  const { isAuthenticated } = useAuth()

  const {
    data: spotlightResponse,
    isLoading: isLoadingSpotlight,
    isError: isSpotlightError,
    refetch: refetchSpotlight,
  } = usePublishedArticles({ pageNumber: 1, pageSize: 5, sortBy: 'latest' })

  const { data: categories = [] } = useCategories()

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
        {isAuthenticated && <HeroSearchSection />}

        <HeroSpotlightSection
          articles={spotlightArticles}
          isLoading={isLoadingSpotlight}
          isError={isSpotlightError}
          onRetry={() => void refetchSpotlight()}
        />

        <CategoryStreamSection categories={categories} />

        <FeaturedNewsroomsSection />
      </div>
    </div>
  )
}
