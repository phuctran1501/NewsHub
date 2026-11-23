import { useState, useEffect } from 'react'
import { Link } from 'react-router-dom'
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

export function StatusPage() {
  const [isDark, setIsDark] = useState(() => {
    if (typeof window === 'undefined') return false
    const stored = localStorage.getItem('theme')
    if (stored) return stored === 'dark'
    return window.matchMedia('(prefers-color-scheme: dark)').matches
  })
  const [probeResult, setProbeResult] = useState<ProbeResult | null>(null)
  const [isProbing, setIsProbing] = useState(false)

  useEffect(() => {
    if (isDark) {
      document.documentElement.classList.add('dark')
    } else {
      document.documentElement.classList.remove('dark')
    }
    localStorage.setItem('theme', isDark ? 'dark' : 'light')
  }, [isDark])

  const toggleTheme = () => {
    setIsDark(prev => !prev)
  }

  const runHealthProbe = async (endpoint: '/health/live' | '/health/ready') => {
    setIsProbing(true)
    const startTime = performance.now()
    const targetUrl = `${env.apiBaseUrl}${endpoint}`

    try {
      const response = await fetch(targetUrl, {
        method: 'GET',
        headers: {
          Accept: 'application/json',
        },
      })
      const durationMs = Math.round(performance.now() - startTime)
      const text = await response.text()

      let formattedPayload = text
      try {
        formattedPayload = JSON.stringify(JSON.parse(text), null, 2)
      } catch {
        // payload không phải JSON, giữ nguyên text
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
        payload: `Network/CORS Error: ${errorMsg}\nTarget URL: ${targetUrl}\n(Gợi ý: Kiểm tra Backend Render đã bật và CORS policy cho phép origin hiện tại).`,
        timestamp: new Date().toLocaleTimeString('vi-VN'),
      })
    } finally {
      setIsProbing(false)
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
