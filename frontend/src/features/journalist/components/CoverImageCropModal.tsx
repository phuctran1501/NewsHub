import { RotateCw } from 'lucide-react'
import { useCallback, useState } from 'react'
import Cropper, { type Area, type Point } from 'react-easy-crop'
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
import { MediaFolder, type MediaFileDto } from '@/types/auth'
import { createCroppedCoverFile } from '@/utils/imageCrop'

interface CoverImageCropModalProps {
  isOpen: boolean
  imageSrc: string
  onClose: () => void
  onChooseAnother: () => void
  onUploaded: (media: MediaFileDto) => void
}

export function CoverImageCropModal({
  isOpen,
  imageSrc,
  onClose,
  onChooseAnother,
  onUploaded,
}: CoverImageCropModalProps) {
  const [crop, setCrop] = useState<Point>({ x: 0, y: 0 })
  const [zoom, setZoom] = useState(1)
  const [rotation, setRotation] = useState(0)
  const [croppedAreaPixels, setCroppedAreaPixels] = useState<Area | null>(null)
  const [isUploading, setIsUploading] = useState(false)

  const handleCropComplete = useCallback((_croppedArea: Area, croppedPixels: Area) => {
    setCroppedAreaPixels(croppedPixels)
  }, [])

  const handleConfirmUpload = async () => {
    if (!croppedAreaPixels || isUploading) return

    setIsUploading(true)
    try {
      const croppedFile = await createCroppedCoverFile(imageSrc, croppedAreaPixels, rotation)
      const uploaded = await mediaService.uploadImage(croppedFile, MediaFolder.ArticleCover)
      toast.success('Đã cắt và tải ảnh bìa lên Cloudinary')
      onUploaded(uploaded)
    } catch (error) {
      toast.error('Tải ảnh bìa thất bại', toAppClientError(error).message)
    } finally {
      setIsUploading(false)
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && !isUploading && onClose()}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle>Căn chỉnh ảnh bìa bài viết (Tỷ lệ 16:9)</DialogTitle>
          <DialogDescription>
            Kéo ảnh, thu phóng hoặc xoay để căn khung 16:9 chuẩn báo điện tử trước khi tải lên.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="relative h-72 w-full overflow-hidden rounded-xl border border-neutral-border dark:border-dark-border bg-dark-bg">
            <Cropper
              image={imageSrc}
              crop={crop}
              zoom={zoom}
              rotation={rotation}
              aspect={16 / 9}
              cropShape="rect"
              showGrid
              onCropChange={setCrop}
              onZoomChange={setZoom}
              onCropComplete={handleCropComplete}
            />
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <label className="flex flex-1 items-center gap-2.5 text-xs text-neutral-text-muted dark:text-dark-text-muted">
              <span className="shrink-0 font-medium">Thu phóng</span>
              <input
                type="range"
                min={1}
                max={3}
                step={0.05}
                value={zoom}
                onChange={(event) => setZoom(Number(event.target.value))}
                className="flex-1 accent-primary dark:accent-secondary cursor-pointer"
              />
            </label>

            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={isUploading}
              onClick={() => setRotation((prev) => (prev + 90) % 360)}
            >
              <RotateCw size={14} strokeWidth={1.75} className="inline mr-1" />
              Xoay 90°
            </Button>
          </div>
        </div>

        <DialogFooter className="sm:justify-between">
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={isUploading}
            onClick={onChooseAnother}
          >
            Chọn ảnh khác
          </Button>
          <div className="flex items-center gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={isUploading}
              onClick={onClose}
            >
              Hủy
            </Button>
            <Button
              type="button"
              variant="primary"
              size="sm"
              isLoading={isUploading}
              onClick={() => void handleConfirmUpload()}
            >
              Cắt & Tải ảnh bìa
            </Button>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
