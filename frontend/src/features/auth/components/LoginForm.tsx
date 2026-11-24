import { useEffect, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { Input } from '@/components/common/Input'
import { useAuth } from '@/hooks/useAuth'
import { toAppClientError } from '@/services/apiClient'
import type { UserProfile } from '@/types/auth'
import { extractFieldErrors, validateEmail } from '@/utils/validation'
import { PasswordInput } from './PasswordInput'

interface LoginFormProps {
  initialEmail?: string
  showRegisteredBanner?: boolean
  registerHref?: string
  onSuccess?: (user: UserProfile) => void
}

const RATE_LIMIT_COOLDOWN_SECONDS = 60

export function LoginForm({
  initialEmail = '',
  showRegisteredBanner = false,
  registerHref,
  onSuccess,
}: LoginFormProps) {
  const { login } = useAuth()
  const [email, setEmail] = useState(initialEmail)
  const [password, setPassword] = useState('')
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [formError, setFormError] = useState<string | null>(null)
  const [rateLimitSeconds, setRateLimitSeconds] = useState(0)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const isRateLimited = rateLimitSeconds > 0

  useEffect(() => {
    if (!isRateLimited) return

    const timerId = window.setInterval(() => {
      setRateLimitSeconds((prev) => (prev <= 1 ? 0 : prev - 1))
    }, 1000)

    return () => {
      window.clearInterval(timerId)
    }
  }, [isRateLimited])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (isRateLimited || isSubmitting) return

    const nextErrors: Record<string, string> = {}
    const emailError = validateEmail(email)
    if (emailError) nextErrors.email = emailError
    if (!password) nextErrors.password = 'Vui lòng nhập mật khẩu.'

    if (Object.keys(nextErrors).length > 0) {
      setFieldErrors(nextErrors)
      setFormError(null)
      return
    }

    setFieldErrors({})
    setFormError(null)
    setIsSubmitting(true)

    try {
      const authenticatedUser = await login({
        email: email.trim(),
        password,
      })
      onSuccess?.(authenticatedUser)
    } catch (err: unknown) {
      const clientErr = toAppClientError(err)

      if (clientErr.status === 429 || clientErr.code === 'TOO_MANY_REQUESTS') {
        setRateLimitSeconds(RATE_LIMIT_COOLDOWN_SECONDS)
        setFormError(null)
        return
      }

      if (clientErr.status === 422 && clientErr.details.length > 0) {
        setFieldErrors(extractFieldErrors(clientErr))
        return
      }

      if (
        clientErr.status === 401 ||
        clientErr.code === 'INVALID_CREDENTIALS' ||
        clientErr.code === 'AUTH_INVALID_CREDENTIALS'
      ) {
        setFormError('Email hoặc mật khẩu không chính xác. Vui lòng kiểm tra lại.')
        return
      }

      setFormError(clientErr.message)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={(e) => void handleSubmit(e)} noValidate className="space-y-4">
      {showRegisteredBanner && !formError && !isRateLimited && (
        <div
          role="status"
          className="rounded-[10px] border border-status-published/35 bg-status-published/10 px-3.5 py-2.5 text-xs text-emerald-800 dark:text-emerald-300 leading-relaxed"
        >
          Đăng ký tài khoản thành công. Vui lòng nhập mật khẩu để đăng nhập vào hệ thống.
        </div>
      )}

      {isRateLimited && (
        <div
          role="alert"
          aria-live="polite"
          className="rounded-[10px] border border-status-pending/45 bg-status-pending/10 px-3.5 py-2.5 text-xs text-amber-800 dark:text-amber-300 leading-relaxed"
        >
          <p className="font-semibold">Tạm khóa đăng nhập do thao tác quá nhanh</p>
          <p className="mt-0.5">
            Bạn đã thử đăng nhập vượt quá giới hạn 5 lần/phút. Vui lòng đợi{' '}
            <span className="font-mono font-bold">{rateLimitSeconds}s</span> trước khi thử lại.
          </p>
        </div>
      )}

      {formError && !isRateLimited && (
        <div
          role="alert"
          className="rounded-[10px] border border-status-rejected/40 bg-status-rejected/10 px-3.5 py-2.5 text-xs text-status-rejected leading-relaxed"
        >
          {formError}
        </div>
      )}

      <Input
        name="email"
        type="email"
        label="Địa chỉ Email"
        placeholder="banbienTap@newshub.vn"
        autoComplete="email"
        value={email}
        onChange={(e) => {
          setEmail(e.target.value)
          if (fieldErrors.email) {
            setFieldErrors((prev) => ({ ...prev, email: '' }))
          }
        }}
        error={fieldErrors.email}
        disabled={isSubmitting || isRateLimited}
      />

      <PasswordInput
        name="password"
        label="Mật khẩu"
        placeholder="Nhập mật khẩu của bạn"
        autoComplete="current-password"
        value={password}
        onChange={(e) => {
          setPassword(e.target.value)
          if (fieldErrors.password) {
            setFieldErrors((prev) => ({ ...prev, password: '' }))
          }
        }}
        error={fieldErrors.password}
        disabled={isSubmitting || isRateLimited}
      />

      <div className="pt-1">
        <Button
          type="submit"
          variant="primary"
          size="lg"
          fullWidth
          isLoading={isSubmitting}
          disabled={isRateLimited}
        >
          {isRateLimited ? `Thử lại sau ${rateLimitSeconds}s` : 'Đăng nhập'}
        </Button>
      </div>

      {registerHref && (
        <p className="pt-2 text-center text-xs text-neutral-text-muted dark:text-dark-text-muted">
          Chưa có tài khoản NewsHub?{' '}
          <Link
            to={registerHref}
            className="font-semibold text-primary dark:text-secondary hover:underline"
          >
            Đăng ký tài khoản mới
          </Link>
        </p>
      )}
    </form>
  )
}
