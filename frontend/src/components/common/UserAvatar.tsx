import { cn } from '@/utils/cn'

interface UserAvatarProps {
  fullName?: string | null
  avatarUrl?: string | null
  size?: 'sm' | 'md' | 'xl'
  className?: string
}

const AVATAR_SIZE_CLASSES = {
  sm: 'h-8 w-8 text-xs',
  md: 'h-9 w-9 text-xs',
  xl: 'h-24 w-24 text-2xl',
} as const

function getUserInitials(fullName?: string | null): string {
  if (!fullName) return 'NH'
  const parts = fullName.trim().split(/\s+/).filter(Boolean)
  if (parts.length === 0) return 'NH'
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase()
  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase()
}

export function UserAvatar({
  fullName,
  avatarUrl,
  size = 'md',
  className,
}: UserAvatarProps) {
  return (
    <span
      className={cn(
        'relative inline-flex shrink-0 items-center justify-center overflow-hidden rounded-full border border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg select-none',
        AVATAR_SIZE_CLASSES[size],
        className,
      )}
    >
      {avatarUrl ? (
        <img
          src={avatarUrl}
          alt={fullName ? `Ảnh đại diện của ${fullName}` : 'Ảnh đại diện'}
          className="h-full w-full object-cover"
        />
      ) : (
        <span className="font-bold text-primary dark:text-secondary">
          {getUserInitials(fullName)}
        </span>
      )}
    </span>
  )
}
