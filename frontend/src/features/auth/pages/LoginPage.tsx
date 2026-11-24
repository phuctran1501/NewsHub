import { motion, useReducedMotion } from 'framer-motion'
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
  const prefersReducedMotion = useReducedMotion()

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
      <motion.div
        initial={prefersReducedMotion ? false : { opacity: 0, y: 8 }}
        animate={{ opacity: 1, y: 0 }}
        transition={{ duration: 0.22, ease: 'easeOut' }}
        className="space-y-3 text-center"
      >
        <h1 className="text-[32px] sm:text-[40px] font-extrabold leading-[1.12] tracking-[-0.03em] text-neutral-text-main dark:text-dark-text-main">
          Đăng nhập <span className="text-primary dark:text-secondary">tài khoản</span>
        </h1>
        <div
          aria-hidden="true"
          className="mx-auto h-1 w-12 rounded-full bg-primary dark:bg-secondary"
        />
        <p className="mx-auto max-w-sm text-sm sm:text-[15px] leading-relaxed text-neutral-text-muted dark:text-dark-text-muted">
          Chào mừng bạn quay lại hệ sinh thái báo điện tử và tòa soạn số NewsHub.
        </p>
      </motion.div>

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
