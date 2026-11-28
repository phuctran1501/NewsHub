import { motion, useReducedMotion } from 'framer-motion'
import { Play } from 'lucide-react'
import { useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { buildArticleDetailPath } from '@/routes/paths'
import type { ArticleSummaryDto } from '@/types/article'
import { cn } from '@/utils/cn'

type MediaFilterKey = 'all' | 'video' | 'photo' | 'infographic' | 'podcast'

interface MultimediaBandSectionProps {
  articles: ArticleSummaryDto[]
}

const MEDIA_TABS: { key: MediaFilterKey; label: string }[] = [
  { key: 'all', label: 'Tất cả' },
  { key: 'video', label: 'Video' },
  { key: 'photo', label: 'Ảnh phóng sự' },
  { key: 'infographic', label: 'Infographic' },
  { key: 'podcast', label: 'Podcast' },
]

const FORMAT_CYCLE: { key: Exclude<MediaFilterKey, 'all'>; badge: string }[] = [
  { key: 'video', badge: 'Video • 05:40' },
  { key: 'photo', badge: 'Ảnh phóng sự' },
  { key: 'infographic', badge: 'Infographic' },
  { key: 'podcast', badge: 'Podcast • 15 phút' },
  { key: 'video', badge: 'Video • 03:20' },
]

export function MultimediaBandSection({ articles }: MultimediaBandSectionProps) {
  const prefersReducedMotion = useReducedMotion()
  const [activeTab, setActiveTab] = useState<MediaFilterKey>('all')

  const mediaItems = useMemo(() => {
    const source = articles.length > 5 ? articles.slice(5, 13) : articles.slice(0, 8)
    return source.map((article, idx) => {
      const format = FORMAT_CYCLE[idx % FORMAT_CYCLE.length]
      return {
        article,
        formatKey: format.key,
        badge: format.badge,
      }
    })
  }, [articles])

  const filteredItems = useMemo(() => {
    if (activeTab === 'all') return mediaItems.slice(0, 5)
    const exact = mediaItems.filter((item) => item.formatKey === activeTab)
    return (exact.length > 0 ? exact : mediaItems).slice(0, 5)
  }, [mediaItems, activeTab])

  if (filteredItems.length === 0) {
    return null
  }

  const [featured, ...secondary] = filteredItems

  return (
    <section
      id="multimedia-band"
      aria-labelledby="multimedia-band-heading"
      className="w-full border-b border-white/10 bg-[#0f1f3d] py-11 text-white"
    >
      <div className="w-full px-4 sm:px-6 lg:px-10 xl:px-14 space-y-6">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-white/15 pb-3.5">
          <h2 id="multimedia-band-heading" className="text-xl font-bold tracking-[-0.015em]">
            Media & Đa phương tiện
          </h2>

          <div
            role="tablist"
            aria-label="Lọc định dạng đa phương tiện"
            className="flex flex-wrap items-center gap-2 text-xs"
          >
            {MEDIA_TABS.map((tab) => {
              const isSelected = activeTab === tab.key
              return (
                <button
                  key={tab.key}
                  type="button"
                  role="tab"
                  aria-selected={isSelected}
                  onClick={() => setActiveTab(tab.key)}
                  className={cn(
                    'relative rounded-full px-3 py-1 font-semibold transition-colors cursor-pointer',
                    isSelected
                      ? 'text-primary'
                      : 'border border-white/20 text-white/80 hover:border-secondary hover:text-white',
                  )}
                >
                  {isSelected && (
                    <motion.span
                      layoutId="multimedia-filter-pill"
                      transition={{ type: 'spring', stiffness: 420, damping: 34 }}
                      aria-hidden="true"
                      className="absolute inset-0 rounded-full bg-secondary"
                    />
                  )}
                  <span className="relative z-10">{tab.label}</span>
                </button>
              )
            })}
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-stretch">
          {/* Featured Media Left (6 cols) */}
          <motion.article
            key={`feat-${featured.article.id}`}
            initial={prefersReducedMotion ? false : { opacity: 0, y: 10 }}
            whileInView={{ opacity: 1, y: 0 }}
            viewport={{ once: true, amount: 0.2 }}
            transition={{ duration: 0.3, ease: [0.16, 1, 0.3, 1] }}
            className="lg:col-span-6 group flex flex-col overflow-hidden rounded-xl border border-white/15 bg-white/5 hover:border-secondary transition-colors duration-150"
          >
            <Link
              to={buildArticleDetailPath(featured.article.slug)}
              className="relative flex min-h-[260px] flex-1 flex-col justify-between overflow-hidden bg-gradient-to-br from-primary via-[#13294d] to-[#0a1428] p-5"
            >
              {featured.article.coverImage?.url && (
                <img
                  src={featured.article.coverImage.url}
                  alt={featured.article.title}
                  loading="lazy"
                  className="absolute inset-0 h-full w-full object-cover opacity-65 transition-transform duration-300 group-hover:scale-[1.03]"
                />
              )}
              <div
                aria-hidden="true"
                className="absolute inset-0 bg-gradient-to-t from-[#0a1428]/90 via-[#0a1428]/35 to-transparent"
              />

              <span className="relative z-10 self-start rounded-full bg-accent-crimson px-2.5 py-0.5 text-[10px] font-bold uppercase tracking-wider text-white">
                {featured.badge}
              </span>

              <div className="relative z-10 flex items-center gap-3">
                <span className="flex h-11 w-11 items-center justify-center rounded-full border border-white/60 text-white group-hover:border-secondary group-hover:bg-secondary group-hover:text-primary transition-colors">
                  <Play size={16} strokeWidth={1.75} />
                </span>
                <span className="text-xs font-medium text-white/85">
                  {featured.article.companyName ?? featured.article.author.fullName}
                </span>
              </div>
            </Link>

            <div className="p-5 space-y-2">
              <h3 className="text-lg font-bold leading-snug text-white group-hover:text-secondary transition-colors">
                <Link to={buildArticleDetailPath(featured.article.slug)}>
                  {featured.article.title}
                </Link>
              </h3>
              {featured.article.excerpt && (
                <p className="text-xs leading-relaxed text-white/75 line-clamp-2">
                  {featured.article.excerpt}
                </p>
              )}
            </div>
          </motion.article>

          {/* 4 Secondary Media Cards Right (6 cols, 2x2) */}
          <div className="lg:col-span-6 grid grid-cols-1 sm:grid-cols-2 gap-5">
            {secondary.map((item, idx) => (
              <motion.article
                key={item.article.id}
                initial={prefersReducedMotion ? false : { opacity: 0, y: 10 }}
                whileInView={{ opacity: 1, y: 0 }}
                viewport={{ once: true, amount: 0.2 }}
                transition={{ duration: 0.28, delay: idx * 0.05, ease: [0.16, 1, 0.3, 1] }}
                className="group flex flex-col overflow-hidden rounded-xl border border-white/15 bg-white/5 hover:border-secondary transition-colors duration-150"
              >
                <Link
                  to={buildArticleDetailPath(item.article.slug)}
                  className="relative flex h-36 items-end overflow-hidden bg-gradient-to-tr from-[#1e3a8a] to-[#0f766e] p-3"
                >
                  {item.article.coverImage?.url && (
                    <img
                      src={item.article.coverImage.url}
                      alt={item.article.title}
                      loading="lazy"
                      className="absolute inset-0 h-full w-full object-cover opacity-70 transition-transform duration-300 group-hover:scale-[1.03]"
                    />
                  )}
                  <div
                    aria-hidden="true"
                    className="absolute inset-0 bg-gradient-to-t from-black/75 via-transparent to-transparent"
                  />
                  <span className="relative z-10 rounded bg-black/65 px-2 py-0.5 text-[10px] font-semibold text-white">
                    {item.badge}
                  </span>
                </Link>

                <div className="flex flex-1 flex-col justify-between gap-2 p-3.5">
                  <h4 className="text-sm font-semibold leading-snug text-white line-clamp-2 group-hover:text-secondary transition-colors">
                    <Link to={buildArticleDetailPath(item.article.slug)}>{item.article.title}</Link>
                  </h4>
                  <span className="text-[11px] text-white/60">
                    {item.article.categories[0]?.name ?? 'Đa phương tiện'}
                  </span>
                </div>
              </motion.article>
            ))}
          </div>
        </div>
      </div>
    </section>
  )
}
