import { Search } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from '@/components/ui/dialog'
import { Separator } from '@/components/ui/separator'
import { useCategories } from '@/features/categories/hooks/useCategories'
import { useAuth } from '@/hooks/useAuth'
import { buildCategoryPath, buildSearchPath, ROUTES } from '@/routes/paths'

interface CommandSearchModalProps {
  isOpen: boolean
  onClose: () => void
}

const FALLBACK_CATEGORY_DESTINATIONS = [
  { label: 'Chuyên mục Thời sự', to: buildCategoryPath('thoi-su') },
  { label: 'Chuyên mục Kinh tế', to: buildCategoryPath('kinh-te') },
  { label: 'Chuyên mục Công nghệ', to: buildCategoryPath('cong-nghe') },
]

export function CommandSearchModal({ isOpen, onClose }: CommandSearchModalProps) {
  const navigate = useNavigate()
  const { isAuthenticated } = useAuth()
  const [query, setQuery] = useState('')
  const { data: categories } = useCategories()

  const searchLabel = isAuthenticated ? 'Tìm kiếm thông minh' : 'Tìm kiếm bài viết'
  const searchRoute = isAuthenticated ? ROUTES.SEARCH : buildSearchPath(undefined, 'keyword')

  const categoryDestinations =
    categories && categories.length > 0
      ? categories.slice(0, 4).map((cat) => ({
          label: `Chuyên mục ${cat.name}`,
          to: buildCategoryPath(cat.slug),
        }))
      : FALLBACK_CATEGORY_DESTINATIONS

  const quickDestinations = [
    { label: 'Trang chủ Thời Báo Số', to: ROUTES.HOME },
    { label: searchLabel, to: searchRoute },
    ...categoryDestinations,
  ]

  const handleClose = () => {
    setQuery('')
    onClose()
  }

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const clean = query.trim()
    if (!clean) return
    handleClose()
    navigate(buildSearchPath(clean, isAuthenticated ? 'ai' : 'keyword'))
  }

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && handleClose()}>
      <DialogContent hideCloseButton className="max-w-lg p-0 overflow-hidden">
        <DialogTitle className="sr-only">Tìm kiếm nhanh trên NewsHub</DialogTitle>
        <DialogDescription className="sr-only">
          Nhập câu hỏi ngữ nghĩa AI hoặc từ khóa bài báo, hoặc chọn nhanh chuyên mục bên dưới.
        </DialogDescription>

        <form onSubmit={handleSubmit} className="flex items-center gap-3 px-4 h-12">
          <Search
            size={16}
            strokeWidth={1.75}
            className="text-neutral-text-muted dark:text-dark-text-muted shrink-0"
          />
          <input
            autoFocus
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder={
              isAuthenticated
                ? 'Nhập câu hỏi ngữ nghĩa AI hoặc từ khóa bài báo...'
                : 'Nhập từ khóa bài báo cần tìm...'
            }
            className="w-full bg-transparent text-sm text-neutral-text-main dark:text-dark-text-main placeholder:text-neutral-text-muted dark:placeholder:text-dark-text-muted outline-none"
          />
          <kbd className="rounded border border-neutral-border dark:border-dark-border px-1.5 py-0.5 font-mono text-[10px] text-neutral-text-muted dark:text-dark-text-muted">
            ESC
          </kbd>
        </form>

        <Separator />

        <div className="p-2 space-y-1">
          <p className="px-2.5 py-1 text-[11px] font-semibold text-neutral-text-muted dark:text-dark-text-muted">
            Điều hướng nhanh
          </p>
          {quickDestinations.map((item) => (
            <button
              key={item.to}
              type="button"
              onClick={() => {
                handleClose()
                navigate(item.to)
              }}
              className="w-full text-left px-2.5 py-2 rounded-[8px] text-xs font-medium text-neutral-text-main dark:text-dark-text-main hover:bg-sidebar-active dark:hover:bg-dark-border hover:text-primary dark:hover:text-secondary transition-colors cursor-pointer"
            >
              {item.label}
            </button>
          ))}
        </div>
      </DialogContent>
    </Dialog>
  )
}
