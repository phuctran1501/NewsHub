import { AnimatePresence, motion, useMotionValueEvent, useReducedMotion, useScroll } from 'framer-motion'
import { ArrowUp } from 'lucide-react'
import { useState } from 'react'
import { buttonVariants } from '@/components/ui/variants'
import { cn } from '@/utils/cn'

const RING_RADIUS = 17
const RING_CIRCUMFERENCE = 2 * Math.PI * RING_RADIUS

export function ScrollToTopButton() {
  const prefersReducedMotion = useReducedMotion()
  const { scrollY, scrollYProgress } = useScroll()
  const [isVisible, setIsVisible] = useState(false)
  const [progress, setProgress] = useState(0)

  useMotionValueEvent(scrollY, 'change', (latest) => {
    setIsVisible(latest > 320)
  })

  useMotionValueEvent(scrollYProgress, 'change', (latest) => {
    setProgress(Math.min(1, Math.max(0, latest)))
  })

  function handleScrollTop() {
    window.scrollTo({
      top: 0,
      behavior: prefersReducedMotion ? 'auto' : 'smooth',
    })
  }

  return (
    <AnimatePresence>
      {isVisible && (
        <motion.button
          type="button"
          onClick={handleScrollTop}
          aria-label="Cuộn lên đầu trang"
          title="Lên đầu trang"
          initial={prefersReducedMotion ? { opacity: 1 } : { opacity: 0, y: 12, scale: 0.92 }}
          animate={{ opacity: 1, y: 0, scale: 1 }}
          exit={prefersReducedMotion ? { opacity: 0 } : { opacity: 0, y: 10, scale: 0.92 }}
          transition={{ duration: 0.2, ease: [0.16, 1, 0.3, 1] }}
          className={cn(
            buttonVariants({ variant: 'outline', size: 'icon' }),
            'fixed bottom-20 left-5 md:bottom-6 md:left-6 z-40 h-11 w-11 rounded-full bg-neutral-surface dark:bg-dark-surface text-primary dark:text-secondary border-neutral-border dark:border-dark-border hover:bg-neutral-bg dark:hover:bg-dark-border/60',
          )}
        >
          <svg
            aria-hidden="true"
            className="pointer-events-none absolute inset-0 h-full w-full -rotate-90"
            viewBox="0 0 40 40"
          >
            <circle
              cx="20"
              cy="20"
              r={RING_RADIUS}
              fill="none"
              stroke="currentColor"
              strokeWidth="1.75"
              strokeDasharray={RING_CIRCUMFERENCE}
              strokeDashoffset={RING_CIRCUMFERENCE * (1 - progress)}
              strokeLinecap="round"
              className="opacity-85"
            />
          </svg>
          <ArrowUp size={16} strokeWidth={2} aria-hidden="true" />
        </motion.button>
      )}
    </AnimatePresence>
  )
}
