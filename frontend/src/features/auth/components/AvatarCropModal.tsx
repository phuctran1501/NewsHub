import { RotateCw } from 'lucide-react'
import { useCallback, useState } from 'react'
import Cropper, { type Area, type Point } from 'react-easy-crop'
import { Button } from '@/components/common/Button'
import { Alert, AlertDescription } from '@/components/ui/alert'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { accountService } from '@/services/accountService'
import { toAppClientError } from '@/services/apiClient'
import { mediaService } from '@/services/mediaService'
import type { UserProfile } from '@/types/auth'
import { createCroppedAvatarFile, createCroppedPreviewDataUrl } from '@/utils/imageCrop'

interface AvatarCropModalProps {
  isOpen: boolean
  imageSrc: string
  userFullName: string
  onClose: () => void
  onChooseAnother: () => void
  onSaved: (updatedUser: UserProfile) => void
}

export function AvatarCropModal({
  isOpen,
  imageSrc,
  userFullName,
  onClose,
  onChooseAnother,
  onSaved,
}: AvatarCropModalProps) {
  const [crop, setCrop] = useState<Point>({ x: 0, y: 0 })
  const [zoom, setZoom] = useState(1)
  const [rotation, setRotation] = useState(0)
  const [croppedAreaPixels, setCroppedAreaPixels] = useState<Area | null>(null)
  const [previewDataUrl, setPreviewDataUrl] = useState<string | null>(null)
  const [pendingCloudUrl, setPendingCloudUrl] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [errorMsg, setErrorMsg] = useState<string | null>(null)

  const handleCropComplete = useCallback(
    (_croppedArea: Area, croppedPixels: Area) => {
      setCroppedAreaPixels(croppedPixels)
      setPendingCloudUrl(null)
      void createCroppedPreviewDataUrl(imageSrc, croppedPixels, rotation)
        .then((dataUrl) => setPreviewDataUrl(dataUrl))
        .catch(() => {
          // Xem trước là tùy chọn; không làm gián đoạn thao tác cắt chính.
        })
    },
    [imageSrc, rotation],
  )

  async function saveProfileAvatarUrl(cloudinaryUrl: string) {
    setIsSaving(true)
    setErrorMsg(null)
    try {
      const updatedUser = await accountService.updateProfile({
        avatarUrl: cloudinaryUrl,
      })
      onSaved(updatedUser)
    } catch (err: unknown) {
      const clientErr = toAppClientError(err)
      setPendingCloudUrl(cloudinaryUrl)
      setErrorMsg(clientErr.message)
    } finally {
      setIsSaving(false)
    }
  }

  async function handleConfirmSave() {
    if (!croppedAreaPixels || isSaving) return

    if (pendingCloudUrl) {
      await saveProfileAvatarUrl(pendingCloudUrl)
      return
    }

    setIsSaving(true)
    setErrorMsg(null)

    try {
      const croppedFile = await createCroppedAvatarFile(imageSrc, croppedAreaPixels, rotation)
      const uploadedMedia = await mediaService.uploadAvatar(croppedFile)
      await saveProfileAvatarUrl(uploadedMedia.url)
    } catch (err: unknown) {
      const clientErr = toAppClientError(err)
      setErrorMsg(clientErr.message)
      setIsSaving(false)
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && !isSaving && onClose()}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>Chỉnh sửa ảnh đại diện</DialogTitle>
          <DialogDescription>
            Kéo ảnh để căn chỉnh vào khung tròn, thu phóng hoặc xoay trước khi xác nhận lưu.
          </DialogDescription>
        </DialogHeader>

        <div className="mt-4 space-y-4">
          <div className="relative h-72 w-full overflow-hidden rounded-xl border border-dark-border bg-[#0f172a]">
            <Cropper
              image={imageSrc}
              crop={crop}
              zoom={zoom}
              rotation={rotation}
              aspect={1}
              cropShape="round"
              showGrid
              onCropChange={setCrop}
              onZoomChange={setZoom}
              onCropComplete={handleCropComplete}
            />
          </div>

          <div className="flex items-center justify-between gap-4">
            <div className="flex-1 space-y-1.5">
              <div className="flex items-center justify-between text-xs font-semibold text-neutral-text-main dark:text-dark-text-main">
                <label htmlFor="avatar-zoom-slider">Thu phóng</label>
                <span className="font-mono text-neutral-text-muted dark:text-dark-text-muted">
                  {zoom.toFixed(1)}x
                </span>
              </div>
              <input
                id="avatar-zoom-slider"
                type="range"
                min={1}
                max={3}
                step={0.05}
                value={zoom}
                disabled={isSaving}
                onChange={(e) => setZoom(Number(e.target.value))}
                className="w-full accent-primary dark:accent-secondary cursor-pointer"
              />
            </div>

            <Button
              type="button"
              variant="outline"
              size="sm"
              disabled={isSaving}
              onClick={() => setRotation((prev) => (prev + 90) % 360)}
            >
              <span className="inline-flex items-center gap-1.5">
                <RotateCw size={14} strokeWidth={1.75} />
                <span>Xoay 90°</span>
              </span>
            </Button>
          </div>

          <div className="flex items-center justify-between gap-3 rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg px-3.5 py-2.5">
            <div className="flex items-center gap-3">
              <div className="flex items-center gap-2">
                {previewDataUrl ? (
                  <>
                    <img
                      src={previewDataUrl}
                      alt={`Xem trước ảnh đại diện cỡ lớn của ${userFullName}`}
                      className="h-11 w-11 rounded-full border border-neutral-border dark:border-dark-border object-cover"
                    />
                    <img
                      src={previewDataUrl}
                      alt={`Xem trước ảnh đại diện trên Header của ${userFullName}`}
                      className="h-8 w-8 rounded-full border border-neutral-border dark:border-dark-border object-cover"
                    />
                  </>
                ) : (
                  <div className="h-11 w-11 rounded-full border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface" />
                )}
              </div>
              <div className="text-left">
                <p className="text-xs font-semibold text-neutral-text-main dark:text-dark-text-main">
                  Xem trước sau khi cắt
                </p>
                <p className="text-[11px] text-neutral-text-muted dark:text-dark-text-muted">
                  Hiển thị đồng bộ trên Hồ sơ cá nhân và thanh Header.
                </p>
              </div>
            </div>
          </div>

          {errorMsg && (
            <Alert variant="destructive">
              <AlertDescription className="mt-0">{errorMsg}</AlertDescription>
            </Alert>
          )}
        </div>

        <DialogFooter className="mt-5 flex-col-reverse sm:flex-row sm:items-center sm:justify-between gap-2 border-t border-neutral-border dark:border-dark-border pt-4">
          <Button
            type="button"
            variant="outline"
            size="sm"
            disabled={isSaving}
            onClick={onChooseAnother}
          >
            Chọn ảnh khác
          </Button>

          <div className="flex items-center justify-end gap-2">
            <Button type="button" variant="outline" size="sm" disabled={isSaving} onClick={onClose}>
              Hủy
            </Button>
            <Button
              type="button"
              variant="primary"
              size="sm"
              isLoading={isSaving}
              disabled={!croppedAreaPixels}
              onClick={() => void handleConfirmSave()}
            >
              {pendingCloudUrl ? 'Thử lưu lại' : 'Xác nhận & Lưu ảnh'}
            </Button>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
