import { useState } from 'react'
import { cn } from '@/utils/cn'

interface UserAvatarProps {
  fullName?: string | null
  avatarUrl?: string | null
  size?: 'sm' | 'md' | 'xl'
  className?: string
}

const AVATAR_SIZE_CLASSES = {
  sm: 'h-8 w-8',
  md: 'h-9 w-9',
  xl: 'h-24 w-24',
} as const

const FALLBACK_LOGO_SIZES = {
  sm: 18,
  md: 20,
  xl: 46,
} as const

function NewsHubAvatarMark({ size }: { size: number }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 40 40"
      fill="none"
      aria-hidden="true"
      className="shrink-0 text-primary dark:text-secondary"
    >
      <path d="M16.5 5L27 5L35 13V14H16.5V5Z" fill="currentColor" />
      <path d="M35 16.5V27L27 35H26V16.5H35Z" fill="currentColor" />
      <path d="M23.5 35H13L5 27V26H23.5V35Z" fill="currentColor" />
      <path d="M5 23.5V13L13 5H14V23.5H5Z" fill="currentColor" />
      <path d="M20 16.2L23.8 20L20 23.8L16.2 20L20 16.2Z" fill="currentColor" />
    </svg>
  )
}

export function UserAvatar({
  fullName,
  avatarUrl,
  size = 'md',
  className,
}: UserAvatarProps) {
  const [failedUrl, setFailedUrl] = useState<string | null>(null)
  const trimmedUrl = avatarUrl?.trim() || null
  const canRenderImage = Boolean(trimmedUrl && failedUrl !== trimmedUrl)

  return (
    <span
      className={cn(
        'relative inline-flex shrink-0 items-center justify-center overflow-hidden rounded-full border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg select-none',
        AVATAR_SIZE_CLASSES[size],
        className,
      )}
    >
      {canRenderImage && trimmedUrl ? (
        <img
          src={trimmedUrl}
          alt={fullName ? `Ảnh đại diện của ${fullName}` : 'Ảnh đại diện'}
          onError={() => setFailedUrl(trimmedUrl)}
          className="h-full w-full object-cover"
        />
      ) : (
        <NewsHubAvatarMark size={FALLBACK_LOGO_SIZES[size]} />
      )}
    </span>
  )
}
