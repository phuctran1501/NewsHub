import Image from '@tiptap/extension-image'
import Placeholder from '@tiptap/extension-placeholder'
import { Color, FontFamily, FontSize, TextStyle } from '@tiptap/extension-text-style'
import Underline from '@tiptap/extension-underline'
import { EditorContent, useEditor } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import {
  Bold,
  ChevronDown,
  Heading2,
  Heading3,
  ImagePlus,
  Italic,
  Link2,
  List,
  ListOrdered,
  Quote,
  Redo2,
  Underline as UnderlineIcon,
  Undo2,
} from 'lucide-react'
import { useEffect, useRef, useState, type ChangeEvent, type FormEvent } from 'react'
import { HexColorPicker } from 'react-colorful'
import { Button } from '@/components/common/Button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { toast } from '@/components/ui/toastManager'
import { toAppClientError } from '@/services/apiClient'
import { mediaService } from '@/services/mediaService'
import { MediaFolder } from '@/types/auth'
import { cn } from '@/utils/cn'

const MAX_INLINE_IMAGE_BYTES = 5 * 1024 * 1024
const ALLOWED_IMAGE_TYPES = new Set(['image/jpeg', 'image/jpg', 'image/png', 'image/webp'])

const FONT_FAMILIES = [
  { label: 'Inter (Mặc định)', value: '' },
  { label: 'Merriweather', value: 'Merriweather, Georgia, serif' },
  { label: 'Georgia', value: 'Georgia, serif' },
  { label: 'Arial', value: 'Arial, Helvetica, sans-serif' },
  { label: 'Times New Roman', value: '"Times New Roman", Times, serif' },
  { label: 'Monospace', value: 'ui-monospace, SFMono-Regular, Consolas, monospace' },
] as const

const FONT_SIZES_PX = ['12px', '14px', '16px', '18px', '20px', '24px', '28px', '32px'] as const

const QUICK_COLOR_SWATCHES = [
  '#1a202c',
  '#1b3b6f',
  '#173eff',
  '#0d9488',
  '#dc2626',
  '#d97706',
  '#475569',
  '#64748b',
  '#7c3aed',
  '#059669',
  '#be123c',
  '#0f172a',
] as const

const NaturalSizeImage = Image.extend({
  addAttributes() {
    return {
      ...this.parent?.(),
      width: {
        default: null,
        parseHTML: (element) => element.getAttribute('width'),
        renderHTML: (attributes) => (attributes.width ? { width: attributes.width } : {}),
      },
      height: {
        default: null,
        parseHTML: (element) => element.getAttribute('height'),
        renderHTML: (attributes) => (attributes.height ? { height: attributes.height } : {}),
      },
    }
  },
})

interface TipTapRichEditorProps {
  value: string
  onChange: (html: string) => void
  placeholder?: string
  disabled?: boolean
}

