import { Search, X } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Button } from '@/components/common/Button'
import { toast } from '@/components/ui/toastManager'
import type { CategorySuggestionDto } from '@/types/article'
import type { CategoryResponseDto } from '@/types/category'
import { cn } from '@/utils/cn'

const INITIAL_VISIBLE_COUNT = 8

function normalizeVietnamese(text: string): string {
  return text
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/đ/g, 'd')
    .replace(/Đ/g, 'D')
    .toLowerCase()
    .trim()
}

interface AiCategorySuggesterProps {
  allCategories: CategoryResponseDto[]
  selectedIds: string[]
  onToggleCategory: (categoryId: string) => void
  suggestions: CategorySuggestionDto[]
  isSuggesting: boolean
  onRequestSuggest: () => void
  canSuggest: boolean
}

export function AiCategorySuggester({
  allCategories,
  selectedIds,
  onToggleCategory,
  suggestions,
  isSuggesting,
  onRequestSuggest,
  canSuggest,
}: AiCategorySuggesterProps) {
  const [searchQuery, setSearchQuery] = useState('')
  const [isExpanded, setIsExpanded] = useState(false)

  const selectedCategories = useMemo(
    () => allCategories.filter((cat) => selectedIds.includes(cat.id)),
    [allCategories, selectedIds],
  )

  const filteredCategories = useMemo(() => {
    const cleanQuery = normalizeVietnamese(searchQuery)
    if (!cleanQuery) return allCategories
    return allCategories.filter(
      (cat) =>
        normalizeVietnamese(cat.name).includes(cleanQuery) ||
        cat.slug.toLowerCase().includes(cleanQuery),
    )
  }, [allCategories, searchQuery])

  const isSearching = searchQuery.trim().length > 0
  const visibleCategories =
    isSearching || isExpanded
      ? filteredCategories
      : filteredCategories.slice(0, INITIAL_VISIBLE_COUNT)
  const hiddenCount = Math.max(0, filteredCategories.length - visibleCategories.length)

  const handleSuggestClick = () => {
    if (!canSuggest) {
      toast.info(
        'Cần thêm nội dung trước khi gọi AI',
        'Vui lòng nhập tiêu đề và một đoạn nội dung ngắn để AI phân tích chính xác.',
      )
      return
    }
    onRequestSuggest()
  }

  return (
    <section className="rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-4 space-y-3.5">
      <div className="flex items-start justify-between gap-2.5">
        <div className="space-y-0.5 min-w-0">
          <h2 className="text-sm font-semibold text-neutral-text-main dark:text-dark-text-main">
            Chuyên mục bài viết
          </h2>
          <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
            Tìm nhanh, chọn thủ công hoặc dùng AI gợi ý.
          </p>
        </div>

        <Button
          type="button"
          variant="secondary"
          size="sm"
          className="shrink-0 whitespace-nowrap"
          isLoading={isSuggesting}
          onClick={handleSuggestClick}
        >
          Gợi ý AI
        </Button>
      </div>

      {/* Selected categories chips */}
      {selectedCategories.length > 0 && (
        <div className="space-y-1.5">
          <p className="text-[11px] font-semibold text-neutral-text-muted dark:text-dark-text-muted">
            Đã chọn ({selectedCategories.length})
          </p>
          <div className="flex flex-wrap gap-1.5">
            {selectedCategories.map((cat) => (
              <button
                key={cat.id}
                type="button"
                onClick={() => onToggleCategory(cat.id)}
                aria-label={`Bỏ chọn chuyên mục ${cat.name}`}
                className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-xs font-semibold bg-primary text-white dark:bg-secondary dark:text-dark-bg transition-colors cursor-pointer"
              >
                <span>{cat.name}</span>
                <X size={12} strokeWidth={2} />
              </button>
            ))}
          </div>
        </div>
      )}

      {isSuggesting && (
        <div
          className="rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg p-3 space-y-2"
          aria-live="polite"
        >
          <p className="text-[11px] font-semibold text-primary dark:text-secondary">
            NewsHub AI đang phân tích tiêu đề và nội dung...
          </p>
          <div className="flex flex-wrap gap-1.5">
            <span className="h-7 w-24 rounded-full bg-neutral-border/70 dark:bg-dark-border animate-pulse" />
            <span className="h-7 w-28 rounded-full bg-neutral-border/70 dark:bg-dark-border animate-pulse" />
            <span className="h-7 w-20 rounded-full bg-neutral-border/70 dark:bg-dark-border animate-pulse" />
          </div>
        </div>
      )}

      {!isSuggesting && suggestions.length > 0 && (
        <div className="rounded-[10px] border border-primary/25 dark:border-secondary/30 bg-neutral-bg dark:bg-dark-bg p-3 space-y-2.5">
          <p className="text-[11px] font-semibold text-primary dark:text-secondary">
            Gợi ý từ NewsHub AI (Bấm để chọn hoặc bỏ chọn)
          </p>
          <div className="space-y-2">
            {suggestions.map((item) => {
              const isSelected = selectedIds.includes(item.categoryId)
              const percent = Math.round(
                item.confidence <= 1 ? item.confidence * 100 : item.confidence,
              )

              return (
                <button
                  key={item.categoryId}
                  type="button"
                  onClick={() => onToggleCategory(item.categoryId)}
                  className={cn(
                    'w-full text-left rounded-lg border px-2.5 py-2 transition-colors duration-150 cursor-pointer',
                    isSelected
                      ? 'border-primary dark:border-secondary bg-primary text-white dark:bg-secondary dark:text-dark-bg'
                      : 'border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface text-neutral-text-main dark:text-dark-text-main hover:border-primary dark:hover:border-secondary',
                  )}
                >
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-xs font-semibold">{item.categoryName}</span>
                    <span className="text-[11px] font-mono opacity-85">{percent}%</span>
                  </div>
                  {item.reason && (
                    <p className="text-[11px] opacity-80 mt-0.5 leading-snug">{item.reason}</p>
                  )}
                </button>
              )
            })}
          </div>
        </div>
      )}

      {/* Search + Compact scrollable category selector */}
      <div className="space-y-2.5">
        <div className="relative">
          <Search
            size={14}
            strokeWidth={1.75}
            className="absolute left-2.5 top-1/2 -translate-y-1/2 text-neutral-text-muted dark:text-dark-text-muted pointer-events-none"
          />
          <input
            type="text"
            value={searchQuery}
            onChange={(event) => setSearchQuery(event.target.value)}
            placeholder={`Tìm trong ${allCategories.length} chuyên mục...`}
            aria-label="Tìm chuyên mục"
            className="w-full rounded-[9px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-bg pl-8 pr-7 py-1.5 text-xs text-neutral-text-main dark:text-dark-text-main placeholder:text-neutral-text-muted/75 focus:outline-none focus:border-primary dark:focus:border-secondary"
          />
          {isSearching && (
            <button
              type="button"
              aria-label="Xóa từ khóa tìm chuyên mục"
              onClick={() => setSearchQuery('')}
              className="absolute right-2 top-1/2 -translate-y-1/2 text-neutral-text-muted hover:text-neutral-text-main dark:text-dark-text-muted dark:hover:text-dark-text-main cursor-pointer"
            >
              <X size={13} strokeWidth={1.75} />
            </button>
          )}
        </div>

        {filteredCategories.length === 0 ? (
          <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted py-2 text-center">
            Không tìm thấy chuyên mục phù hợp.
          </p>
        ) : (
          <div
            className={cn(
              'flex flex-wrap gap-1.5',
              (isExpanded || isSearching) && 'max-h-44 overflow-y-auto pr-1',
            )}
          >
            {visibleCategories.map((category) => {
              const isSelected = selectedIds.includes(category.id)
              return (
                <button
                  key={category.id}
                  type="button"
                  aria-pressed={isSelected}
                  onClick={() => onToggleCategory(category.id)}
                  className={cn(
                    'px-2.5 py-1 rounded-full text-xs font-semibold border transition-colors duration-150 cursor-pointer',
                    isSelected
                      ? 'bg-primary text-white border-primary dark:bg-secondary dark:text-dark-bg dark:border-secondary'
                      : 'bg-neutral-surface dark:bg-dark-surface text-neutral-text-main dark:text-dark-text-main border-neutral-border dark:border-dark-border hover:border-primary dark:hover:border-secondary',
                  )}
                >
                  {category.name}
                </button>
              )
            })}
          </div>
        )}

        {!isSearching && allCategories.length > INITIAL_VISIBLE_COUNT && (
          <Button
            type="button"
            variant="outline"
            size="sm"
            fullWidth
            onClick={() => setIsExpanded((prev) => !prev)}
          >
            {isExpanded ? 'Thu gọn danh sách' : `Xem thêm ${hiddenCount} chuyên mục`}
          </Button>
        )}
      </div>
    </section>
  )
}
