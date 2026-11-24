import { useId, useState, type FormEvent } from 'react'
import { Button } from '@/components/common/Button'
import { Input } from '@/components/common/Input'
import { toAppClientError } from '@/services/apiClient'
import { authService } from '@/services/authService'
import {
  BIO_MAX_LENGTH,
  extractFieldErrors,
  normalizeVietnamPhone,
  validateBio,
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
  const bioFieldId = useId()
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [pressCardNumber, setPressCardNumber] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [bio, setBio] = useState('')
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const normalizedPhonePreview = normalizeVietnamPhone(phoneNumber)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (isSubmitting) return

    const nextErrors: Record<string, string> = {}
    const nameErr = validateFullName(fullName)
    const emailErr = validateEmail(email)
    const passErr = validatePassword(password)
    const cardErr = validatePressCardNumber(pressCardNumber, true)
    const phoneErr = validateVietnamPhone(phoneNumber)
    const bioErr = validateBio(bio)

    if (nameErr) nextErrors.fullName = nameErr
    if (emailErr) nextErrors.email = emailErr
    if (passErr) nextErrors.password = passErr
    if (cardErr) nextErrors.pressCardNumber = cardErr
    if (phoneErr) nextErrors.phoneNumber = phoneErr
    if (bioErr) nextErrors.bio = bioErr

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
        pressCardNumber: pressCardNumber.trim(),
        phoneNumber: phoneNumber.trim() ? phoneNumber.trim() : null,
        bio: bio.trim() ? bio.trim() : null,
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
      <div className="rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg px-3.5 py-2.5 text-xs text-neutral-text-muted dark:text-dark-text-muted leading-relaxed">
        Tài khoản Nhà báo yêu cầu nhập Số Thẻ Nhà Báo hợp lệ và được kích hoạt đầy đủ quyền soạn
        thảo, xuất bản tự do cũng như tham gia Tòa soạn ngay sau khi đăng ký.
      </div>

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
        placeholder="Trần Minh Khoa"
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
        placeholder="nhabao@toasoan.vn"
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

      <Input
        name="pressCardNumber"
        label="Số Thẻ Nhà Báo *"
        placeholder="Ví dụ: NB-2026-00123"
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

      <Input
        name="phoneNumber"
        type="tel"
        label="Số điện thoại di động"
        placeholder="0912 345 678 hoặc +84912345678"
        autoComplete="tel"
        value={phoneNumber}
        onChange={(e) => {
          setPhoneNumber(e.target.value)
          if (fieldErrors.phoneNumber) setFieldErrors((prev) => ({ ...prev, phoneNumber: '' }))
        }}
        error={fieldErrors.phoneNumber}
        hint={
          normalizedPhonePreview
            ? `Định dạng lưu trữ chuẩn quốc tế E.164: ${normalizedPhonePreview}`
            : 'Hỗ trợ đầu số di động Việt Nam (03, 05, 07, 08, 09).'
        }
        disabled={isSubmitting}
      />

      <div className="space-y-1.5">
        <div className="flex items-center justify-between">
          <label
            htmlFor={bioFieldId}
            className="block text-xs font-semibold text-neutral-text-main dark:text-dark-text-main"
          >
            Tiểu sử / Lĩnh vực chuyên môn
          </label>
          <span className="font-mono text-[11px] text-neutral-text-muted dark:text-dark-text-muted">
            {bio.length}/{BIO_MAX_LENGTH}
          </span>
        </div>
        <textarea
          id={bioFieldId}
          name="bio"
          rows={3}
          maxLength={BIO_MAX_LENGTH}
          placeholder="Phóng viên chuyên trách Kinh tế vĩ mô & Công nghệ..."
          value={bio}
          onChange={(e) => {
            setBio(e.target.value)
            if (fieldErrors.bio) setFieldErrors((prev) => ({ ...prev, bio: '' }))
          }}
          disabled={isSubmitting}
          aria-invalid={Boolean(fieldErrors.bio)}
          className={`w-full rounded-[10px] border px-3.5 py-2.5 text-sm bg-neutral-surface dark:bg-dark-bg text-neutral-text-main dark:text-dark-text-main placeholder:text-neutral-text-muted dark:placeholder:text-dark-text-muted outline-none transition-colors duration-150 resize-y ${
            fieldErrors.bio
              ? 'border-status-rejected focus:border-status-rejected'
              : 'border-neutral-border dark:border-dark-border focus:border-primary dark:focus:border-secondary'
          }`}
        />
        {fieldErrors.bio && <p className="text-xs text-status-rejected">{fieldErrors.bio}</p>}
      </div>

      <div className="pt-1">
        <Button type="submit" variant="primary" size="lg" fullWidth isLoading={isSubmitting}>
          Tạo tài khoản Nhà báo
        </Button>
      </div>
    </form>
  )
}
