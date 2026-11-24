import { useLocation, useNavigate, useSearchParams } from 'react-router-dom'
import { ROUTES } from '@/routes/paths'
import { getSafeRedirectPath } from '@/utils/validation'
import { LoginForm } from '../components/LoginForm'

interface LocationRedirectState {
  from?: {
    pathname?: string
    search?: string
  }
}

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const [searchParams] = useSearchParams()

  const registered = searchParams.get('registered') === '1'
  const prefilledEmail = searchParams.get('email') ?? ''
  const rawRedirect = searchParams.get('redirect')

  const stateFrom = (location.state as LocationRedirectState | null)?.from
  const fallbackFromState = stateFrom?.pathname
    ? `${stateFrom.pathname}${stateFrom.search ?? ''}`
    : ROUTES.HOME

  const safeRedirect = getSafeRedirectPath(rawRedirect ?? fallbackFromState, ROUTES.HOME)

  const registerHref =
    safeRedirect !== ROUTES.HOME
      ? `${ROUTES.AUTH.REGISTER}?redirect=${encodeURIComponent(safeRedirect)}`
      : ROUTES.AUTH.REGISTER

  return (
    <div className="space-y-6">
      <div className="space-y-2 text-center">
        <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-neutral-text-main dark:text-dark-text-main">
          Đăng nhập
        </h1>
        <p className="mx-auto max-w-sm text-sm leading-relaxed text-neutral-text-muted dark:text-dark-text-muted">
          Chào mừng bạn quay lại hệ sinh thái báo điện tử và tòa soạn số NewsHub.
        </p>
      </div>

      <LoginForm
        key={prefilledEmail}
        initialEmail={prefilledEmail}
        showRegisteredBanner={registered}
        registerHref={registerHref}
        onSuccess={() => {
          navigate(safeRedirect, { replace: true })
        }}
      />
    </div>
  )
}
