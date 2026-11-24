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
import { useDebounce } from '@/hooks/useDebounce'
import { ROUTES } from '@/routes/paths'

interface CommandSearchModalProps {
  isOpen: boolean
  onClose: () => void
}

const QUICK_DESTINATIONS = [
  { label: 'Trang chủ Thời Báo Số', to: ROUTES.HOME },
  { label: 'Chuyên mục Thời sự', to: '/?category=thoi-su' },
  { label: 'Chuyên mục Kinh tế & Tài chính', to: '/?category=kinh-te' },
  { label: 'Chuyên mục Công nghệ & Dữ liệu', to: '/?category=cong-nghe' },
  { label: 'Danh bạ Tòa soạn số', to: '/?section=companies' },
]

export function CommandSearchModal({ isOpen, onClose }: CommandSearchModalProps) {
  const navigate = useNavigate()
  const [query, setQuery] = useState('')
  const debouncedQuery = useDebounce(query, 250)

  const handleClose = () => {
    setQuery('')
    onClose()
  }

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const clean = (debouncedQuery || query).trim()
    if (!clean) return
    handleClose()
    navigate(`/?q=${encodeURIComponent(clean)}`)
  }

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && handleClose()}>
      <DialogContent hideCloseButton className="max-w-lg p-0 overflow-hidden">
        <DialogTitle className="sr-only">Tìm kiếm nhanh trên NewsHub</DialogTitle>
        <DialogDescription className="sr-only">
          Nhập từ khóa bài báo hoặc chọn nhanh chuyên mục bên dưới.
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
            placeholder="Nhập từ khóa bài báo hoặc câu hỏi ngữ nghĩa..."
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
          {QUICK_DESTINATIONS.map((item) => (
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
