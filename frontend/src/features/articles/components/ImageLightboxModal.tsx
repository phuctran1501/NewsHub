import { AnimatePresence, motion, useReducedMotion } from 'framer-motion'
import { X } from 'lucide-react'
import { useEffect } from 'react'

interface ImageLightboxModalProps {
  image: { src: string; alt?: string } | null
  onClose: () => void
}

export function ImageLightboxModal({ image, onClose }: ImageLightboxModalProps) {
  const prefersReducedMotion = useReducedMotion()

  useEffect(() => {
    if (!image) return
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === 'Escape') {
        onClose()
      }
    }
    window.addEventListener('keydown', handleKeyDown)
    return () => window.removeEventListener('keydown', handleKeyDown)
  }, [image, onClose])

  return (
    <AnimatePresence>
      {image && (
        <motion.div
          role="dialog"
          aria-modal="true"
          aria-label={image.alt ?? 'Xem ảnh phóng to'}
          initial={prefersReducedMotion ? { opacity: 1 } : { opacity: 0 }}
          animate={{ opacity: 1 }}
          exit={prefersReducedMotion ? { opacity: 0 } : { opacity: 0 }}
          transition={{ duration: 0.16 }}
          onClick={onClose}
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/85 p-4 sm:p-8"
        >
          <div
            onClick={(event) => event.stopPropagation()}
            className="flex max-h-[90vh] max-w-5xl flex-col gap-3"
          >
            <div className="flex justify-end">
              <button
                type="button"
                onClick={onClose}
                aria-label="Đóng ảnh phóng to"
                className="inline-flex h-9 items-center gap-1.5 rounded-lg border border-white/25 bg-black/40 px-3 text-xs font-semibold text-white hover:border-white hover:bg-white/10 transition-colors cursor-pointer"
              >
                <X size={15} strokeWidth={1.75} />
                <span>Đóng</span>
              </button>
            </div>

            <img
              src={image.src}
              alt={image.alt ?? 'Ảnh phóng sự'}
              className="max-h-[76vh] w-auto max-w-full rounded-xl border border-white/15 object-contain bg-black"
            />

            {image.alt && (
              <p className="text-center text-xs leading-relaxed text-white/80">{image.alt}</p>
            )}
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  )
}
