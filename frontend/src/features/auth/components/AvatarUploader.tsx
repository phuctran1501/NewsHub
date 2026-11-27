import { Upload } from 'lucide-react'
import { useEffect, useRef, useState, type ChangeEvent, type RefObject } from 'react'
import { UserAvatar } from '@/components/common/UserAvatar'
import { toast } from '@/components/ui/toastManager'
import type { UserProfile } from '@/types/auth'
import { validateAvatarFile } from '@/utils/validation'
import { AvatarCropModal } from './AvatarCropModal'

interface AvatarUploaderProps {
  user: UserProfile
  fileInputRef: RefObject<HTMLInputElement | null>
  onAvatarUpdated: (updatedUser: UserProfile) => void
}

export function AvatarUploader({ user, fileInputRef, onAvatarUpdated }: AvatarUploaderProps) {
  const objectUrlRef = useRef<string | null>(null)
  const [selectedImageUrl, setSelectedImageUrl] = useState<string | null>(null)

  const revokeObjectUrl = () => {
    if (objectUrlRef.current) {
      URL.revokeObjectURL(objectUrlRef.current)
      objectUrlRef.current = null
    }
  }

  useEffect(() => {
    return () => {
      if (objectUrlRef.current) {
        URL.revokeObjectURL(objectUrlRef.current)
      }
    }
  }, [])

  function handleFileSelected(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0]
    event.target.value = ''
    if (!file) return

    const validationErr = validateAvatarFile(file)
    if (validationErr) {
      toast.error('Ảnh đại diện không hợp lệ', validationErr)
      return
    }

    revokeObjectUrl()
    const nextUrl = URL.createObjectURL(file)
    objectUrlRef.current = nextUrl
    setSelectedImageUrl(nextUrl)
  }

  function handleCloseModal() {
    setSelectedImageUrl(null)
    revokeObjectUrl()
  }

  function handleSaved(updatedUser: UserProfile) {
    onAvatarUpdated(updatedUser)
    setSelectedImageUrl(null)
    revokeObjectUrl()
    toast.success('Đã cập nhật ảnh đại diện', 'Ảnh mới đã được đồng bộ trên toàn hệ thống.')
  }

  return (
    <>
      <div className="relative inline-flex shrink-0">
        <UserAvatar
          fullName={user.fullName}
          avatarUrl={user.avatarUrl}
          size="xl"
          className="ring-4 ring-neutral-surface dark:ring-dark-surface"
        />

        <button
          type="button"
          onClick={() => fileInputRef.current?.click()}
          aria-label="Chọn ảnh đại diện mới"
          title="Đổi ảnh đại diện"
          className="absolute right-0 bottom-0 inline-flex h-8 w-8 items-center justify-center rounded-full border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface text-neutral-text-main dark:text-dark-text-main hover:border-primary dark:hover:border-secondary hover:text-primary dark:hover:text-secondary transition-colors cursor-pointer"
        >
          <Upload size={14} strokeWidth={1.75} />
        </button>

        <input
          ref={fileInputRef}
          type="file"
          accept=".jpg,.jpeg,.png,.webp,image/jpeg,image/png,image/webp"
          onChange={handleFileSelected}
          className="sr-only"
          aria-label="Chọn tệp ảnh đại diện"
        />
      </div>

      {selectedImageUrl && (
        <AvatarCropModal
          key={selectedImageUrl}
          isOpen
          imageSrc={selectedImageUrl}
          userFullName={user.fullName}
          onClose={handleCloseModal}
          onChooseAnother={() => fileInputRef.current?.click()}
          onSaved={handleSaved}
        />
      )}
    </>
  )
}