export function TipTapRichEditor({
  value,
  onChange,
  placeholder = 'Bắt đầu viết nội dung bài báo của bạn tại đây...',
  disabled = false,
}: TipTapRichEditorProps) {
  const imageInputRef = useRef<HTMLInputElement | null>(null)
  const colorPopoverRef = useRef<HTMLDivElement | null>(null)
  const [isUploadingImage, setIsUploadingImage] = useState(false)
  const [isColorOpen, setIsColorOpen] = useState(false)
  const [linkDialogOpen, setLinkDialogOpen] = useState(false)
  const [linkUrl, setLinkUrl] = useState('')
  const [linkText, setLinkText] = useState('')
  const [linkError, setLinkError] = useState<string | null>(null)

  const editor = useEditor({
    extensions: [
      StarterKit.configure({
        heading: { levels: [2, 3] },
        underline: false,
        link: {
          openOnClick: false,
          HTMLAttributes: {
            rel: 'noopener noreferrer',
            target: '_blank',
          },
        },
      }),
      Underline,
      TextStyle,
      Color,
      FontFamily,
      FontSize,
      NaturalSizeImage.configure({
        HTMLAttributes: {
          class: 'nh-inline-reportage-img rounded-[10px] border border-neutral-border dark:border-dark-border my-4',
        },
      }),
      Placeholder.configure({
        placeholder,
      }),
    ],
    content: value,
    editable: !disabled,
    onUpdate: ({ editor: currentEditor }) => {
      if (currentEditor.isDestroyed) return
      onChange(currentEditor.getHTML())
    },
  })

  useEffect(() => {
    if (!editor || editor.isDestroyed) return
    editor.setEditable(!disabled)
  }, [editor, disabled])

  useEffect(() => {
    if (!editor || editor.isDestroyed) return
    if (value !== editor.getHTML()) {
      editor.commands.setContent(value || '', { emitUpdate: false })
    }
  }, [editor, value])

  useEffect(() => {
    if (!isColorOpen) return
    const handlePointerDown = (event: MouseEvent) => {
      if (colorPopoverRef.current && !colorPopoverRef.current.contains(event.target as Node)) {
        setIsColorOpen(false)
      }
    }
    document.addEventListener('mousedown', handlePointerDown)
    return () => document.removeEventListener('mousedown', handlePointerDown)
  }, [isColorOpen])

  const openLinkDialog = () => {
    if (!editor || editor.isDestroyed) return
    const existingHref = (editor.getAttributes('link').href as string | undefined) ?? ''
    const { from, to } = editor.state.selection
    const selectedText = from !== to ? editor.state.doc.textBetween(from, to, ' ') : ''
    setLinkUrl(existingHref)
    setLinkText(selectedText)
    setLinkError(null)
    setLinkDialogOpen(true)
  }

  const handleApplyLink = (event: FormEvent) => {
    event.preventDefault()
    if (!editor || editor.isDestroyed) return

    const trimmedUrl = linkUrl.trim()
    if (!trimmedUrl) {
      editor.chain().focus().extendMarkRange('link').unsetLink().run()
      setLinkDialogOpen(false)
      return
    }

    if (!/^https?:\/\//i.test(trimmedUrl)) {
      setLinkError('Đường dẫn phải bắt đầu bằng http:// hoặc https://')
      return
    }

    const { from, to } = editor.state.selection
    if (from === to) {
      const displayLabel = linkText.trim() || trimmedUrl
      editor
        .chain()
        .focus()
        .insertContent({
          type: 'text',
          text: displayLabel,
          marks: [{ type: 'link', attrs: { href: trimmedUrl } }],
        })
        .run()
    } else {
      editor.chain().focus().extendMarkRange('link').setLink({ href: trimmedUrl }).run()
    }

    setLinkDialogOpen(false)
  }

  const handleRemoveLink = () => {
    if (!editor || editor.isDestroyed) return
    editor.chain().focus().extendMarkRange('link').unsetLink().run()
    setLinkDialogOpen(false)
  }

  const handleInlineImageChange = async (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file || !editor || editor.isDestroyed) return

    if (!ALLOWED_IMAGE_TYPES.has(file.type.toLowerCase())) {
      toast.error('Định dạng ảnh không hỗ trợ', 'Chỉ chấp nhận .jpg, .jpeg, .png hoặc .webp.')
      return
    }

    if (file.size > MAX_INLINE_IMAGE_BYTES) {
      toast.error('Ảnh vượt quá 5MB', 'Vui lòng chọn ảnh có dung lượng nhỏ hơn 5MB.')
      return
    }

    setIsUploadingImage(true)
    try {
      const uploaded = await mediaService.uploadImage(file, MediaFolder.ArticleBody)
      const imageAttrs: Record<string, unknown> = {
        src: uploaded.url,
        alt: uploaded.originalFileName,
      }
      if (uploaded.width > 0) imageAttrs.width = uploaded.width
      if (uploaded.height > 0) imageAttrs.height = uploaded.height

      editor
        .chain()
        .focus()
        .insertContent({
          type: 'image',
          attrs: imageAttrs,
        })
        .run()
      toast.success('Đã chèn ảnh phóng sự đúng kích thước gốc')
    } catch (error) {
      toast.error('Tải ảnh phóng sự thất bại', toAppClientError(error).message)
    } finally {
      setIsUploadingImage(false)
    }
  }

  if (!editor || editor.isDestroyed) {
    return (
      <div className="min-h-[360px] rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg animate-pulse" />
    )
  }

  const activeFontFamily = (editor.getAttributes('textStyle').fontFamily as string | undefined) ?? ''
  const activeFontSize = (editor.getAttributes('textStyle').fontSize as string | undefined) ?? '16px'
  const activeColor = (editor.getAttributes('textStyle').color as string | undefined) ?? '#1a202c'

  const toolBtnClass = (active: boolean) =>
    cn(
      'inline-flex items-center justify-center gap-1 h-8 px-2.5 rounded-[8px] text-xs font-semibold border transition-colors duration-150 cursor-pointer disabled:opacity-45',
      active
        ? 'bg-primary text-white border-primary dark:bg-secondary dark:text-dark-bg dark:border-secondary'
        : 'bg-neutral-surface dark:bg-dark-surface text-neutral-text-main dark:text-dark-text-main border-neutral-border dark:border-dark-border hover:border-primary dark:hover:border-secondary',
    )

  return (
    <div className="rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface">
      <input
        ref={imageInputRef}
        type="file"
        accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp"
        className="hidden"
        onChange={handleInlineImageChange}
        disabled={disabled || isUploadingImage}
      />

      {/* Word-inspired Pure 2D Toolbar — flush at top of editor card */}
      <div
        role="toolbar"
        aria-label="Thanh công cụ định dạng văn bản"
        className="flex flex-wrap items-center gap-1.5 px-3 py-2.5 border-b border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg rounded-t-[12px]"
      >
        {/* Font Family & Font Size (px) */}
        <select
          aria-label="Phông chữ"
          title="Phông chữ"
          value={activeFontFamily}
          onChange={(event) => {
            const font = event.target.value
            if (!font) {
              editor.chain().focus().unsetFontFamily().run()
            } else {
              editor.chain().focus().setFontFamily(font).run()
            }
          }}
          className="h-8 max-w-[148px] rounded-[8px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface px-2 text-xs font-medium text-neutral-text-main dark:text-dark-text-main outline-none focus:border-primary dark:focus:border-secondary cursor-pointer"
        >
          {FONT_FAMILIES.map((font) => (
            <option key={font.label} value={font.value}>
              {font.label}
            </option>
          ))}
        </select>

        <select
          aria-label="Cỡ chữ (px)"
          title="Cỡ chữ theo pixel"
          value={FONT_SIZES_PX.includes(activeFontSize as (typeof FONT_SIZES_PX)[number]) ? activeFontSize : '16px'}
          onChange={(event) => {
            const size = event.target.value
            if (size === '16px') {
              editor.chain().focus().unsetFontSize().run()
            } else {
              editor.chain().focus().setFontSize(size).run()
            }
          }}
          className="h-8 rounded-[8px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface px-2 text-xs font-medium text-neutral-text-main dark:text-dark-text-main outline-none focus:border-primary dark:focus:border-secondary cursor-pointer"
        >
          {FONT_SIZES_PX.map((size) => (
            <option key={size} value={size}>
              {size}
            </option>
          ))}
        </select>

        <span className="h-5 w-px bg-neutral-border dark:bg-dark-border mx-0.5" aria-hidden="true" />

        <button
          type="button"
          title="Tiêu đề mục H2"
          aria-label="Tiêu đề mục H2"
          onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
          className={toolBtnClass(editor.isActive('heading', { level: 2 }))}
        >
          <Heading2 size={15} strokeWidth={1.75} />
        </button>

        <button
          type="button"
          title="Tiêu đề phụ H3"
          aria-label="Tiêu đề phụ H3"
          onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()}
          className={toolBtnClass(editor.isActive('heading', { level: 3 }))}
        >
          <Heading3 size={15} strokeWidth={1.75} />
        </button>

        <span className="h-5 w-px bg-neutral-border dark:bg-dark-border mx-0.5" aria-hidden="true" />

        <button
          type="button"
          title="Chữ đậm (Ctrl+B)"
          aria-label="Chữ đậm"
          onClick={() => editor.chain().focus().toggleBold().run()}
          className={toolBtnClass(editor.isActive('bold'))}
        >
          <Bold size={15} strokeWidth={1.75} />
        </button>

        <button
          type="button"
          title="Chữ nghiêng (Ctrl+I)"
          aria-label="Chữ nghiêng"
          onClick={() => editor.chain().focus().toggleItalic().run()}
          className={toolBtnClass(editor.isActive('italic'))}
        >
          <Italic size={15} strokeWidth={1.75} />
        </button>

        <button
          type="button"
          title="Gạch dưới (Ctrl+U)"
          aria-label="Gạch dưới"
          onClick={() => editor.chain().focus().toggleUnderline().run()}
          className={toolBtnClass(editor.isActive('underline'))}
        >
          <UnderlineIcon size={15} strokeWidth={1.75} />
        </button>

        {/* Color Picker Popover using react-colorful */}
        <div ref={colorPopoverRef} className="relative">
          <button
            type="button"
            title="Màu chữ"
            aria-label="Chọn màu chữ"
            aria-expanded={isColorOpen}
            onClick={() => setIsColorOpen((prev) => !prev)}
            className={toolBtnClass(isColorOpen)}
          >
            <span
              className="font-bold leading-none pb-0.5 border-b-2"
              style={{ borderBottomColor: activeColor }}
            >
              A
            </span>
            <ChevronDown size={12} strokeWidth={1.75} />
          </button>

          {isColorOpen && (
            <div className="absolute left-0 top-full mt-1.5 z-40 w-60 rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-3 shadow-[0_8px_24px_rgba(15,23,42,0.08)] space-y-3">
              <div className="space-y-1.5">
                <p className="text-[11px] font-semibold text-neutral-text-muted dark:text-dark-text-muted">
                  Bảng màu chữ
                </p>
                <div className="nh-color-picker-wrap">
                  <HexColorPicker
                    color={activeColor}
                    onChange={(hex) => editor.chain().focus().setColor(hex).run()}
                  />
                </div>
              </div>

              <div className="grid grid-cols-6 gap-1.5">
                {QUICK_COLOR_SWATCHES.map((swatch) => (
                  <button
                    key={swatch}
                    type="button"
                    title={swatch}
                    aria-label={`Chọn màu ${swatch}`}
                    onClick={() => {
                      editor.chain().focus().setColor(swatch).run()
                      setIsColorOpen(false)
                    }}
                    className="h-6 w-full rounded-[6px] border border-neutral-border dark:border-dark-border cursor-pointer"
                    style={{ backgroundColor: swatch }}
                  />
                ))}
              </div>

              <Button
                type="button"
                variant="outline"
                size="sm"
                fullWidth
                onClick={() => {
                  editor.chain().focus().unsetColor().run()
                  setIsColorOpen(false)
                }}
              >
                Đặt lại màu mặc định
              </Button>
            </div>
          )}
        </div>

        <span className="h-5 w-px bg-neutral-border dark:bg-dark-border mx-0.5" aria-hidden="true" />

        <button
          type="button"
          title="Trích dẫn"
          aria-label="Trích dẫn"
          onClick={() => editor.chain().focus().toggleBlockquote().run()}
          className={toolBtnClass(editor.isActive('blockquote'))}
        >
          <Quote size={15} strokeWidth={1.75} />
        </button>

        <button
          type="button"
          title="Danh sách chấm đầu dòng"
          aria-label="Danh sách chấm đầu dòng"
          onClick={() => editor.chain().focus().toggleBulletList().run()}
          className={toolBtnClass(editor.isActive('bulletList'))}
        >
          <List size={15} strokeWidth={1.75} />
        </button>

        <button
          type="button"
          title="Danh sách đánh số"
          aria-label="Danh sách đánh số"
          onClick={() => editor.chain().focus().toggleOrderedList().run()}
          className={toolBtnClass(editor.isActive('orderedList'))}
        >
          <ListOrdered size={15} strokeWidth={1.75} />
        </button>

        <button
          type="button"
          title="Chèn hoặc sửa liên kết"
          aria-label="Chèn liên kết"
          onClick={openLinkDialog}
          className={toolBtnClass(editor.isActive('link'))}
        >
          <Link2 size={15} strokeWidth={1.75} />
        </button>

        <span className="h-5 w-px bg-neutral-border dark:bg-dark-border mx-0.5" aria-hidden="true" />

        <button
          type="button"
          disabled={disabled || isUploadingImage}
          onClick={() => imageInputRef.current?.click()}
          className="inline-flex items-center justify-center gap-1.5 h-8 px-2.5 rounded-[8px] text-xs font-semibold bg-secondary-light text-primary hover:bg-secondary/70 dark:bg-secondary/15 dark:text-secondary dark:hover:bg-secondary/25 transition-colors duration-150 cursor-pointer disabled:opacity-50"
        >
          <ImagePlus size={14} strokeWidth={1.75} />
          <span>{isUploadingImage ? 'Đang tải...' : 'Chèn ảnh'}</span>
        </button>

        <div className="ml-auto flex items-center gap-1">
          <button
            type="button"
            title="Hoàn tác"
            aria-label="Hoàn tác"
            disabled={!editor.can().undo()}
            onClick={() => editor.chain().focus().undo().run()}
            className={toolBtnClass(false)}
          >
            <Undo2 size={14} strokeWidth={1.75} />
          </button>
          <button
            type="button"
            title="Làm lại"
            aria-label="Làm lại"
            disabled={!editor.can().redo()}
            onClick={() => editor.chain().focus().redo().run()}
            className={toolBtnClass(false)}
          >
            <Redo2 size={14} strokeWidth={1.75} />
          </button>
        </div>
      </div>

      <EditorContent
        editor={editor}
        className="nh-tiptap-editor nh-article-prose px-5 py-4 min-h-[380px] focus-within:outline-none"
      />

      {/* Professional Link Insert/Edit Dialog (replaces window.prompt) */}
      <Dialog open={linkDialogOpen} onOpenChange={setLinkDialogOpen}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Chèn hoặc chỉnh sửa liên kết</DialogTitle>
            <DialogDescription>
              Nhập đường dẫn URL hợp lệ bắt đầu bằng https:// hoặc http://.
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleApplyLink} className="space-y-3.5">
            <div className="space-y-1.5">
              <label
                htmlFor="tiptap-link-url"
                className="text-xs font-semibold text-neutral-text-main dark:text-dark-text-main block"
              >
                Đường dẫn URL
              </label>
              <input
                id="tiptap-link-url"
                type="url"
                value={linkUrl}
                onChange={(event) => {
                  setLinkUrl(event.target.value)
                  if (linkError) setLinkError(null)
                }}
                placeholder="https://newshub.vn/..."
                autoFocus
                className="w-full rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-bg px-3 py-2 text-xs text-neutral-text-main dark:text-dark-text-main focus:outline-none focus:border-primary dark:focus:border-secondary"
              />
              {linkError && (
                <p className="text-[11px] font-medium text-status-rejected">{linkError}</p>
              )}
            </div>

            {editor.state.selection.empty && (
              <div className="space-y-1.5">
                <label
                  htmlFor="tiptap-link-label"
                  className="text-xs font-semibold text-neutral-text-main dark:text-dark-text-main block"
                >
                  Văn bản hiển thị (tùy chọn)
                </label>
                <input
                  id="tiptap-link-label"
                  type="text"
                  value={linkText}
                  onChange={(event) => setLinkText(event.target.value)}
                  placeholder="VD: Xem báo cáo chi tiết"
                  className="w-full rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-bg px-3 py-2 text-xs text-neutral-text-main dark:text-dark-text-main focus:outline-none focus:border-primary dark:focus:border-secondary"
                />
              </div>
            )}

            <DialogFooter className="sm:justify-between">
              {editor.isActive('link') ? (
                <Button type="button" variant="outline" size="sm" onClick={handleRemoveLink}>
                  Gỡ liên kết
                </Button>
              ) : (
                <span />
              )}
              <div className="flex items-center gap-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => setLinkDialogOpen(false)}
                >
                  Hủy
                </Button>
                <Button type="submit" variant="primary" size="sm">
                  Áp dụng liên kết
                </Button>
              </div>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  )
}
