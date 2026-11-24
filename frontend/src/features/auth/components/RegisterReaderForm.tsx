import { useState, type FormEvent } from 'react'
import { Button } from '@/components/common/Button'
import { Input } from '@/components/common/Input'
import { toAppClientError } from '@/services/apiClient'
import { authService } from '@/services/authService'
import {
  extractFieldErrors,
  validateEmail,
  validateFullName,
  validatePassword,
} from '@/utils/validation'
import { PasswordInput } from './PasswordInput'

interface RegisterReaderFormProps {
  onRegistered: (registeredEmail: string) => void
}

export function RegisterReaderForm({ onRegistered }: RegisterReaderFormProps) {
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (isSubmitting) return

    const nextErrors: Record<string, string> = {}
    const nameErr = validateFullName(fullName)
    const emailErr = validateEmail(email)
    const passErr = validatePassword(password)

    if (nameErr) nextErrors.fullName = nameErr
    if (emailErr) nextErrors.email = emailErr
    if (passErr) nextErrors.password = passErr

    if (Object.keys(nextErrors).length > 0) {
      setFieldErrors(nextErrors)
      setFormError(null)
      return
    }

    setFieldErrors({})
    setFormError(null)
    setIsSubmitting(true)

    try {
      const normalizedEmail = email.trim()
      await authService.registerReader({
        fullName: fullName.trim(),
        email: normalizedEmail,
        password,
      })
      onRegistered(normalizedEmail)
    } catch (err: unknown) {
      const clientErr = toAppClientError(err)

      if (clientErr.status === 409 || clientErr.code === 'EMAIL_ALREADY_EXISTS') {
        setFieldErrors({
          email: 'Địa chỉ email này đã được đăng ký. Vui lòng dùng email khác hoặc đăng nhập.',
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
      {formError && (
        <div
          role="alert"
          className="rounded-[10px] border border-status-rejected/40 bg-status-rejected/10 px-3.5 py-2.5 text-xs text-status-rejected leading-relaxed"
        >
          {formError}
        </div>
      )}

      <Input
        name="fullName"
        label="Họ và tên *"
        placeholder="Nguyễn Văn An"
        autoComplete="name"
        value={fullName}
        onChange={(e) => {
          setFullName(e.target.value)
          if (fieldErrors.fullName) setFieldErrors((prev) => ({ ...prev, fullName: '' }))
        }}
        error={fieldErrors.fullName}
        disabled={isSubmitting}
      />

      <Input
        name="email"
        type="email"
        label="Địa chỉ Email *"
        placeholder="docgia@example.com"
        autoComplete="email"
        value={email}
        onChange={(e) => {
          setEmail(e.target.value)
          if (fieldErrors.email) setFieldErrors((prev) => ({ ...prev, email: '' }))
        }}
        error={fieldErrors.email}
        disabled={isSubmitting}
      />

      <PasswordInput
        name="password"
        label="Mật khẩu *"
        placeholder="Tối thiểu 8 ký tự, gồm chữ hoa, thường và số"
        autoComplete="new-password"
        value={password}
        onChange={(e) => {
          setPassword(e.target.value)
          if (fieldErrors.password) setFieldErrors((prev) => ({ ...prev, password: '' }))
        }}
        error={fieldErrors.password}
        disabled={isSubmitting}
        showStrengthChecklist
      />

      <div className="pt-1">
        <Button type="submit" variant="primary" size="lg" fullWidth isLoading={isSubmitting}>
          Tạo tài khoản Độc giả
        </Button>
      </div>
    </form>
  )
}
