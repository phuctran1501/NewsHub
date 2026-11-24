import { useState, type FormEvent } from 'react'
import { Button } from '@/components/common/Button'
import { accountService } from '@/services/accountService'
import { toAppClientError } from '@/services/apiClient'
import { extractFieldErrors, validatePassword } from '@/utils/validation'
import { PasswordInput } from './PasswordInput'

export function ChangePasswordForm() {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (isSubmitting) return

    const nextErrors: Record<string, string> = {}
    if (!currentPassword) {
      nextErrors.currentPassword = 'Vui lòng nhập mật khẩu hiện tại.'
    }

    const newPassErr = validatePassword(newPassword)
    if (newPassErr) {
      nextErrors.newPassword = newPassErr
    } else if (newPassword === currentPassword) {
      nextErrors.newPassword = 'Mật khẩu mới phải khác mật khẩu hiện tại.'
    }

    if (!confirmPassword) {
      nextErrors.confirmPassword = 'Vui lòng nhập lại mật khẩu mới.'
    } else if (confirmPassword !== newPassword) {
      nextErrors.confirmPassword = 'Xác nhận mật khẩu mới không khớp.'
    }

    if (Object.keys(nextErrors).length > 0) {
      setFieldErrors(nextErrors)
      setFormError(null)
      setSuccessMsg(null)
      return
    }

    setFieldErrors({})
    setFormError(null)
    setSuccessMsg(null)
    setIsSubmitting(true)

    try {
      await accountService.changePassword({
        currentPassword,
        newPassword,
        confirmPassword,
      })
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
      setSuccessMsg(
        'Đổi mật khẩu thành công. Các phiên đăng nhập hiện có trên thiết bị khác vẫn được giữ nguyên.',
      )
    } catch (err: unknown) {
      const clientErr = toAppClientError(err)

      if (clientErr.code === 'AUTH_INVALID_CURRENT_PASSWORD') {
        setFieldErrors({
          currentPassword: 'Mật khẩu hiện tại không chính xác.',
        })
        return
      }

      if (clientErr.status === 422 && clientErr.details.length > 0) {
        setFieldErrors(extractFieldErrors(clientErr))
        return
      }

      setFormError(clientErr.message)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={(e) => void handleSubmit(e)} noValidate className="space-y-4">
      <div>
        <h2 className="text-base font-bold text-neutral-text-main dark:text-dark-text-main">
          Đổi mật khẩu
        </h2>
        <p className="mt-1 text-xs text-neutral-text-muted dark:text-dark-text-muted">
          Cập nhật mật khẩu định kỳ để bảo vệ tài khoản. Thao tác này không làm gián đoạn phiên đăng
          nhập trên các thiết bị khác của bạn.
        </p>
      </div>

      {formError && (
        <div
          role="alert"
          className="rounded-[10px] border border-status-rejected/40 bg-status-rejected/10 px-3.5 py-2.5 text-xs text-status-rejected leading-relaxed"
        >
          {formError}
        </div>
      )}

      {successMsg && (
        <div
          role="status"
          className="rounded-[10px] border border-status-published/35 bg-status-published/10 px-3.5 py-2.5 text-xs text-emerald-800 dark:text-emerald-300 leading-relaxed"
        >
          {successMsg}
        </div>
      )}

      <PasswordInput
        name="currentPassword"
        label="Mật khẩu hiện tại *"
        placeholder="Nhập mật khẩu đang sử dụng"
        autoComplete="current-password"
        value={currentPassword}
        onChange={(e) => {
          setCurrentPassword(e.target.value)
          if (fieldErrors.currentPassword) {
            setFieldErrors((prev) => ({ ...prev, currentPassword: '' }))
          }
        }}
        error={fieldErrors.currentPassword}
        disabled={isSubmitting}
      />

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4 items-start">
        <PasswordInput
          name="newPassword"
          label="Mật khẩu mới *"
          placeholder="Tối thiểu 8 ký tự"
          autoComplete="new-password"
          value={newPassword}
          onChange={(e) => {
            setNewPassword(e.target.value)
            if (fieldErrors.newPassword) {
              setFieldErrors((prev) => ({ ...prev, newPassword: '' }))
            }
          }}
          error={fieldErrors.newPassword}
          disabled={isSubmitting}
          showStrengthChecklist
        />

        <PasswordInput
          name="confirmPassword"
          label="Xác nhận mật khẩu mới *"
          placeholder="Nhập lại mật khẩu mới"
          autoComplete="new-password"
          value={confirmPassword}
          onChange={(e) => {
            setConfirmPassword(e.target.value)
            if (fieldErrors.confirmPassword) {
              setFieldErrors((prev) => ({ ...prev, confirmPassword: '' }))
            }
          }}
          error={fieldErrors.confirmPassword}
          disabled={isSubmitting}
        />
      </div>

      <div className="flex justify-end pt-1">
        <Button type="submit" variant="primary" size="md" isLoading={isSubmitting}>
          Cập nhật mật khẩu
        </Button>
      </div>
    </form>
  )
}
