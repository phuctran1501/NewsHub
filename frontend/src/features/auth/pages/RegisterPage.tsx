import { AnimatePresence, motion, useReducedMotion } from 'framer-motion'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { Tabs, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { ROUTES } from '@/routes/paths'
import { getSafeRedirectPath } from '@/utils/validation'
import { RegisterJournalistForm } from '../components/RegisterJournalistForm'
import { RegisterReaderForm } from '../components/RegisterReaderForm'

type RegisterTabValue = 'reader' | 'journalist'

const REGISTER_TABS: ReadonlyArray<{ value: RegisterTabValue; label: string }> = [
  { value: 'reader', label: 'Độc giả' },
  { value: 'journalist', label: 'Nhà báo' },
]

export function RegisterPage() {
  const navigate = useNavigate()
  const [searchParams, setSearchParams] = useSearchParams()
  const prefersReducedMotion = useReducedMotion()

  const activeTab: RegisterTabValue =
    searchParams.get('tab') === 'journalist' ? 'journalist' : 'reader'

  const rawRedirect = searchParams.get('redirect')
  const safeRedirect = getSafeRedirectPath(rawRedirect, ROUTES.HOME)

  const loginHref =
    safeRedirect !== ROUTES.HOME
      ? `${ROUTES.AUTH.LOGIN}?redirect=${encodeURIComponent(safeRedirect)}`
      : ROUTES.AUTH.LOGIN

  function handleTabChange(nextValue: string) {
    const nextTab: RegisterTabValue = nextValue === 'journalist' ? 'journalist' : 'reader'
    const nextParams = new URLSearchParams(searchParams)
    if (nextTab === 'journalist') {
      nextParams.set('tab', 'journalist')
    } else {
      nextParams.delete('tab')
    }
    setSearchParams(nextParams, { replace: true })
  }

  function handleRegistrationSuccess(registeredEmail: string) {
    const params = new URLSearchParams()
    params.set('registered', '1')
    params.set('email', registeredEmail)
    if (safeRedirect !== ROUTES.HOME) {
      params.set('redirect', safeRedirect)
    }
    navigate(`${ROUTES.AUTH.LOGIN}?${params.toString()}`, { replace: true })
  }

  return (
    <div className="space-y-5">
      <div className="space-y-2 text-center">
        <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-neutral-text-main dark:text-dark-text-main">
          Tạo tài khoản
        </h1>
        <p className="mx-auto max-w-sm text-sm leading-relaxed text-neutral-text-muted dark:text-dark-text-muted">
          Chọn vai trò để tham gia cộng đồng độc giả hoặc bắt đầu tác nghiệp báo chí.
        </p>
      </div>

      <Tabs value={activeTab} onValueChange={handleTabChange} className="w-full">
        <TabsList
          aria-label="Chọn vai trò đăng ký"
          className="bg-neutral-surface dark:bg-dark-bg p-1"
        >
          {REGISTER_TABS.map((tab) => {
            const isSelected = activeTab === tab.value
            return (
              <TabsTrigger
                key={tab.value}
                value={tab.value}
                className="relative z-10 data-[state=active]:bg-transparent dark:data-[state=active]:bg-transparent data-[state=active]:text-white dark:data-[state=active]:text-dark-bg"
              >
                {isSelected && (
                  <motion.span
                    layoutId="register-role-tab-pill"
                    transition={
                      prefersReducedMotion
                        ? { duration: 0 }
                        : { type: 'spring', stiffness: 430, damping: 34 }
                    }
                    aria-hidden="true"
                    className="absolute inset-0 rounded-[8px] bg-primary dark:bg-secondary"
                  />
                )}
                <span className="relative z-10">{tab.label}</span>
              </TabsTrigger>
            )
          })}
        </TabsList>

        <div className="mt-5">
          <AnimatePresence mode="wait" initial={false}>
            <motion.div
              key={activeTab}
              initial={prefersReducedMotion ? { opacity: 1 } : { opacity: 0, y: 6 }}
              animate={{ opacity: 1, y: 0 }}
              exit={prefersReducedMotion ? { opacity: 0 } : { opacity: 0, y: -4 }}
              transition={{ duration: 0.16, ease: 'easeOut' }}
            >
              {activeTab === 'reader' ? (
                <RegisterReaderForm onRegistered={handleRegistrationSuccess} />
              ) : (
                <RegisterJournalistForm onRegistered={handleRegistrationSuccess} />
              )}
            </motion.div>
          </AnimatePresence>
        </div>
      </Tabs>

      <p className="pt-1 text-center text-xs sm:text-sm text-neutral-text-muted dark:text-dark-text-muted">
        Đã có tài khoản?{' '}
        <Link
          to={loginHref}
          className="font-bold text-neutral-text-main dark:text-dark-text-main hover:text-primary dark:hover:text-secondary transition-colors"
        >
          Đăng nhập ngay
        </Link>
      </p>
    </div>
  )
}
