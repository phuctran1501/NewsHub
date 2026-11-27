import { AnimatePresence, motion, useReducedMotion } from 'framer-motion'
import { useEffect, useId, useRef, useState, type FormEvent } from 'react'
import { Badge } from '@/components/common/Badge'
import { Button } from '@/components/common/Button'
import { Input } from '@/components/common/Input'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Label } from '@/components/ui/label'
import { useAuth } from '@/hooks/useAuth'
import { accountService } from '@/services/accountService'
import { toAppClientError } from '@/services/apiClient'
import { resolveUserRoleName, type UserProfile, type UserRoleName } from '@/types/auth'
import { cn } from '@/utils/cn'
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

type ProfileTabKey = 'info' | 'security'

const PROFILE_TABS: ReadonlyArray<{ key: ProfileTabKey; label: string }> = [
  { key: 'info', label: 'Thông tin cá nhân' },
  { key: 'security', label: 'Bảo mật & Mật khẩu' },
]

function getRoleSummary(role: UserRoleName, pressCardNumber: string | null): {
  label: string
  variant: 'neutral' | 'category'
} {
  if (role === 'Admin') {
    return { label: 'Quản trị viên', variant: 'category' }
  }
  if (role === 'Journalist') {
    return {
      label: pressCardNumber ? `Nhà báo • ${pressCardNumber}` : 'Nhà báo',
      variant: 'category',
    }
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

  function handleReset() {
    setFullName(user.fullName)
    setPhoneNumber(user.phoneNumber ?? '')
    setPressCardNumber(user.pressCardNumber ?? '')
    setBio(user.bio ?? '')
    setFieldErrors({})
    setFormError(null)
    setSuccessMsg(null)
  }

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
    <form onSubmit={(e) => void handleSubmit(e)} noValidate className="space-y-5">
      <div>
        <h2 className="text-lg font-bold text-neutral-text-main dark:text-dark-text-main">
          Thông tin cá nhân
        </h2>
        <p className="mt-1 text-xs sm:text-sm text-neutral-text-muted dark:text-dark-text-muted">
          Cập nhật thông tin định danh và tiểu sử hiển thị công khai trên hệ thống NewsHub.
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

      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
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
          label="Địa chỉ Email"
          value={user.email}
          disabled
          hint="Email định danh tài khoản và không thể thay đổi."
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        <Input
          name="phoneNumber"
          type="tel"
          label="Số điện thoại"
          placeholder="0912 345 678"
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
            label="Số thẻ nhà báo *"
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

      <div className="flex items-center justify-end gap-2.5 border-t border-neutral-border dark:border-dark-border pt-5">
        {isDirty && (
          <Button type="button" variant="outline" size="md" disabled={isSaving} onClick={handleReset}>
            Hoàn tác
          </Button>
        )}
        <Button type="submit" variant="primary" size="md" isLoading={isSaving} disabled={!isDirty}>
          Lưu thay đổi
        </Button>
      </div>
    </form>
  )
}

