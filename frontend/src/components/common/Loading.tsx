import { cn } from '@/utils/cn'

export type LoadingMode = 'inline' | 'section' | 'overlay' | 'fullscreen'
export type LoadingSize = 'sm' | 'md' | 'lg'

interface LoadingProps {
  mode?: LoadingMode
  size?: LoadingSize
  label?: string
  className?: string
}

const ICON_SIZES: Record<LoadingSize, number> = {
  sm: 18,
  md: 32,
  lg: 44,
}

function BrandApertureMark({ size }: { size: number }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 40 40"
      fill="none"
      aria-hidden="true"
      className="nh-aperture-spin shrink-0 text-primary dark:text-secondary"
    >
      <path d="M16.5 5L27 5L35 13V14H16.5V5Z" fill="currentColor" />
      <path d="M35 16.5V27L27 35H26V16.5H35Z" fill="currentColor" />
      <path d="M23.5 35H13L5 27V26H23.5V35Z" fill="currentColor" />
      <path d="M5 23.5V13L13 5H14V23.5H5Z" fill="currentColor" />
      <path
        d="M20 16.2L23.8 20L20 23.8L16.2 20L20 16.2Z"
        fill="currentColor"
        className="nh-aperture-core"
      />
    </svg>
  )
}

export function Loading({ mode = 'section', size, label, className }: LoadingProps) {
  const resolvedSize: LoadingSize =
    size ?? (mode === 'inline' ? 'sm' : mode === 'fullscreen' ? 'lg' : 'md')
  const iconPx = ICON_SIZES[resolvedSize]
  const ariaLabel = label ?? 'Đang tải dữ liệu'

  if (mode === 'inline') {
    return (
      <span
        role="status"
        aria-live="polite"
        aria-label={ariaLabel}
        className={cn('inline-flex items-center gap-2.5 text-xs font-medium', className)}
      >
        <BrandApertureMark size={iconPx} />
        {label && (
          <span className="text-neutral-text-muted dark:text-dark-text-muted">{label}</span>
        )}
      </span>
    )
  }

  if (mode === 'overlay') {
    return (
      <div
        role="status"
        aria-live="polite"
        aria-label={ariaLabel}
        className={cn(
          'absolute inset-0 z-20 flex flex-col items-center justify-center gap-3 rounded-[inherit] bg-neutral-surface/80 dark:bg-dark-bg/80 backdrop-blur-[2px]',
          className,
        )}
      >
        <BrandApertureMark size={iconPx} />
        {label && (
          <p className="text-xs font-medium text-neutral-text-muted dark:text-dark-text-muted">
            {label}
          </p>
        )}
      </div>
    )
  }

  if (mode === 'fullscreen') {
    return (
      <div
        role="status"
        aria-live="polite"
        aria-label={ariaLabel}
        className={cn(
          'min-h-screen w-full flex flex-col items-center justify-center gap-3.5 bg-neutral-bg dark:bg-dark-bg px-6',
          className,
        )}
      >
        <BrandApertureMark size={iconPx} />
        {label && (
          <p className="text-sm font-medium text-neutral-text-muted dark:text-dark-text-muted">
            {label}
          </p>
        )}
      </div>
    )
  }

  return (
    <div
      role="status"
      aria-live="polite"
      aria-label={ariaLabel}
      className={cn(
        'w-full flex flex-col items-center justify-center gap-3 py-12 px-4',
        className,
      )}
    >
      <BrandApertureMark size={iconPx} />
      {label && (
        <p className="text-xs sm:text-sm font-medium text-neutral-text-muted dark:text-dark-text-muted">
          {label}
        </p>
      )}
    </div>
  )
}
