import { Button } from '@/components/common/Button'
import { toast } from '@/components/ui/toastManager'
import type { CategorySuggestionDto } from '@/types/article'
import type { CategoryResponseDto } from '@/types/category'
import { cn } from '@/utils/cn'

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
      <div className="flex items-start justify-between gap-2">
        <div className="space-y-0.5">
          <h2 className="text-sm font-semibold text-neutral-text-main dark:text-dark-text-main">
            Chuyên mục bài viết
          </h2>
          <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
            Chọn thủ công hoặc để NewsHub AI gợi ý theo nội dung.
          </p>
        </div>

        <Button
          type="button"
          variant="secondary"
          size="sm"
          isLoading={isSuggesting}
          onClick={handleSuggestClick}
        >
          Gợi ý AI
        </Button>
      </div>

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

      <div className="space-y-2">
        <p className="text-[11px] font-semibold text-neutral-text-muted dark:text-dark-text-muted">
          Tất cả chuyên mục ({selectedIds.length} đã chọn)
        </p>
        <div className="flex flex-wrap gap-1.5">
          {allCategories.map((category) => {
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
      </div>
    </section>
  )
}
