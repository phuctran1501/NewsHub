import { AnimatePresence, motion } from 'framer-motion'
import { useState, type FormEvent } from 'react'
import { Button } from '@/components/common/Button'
import { Input } from '@/components/common/Input'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { toAppClientError } from '@/services/apiClient'
import { authService } from '@/services/authService'
import {
  extractFieldErrors,
  validateEmail,
  validateFullName,
  validatePassword,
  validatePressCardNumber,
  validateVietnamPhone,
} from '@/utils/validation'
import { PasswordInput } from './PasswordInput'

interface RegisterJournalistFormProps {
  onRegistered: (registeredEmail: string) => void
}

export function RegisterJournalistForm({ onRegistered }: RegisterJournalistFormProps) {
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [pressCardNumber, setPressCardNumber] = useState('')
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
    const phoneErr = validateVietnamPhone(phoneNumber)
    const cardErr = validatePressCardNumber(pressCardNumber, true)

    if (nameErr) nextErrors.fullName = nameErr
    if (emailErr) nextErrors.email = emailErr
    if (passErr) nextErrors.password = passErr
    if (phoneErr) nextErrors.phoneNumber = phoneErr
    if (cardErr) nextErrors.pressCardNumber = cardErr

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
      await authService.registerJournalist({
        fullName: fullName.trim(),
        email: normalizedEmail,
        password,
        phoneNumber: phoneNumber.trim() ? phoneNumber.trim() : null,
        pressCardNumber: pressCardNumber.trim(),
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
    <form onSubmit={(e) => void handleSubmit(e)} noValidate className="space-y-3.5">
      <AnimatePresence mode="wait">
        {formError && (
          <motion.div
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
      </AnimatePresence>

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
        placeholder="name@example.com"
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
        placeholder="Nhập mật khẩu"
        autoComplete="new-password"
        value={password}
        onChange={(e) => {
          setPassword(e.target.value)
          if (fieldErrors.password) setFieldErrors((prev) => ({ ...prev, password: '' }))
        }}
        error={fieldErrors.password}
        disabled={isSubmitting}
      />

      <Input
        name="phoneNumber"
        type="tel"
        label="Số điện thoại"
        placeholder="0912 345 678"
        autoComplete="tel"
        value={phoneNumber}
        onChange={(e) => {
          setPhoneNumber(e.target.value)
          if (fieldErrors.phoneNumber) setFieldErrors((prev) => ({ ...prev, phoneNumber: '' }))
        }}
        error={fieldErrors.phoneNumber}
        disabled={isSubmitting}
      />

      <Input
        name="pressCardNumber"
        label="Số thẻ nhà báo *"
        placeholder="NB-2026-00123"
        value={pressCardNumber}
        onChange={(e) => {
          setPressCardNumber(e.target.value)
          if (fieldErrors.pressCardNumber) {
            setFieldErrors((prev) => ({ ...prev, pressCardNumber: '' }))
          }
        }}
        error={fieldErrors.pressCardNumber}
        disabled={isSubmitting}
      />

      <div className="pt-1">
        <Button type="submit" variant="primary" size="lg" fullWidth isLoading={isSubmitting}>
          Tạo tài khoản Nhà báo
        </Button>
      </div>
    </form>
  )
}
