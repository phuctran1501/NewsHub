import { useState, type FormEvent } from 'react'
import { Button } from '@/components/common/Button'
import { toast } from '@/components/ui/toastManager'
import { accountService } from '@/services/accountService'
import { toAppClientError } from '@/services/apiClient'
import { extractFieldErrors, validatePassword } from '@/utils/validation'
import { PasswordInput } from './PasswordInput'

export function ChangePasswordForm() {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
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
      return
    }

    setFieldErrors({})
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
      toast.success(
        'Đổi mật khẩu thành công',
        'Phiên đăng nhập trên các thiết bị khác vẫn được giữ nguyên.',
      )
    } catch (err: unknown) {
      const clientErr = toAppClientError(err)

      if (clientErr.code === 'AUTH_INVALID_CURRENT_PASSWORD') {
        setFieldErrors({
          currentPassword: 'Mật khẩu hiện tại không chính xác.',
        })
        toast.error('Mật khẩu hiện tại không đúng', 'Vui lòng kiểm tra và thử lại.')
        return
      }

      if (clientErr.status === 422 && clientErr.details.length > 0) {
        setFieldErrors(extractFieldErrors(clientErr))
        return
      }

      toast.error('Không thể đổi mật khẩu', clientErr.message)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={(e) => void handleSubmit(e)} noValidate className="space-y-5">
      <div>
        <h2 className="text-lg font-bold text-neutral-text-main dark:text-dark-text-main">
          Đổi mật khẩu
        </h2>
        <p className="mt-1 text-xs sm:text-sm text-neutral-text-muted dark:text-dark-text-muted">
          Cập nhật mật khẩu định kỳ để bảo vệ tài khoản. Thao tác này không làm gián đoạn phiên đăng
          nhập trên các thiết bị khác của bạn.
        </p>
      </div>

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

      <div className="grid grid-cols-1 md:grid-cols-2 gap-5 items-start">
        <PasswordInput
          name="newPassword"
          label="Mật khẩu mới *"
          placeholder="Nhập mật khẩu mới"
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

      <div className="flex justify-end border-t border-neutral-border dark:border-dark-border pt-5">
        <Button type="submit" variant="primary" size="md" isLoading={isSubmitting}>
          Cập nhật mật khẩu
        </Button>
      </div>
    </form>
  )
}
