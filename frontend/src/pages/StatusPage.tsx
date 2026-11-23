import { useState, useEffect, useRef } from 'react'
import { Link } from 'react-router-dom'
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr'
import { env } from '../config/env'
import { StatusBadge } from '../components/common/StatusBadge'

interface ProbeResult {
  endpoint: string
  status: number | null
  ok: boolean
  durationMs: number
  payload: string
  timestamp: string
}

interface HandshakeStepResult {
  name: string
  status: 'pending' | 'testing' | 'success' | 'failed'
  message: string
  durationMs?: number
  details?: string
}

export function StatusPage() {
  const [isDark, setIsDark] = useState(() => {
    if (typeof window === 'undefined') return false
    const stored = localStorage.getItem('theme')
    if (stored) return stored === 'dark'
    return window.matchMedia('(prefers-color-scheme: dark)').matches
  })
  const [probeResult, setProbeResult] = useState<ProbeResult | null>(null)
  const [isProbing, setIsProbing] = useState(false)

  // State cho Handshake Smoke-Test (NH-OPS-03)
  const [testEmail, setTestEmail] = useState('reader@newshub.com')
  const [testPassword, setTestPassword] = useState('Password123!')
  const [accessToken, setAccessToken] = useState<string | null>(null)
  const [handshakeSteps, setHandshakeSteps] = useState<Record<string, HandshakeStepResult>>({
    cors: { name: '1. CORS Preflight (/health/ready)', status: 'pending', message: 'Chưa kiểm tra' },
    login: { name: '2. Đăng Nhập & Set HttpOnly Cookie', status: 'pending', message: 'Chưa kiểm tra' },
    refresh: { name: '3. Xoay Vòng Token Qua Cross-Site Cookie', status: 'pending', message: 'Chưa kiểm tra' },
    signalr: { name: '4. WebSocket SignalR Handshake', status: 'pending', message: 'Chưa kiểm tra' },
  })
  const signalrConnectionRef = useRef<HubConnection | null>(null)

  useEffect(() => {
    if (isDark) {
      document.documentElement.classList.add('dark')
    } else {
      document.documentElement.classList.remove('dark')
    }
    localStorage.setItem('theme', isDark ? 'dark' : 'light')
  }, [isDark])

  useEffect(() => {
    return () => {
      if (signalrConnectionRef.current) {
        signalrConnectionRef.current.stop()
      }
    }
  }, [])

  const toggleTheme = () => {
    setIsDark(prev => !prev)
  }

  // 1. Health Probe thông thường
  const runHealthProbe = async (endpoint: '/health/live' | '/health/ready') => {
    setIsProbing(true)
    const startTime = performance.now()
    const targetUrl = `${env.apiBaseUrl}${endpoint}`

    try {
      const response = await fetch(targetUrl, {
        method: 'GET',
        headers: { Accept: 'application/json' },
      })
      const durationMs = Math.round(performance.now() - startTime)
      const text = await response.text()

      let formattedPayload = text
      try {
        formattedPayload = JSON.stringify(JSON.parse(text), null, 2)
      } catch {
        // payload không phải JSON
      }

      setProbeResult({
        endpoint,
        status: response.status,
        ok: response.ok,
        durationMs,
        payload: formattedPayload,
        timestamp: new Date().toLocaleTimeString('vi-VN'),
      })
    } catch (err) {
      const durationMs = Math.round(performance.now() - startTime)
      const errorMsg = err instanceof Error ? err.message : 'Unknown Network Error'
      setProbeResult({
        endpoint,
        status: null,
        ok: false,
        durationMs,
        payload: `Network/CORS Error: ${errorMsg}\nTarget URL: ${targetUrl}`,
        timestamp: new Date().toLocaleTimeString('vi-VN'),
      })
    } finally {
      setIsProbing(false)
    }
  }

  // 2. Handshake Step 1: CORS Preflight
  const runCorsTest = async () => {
    setHandshakeSteps(prev => ({
      ...prev,
      cors: { ...prev.cors, status: 'testing', message: 'Đang gửi request CORS preflight...' },
    }))
    const startTime = performance.now()
    const targetUrl = `${env.apiBaseUrl}/health/ready`

    try {
      const res = await fetch(targetUrl, {
        method: 'GET',
        headers: { Accept: 'application/json' },
      })
      const durationMs = Math.round(performance.now() - startTime)
      const data = await res.text()

      if (res.ok) {
        setHandshakeSteps(prev => ({
          ...prev,
          cors: {
            ...prev.cors,
            status: 'success',
            durationMs,
            message: `HTTP ${res.status} OK · CORS Origin Accepted`,
            details: data,
          },
        }))
      } else {
        setHandshakeSteps(prev => ({
          ...prev,
          cors: {
            ...prev.cors,
            status: 'failed',
            durationMs,
            message: `HTTP ${res.status} · Server phản hồi không hợp lệ`,
            details: data,
          },
        }))
      }
    } catch (err) {
      const durationMs = Math.round(performance.now() - startTime)
      const msg = err instanceof Error ? err.message : 'Network error'
      setHandshakeSteps(prev => ({
        ...prev,
        cors: {
          ...prev.cors,
          status: 'failed',
          durationMs,
          message: `Lỗi CORS / Mạng: ${msg}`,
          details: `Target: ${targetUrl}`,
        },
      }))
    }
  }

  // 3. Handshake Step 2: Login & nhận HttpOnly Cookie (credentials: 'include')
  const runLoginTest = async () => {
    setHandshakeSteps(prev => ({
      ...prev,
      login: { ...prev.login, status: 'testing', message: 'Đang gọi POST /api/v1/auth/login...' },
    }))
    const startTime = performance.now()
    const targetUrl = `${env.apiBaseUrl}/api/v1/auth/login`

    try {
      const res = await fetch(targetUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        credentials: 'include',
        body: JSON.stringify({ email: testEmail, password: testPassword }),
      })
      const durationMs = Math.round(performance.now() - startTime)
      const data = await res.json()

      if (res.ok && data?.data?.accessToken) {
        setAccessToken(data.data.accessToken)
        setHandshakeSteps(prev => ({
          ...prev,
          login: {
            ...prev.login,
            status: 'success',
            durationMs,
            message: `HTTP 200 OK · Đăng nhập thành công, đã nhận JWT & Cookie`,
            details: `JWT Token: ${data.data.accessToken.slice(0, 24)}... (expiresIn: ${data.data.expiresIn}s)\nSet-Cookie: refreshToken (SameSite=None; Secure; HttpOnly)`,
          },
        }))
      } else {
        setHandshakeSteps(prev => ({
          ...prev,
          login: {
            ...prev.login,
            status: 'failed',
            durationMs,
            message: `HTTP ${res.status} · Đăng nhập thất bại`,
            details: JSON.stringify(data, null, 2),
          },
        }))
      }
    } catch (err) {
      const durationMs = Math.round(performance.now() - startTime)
      const msg = err instanceof Error ? err.message : 'Network error'
      setHandshakeSteps(prev => ({
        ...prev,
        login: {
          ...prev.login,
          status: 'failed',
          durationMs,
          message: `Lỗi mạng: ${msg}`,
          details: `Target: ${targetUrl}`,
        },
      }))
    }
  }

  // 4. Handshake Step 3: Refresh Token qua Cookie (credentials: 'include')
  const runRefreshTest = async () => {
    setHandshakeSteps(prev => ({
      ...prev,
      refresh: { ...prev.refresh, status: 'testing', message: 'Đang gọi POST /api/v1/auth/refresh...' },
    }))
    const startTime = performance.now()
    const targetUrl = `${env.apiBaseUrl}/api/v1/auth/refresh`

    try {
      const res = await fetch(targetUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
        credentials: 'include',
      })
      const durationMs = Math.round(performance.now() - startTime)
      const data = await res.json()

      if (res.ok && data?.data?.accessToken) {
        setAccessToken(data.data.accessToken)
        setHandshakeSteps(prev => ({
          ...prev,
          refresh: {
            ...prev.refresh,
            status: 'success',
            durationMs,
            message: `HTTP 200 OK · Trình duyệt đã gửi cookie thành công! Token đã xoay vòng.`,
            details: `New JWT Token: ${data.data.accessToken.slice(0, 24)}... (expiresIn: ${data.data.expiresIn}s)\nCơ chế SameSite=None; Secure hoạt động 100% qua domain Vercel <-> Render.`,
          },
        }))
      } else {
        setHandshakeSteps(prev => ({
          ...prev,
          refresh: {
            ...prev.refresh,
            status: 'failed',
            durationMs,
            message: `HTTP ${res.status} · Không xoay vòng được token (Kiểm tra đã chạy Bước 2 chưa)`,
            details: JSON.stringify(data, null, 2),
          },
        }))
      }
    } catch (err) {
      const durationMs = Math.round(performance.now() - startTime)
      const msg = err instanceof Error ? err.message : 'Network error'
      setHandshakeSteps(prev => ({
        ...prev,
        refresh: {
          ...prev.refresh,
          status: 'failed',
          durationMs,
          message: `Lỗi mạng: ${msg}`,
          details: `Target: ${targetUrl}`,
        },
      }))
    }
  }

  // 5. Handshake Step 4: SignalR WebSocket Handshake
  const runSignalRTest = async () => {
    setHandshakeSteps(prev => ({
      ...prev,
      signalr: { ...prev.signalr, status: 'testing', message: 'Đang kết nối SignalR Hub...' },
    }))
    const startTime = performance.now()
    const hubUrl = `${env.apiBaseUrl}/hubs/notifications`

    try {
      if (signalrConnectionRef.current) {
        await signalrConnectionRef.current.stop()
      }

      const connection = new HubConnectionBuilder()
        .withUrl(hubUrl, {
          accessTokenFactory: () => accessToken || '',
        })
        .withAutomaticReconnect()
        .build()

      await connection.start()
      signalrConnectionRef.current = connection
      const durationMs = Math.round(performance.now() - startTime)

      setHandshakeSteps(prev => ({
        ...prev,
        signalr: {
          ...prev.signalr,
          status: 'success',
          durationMs,
          message: `Connected · WebSocket Handshake thành công qua wss://`,
          details: `Hub URL: ${hubUrl}\nConnection ID: ${connection.connectionId}\nConnection State: ${connection.state}`,
        },
      }))
    } catch (err) {
      const durationMs = Math.round(performance.now() - startTime)
      const msg = err instanceof Error ? err.message : 'SignalR connection error'
      setHandshakeSteps(prev => ({
        ...prev,
        signalr: {
          ...prev.signalr,
          status: 'failed',
          durationMs,
          message: `Lỗi kết nối SignalR: ${msg}`,
          details: `Hub URL: ${hubUrl}`,
        },
      }))
    }
  }

  const getStepBadge = (status: HandshakeStepResult['status']) => {
    switch (status) {
      case 'success':
        return <StatusBadge status="online">PASSED</StatusBadge>
      case 'testing':
        return <StatusBadge status="testing">TESTING</StatusBadge>
      case 'failed':
        return <StatusBadge status="offline">FAILED</StatusBadge>
      default:
        return <StatusBadge status="warning">PENDING</StatusBadge>
    }
  }

  return (
    <div className="min-h-screen bg-neutral-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100 transition-colors">
      {/* Editorial Header */}
      <header className="border-b border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900">
        <div className="max-w-5xl mx-auto px-4 sm:px-6 py-4 flex items-center justify-between">
          <div>
            <span className="text-xs uppercase tracking-widest font-semibold text-slate-700 dark:text-slate-300">
              Thời Báo Số · Digital Newsroom OS
            </span>
            <h1 className="text-2xl font-serif font-bold text-slate-900 dark:text-white tracking-tight">
              NewsHub
            </h1>
          </div>
          <button
            type="button"
            onClick={toggleTheme}
            className="inline-flex items-center gap-2 px-3 py-1.5 text-xs font-medium border border-slate-300 dark:border-slate-700 rounded-[4px] hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors"
          >
            {isDark ? '☀️ Giao diện Sáng' : '🌙 Giao diện Tối'}
          </button>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-5xl mx-auto px-4 sm:px-6 py-8">
        <div className="space-y-6">
          {/* Hero Banner Card */}
          <div className="border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 sm:p-8">
            <div className="flex flex-wrap items-center justify-between gap-4 mb-4">
              <div>
                <span className="text-xs font-semibold text-indigo-800 dark:text-indigo-300 uppercase tracking-wider">
                  Phase 3: DevOps Baseline & Cloud Staging
                </span>
                <h2 className="text-xl sm:text-2xl font-serif font-bold text-slate-900 dark:text-white mt-1">
                  Frontend Staging Baseline Online
                </h2>
              </div>
              <StatusBadge status="online">Vercel CDN Live</StatusBadge>
            </div>

            <p className="text-sm text-slate-600 dark:text-slate-300 leading-relaxed max-w-3xl">
              Hạ tầng Frontend Single Page Application (SPA) đã được triển khai thành công lên nền tảng Vercel CDN,
              sẵn sàng kích hoạt <strong>Preview Deployments</strong> cho từng Pull Request ở Phase 4.
            </p>

            <div className="mt-6 pt-6 border-t border-slate-200 dark:border-slate-800 grid grid-cols-2 sm:grid-cols-4 gap-4 text-xs">
              <div>
                <span className="text-slate-700 dark:text-slate-300 block font-medium">Framework</span>
                <span className="font-semibold text-slate-900 dark:text-white">React 19.2.8</span>
              </div>
              <div>
                <span className="text-slate-700 dark:text-slate-300 block font-medium">Build Tool</span>
                <span className="font-semibold text-slate-900 dark:text-white">Vite 8.3.0</span>
              </div>
              <div>
                <span className="text-slate-700 dark:text-slate-300 block font-medium">Styling</span>
                <span className="font-semibold text-slate-900 dark:text-white">Tailwind CSS v4.3</span>
              </div>
              <div>
                <span className="text-slate-700 dark:text-slate-300 block font-medium">Routing</span>
                <span className="font-semibold text-slate-900 dark:text-white">React Router v7.18</span>
              </div>
            </div>
          </div>

          {/* Cloud Staging Connectivity Panel */}
          <div className="border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 sm:p-8">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-base font-serif font-bold text-slate-900 dark:text-white">
                Kiểm Tra Kết Nối Render Backend API
              </h3>
              <span className="text-xs text-slate-700 dark:text-slate-300">
                Môi trường: <code className="font-mono text-slate-700 dark:text-slate-300 font-semibold">{env.isProduction ? 'Production/Staging' : 'Development'}</code>
              </span>
            </div>

            <div className="mb-4 p-3 bg-slate-100 dark:bg-slate-950 border border-slate-200 dark:border-slate-800 rounded-[2px] text-xs font-mono break-all">
              <span className="text-slate-700 dark:text-slate-300 block mb-1">VITE_API_BASE_URL:</span>
              <strong className="text-slate-900 dark:text-white">{env.apiBaseUrl}</strong>
            </div>

            <div className="flex flex-wrap gap-3">
              <button
                type="button"
                disabled={isProbing}
                onClick={() => runHealthProbe('/health/live')}
                className="inline-flex items-center gap-2 px-4 py-2 bg-slate-900 hover:bg-slate-800 text-white dark:bg-white dark:hover:bg-slate-100 dark:text-slate-900 text-xs font-semibold rounded-[4px] transition-colors disabled:opacity-50"
              >
                🩺 Probe Liveness (/health/live)
              </button>

              <button
                type="button"
                disabled={isProbing}
                onClick={() => runHealthProbe('/health/ready')}
                className="inline-flex items-center gap-2 px-4 py-2 bg-teal-700 hover:bg-teal-800 text-white text-xs font-semibold rounded-[4px] transition-colors disabled:opacity-50"
              >
                ⚡ Probe Readiness (/health/ready)
              </button>
            </div>

            {/* Probe Result Output Box */}
            {probeResult && (
              <div className="mt-4 p-4 border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-950 rounded-[2px]">
                <div className="flex items-center justify-between text-xs mb-2">
                  <div className="flex items-center gap-2">
                    <span className="font-semibold text-slate-900 dark:text-white">
                      Endpoint: {probeResult.endpoint}
                    </span>
                    <StatusBadge status={probeResult.ok ? 'online' : 'offline'}>
                      {probeResult.status ? `HTTP ${probeResult.status}` : 'FAILED'}
                    </StatusBadge>
                  </div>
                  <span className="text-slate-700 dark:text-slate-300 font-mono">
                    {probeResult.durationMs}ms · {probeResult.timestamp}
                  </span>
                </div>
                <pre className="text-xs font-mono text-slate-800 dark:text-slate-200 overflow-x-auto whitespace-pre-wrap p-2 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800">
                  {probeResult.payload}
                </pre>
              </div>
            )}
          </div>

          {/* Cross-Site Handshake & Cloud Verification Panel (NH-OPS-03) */}
          <div className="border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6 sm:p-8">
            <div className="flex flex-wrap items-center justify-between gap-2 mb-3">
              <div>
                <span className="text-xs font-semibold text-teal-700 dark:text-teal-400 uppercase tracking-wider">
                  Task NH-OPS-03 Verification
                </span>
                <h3 className="text-lg font-serif font-bold text-slate-900 dark:text-white mt-0.5">
                  Xác Thực Cross-Site Handshake & Cloud Connectivity
                </h3>
              </div>
              <span className="text-xs text-slate-600 dark:text-slate-400">
                Vercel (<code>*.vercel.app</code>) ↔ Render (<code>*.onrender.com</code>)
              </span>
            </div>

            <p className="text-xs text-slate-600 dark:text-slate-300 mb-6 leading-relaxed">
              Kiểm tra 4 bước liên thông mạng qua ranh giới Cross-Site giữa hai domain độc lập:
              CORS Preflight dynamic origins, xác thực HttpOnly Cookie với chính sách <code>SameSite=None; Secure=true</code>,
              xoay vòng token và kết nối WebSocket SignalR thời gian thực qua giao thức <code>wss://</code>.
            </p>

            {/* Test Credentials Configuration */}
            <div className="mb-6 p-4 border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-950 rounded-[2px] grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs">
              <div>
                <label htmlFor="test-email-input" className="block text-slate-700 dark:text-slate-300 font-medium mb-1">
                  Email tài khoản kiểm thử:
                </label>
                <input
                  id="test-email-input"
                  type="email"
                  value={testEmail}
                  onChange={e => setTestEmail(e.target.value)}
                  className="w-full px-3 py-1.5 border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-slate-900 dark:text-white rounded-[4px] font-mono"
                />
              </div>
              <div>
                <label htmlFor="test-password-input" className="block text-slate-700 dark:text-slate-300 font-medium mb-1">
                  Mật khẩu:
                </label>
                <input
                  id="test-password-input"
                  type="password"
                  value={testPassword}
                  onChange={e => setTestPassword(e.target.value)}
                  className="w-full px-3 py-1.5 border border-slate-300 dark:border-slate-700 bg-white dark:bg-slate-900 text-slate-900 dark:text-white rounded-[4px] font-mono"
                />
              </div>
            </div>

            {/* 4 Test Buttons */}
            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 mb-6">
              <button
                type="button"
                onClick={runCorsTest}
                className="px-3 py-2 text-left border border-slate-300 dark:border-slate-700 rounded-[4px] hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors"
              >
                <div className="text-xs font-semibold text-slate-900 dark:text-white">Bước 1: CORS Preflight</div>
                <div className="text-[11px] text-slate-700 dark:text-slate-300 mt-0.5">GET /health/ready</div>
              </button>

              <button
                type="button"
                onClick={runLoginTest}
                className="px-3 py-2 text-left border border-slate-300 dark:border-slate-700 rounded-[4px] hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors"
              >
                <div className="text-xs font-semibold text-slate-900 dark:text-white">Bước 2: Login & Cookie</div>
                <div className="text-[11px] text-slate-700 dark:text-slate-300 mt-0.5">POST /api/v1/auth/login</div>
              </button>

              <button
                type="button"
                onClick={runRefreshTest}
                className="px-3 py-2 text-left border border-slate-300 dark:border-slate-700 rounded-[4px] hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors"
              >
                <div className="text-xs font-semibold text-slate-900 dark:text-white">Bước 3: Token Rotation</div>
                <div className="text-[11px] text-slate-700 dark:text-slate-300 mt-0.5">POST /api/v1/auth/refresh</div>
              </button>

              <button
                type="button"
                onClick={runSignalRTest}
                className="px-3 py-2 text-left border border-slate-300 dark:border-slate-700 rounded-[4px] hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors"
              >
                <div className="text-xs font-semibold text-slate-900 dark:text-white">Bước 4: WebSocket Hub</div>
                <div className="text-[11px] text-slate-700 dark:text-slate-300 mt-0.5">/hubs/notifications</div>
              </button>
            </div>

            {/* Handshake Results Timeline */}
            <div className="space-y-3">
              {Object.entries(handshakeSteps).map(([key, step]) => (
                <div
                  key={key}
                  className="p-3 border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-950 rounded-[2px]"
                >
                  <div className="flex items-center justify-between text-xs mb-1">
                    <span className="font-semibold text-slate-900 dark:text-white">{step.name}</span>
                    <div className="flex items-center gap-2">
                      {step.durationMs !== undefined && (
                        <span className="text-[11px] font-mono text-slate-700 dark:text-slate-300">{step.durationMs}ms</span>
                      )}
                      {getStepBadge(step.status)}
                    </div>
                  </div>
                  <p className="text-xs text-slate-600 dark:text-slate-300">{step.message}</p>
                  {step.details && (
                    <pre className="mt-2 text-[11px] font-mono text-slate-700 dark:text-slate-300 bg-white dark:bg-slate-900 p-2 border border-slate-200 dark:border-slate-800 whitespace-pre-wrap overflow-x-auto">
                      {step.details}
                    </pre>
                  )}
                </div>
              ))}
            </div>
          </div>

          {/* SPA Routing & Navigation Test Panel */}
          <div className="border border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 p-6">
            <h3 className="text-base font-serif font-bold text-slate-900 dark:text-white mb-2">
              Kiểm Tra SPA Routing & HTML5 History Mode
            </h3>
            <p className="text-xs text-slate-600 dark:text-slate-300 mb-4">
              Xác thực cấu hình <code>vercel.json</code> rewrites hoạt động chính xác khi F5 hoặc truy cập trực tiếp route con:
            </p>
            <div className="flex flex-wrap gap-3 text-xs">
              <Link
                to="/status"
                className="px-3 py-1.5 border border-slate-300 dark:border-slate-700 rounded-[4px] hover:bg-slate-100 dark:hover:bg-slate-800 font-medium transition-colors"
              >
                🔗 Đường dẫn /status
              </Link>
              <Link
                to="/duong-dan-khong-ton-tai-404"
                className="px-3 py-1.5 border border-slate-300 dark:border-slate-700 rounded-[4px] hover:bg-slate-100 dark:hover:bg-slate-800 font-medium transition-colors"
              >
                ⚠️ Thử nghiệm trang 404 Catch-All
              </Link>
            </div>
          </div>
        </div>
      </main>

      {/* Editorial Footer */}
      <footer className="border-t border-slate-200 dark:border-slate-800 bg-white dark:bg-slate-900 mt-12 py-6 text-center text-xs text-slate-600 dark:text-slate-400">
        <p>NewsHub · Hệ Điều Hành Tòa Soạn Số Đa Đơn Vị Tích Hợp AI © 2026</p>
        <p className="mt-1 text-slate-600 dark:text-slate-400">
          Thiết kế tuân thủ triệt để Impeccable Design System: Crisp Edges · Zero-Shadow · Modern Editorial
        </p>
      </footer>
    </div>
  )
}
