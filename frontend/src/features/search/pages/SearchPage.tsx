import { Search } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import { Badge } from '@/components/common/Badge'
import { Button } from '@/components/common/Button'
import { EmptyState } from '@/components/common/EmptyState'
import { Loading } from '@/components/common/Loading'
import { Pagination } from '@/components/common/Pagination'
import { ArticleCardSkeleton } from '@/components/common/Skeleton'
import { ArticleCard } from '@/features/articles/components/ArticleCard'
import { useAuth } from '@/hooks/useAuth'
import { AppClientError } from '@/types/api'
import type { SearchMode } from '@/types/search'
import { cn } from '@/utils/cn'
import { SemanticResultCard } from '../components/SemanticResultCard'
import { useAiSemanticSearch, useKeywordSearch } from '../hooks/useArticleSearch'

const SUGGESTED_QUERIES = [
  'Tác động của trí tuệ nhân tạo đến báo chí hiện đại',
  'Xu hướng kinh tế số và đầu tư bán dẫn tại Việt Nam',
  'Chuyển đổi mô hình tòa soạn số đa đơn vị',
]

const KEYWORD_PAGE_SIZE = 9

function parseMode(raw: string | null): SearchMode {
  return raw === 'keyword' ? 'keyword' : 'ai'
}

function parsePage(raw: string | null): number {
  const parsed = Number.parseInt(raw ?? '1', 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : 1
}

export function SearchPage() {
  const { isAuthenticated, isInitializing } = useAuth()
  const [searchParams, setSearchParams] = useSearchParams()

  const activeQuery = (searchParams.get('q') ?? '').trim()
  const requestedMode = parseMode(searchParams.get('mode'))
  const mode: SearchMode = isAuthenticated ? requestedMode : 'keyword'
  const pageNumber = parsePage(searchParams.get('page'))

  const [inputValue, setInputValue] = useState(activeQuery)
  const [prevActiveQuery, setPrevActiveQuery] = useState(activeQuery)

  if (activeQuery !== prevActiveQuery) {
    setPrevActiveQuery(activeQuery)
    setInputValue(activeQuery)
  }

  const aiSearch = useAiSemanticSearch(
    activeQuery,
    10,
    !isInitializing && isAuthenticated && mode === 'ai',
  )
  const keywordSearch = useKeywordSearch(
    {
      query: activeQuery,
      pageNumber,
      pageSize: KEYWORD_PAGE_SIZE,
      sortBy: 'latest',
    },
    !isInitializing && mode === 'keyword',
  )

  function navigateSearch(next: {
    q?: string
    mode?: SearchMode
    page?: number
  }) {
    const nextParams = new URLSearchParams()
    const finalQ = (next.q ?? activeQuery).trim()
    const finalMode = next.mode ?? mode
    const finalPage = next.page ?? 1

    if (finalQ) nextParams.set('q', finalQ)
    if (finalMode === 'keyword') nextParams.set('mode', 'keyword')
    if (finalMode === 'keyword' && finalPage > 1) nextParams.set('page', String(finalPage))

    setSearchParams(nextParams)
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const clean = inputValue.trim()
    if (!clean) return
    navigateSearch({ q: clean, page: 1 })
  }

  const isAiRateLimited =
    aiSearch.isError && aiSearch.error instanceof AppClientError && aiSearch.error.status === 429

  return (
    <div className="mx-auto max-w-7xl px-6 py-8 lg:px-12 lg:py-10 space-y-8">
      <section
        aria-labelledby="search-page-heading"
        className="rounded-2xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-6 sm:p-8 space-y-5"
      >
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <h1
            id="search-page-heading"
            className="text-2xl font-extrabold tracking-tight text-neutral-text-main dark:text-dark-text-main"
          >
            Tìm kiếm tin tức
          </h1>

          {isAuthenticated && (
            <div
              role="tablist"
              aria-label="Chế độ tìm kiếm"
              className="inline-flex rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg p-1 gap-1 self-start sm:self-auto"
            >
              <button
                type="button"
                role="tab"
                aria-selected={mode === 'ai'}
                onClick={() => navigateSearch({ mode: 'ai', page: 1 })}
                className={cn(
                  'rounded-[7px] px-3.5 py-1.5 text-xs font-semibold transition-colors cursor-pointer',
                  mode === 'ai'
                    ? 'bg-primary text-white dark:bg-secondary dark:text-dark-bg'
                    : 'text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main',
                )}
              >
                Tìm kiếm thông minh
              </button>
              <button
                type="button"
                role="tab"
                aria-selected={mode === 'keyword'}
                onClick={() => navigateSearch({ mode: 'keyword', page: 1 })}
                className={cn(
                  'rounded-[7px] px-3.5 py-1.5 text-xs font-semibold transition-colors cursor-pointer',
                  mode === 'keyword'
                    ? 'bg-primary text-white dark:bg-secondary dark:text-dark-bg'
                    : 'text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main',
                )}
              >
                Từ khóa tiêu đề
              </button>
            </div>
          )}
        </div>

        <form onSubmit={handleSubmit} className="space-y-3">
          <div className="flex items-center gap-2.5 rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg p-1.5 pl-4 focus-within:border-primary dark:focus-within:border-secondary transition-colors duration-150">
            <Search
              size={16}
              strokeWidth={1.75}
              className="shrink-0 text-neutral-text-muted dark:text-dark-text-muted"
              aria-hidden="true"
            />
            <input
              type="search"
              value={inputValue}
              onChange={(event) => setInputValue(event.target.value)}
              maxLength={mode === 'ai' ? 200 : 100}
              placeholder={
                mode === 'ai'
                  ? 'Đặt câu hỏi ngôn ngữ tự nhiên (VD: Ảnh hưởng của AI đến thị trường lao động)...'
                  : 'Nhập từ khóa có trong tiêu đề bài viết...'
              }
              aria-label="Nội dung tìm kiếm bài viết"
              className="w-full bg-transparent py-2 text-sm text-neutral-text-main dark:text-dark-text-main placeholder:text-neutral-text-muted dark:placeholder:text-dark-text-muted outline-none"
            />
            <Button type="submit" variant="primary" size="sm" className="h-9 px-4 shrink-0">
              Tìm kiếm
            </Button>
          </div>

          <div className="flex flex-wrap items-center justify-between gap-2 text-xs text-neutral-text-muted dark:text-dark-text-muted">
            <div className="flex flex-wrap items-center gap-2">
              {mode === 'ai' ? (
                <Badge variant="ai">Tìm kiếm thông minh</Badge>
              ) : (
                <Badge variant="category">Tìm kiếm từ khóa</Badge>
              )}
              {activeQuery && mode === 'ai' && aiSearch.data && (
                <span>
                  Tìm thấy{' '}
                  <strong className="text-neutral-text-main dark:text-dark-text-main">
                    {aiSearch.data.length}
                  </strong>{' '}
                  bài viết tương đồng cho &ldquo;{activeQuery}&rdquo;
                </span>
              )}
              {activeQuery && mode === 'keyword' && keywordSearch.data && (
                <span>
                  Tìm thấy{' '}
                  <strong className="text-neutral-text-main dark:text-dark-text-main">
                    {keywordSearch.data.meta.totalCount}
                  </strong>{' '}
                  kết quả cho &ldquo;{activeQuery}&rdquo;
                </span>
              )}
            </div>

            {mode === 'ai' && <span>Tối đa 200 ký tự</span>}
          </div>
        </form>
      </section>

      {isInitializing && activeQuery ? (
        <Loading mode="section" size="md" label="Đang tải kết quả tìm kiếm..." />
      ) : !activeQuery ? (
        isAuthenticated ? (
          <section className="rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-6 sm:p-8 space-y-4">
            <h2 className="text-base font-bold text-neutral-text-main dark:text-dark-text-main">
              Gợi ý câu hỏi tìm kiếm thông minh
            </h2>
            <p className="text-sm text-neutral-text-muted dark:text-dark-text-muted">
              Nhập một câu hỏi tự nhiên hoặc chọn nhanh một chủ đề bên dưới để khám phá kho bài viết
              đã xuất bản.
            </p>
            <div className="flex flex-wrap gap-2">
              {SUGGESTED_QUERIES.map((sample) => (
                <button
                  key={sample}
                  type="button"
                  onClick={() => {
                    setInputValue(sample)
                    navigateSearch({ q: sample, mode: 'ai', page: 1 })
                  }}
                  className="rounded-full border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg px-3.5 py-1.5 text-xs font-medium text-neutral-text-main dark:text-dark-text-main hover:border-primary dark:hover:border-secondary hover:text-primary dark:hover:text-secondary transition-colors cursor-pointer"
                >
                  {sample}
                </button>
              ))}
            </div>
          </section>
        ) : (
          <EmptyState
            title="Nhập từ khóa để tìm kiếm bài viết"
            description="Nhập từ khóa có trong tiêu đề bài viết ở ô phía trên để tra cứu nhanh toàn bộ kho tin đã xuất bản."
          />
        )
      ) : mode === 'ai' ? (
        aiSearch.isLoading || aiSearch.isFetching ? (
          <Loading
            mode="section"
            size="md"
            label="Đang phân tích ngữ nghĩa với NewsHub AI..."
          />
        ) : isAiRateLimited ? (
          <EmptyState
            title="Bạn đang thao tác tìm kiếm thông minh quá nhanh"
            description="Hệ thống giới hạn 30 lượt tìm kiếm thông minh mỗi phút để đảm bảo hiệu năng. Bạn có thể chuyển sang tìm kiếm từ khóa ngay."
            actionLabel="Chuyển sang tìm theo từ khóa"
            onAction={() => navigateSearch({ mode: 'keyword', page: 1 })}
          />
        ) : aiSearch.isError ? (
          <EmptyState
            title="Không thể thực hiện tìm kiếm thông minh"
            description="Dịch vụ tìm kiếm thông minh tạm thời gián đoạn. Bạn có thể thử lại hoặc tìm bằng từ khóa tiêu đề."
            actionLabel="Chuyển sang tìm theo từ khóa"
            onAction={() => navigateSearch({ mode: 'keyword', page: 1 })}
          />
        ) : (aiSearch.data?.length ?? 0) === 0 ? (
          <EmptyState
            title="Chưa tìm thấy bài viết khớp ngữ nghĩa"
            description={`Không có bài viết nào đạt ngưỡng tương đồng cho "${activeQuery}". Hãy thử diễn đạt ngắn gọn hơn hoặc tìm theo từ khóa tiêu đề.`}
            actionLabel="Thử tìm theo từ khóa"
            onAction={() => navigateSearch({ mode: 'keyword', page: 1 })}
          />
        ) : (
          <div className="space-y-3">
            {(aiSearch.data ?? []).map((item) => (
              <SemanticResultCard key={item.articleId} item={item} />
            ))}
          </div>
        )
      ) : keywordSearch.isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {[0, 1, 2, 3, 4, 5].map((idx) => (
            <ArticleCardSkeleton key={idx} />
          ))}
        </div>
      ) : keywordSearch.isError ? (
        <EmptyState
          title="Không thể tải kết quả tìm kiếm"
          description="Kết nối máy chủ đang gặp sự cố. Vui lòng thử lại."
          actionLabel="Thử lại"
          onAction={() => void keywordSearch.refetch()}
        />
      ) : (keywordSearch.data?.data.length ?? 0) === 0 ? (
        <EmptyState
          title="Không có bài viết nào khớp từ khóa"
          description={
            isAuthenticated
              ? `Không tìm thấy tiêu đề nào chứa "${activeQuery}". Hãy thử chuyển sang Tìm kiếm thông minh để mở rộng theo ý nghĩa câu hỏi.`
              : `Không tìm thấy tiêu đề nào chứa "${activeQuery}". Hãy thử lại với từ khóa ngắn gọn hơn.`
          }
          actionLabel={isAuthenticated ? 'Thử Tìm kiếm thông minh' : undefined}
          onAction={isAuthenticated ? () => navigateSearch({ mode: 'ai', page: 1 }) : undefined}
        />
      ) : (
        <div className="space-y-6">
          <div
            className={cn(
              'grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 transition-opacity duration-150',
              keywordSearch.isFetching && 'opacity-70',
            )}
          >
            {(keywordSearch.data?.data ?? []).map((article) => (
              <ArticleCard key={article.id} article={article} />
            ))}
          </div>

          {keywordSearch.data?.meta && (
            <Pagination
              pageNumber={keywordSearch.data.meta.pageNumber}
              pageSize={keywordSearch.data.meta.pageSize}
              totalCount={keywordSearch.data.meta.totalCount}
              totalPages={keywordSearch.data.meta.totalPages}
              hasPreviousPage={keywordSearch.data.meta.hasPreviousPage}
              hasNextPage={keywordSearch.data.meta.hasNextPage}
              isFetching={keywordSearch.isFetching}
              onPageChange={(nextPage) => {
                navigateSearch({ page: nextPage })
                window.scrollTo({ top: 0, behavior: 'smooth' })
              }}
            />
          )}
        </div>
      )}
    </div>
  )
}
