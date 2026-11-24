import { AnimatePresence, motion } from 'framer-motion'
import { useEffect, useId, useState, type FormEvent } from 'react'
import { Badge } from '@/components/common/Badge'
import { Button } from '@/components/common/Button'
import { Input } from '@/components/common/Input'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Label } from '@/components/ui/label'
import { Separator } from '@/components/ui/separator'
import { useAuth } from '@/hooks/useAuth'
import { accountService } from '@/services/accountService'
import { toAppClientError } from '@/services/apiClient'
import { resolveUserRoleName, type UserProfile, type UserRoleName } from '@/types/auth'
import {
  BIO_MAX_LENGTH,
  extractFieldErrors,
  normalizeVietnamPhone,
  validateBio,
  validateFullName,
  validatePressCardNumber,
  validateVietnamPhone,
} from '@/utils/validation'
import { AvatarUploader } from '../components/AvatarUploader'
import { ChangePasswordForm } from '../components/ChangePasswordForm'

function getRoleBadgeMeta(role: UserRoleName): {
  label: string
  variant: 'neutral' | 'published' | 'category'
} {
  if (role === 'Admin') {
    return { label: 'Quản trị viên', variant: 'category' }
  }
  if (role === 'Journalist') {
    return { label: 'Nhà báo', variant: 'published' }
  }
  return { label: 'Độc giả', variant: 'neutral' }
}

function formatDateVi(isoString: string): string {
  const parsed = new Date(isoString)
  if (Number.isNaN(parsed.getTime())) return '—'
  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(parsed)
}

interface ProfileDetailsFormProps {
  user: UserProfile
  roleName: UserRoleName
  onProfileUpdated: (updated: UserProfile) => void
}

