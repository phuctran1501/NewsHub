import { motion, useReducedMotion } from 'framer-motion'

interface PressOutlet {
  id: string
  name: string
  logoSrc: string
  heightClass: string
}

const PRESS_OUTLETS: PressOutlet[] = [
  { id: 'vnexpress', name: 'VnExpress', logoSrc: '/logos/press/vnexpress.svg', heightClass: 'h-6' },
  { id: 'tuoitre', name: 'Tuổi Trẻ', logoSrc: '/logos/press/tuoitre.svg', heightClass: 'h-7' },
  { id: 'reuters', name: 'Reuters', logoSrc: '/logos/press/reuters.svg', heightClass: 'h-6' },
  { id: 'bloomberg', name: 'Bloomberg', logoSrc: '/logos/press/bloomberg.svg', heightClass: 'h-5' },
  { id: 'guardian', name: 'The Guardian', logoSrc: '/logos/press/the-guardian.svg', heightClass: 'h-6' },
  { id: 'cnn', name: 'CNN', logoSrc: '/logos/press/cnn.svg', heightClass: 'h-6' },
  {
    id: 'ft',
    name: 'Financial Times',
    logoSrc: '/logos/press/financial-times.svg',
    heightClass: 'h-5',
  },
  {
    id: 'nytimes',
    name: 'The New York Times',
    logoSrc: '/logos/press/nytimes.svg',
    heightClass: 'h-6',
  },
]

export function PressEcosystemMarquee() {
  const prefersReducedMotion = useReducedMotion()

  return (
    <motion.section
      aria-labelledby="press-ecosystem-heading"
      initial={prefersReducedMotion ? false : { opacity: 0, y: 10 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true, amount: 0.2 }}
      transition={{ duration: 0.32, ease: [0.16, 1, 0.3, 1] }}
      className="space-y-5"
    >
      <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-2 border-b border-neutral-border dark:border-dark-border pb-3">
        <h2
          id="press-ecosystem-heading"
          className="text-xl font-bold tracking-[-0.015em] text-neutral-text-main dark:text-dark-text-main"
        >
          Hệ sinh thái đa tòa soạn
        </h2>
      </div>

      <div
        title="Rê chuột để tạm dừng dải logo"
        className="nh-press-marquee overflow-hidden mask-[linear-gradient(to_right,transparent,#000_7%,#000_93%,transparent)]"
      >
        <div className="nh-press-marquee-track py-1">
          {[...PRESS_OUTLETS, ...PRESS_OUTLETS].map((outlet, index) => (
            <div
              key={`${outlet.id}-${index}`}
              aria-hidden={index >= PRESS_OUTLETS.length ? true : undefined}
              className="flex h-16 min-w-44 shrink-0 items-center justify-center rounded-xl border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-white/95 px-6 hover:border-primary dark:hover:border-secondary-dark transition-colors duration-150"
            >
              <img
                src={outlet.logoSrc}
                alt={index < PRESS_OUTLETS.length ? outlet.name : ''}
                loading="lazy"
                className={`${outlet.heightClass} w-auto max-w-[136px] object-contain select-none`}
              />
            </div>
          ))}
        </div>
      </div>
    </motion.section>
  )
}
