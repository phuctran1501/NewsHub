import { useQuery } from '@tanstack/react-query'
import { AnimatePresence, motion, useReducedMotion } from 'framer-motion'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '@/hooks/useAuth'
import { queryKeys } from '@/lib/queryKeys'
import { ROUTES } from '@/routes/paths'
import {
  type CityWeatherItem,
  type MarketTickerItemDto,
  utilityService,
} from '@/services/utilityService'
import { cn } from '@/utils/cn'

const FALLBACK_WEATHER_CITIES: CityWeatherItem[] = [
  { city: 'TP.HCM', temp: '31°C', condition: 'Nắng nhẹ' },
  { city: 'Hà Nội', temp: '23°C', condition: 'Mát mẻ' },
  { city: 'Đà Nẵng', temp: '27°C', condition: 'Có mây' },
  { city: 'Cần Thơ', temp: '30°C', condition: 'Nắng ráo' },
  { city: 'Hải Phòng', temp: '22°C', condition: 'Se lạnh' },
  { city: 'Huế', temp: '25°C', condition: 'Mưa nhẹ' },
  { city: 'Nha Trang', temp: '29°C', condition: 'Nắng ấm' },
  { city: 'Đà Lạt', temp: '19°C', condition: 'Sương sớm' },
]

const FALLBACK_MARKET_INDICES: MarketTickerItemDto[] = [
  { symbol: 'VN-INDEX', value: '1,286.42', change: '+0.48%', isPositive: true, isLive: false },
  { symbol: 'VN30', value: '1,342.15', change: '+0.62%', isPositive: true, isLive: false },
  { symbol: 'HNX-INDEX', value: '234.18', change: '-0.19%', isPositive: false, isLive: false },
  { symbol: 'UPCOM', value: '93.45', change: '+0.14%', isPositive: true, isLive: false },
  { symbol: 'USD/VND', value: '25,380', change: 'Tỷ giá', isPositive: true, isLive: false },
  { symbol: 'SJC 9999', value: '84.50tr', change: 'Bán ra', isPositive: true, isLive: false },
]

const WEATHER_INTERVAL_MS = 3800
const MARKET_INTERVAL_MS = 4500

