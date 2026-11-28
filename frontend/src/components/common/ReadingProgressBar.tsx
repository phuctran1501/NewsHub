import { motion, useReducedMotion, useScroll, useSpring } from 'framer-motion'

export function ReadingProgressBar() {
  const prefersReducedMotion = useReducedMotion()
  const { scrollYProgress } = useScroll()
  const scaleX = useSpring(scrollYProgress, {
    stiffness: 260,
    damping: 32,
    restDelta: 0.001,
  })

  if (prefersReducedMotion) {
    return null
  }

  return (
    <motion.div
      aria-hidden="true"
      style={{ scaleX }}
      className="fixed top-0 left-0 right-0 z-50 h-[2px] origin-left bg-primary dark:bg-secondary pointer-events-none"
    />
  )
}
