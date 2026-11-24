interface LogoProps {
  variant?: 'horizontal' | 'full' | 'mark'
  size?: 'sm' | 'md' | 'lg'
  className?: string
}

const SIZE_MAP = {
  sm: { mark: 26, textClass: 'text-lg', gapClass: 'gap-2' },
  md: { mark: 32, textClass: 'text-[22px]', gapClass: 'gap-2.5' },
  lg: { mark: 42, textClass: 'text-[28px]', gapClass: 'gap-3' },
} as const

function ConvergenceApertureMark({ size }: { size: number }) {
  return (
    <svg
      width={size}
      height={size}
      viewBox="0 0 40 40"
      fill="none"
      aria-hidden="true"
      className="shrink-0"
    >
      <path d="M16.5 5L27 5L35 13V14H16.5V5Z" fill="currentColor" />
      <path d="M35 16.5V27L27 35H26V16.5H35Z" fill="currentColor" />
      <path d="M23.5 35H13L5 27V26H23.5V35Z" fill="currentColor" />
      <path d="M5 23.5V13L13 5H14V23.5H5Z" fill="currentColor" />
      <path d="M20 16.2L23.8 20L20 23.8L16.2 20L20 16.2Z" fill="currentColor" />
    </svg>
  )
}

export function Logo({
  variant = 'horizontal',
  size = 'md',
  className = '',
}: LogoProps) {
  const config = SIZE_MAP[size]
  const brandColorClass = 'text-primary dark:text-dark-text-main select-none'

  if (variant === 'mark') {
    return (
      <span className={`inline-flex items-center ${brandColorClass} ${className}`}>
        <ConvergenceApertureMark size={config.mark} />
        <span className="sr-only">NewsHub</span>
      </span>
    )
  }

  if (variant === 'full') {
    return (
      <span
        className={`inline-flex flex-col items-center text-center gap-2 ${brandColorClass} ${className}`}
      >
        <ConvergenceApertureMark size={config.mark} />
        <span
          className={`font-brand font-bold leading-none tracking-[-0.02em] ${config.textClass}`}
        >
          NewsHub
        </span>
      </span>
    )
  }

  return (
    <span
      className={`inline-flex items-center ${config.gapClass} ${brandColorClass} ${className}`}
    >
      <ConvergenceApertureMark size={config.mark} />
      <span
        className={`font-brand font-bold leading-none tracking-[-0.02em] ${config.textClass}`}
      >
        NewsHub
      </span>
    </span>
  )
}
