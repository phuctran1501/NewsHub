import type { ApiResponse } from '@/types/api'
import { apiClient, unwrapData } from './apiClient'

export interface CityWeatherItem {
  city: string
  temp: string
  condition: string
}

export interface MarketTickerItemDto {
  symbol: string
  value: string
  change: string
  isPositive: boolean
  isLive: boolean
}

export interface MarketTickerResponseDto {
  items: MarketTickerItemDto[]
  updatedAt: string
  isLiveData: boolean
}

interface VietnamCityCoord {
  city: string
  lat: number
  lon: number
}

const VIETNAM_CITIES: VietnamCityCoord[] = [
  { city: 'TP.HCM', lat: 10.8231, lon: 106.6297 },
  { city: 'Hà Nội', lat: 21.0285, lon: 105.8542 },
  { city: 'Đà Nẵng', lat: 16.0544, lon: 108.2022 },
  { city: 'Cần Thơ', lat: 10.0452, lon: 105.7469 },
  { city: 'Hải Phòng', lat: 20.8449, lon: 106.6881 },
  { city: 'Huế', lat: 16.4637, lon: 107.5909 },
  { city: 'Nha Trang', lat: 12.2388, lon: 109.1967 },
  { city: 'Đà Lạt', lat: 11.9404, lon: 108.4583 },
]

interface OpenMeteoEntry {
  current?: {
    temperature_2m?: number
    weather_code?: number
  }
}

function mapWmoCodeToVietnamese(code: number | undefined): string {
  if (code === undefined) return 'Ổn định'
  if (code === 0) return 'Trời quang'
  if (code === 1 || code === 2) return 'Ít mây'
  if (code === 3) return 'Nhiều mây'
  if (code === 45 || code === 48) return 'Sương mù'
  if (code >= 51 && code <= 57) return 'Mưa phùn'
  if (code === 61 || code === 80) return 'Mưa nhẹ'
  if (code === 63 || code === 81) return 'Mưa vừa'
  if (code === 65 || code === 82) return 'Mưa to'
  if (code >= 95) return 'Có dông'
  return 'Có mây'
}

export const utilityService = {
  async getVietnamWeather(signal?: AbortSignal): Promise<CityWeatherItem[]> {
    const latitudes = VIETNAM_CITIES.map((c) => c.lat).join(',')
    const longitudes = VIETNAM_CITIES.map((c) => c.lon).join(',')
    const url =
      `https://api.open-meteo.com/v1/forecast?latitude=${latitudes}&longitude=${longitudes}` +
      `&current=temperature_2m,weather_code&timezone=Asia%2FHo_Chi_Minh`

    const response = await fetch(url, { signal })
    if (!response.ok) {
      throw new Error(`Open-Meteo HTTP ${response.status}`)
    }

    const raw = (await response.json()) as OpenMeteoEntry | OpenMeteoEntry[]
    const entries = Array.isArray(raw) ? raw : [raw]

    return VIETNAM_CITIES.map((coord, idx) => {
      const current = entries[idx]?.current
      const tempValue =
        typeof current?.temperature_2m === 'number'
          ? `${Math.round(current.temperature_2m)}°C`
          : '--°C'

      return {
        city: coord.city,
        temp: tempValue,
        condition: mapWmoCodeToVietnamese(current?.weather_code),
      }
    })
  },

  async getMarketTicker(signal?: AbortSignal): Promise<MarketTickerResponseDto> {
    return unwrapData(
      apiClient.get<ApiResponse<MarketTickerResponseDto>>('/api/v1/public/market-ticker', {
        signal,
      }),
    )
  },
}