function formatVietnameseToday(): string {
  const formatted = new Intl.DateTimeFormat('vi-VN', {
    weekday: 'long',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(new Date())

  return formatted.charAt(0).toUpperCase() + formatted.slice(1)
}

export function TopUtilityBar() {
  const { roleName } = useAuth()
  const prefersReducedMotion = useReducedMotion()
  const todayLabel = useMemo(() => formatVietnameseToday(), [])

  const { data: liveWeather } = useQuery({
    queryKey: queryKeys.utility.weather(),
    queryFn: ({ signal }) => utilityService.getVietnamWeather(signal),
    staleTime: 15 * 60 * 1000,
    gcTime: 30 * 60 * 1000,
    retry: 1,
  })

  const { data: liveMarket } = useQuery({
    queryKey: queryKeys.utility.marketTicker(),
    queryFn: ({ signal }) => utilityService.getMarketTicker(signal),
    staleTime: 5 * 60 * 1000,
    gcTime: 15 * 60 * 1000,
    retry: 1,
  })

  const weatherCities = liveWeather && liveWeather.length > 0 ? liveWeather : FALLBACK_WEATHER_CITIES
  const marketIndices =
    liveMarket?.items && liveMarket.items.length > 0 ? liveMarket.items : FALLBACK_MARKET_INDICES

  const [weatherIndex, setWeatherIndex] = useState(0)
  const [marketIndex, setMarketIndex] = useState(0)
  const [isWeatherPaused, setIsWeatherPaused] = useState(false)
  const [isMarketPaused, setIsMarketPaused] = useState(false)

  useEffect(() => {
    if (prefersReducedMotion || isWeatherPaused || weatherCities.length <= 1) return
    const timer = window.setInterval(() => {
      setWeatherIndex((prev) => (prev + 1) % weatherCities.length)
    }, WEATHER_INTERVAL_MS)
    return () => window.clearInterval(timer)
  }, [prefersReducedMotion, isWeatherPaused, weatherCities.length])

  useEffect(() => {
    if (prefersReducedMotion || isMarketPaused || marketIndices.length <= 1) return
    const timer = window.setInterval(() => {
      setMarketIndex((prev) => (prev + 1) % marketIndices.length)
    }, MARKET_INTERVAL_MS)
    return () => window.clearInterval(timer)
  }, [prefersReducedMotion, isMarketPaused, marketIndices.length])

  const currentWeather = weatherCities[weatherIndex % weatherCities.length]
  const currentMarket = marketIndices[marketIndex % marketIndices.length]

  const journalistHref =
    roleName === 'Journalist' || roleName === 'Admin' ? ROUTES.CMS.ROOT : ROUTES.AUTH.REGISTER
  const journalistLabel =
    roleName === 'Admin'
      ? 'Cổng Quản trị'
      : roleName === 'Journalist'
        ? 'Bàn làm việc Nhà báo'
        : 'Dành cho Nhà báo'

  return (
    <div
      aria-label="Thanh tiện ích tòa soạn"
      className="hidden md:block w-full border-b border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg"
    >
      <div className="flex h-9 w-full items-center justify-between gap-4 px-4 sm:px-6 lg:px-10 xl:px-14 text-xs text-neutral-text-muted dark:text-dark-text-muted">
        <div className="flex items-center gap-3.5 min-w-0">
          <span className="font-medium text-neutral-text-main dark:text-dark-text-main whitespace-nowrap">
            {todayLabel}
          </span>

          <span
            aria-hidden="true"
            className="hidden lg:inline text-neutral-border dark:text-dark-border"
          >
            |
          </span>

          {/* Live Open-Meteo Weather Ticker (Vertical Jump-Up Transition) */}
          <button
            type="button"
            onClick={() => setWeatherIndex((prev) => (prev + 1) % weatherCities.length)}
            onMouseEnter={() => setIsWeatherPaused(true)}
            onMouseLeave={() => setIsWeatherPaused(false)}
            title="Dữ liệu thời tiết Open-Meteo • Nhấn để chuyển tỉnh thành"
            aria-label={`Thời tiết ${currentWeather.city}: ${currentWeather.temp}, ${currentWeather.condition}`}
            className="hidden lg:inline-flex h-5 w-[188px] items-center overflow-hidden text-left hover:text-neutral-text-main dark:hover:text-dark-text-main transition-colors cursor-pointer"
          >
            <AnimatePresence mode="wait" initial={false}>
              <motion.span
                key={currentWeather.city}
                initial={prefersReducedMotion ? false : { y: 12, opacity: 0 }}
                animate={{ y: 0, opacity: 1 }}
                exit={prefersReducedMotion ? { opacity: 0 } : { y: -12, opacity: 0 }}
                transition={{ duration: 0.28, ease: [0.16, 1, 0.3, 1] }}
                className="inline-flex items-center gap-1.5 whitespace-nowrap tabular-nums"
              >
                <span className="font-semibold text-neutral-text-main dark:text-dark-text-main">
                  {currentWeather.city}
                </span>
                <span className="font-mono text-[11px] font-semibold text-primary dark:text-secondary">
                  {currentWeather.temp}
                </span>
                <span aria-hidden="true">•</span>
                <span>{currentWeather.condition}</span>
              </motion.span>
            </AnimatePresence>
          </button>

          <span
            aria-hidden="true"
            className="hidden xl:inline text-neutral-border dark:text-dark-border"
          >
            |
          </span>

          {/* Live Backend Market Ticker (Vertical Jump-Up Transition) */}
          <button
            type="button"
            onClick={() => setMarketIndex((prev) => (prev + 1) % marketIndices.length)}
            onMouseEnter={() => setIsMarketPaused(true)}
            onMouseLeave={() => setIsMarketPaused(false)}
            title="Chỉ số thị trường • Nhấn để chuyển chỉ số tiếp theo"
            aria-label={`Chỉ số ${currentMarket.symbol}: ${currentMarket.value} (${currentMarket.change})`}
            className="hidden xl:inline-flex h-5 w-[225px] items-center overflow-hidden text-left hover:text-neutral-text-main dark:hover:text-dark-text-main transition-colors cursor-pointer"
          >
            <AnimatePresence mode="wait" initial={false}>
              <motion.span
                key={currentMarket.symbol}
                initial={prefersReducedMotion ? false : { y: 12, opacity: 0 }}
                animate={{ y: 0, opacity: 1 }}
                exit={prefersReducedMotion ? { opacity: 0 } : { y: -12, opacity: 0 }}
                transition={{ duration: 0.28, ease: [0.16, 1, 0.3, 1] }}
                className="inline-flex items-center gap-1.5 font-mono text-[11px] whitespace-nowrap tabular-nums"
              >
                <span className="font-semibold text-neutral-text-main dark:text-dark-text-main">
                  {currentMarket.symbol}
                </span>
                <strong
                  className={cn(
                    'font-semibold',
                    currentMarket.isPositive ? 'text-accent-action' : 'text-accent-crimson',
                  )}
                >
                  {currentMarket.value} ({currentMarket.change})
                </strong>
              </motion.span>
            </AnimatePresence>
          </button>
        </div>

        <nav aria-label="Lối tắt tòa soạn" className="flex items-center gap-4 shrink-0">
          <a
            href="/#live-wire"
            className="hover:text-primary dark:hover:text-secondary transition-colors"
          >
            Tin mới nhất
          </a>
          <a
            href="/#opinion-col"
            className="hover:text-primary dark:hover:text-secondary transition-colors"
          >
            Góc nhìn
          </a>
          <a
            href="/#multimedia-band"
            className="hover:text-primary dark:hover:text-secondary transition-colors"
          >
            Media
          </a>
          <a
            href="/#companies"
            className="hover:text-primary dark:hover:text-secondary transition-colors"
          >
            Tòa soạn đối tác
          </a>
          <Link
            to={journalistHref}
            className="font-semibold text-primary dark:text-secondary hover:underline"
          >
            {journalistLabel}
          </Link>
        </nav>
      </div>
    </div>
  )
}
