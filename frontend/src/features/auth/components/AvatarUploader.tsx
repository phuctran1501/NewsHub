import { AnimatePresence, motion } from 'framer-motion'
import { Upload } from 'lucide-react'
import { useEffect, useRef, useState, type ChangeEvent } from 'react'
import { Button } from '@/components/common/Button'
import { UserAvatar } from '@/components/common/UserAvatar'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { accountService } from '@/services/accountService'
import { toAppClientError } from '@/services/apiClient'
import { mediaService } from '@/services/mediaService'
import type { UserProfile } from '@/types/auth'
import { validateAvatarFile } from '@/utils/validation'

interface AvatarUploaderProps {
  user: UserProfile
  onAvatarUpdated: (updatedUser: UserProfile) => void
}

export function AvatarUploader({ user, onAvatarUpdated }: AvatarUploaderProps) {
  const fileInputRef = useRef<HTMLInputElement | null>(null)
  const previewObjectUrlRef = useRef<string | null>(null)

  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [pendingCloudUrl, setPendingCloudUrl] = useState<string | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [errorMsg, setErrorMsg] = useState<string | null>(null)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)

  useEffect(() => {
    return () => {
      if (previewObjectUrlRef.current) {
        URL.revokeObjectURL(previewObjectUrlRef.current)
      }
    }
  }, [])

  function revokeCurrentPreview() {
    if (previewObjectUrlRef.current) {
      URL.revokeObjectURL(previewObjectUrlRef.current)
      previewObjectUrlRef.current = null
    }
  }

  async function saveAvatarToProfile(cloudinaryUrl: string) {
    setIsUploading(true)
    setErrorMsg(null)
    try {
      const updatedUser = await accountService.updateProfile({
        avatarUrl: cloudinaryUrl,
      })
      onAvatarUpdated(updatedUser)
      revokeCurrentPreview()
      setPreviewUrl(null)
      setPendingCloudUrl(null)
      setSuccessMsg('Đã cập nhật ảnh đại diện thành công.')
    } catch (err: unknown) {
      const clientErr = toAppClientError(err)
      setPendingCloudUrl(cloudinaryUrl)
      setErrorMsg(clientErr.message)
    } finally {
      setIsUploading(false)
    }
  }

  async function handleFileSelected(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return

    setSuccessMsg(null)
    const validationErr = validateAvatarFile(file)
    if (validationErr) {
      setErrorMsg(validationErr)
      return
    }

    revokeCurrentPreview()
    const nextObjectUrl = URL.createObjectURL(file)
    previewObjectUrlRef.current = nextObjectUrl
    setPreviewUrl(nextObjectUrl)
    setPendingCloudUrl(null)
    setErrorMsg(null)
    setIsUploading(true)

    try {
      const uploadedMedia = await mediaService.uploadAvatar(file)
      await saveAvatarToProfile(uploadedMedia.url)
    } catch (err: unknown) {
      const clientErr = toAppClientError(err)
      setErrorMsg(clientErr.message)
      setIsUploading(false)
    }
  }

  const displayedAvatar = previewUrl ?? user.avatarUrl

  return (
    <div className="flex flex-col items-center text-center space-y-3">
      <UserAvatar fullName={user.fullName} avatarUrl={displayedAvatar} size="xl" />

      <input
        ref={fileInputRef}
        type="file"
        accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp"
        onChange={(e) => void handleFileSelected(e)}
        className="sr-only"
        aria-label="Chọn tệp ảnh đại diện"
      />

      <div className="flex flex-wrap items-center justify-center gap-2">
        <Button
          type="button"
          variant="outline"
          size="sm"
          isLoading={isUploading}
          onClick={() => fileInputRef.current?.click()}
        >
          <span className="inline-flex items-center gap-1.5">
            {!isUploading && <Upload size={14} strokeWidth={1.75} />}
            <span>{isUploading ? 'Đang tải lên...' : 'Tải ảnh đại diện'}</span>
          </span>
        </Button>

        {pendingCloudUrl && !isUploading && (
          <Button
            type="button"
            variant="action"
            size="sm"
            onClick={() => void saveAvatarToProfile(pendingCloudUrl)}
          >
            Thử lưu lại
          </Button>
        )}
      </div>

      <p className="text-[11px] text-neutral-text-muted dark:text-dark-text-muted">
        Hỗ trợ JPG, PNG, WebP (tối đa 5MB, tự động tối ưu WebP).
      </p>

      <AnimatePresence mode="wait">
        {errorMsg && (
          <motion.div
            key="avatar-err"
            initial={{ opacity: 0, y: -4 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.15 }}
            className="w-full text-left"
          >
            <Alert variant="destructive">
              <AlertDescription className="mt-0">{errorMsg}</AlertDescription>
            </Alert>
          </motion.div>
        )}

        {successMsg && !errorMsg && (
          <motion.div
            key="avatar-ok"
            initial={{ opacity: 0, y: -4 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.15 }}
            className="w-full text-left"
          >
            <Alert variant="success" role="status">
              <AlertDescription className="mt-0">{successMsg}</AlertDescription>
            </Alert>
          </motion.div>
        )}
      </AnimatePresence>
    </div>
  )
}
