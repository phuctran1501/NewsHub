const VI_DATE_FORMATTER = new Intl.DateTimeFormat('vi-VN', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
})

const VI_TIME_FORMATTER = new Intl.DateTimeFormat('vi-VN', {
  hour: '2-digit',
  minute: '2-digit',
  hour12: false,
})

export function formatPublishedDate(isoString: string | null | undefined): string {
  if (!isoString) return 'Vừa cập nhật'
  const date = new Date(isoString)
  if (Number.isNaN(date.getTime())) return 'Vừa cập nhật'
  return VI_DATE_FORMATTER.format(date)
}

export function formatTickerTime(isoString: string | null | undefined): string {
  if (!isoString) return 'Mới'
  const date = new Date(isoString)
  if (Number.isNaN(date.getTime())) return 'Mới'
  return VI_TIME_FORMATTER.format(date)
}

export function formatRelativeTime(isoString: string | null | undefined): string {
  if (!isoString) return 'Vừa xuất bản'
  const date = new Date(isoString)
  if (Number.isNaN(date.getTime())) return 'Vừa xuất bản'

  const diffMs = Date.now() - date.getTime()
  const diffMinutes = Math.floor(diffMs / 60_000)

  if (diffMinutes < 1) return 'Vừa xong'
  if (diffMinutes < 60) return `${diffMinutes} phút trước`

  const diffHours = Math.floor(diffMinutes / 60)
  if (diffHours < 24) return `${diffHours} giờ trước`

  const diffDays = Math.floor(diffHours / 24)
  if (diffDays === 1) return 'Hôm qua'
  if (diffDays < 7) return `${diffDays} ngày trước`

  return VI_DATE_FORMATTER.format(date)
}

export function formatViewCount(views: number): string {
  if (views >= 1_000_000) {
    return `${(views / 1_000_000).toFixed(1).replace(/\.0$/, '')}M lượt xem`
  }
  if (views >= 1_000) {
    return `${(views / 1_000).toFixed(1).replace(/\.0$/, '')}K lượt xem`
  }
  return `${views.toLocaleString('vi-VN')} lượt xem`
}
