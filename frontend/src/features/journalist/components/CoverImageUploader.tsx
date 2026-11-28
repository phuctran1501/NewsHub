import { ImagePlus } from 'lucide-react'
import { useEffect, useRef, useState, type ChangeEvent } from 'react'
import { Button } from '@/components/common/Button'
import { toast } from '@/components/ui/toastManager'
import { CoverImageCropModal } from './CoverImageCropModal'

const MAX_FILE_SIZE_BYTES = 5 * 1024 * 1024
const ALLOWED_TYPES = new Set(['image/jpeg', 'image/jpg', 'image/png', 'image/webp'])

export interface CoverImageValue {
  id: string
  url: string
  width?: number
  height?: number
  format?: string
}

interface CoverImageUploaderProps {
  value: CoverImageValue | null
  onChange: (next: CoverImageValue | null) => void
  disabled?: boolean
}

export function CoverImageUploader({
  value,
  onChange,
  disabled = false,
}: CoverImageUploaderProps) {
  const inputRef = useRef<HTMLInputElement | null>(null)
  const [cropSourceUrl, setCropSourceUrl] = useState<string | null>(null)

  useEffect(() => {
    return () => {
      if (cropSourceUrl) {
        URL.revokeObjectURL(cropSourceUrl)
      }
    }
  }, [cropSourceUrl])

  const closeCropModal = () => {
    if (cropSourceUrl) {
      URL.revokeObjectURL(cropSourceUrl)
    }
    setCropSourceUrl(null)
  }

  const handleFileChange = (event: ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return

    if (!ALLOWED_TYPES.has(file.type.toLowerCase())) {
      toast.error('Định dạng ảnh không hỗ trợ', 'Chỉ chấp nhận tệp .jpg, .jpeg, .png hoặc .webp.')
      return
    }

    if (file.size > MAX_FILE_SIZE_BYTES) {
      toast.error('Ảnh vượt quá 5MB', 'Vui lòng chọn ảnh bìa có dung lượng nhỏ hơn 5MB.')
      return
    }

    if (cropSourceUrl) {
      URL.revokeObjectURL(cropSourceUrl)
    }
    setCropSourceUrl(URL.createObjectURL(file))
  }

  return (
    <div className="space-y-2">
      <input
        ref={inputRef}
        type="file"
        accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp"
        className="hidden"
        onChange={handleFileChange}
        disabled={disabled}
      />

      {value ? (
        <div className="rounded-xl border border-neutral-border dark:border-dark-border overflow-hidden bg-neutral-bg dark:bg-dark-bg">
          <div className="relative aspect-video w-full overflow-hidden">
            <img
              src={value.url}
              alt="Ảnh bìa bài viết"
              className="w-full h-full object-cover"
            />
          </div>
          <div className="px-3.5 py-2.5 flex flex-wrap items-center justify-between gap-2 border-t border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface">
            <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted truncate">
              Ảnh bìa (16:9)
              {value.width && value.height ? ` · ${value.width}×${value.height}` : ''}
            </p>
            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant="secondary"
                size="sm"
                disabled={disabled}
                onClick={() => inputRef.current?.click()}
              >
                Đổi ảnh bìa
              </Button>
              <Button
                type="button"
                variant="outline"
                size="sm"
                disabled={disabled}
                onClick={() => onChange(null)}
              >
                Gỡ ảnh
              </Button>
            </div>
          </div>
        </div>
      ) : (
        <button
          type="button"
          disabled={disabled}
          onClick={() => inputRef.current?.click()}
          className="w-full rounded-xl border border-dashed border-neutral-border dark:border-dark-border bg-neutral-bg/70 dark:bg-dark-bg hover:border-primary dark:hover:border-secondary px-5 py-8 flex flex-col items-center justify-center gap-2 text-center transition-colors duration-150 cursor-pointer disabled:opacity-50"
        >
          <ImagePlus
            size={22}
            strokeWidth={1.75}
            className="text-neutral-text-muted dark:text-dark-text-muted"
          />
          <div className="space-y-0.5">
            <p className="text-xs font-semibold text-neutral-text-main dark:text-dark-text-main">
              Chọn ảnh bìa bài viết (Tự động mở khung cắt 16:9)
            </p>
            <p className="text-[11px] text-neutral-text-muted dark:text-dark-text-muted">
              Định dạng JPG, PNG, WebP · Tối đa 5MB · Tối ưu WebP trên Cloudinary
            </p>
          </div>
        </button>
      )}

      {cropSourceUrl && (
        <CoverImageCropModal
          isOpen={Boolean(cropSourceUrl)}
          imageSrc={cropSourceUrl}
          onClose={closeCropModal}
          onChooseAnother={() => {
            closeCropModal()
            inputRef.current?.click()
          }}
          onUploaded={(uploaded) => {
            onChange({
              id: uploaded.id,
              url: uploaded.url,
              width: uploaded.width,
              height: uploaded.height,
              format: uploaded.format,
            })
            closeCropModal()
          }}
        />
      )}
    </div>
  )
}
