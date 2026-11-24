import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { ROUTES } from '@/routes/paths'
import { getSafeRedirectPath } from '@/utils/validation'
import { RegisterJournalistForm } from '../components/RegisterJournalistForm'
import { RegisterReaderForm } from '../components/RegisterReaderForm'

type RegisterTabValue = 'reader' | 'journalist'

export function RegisterPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()

  const initialTab: RegisterTabValue =
    searchParams.get('tab') === 'journalist' ? 'journalist' : 'reader'
  const [activeTab, setActiveTab] = useState<RegisterTabValue>(initialTab)

  const rawRedirect = searchParams.get('redirect')
  const safeRedirect = getSafeRedirectPath(rawRedirect, ROUTES.HOME)

  const loginHref =
    safeRedirect !== ROUTES.HOME
      ? `${ROUTES.AUTH.LOGIN}?redirect=${encodeURIComponent(safeRedirect)}`
      : ROUTES.AUTH.LOGIN

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
    <div className="space-y-6">
      <div className="space-y-1.5 text-center">
        <h1 className="text-2xl font-bold tracking-tight text-neutral-text-main dark:text-dark-text-main">
          Tạo tài khoản mới
        </h1>
        <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
          Chọn vai trò phù hợp để bắt đầu đọc tin tức hoặc tác nghiệp báo chí trên NewsHub.
        </p>
      </div>

      <Tabs
        value={activeTab}
        onValueChange={(val) => setActiveTab(val === 'journalist' ? 'journalist' : 'reader')}
      >
        <TabsList aria-label="Chọn loại tài khoản đăng ký">
          <TabsTrigger value="reader">Độc giả</TabsTrigger>
          <TabsTrigger value="journalist">Nhà báo</TabsTrigger>
        </TabsList>

        <TabsContent value="reader">
          <RegisterReaderForm onRegistered={handleRegistrationSuccess} />
        </TabsContent>

        <TabsContent value="journalist">
          <RegisterJournalistForm onRegistered={handleRegistrationSuccess} />
        </TabsContent>
      </Tabs>

      <p className="text-center text-xs text-neutral-text-muted dark:text-dark-text-muted">
        Đã có tài khoản NewsHub?{' '}
        <Link
          to={loginHref}
          className="font-semibold text-primary dark:text-secondary hover:underline"
        >
          Đăng nhập ngay
        </Link>
      </p>
    </div>
  )
}
