interface SpinnerProps {
  size?: 'sm' | 'md' | 'lg'
  className?: string
  label?: string
}

const SPINNER_SIZES = {
  sm: 14,
  md: 18,
  lg: 24,
} as const

export function Spinner({
  size = 'md',
  className = '',
  label = 'Đang tải dữ liệu',
}: SpinnerProps) {
  const px = SPINNER_SIZES[size]

  return (
    <span role="status" aria-label={label} className={`inline-flex items-center ${className}`}>
      <svg
        width={px}
        height={px}
        viewBox="0 0 24 24"
        fill="none"
        stroke="currentColor"
        strokeWidth={1.75}
        strokeLinecap="round"
        className="animate-spin"
        aria-hidden="true"
      >
        <path d="M21 12a9 9 0 1 1-6.219-8.56" />
      </svg>
    </span>
  )
}
