import { ArticleCardSkeleton } from '@/components/common/Skeleton'
import type { ArticleSummaryDto } from '@/types/article'
import { ArticleCard } from './ArticleCard'

interface RelatedArticlesSectionProps {
  articles: ArticleSummaryDto[]
  isLoading: boolean
}

export function RelatedArticlesSection({ articles, isLoading }: RelatedArticlesSectionProps) {
  if (!isLoading && articles.length === 0) {
    return null
  }

  return (
    <section
      aria-labelledby="related-articles-heading"
      className="w-full bg-neutral-bg dark:bg-dark-bg py-10"
    >
      <div className="w-full px-4 sm:px-6 lg:px-10 xl:px-14 space-y-5">
        <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-2 border-b border-neutral-border dark:border-dark-border pb-3">
          <h2
            id="related-articles-heading"
            className="text-xl font-bold tracking-[-0.015em] text-neutral-text-main dark:text-dark-text-main"
          >
            Bài viết liên quan
          </h2>
          <span className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
            Gợi ý bài viết cùng chủ đề & tương đồng ngữ nghĩa
          </span>
        </div>

        {isLoading ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
            {[0, 1, 2, 3].map((idx) => (
              <ArticleCardSkeleton key={idx} />
            ))}
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
            {articles.map((article) => (
              <ArticleCard key={article.id} article={article} />
            ))}
          </div>
        )}
      </div>
    </section>
  )
}