export function ProfilePage() {
  const { user, updateCurrentUser } = useAuth()
  const prefersReducedMotion = useReducedMotion()
  const avatarFileInputRef = useRef<HTMLInputElement | null>(null)
  const [activeTab, setActiveTab] = useState<ProfileTabKey>('info')
  const [avatarFeedback, setAvatarFeedback] = useState<{
    type: 'success' | 'error'
    message: string
  } | null>(null)

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
  const roleSummary = getRoleSummary(resolvedRole, user.pressCardNumber)

  return (
    <div className="mx-auto w-full max-w-6xl px-6 lg:px-12 py-8 lg:py-10 space-y-6">
      <section className="overflow-hidden rounded-2xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface">
        <div className="relative h-24 bg-primary dark:bg-[#122a52] overflow-hidden">
          <svg
            aria-hidden="true"
            width="160"
            height="160"
            viewBox="0 0 40 40"
            fill="none"
            className="pointer-events-none absolute right-6 -top-5 opacity-15"
          >
            <path d="M16.5 5L27 5L35 13V14H16.5V5Z" fill="#a6e1fa" />
            <path d="M35 16.5V27L27 35H26V16.5H35Z" fill="#a6e1fa" />
            <path d="M23.5 35H13L5 27V26H23.5V35Z" fill="#a6e1fa" />
            <path d="M5 23.5V13L13 5H14V23.5H5Z" fill="#a6e1fa" />
          </svg>
        </div>

        <div className="px-6 sm:px-8 pb-6">
          <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
            <div className="flex flex-col sm:flex-row sm:items-start gap-4 sm:gap-5 relative z-10">
              <div className="-mt-12">
                <AvatarUploader
                  user={user}
                  fileInputRef={avatarFileInputRef}
                  onAvatarUpdated={updateCurrentUser}
                  onFeedback={setAvatarFeedback}
                />
              </div>

              <div className="space-y-1.5 pt-1 sm:pt-3.5">
                <h1 className="text-xl sm:text-2xl font-extrabold tracking-tight text-neutral-text-main dark:text-dark-text-main">
                  {user.fullName}
                </h1>
                <div className="flex flex-wrap items-center gap-2 text-xs sm:text-sm text-neutral-text-muted dark:text-dark-text-muted">
                  <span>{user.email}</span>
                  <span aria-hidden="true">•</span>
                  <Badge variant={roleSummary.variant}>{roleSummary.label}</Badge>
                </div>
              </div>
            </div>

            <div className="sm:pt-4">
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => avatarFileInputRef.current?.click()}
              >
                Đổi ảnh đại diện
              </Button>
            </div>
          </div>

          <AnimatePresence mode="wait">
            {avatarFeedback && (
              <motion.div
                key={avatarFeedback.message}
                initial={{ opacity: 0, y: -4 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0 }}
                transition={{ duration: 0.16 }}
                className="mt-4"
              >
                <Alert
                  variant={avatarFeedback.type === 'success' ? 'success' : 'destructive'}
                  role={avatarFeedback.type === 'success' ? 'status' : 'alert'}
                >
                  <AlertDescription className="mt-0">{avatarFeedback.message}</AlertDescription>
                </Alert>
              </motion.div>
            )}
          </AnimatePresence>
        </div>
      </section>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        <aside
          aria-label="Danh mục cài đặt hồ sơ"
          className="lg:col-span-3 rounded-2xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-2"
        >
          <nav className="flex lg:flex-col gap-1">
            {PROFILE_TABS.map((tab) => {
              const isSelected = activeTab === tab.key
              return (
                <button
                  key={tab.key}
                  type="button"
                  onClick={() => setActiveTab(tab.key)}
                  className={cn(
                    'relative flex-1 lg:flex-none text-center lg:text-left px-3.5 py-2.5 rounded-[10px] text-xs sm:text-sm font-semibold transition-colors cursor-pointer',
                    isSelected
                      ? 'text-primary dark:text-secondary'
                      : 'text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main hover:bg-neutral-bg dark:hover:bg-dark-border/40',
                  )}
                >
                  {isSelected && (
                    <motion.span
                      layoutId="profile-settings-active-tab"
                      transition={
                        prefersReducedMotion
                          ? { duration: 0 }
                          : { type: 'spring', stiffness: 430, damping: 34 }
                      }
                      aria-hidden="true"
                      className="absolute inset-0 rounded-[10px] bg-sidebar-active dark:bg-dark-border"
                    />
                  )}
                  <span className="relative z-10">{tab.label}</span>
                </button>
              )
            })}
          </nav>

          <div className="hidden lg:flex flex-col gap-2 mt-2 border-t border-neutral-border dark:border-dark-border px-3.5 pt-3.5 pb-2 text-xs text-neutral-text-muted dark:text-dark-text-muted">
            <div className="flex items-center justify-between gap-2">
              <span>Trạng thái</span>
              <span className="font-semibold text-neutral-text-main dark:text-dark-text-main">
                {user.isActive ? 'Đang hoạt động' : 'Đã tạm khóa'}
              </span>
            </div>
            <div className="flex items-center justify-between gap-2">
              <span>Tham gia</span>
              <span className="font-mono font-medium text-neutral-text-main dark:text-dark-text-main">
                {formatDateVi(user.createdAt)}
              </span>
            </div>
          </div>
        </aside>

        <section className="lg:col-span-9 rounded-2xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-6 sm:p-8">
          <AnimatePresence mode="wait" initial={false}>
            {activeTab === 'info' ? (
              <motion.div
                key="profile-info-tab"
                initial={prefersReducedMotion ? { opacity: 1 } : { opacity: 0, y: 6 }}
                animate={{ opacity: 1, y: 0 }}
                exit={prefersReducedMotion ? { opacity: 0 } : { opacity: 0, y: -4 }}
                transition={{ duration: 0.16, ease: 'easeOut' }}
              >
                <ProfileDetailsForm
                  key={user.id}
                  user={user}
                  roleName={resolvedRole}
                  onProfileUpdated={updateCurrentUser}
                />
              </motion.div>
            ) : (
              <motion.div
                key="profile-security-tab"
                initial={prefersReducedMotion ? { opacity: 1 } : { opacity: 0, y: 6 }}
                animate={{ opacity: 1, y: 0 }}
                exit={prefersReducedMotion ? { opacity: 0 } : { opacity: 0, y: -4 }}
                transition={{ duration: 0.16, ease: 'easeOut' }}
              >
                <ChangePasswordForm />
              </motion.div>
            )}
          </AnimatePresence>
        </section>
      </div>
    </div>
  )
}