function ProfileDetailsForm({ user, roleName, onProfileUpdated }: ProfileDetailsFormProps) {
  const bioFieldId = useId()
  const isJournalist = roleName === 'Journalist'

  const [fullName, setFullName] = useState(user.fullName)
  const [phoneNumber, setPhoneNumber] = useState(user.phoneNumber ?? '')
  const [pressCardNumber, setPressCardNumber] = useState(user.pressCardNumber ?? '')
  const [bio, setBio] = useState(user.bio ?? '')
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  const normalizedPhonePreview = normalizeVietnamPhone(phoneNumber)
  const effectiveTypedPhone = phoneNumber.trim()
    ? (normalizedPhonePreview ?? phoneNumber.trim())
    : ''

  const isDirty =
    fullName.trim() !== user.fullName ||
    effectiveTypedPhone !== (user.phoneNumber ?? '') ||
    bio.trim() !== (user.bio ?? '') ||
    (isJournalist && pressCardNumber.trim() !== (user.pressCardNumber ?? ''))

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!isDirty || isSaving) return

    const nextErrors: Record<string, string> = {}
    const nameErr = validateFullName(fullName)
    const phoneErr = validateVietnamPhone(phoneNumber)
    const bioErr = validateBio(bio)
    const cardErr = isJournalist ? validatePressCardNumber(pressCardNumber, true) : null

    if (nameErr) nextErrors.fullName = nameErr
    if (phoneErr) nextErrors.phoneNumber = phoneErr
    if (bioErr) nextErrors.bio = bioErr
    if (cardErr) nextErrors.pressCardNumber = cardErr

    if (Object.keys(nextErrors).length > 0) {
      setFieldErrors(nextErrors)
      setFormError(null)
      setSuccessMsg(null)
      return
    }

    setFieldErrors({})
    setFormError(null)
    setSuccessMsg(null)
    setIsSaving(true)

    try {
      const updated = await accountService.updateProfile({
        fullName: fullName.trim(),
        phoneNumber: phoneNumber.trim(),
        bio: bio.trim(),
        ...(isJournalist ? { pressCardNumber: pressCardNumber.trim() } : {}),
      })
      onProfileUpdated(updated)
      setFullName(updated.fullName)
      setPhoneNumber(updated.phoneNumber ?? '')
      setPressCardNumber(updated.pressCardNumber ?? '')
      setBio(updated.bio ?? '')
      setSuccessMsg('Đã lưu thay đổi thông tin hồ sơ cá nhân.')
    } catch (err: unknown) {
      const clientErr = toAppClientError(err)
      if (clientErr.status === 422 && clientErr.details.length > 0) {
        setFieldErrors(extractFieldErrors(clientErr))
        return
      }
      setFormError(clientErr.message)
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <form onSubmit={(e) => void handleSubmit(e)} noValidate className="space-y-4">
      <div>
        <h2 className="text-base font-bold text-neutral-text-main dark:text-dark-text-main">
          Thông tin cá nhân
        </h2>
        <p className="mt-1 text-xs text-neutral-text-muted dark:text-dark-text-muted">
          Thông tin hiển thị công khai trên các hoạt động của bạn tại NewsHub.
        </p>
      </div>

      <AnimatePresence mode="wait">
        {formError && (
          <motion.div
            key="error"
            initial={{ opacity: 0, y: -4 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.16 }}
          >
            <Alert variant="destructive">
              <AlertDescription className="mt-0">{formError}</AlertDescription>
            </Alert>
          </motion.div>
        )}

        {successMsg && (
          <motion.div
            key="success"
            initial={{ opacity: 0, y: -4 }}
            animate={{ opacity: 1, y: 0 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.16 }}
          >
            <Alert variant="success" role="status">
              <AlertDescription className="mt-0">{successMsg}</AlertDescription>
            </Alert>
          </motion.div>
        )}
      </AnimatePresence>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Input
          name="fullName"
          label="Họ và tên *"
          value={fullName}
          onChange={(e) => {
            setFullName(e.target.value)
            setSuccessMsg(null)
            if (fieldErrors.fullName) setFieldErrors((prev) => ({ ...prev, fullName: '' }))
          }}
          error={fieldErrors.fullName}
          disabled={isSaving}
        />

        <Input
          name="email"
          type="email"
          label="Địa chỉ Email đăng nhập"
          value={user.email}
          disabled
          hint="Email định danh tài khoản và không thể thay đổi."
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Input
          name="phoneNumber"
          type="tel"
          label="Số điện thoại di động"
          placeholder="0912 345 678 hoặc +84912345678"
          value={phoneNumber}
          onChange={(e) => {
            setPhoneNumber(e.target.value)
            setSuccessMsg(null)
            if (fieldErrors.phoneNumber) setFieldErrors((prev) => ({ ...prev, phoneNumber: '' }))
          }}
          error={fieldErrors.phoneNumber}
          hint={
            normalizedPhonePreview
              ? `Chuẩn hóa E.164: ${normalizedPhonePreview}`
              : 'Để trống nếu bạn muốn xóa số điện thoại.'
          }
          disabled={isSaving}
        />

        {isJournalist && (
          <Input
            name="pressCardNumber"
            label="Số Thẻ Nhà Báo *"
            placeholder="NB-2026-00123"
            value={pressCardNumber}
            onChange={(e) => {
              setPressCardNumber(e.target.value)
              setSuccessMsg(null)
              if (fieldErrors.pressCardNumber) {
                setFieldErrors((prev) => ({ ...prev, pressCardNumber: '' }))
              }
            }}
            error={fieldErrors.pressCardNumber}
            disabled={isSaving}
          />
        )}
      </div>

      <div className="space-y-1.5">
        <div className="flex items-center justify-between">
          <Label htmlFor={bioFieldId}>Tiểu sử giới thiệu</Label>
          <span className="font-mono text-[11px] text-neutral-text-muted dark:text-dark-text-muted">
            {bio.length}/{BIO_MAX_LENGTH}
          </span>
        </div>
        <textarea
          id={bioFieldId}
          name="bio"
          rows={4}
          maxLength={BIO_MAX_LENGTH}
          placeholder="Giới thiệu ngắn gọn về bạn hoặc lĩnh vực chuyên môn..."
          value={bio}
          onChange={(e) => {
            setBio(e.target.value)
            setSuccessMsg(null)
            if (fieldErrors.bio) setFieldErrors((prev) => ({ ...prev, bio: '' }))
          }}
          disabled={isSaving}
          aria-invalid={Boolean(fieldErrors.bio)}
          className={`w-full rounded-[10px] border px-3.5 py-2.5 text-sm bg-neutral-surface dark:bg-dark-bg text-neutral-text-main dark:text-dark-text-main placeholder:text-neutral-text-muted dark:placeholder:text-dark-text-muted outline-none transition-colors duration-150 resize-y ${
            fieldErrors.bio
              ? 'border-status-rejected focus:border-status-rejected'
              : 'border-neutral-border dark:border-dark-border focus:border-primary dark:focus:border-secondary'
          }`}
        />
        {fieldErrors.bio && <p className="text-xs text-status-rejected">{fieldErrors.bio}</p>}
      </div>

      <div className="flex justify-end pt-1">
        <Button
          type="submit"
          variant="action"
          size="md"
          isLoading={isSaving}
          disabled={!isDirty}
        >
          Lưu thay đổi
        </Button>
      </div>
    </form>
  )
}

export function ProfilePage() {
  const { user, updateCurrentUser } = useAuth()

  useEffect(() => {
    let cancelled = false
    async function syncLatestProfile() {
      try {
        const latest = await accountService.getMyProfile()
        if (!cancelled) {
          updateCurrentUser(latest)
        }
      } catch {
        // Giữ nguyên state hiện tại nếu mạng chập chờn; apiClient đã tự xử lý 401 refresh.
      }
    }
    void syncLatestProfile()
    return () => {
      cancelled = true
    }
  }, [updateCurrentUser])

  if (!user) {
    return null
  }

  const resolvedRole = resolveUserRoleName(user.role)
  const roleBadge = getRoleBadgeMeta(resolvedRole)

  return (
    <div className="mx-auto w-full max-w-5xl space-y-6">
      <div className="space-y-1">
        <h1 className="text-2xl font-bold tracking-tight text-neutral-text-main dark:text-dark-text-main">
          Hồ sơ cá nhân
        </h1>
        <p className="text-sm text-neutral-text-muted dark:text-dark-text-muted">
          Quản lý thông tin định danh, ảnh đại diện và thiết lập bảo mật cho tài khoản NewsHub.
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        <aside className="lg:col-span-4 rounded-2xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-6 space-y-5">
          <AvatarUploader user={user} onAvatarUpdated={updateCurrentUser} />

          <Separator />

          <div className="space-y-3 text-xs">
            <div className="flex items-center justify-between gap-2">
              <span className="text-neutral-text-muted dark:text-dark-text-muted">Vai trò</span>
              <Badge variant={roleBadge.variant}>{roleBadge.label}</Badge>
            </div>

            <div className="flex items-center justify-between gap-2">
              <span className="text-neutral-text-muted dark:text-dark-text-muted">Trạng thái</span>
              <Badge variant={user.isActive ? 'published' : 'rejected'}>
                {user.isActive ? 'Đang hoạt động' : 'Đã tạm khóa'}
              </Badge>
            </div>

            <div className="flex items-center justify-between gap-2">
              <span className="text-neutral-text-muted dark:text-dark-text-muted">
                Ngày tham gia
              </span>
              <span className="font-mono font-medium text-neutral-text-main dark:text-dark-text-main">
                {formatDateVi(user.createdAt)}
              </span>
            </div>
          </div>

          {resolvedRole === 'Journalist' && (
            <>
              <Separator />
              <div className="space-y-2 text-xs">
                <div className="flex items-center justify-between gap-2">
                  <span className="font-semibold text-neutral-text-main dark:text-dark-text-main">
                    Thông tin Thẻ Nhà Báo
                  </span>
                  <Badge variant="published">Đã kích hoạt</Badge>
                </div>
                <div className="flex items-center justify-between gap-2">
                  <span className="text-neutral-text-muted dark:text-dark-text-muted">
                    Mã số thẻ
                  </span>
                  <span className="font-mono font-semibold text-neutral-text-main dark:text-dark-text-main">
                    {user.pressCardNumber || 'Chưa cập nhật'}
                  </span>
                </div>
                <p className="text-[11px] text-neutral-text-muted dark:text-dark-text-muted leading-relaxed">
                  Tài khoản Nhà báo được kích hoạt đầy đủ quyền soạn thảo, xuất bản bài viết tự do
                  và tham gia các Tòa soạn trên hệ thống.
                </p>
              </div>
            </>
          )}
        </aside>

        <div className="lg:col-span-8 space-y-6">
          <section className="rounded-2xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-6">
            <ProfileDetailsForm
              key={user.id}
              user={user}
              roleName={resolvedRole}
              onProfileUpdated={updateCurrentUser}
            />
          </section>

          <section className="rounded-2xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-6">
            <ChangePasswordForm />
          </section>
        </div>
      </div>
    </div>
  )
}
