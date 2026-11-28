import DOMPurify from 'dompurify'
import { useMemo, type MouseEvent } from 'react'

interface ArticleRichContentProps {
  html: string
  onImageClick: (src: string, alt?: string) => void
}

export function ArticleRichContent({ html, onImageClick }: ArticleRichContentProps) {
  const sanitizedHtml = useMemo(() => {
    const clean = DOMPurify.sanitize(html, {
      USE_PROFILES: { html: true },
      FORBID_TAGS: ['script', 'style', 'iframe', 'object', 'embed', 'form'],
      FORBID_ATTR: ['onerror', 'onload', 'onclick', 'onmouseover', 'style'],
      ADD_ATTR: ['target', 'rel'],
    })

    // Chuẩn hóa rel an toàn cho các liên kết ngoài mở tab mới
    return clean.replace(
      /<a\s+([^>]*target=["']_blank["'][^>]*)>/gi,
      (match) => (match.includes('rel=') ? match : match.replace('>', ' rel="noopener noreferrer">')),
    )
  }, [html])

  function handleContentClick(event: MouseEvent<HTMLDivElement>) {
    const target = event.target as HTMLElement | null
    if (target && target.tagName === 'IMG') {
      const img = target as HTMLImageElement
      if (img.src) {
        onImageClick(img.src, img.alt || undefined)
      }
    }
  }

  return (
    <div
      onClick={handleContentClick}
      className="nh-article-prose text-neutral-text-main dark:text-dark-text-main"
      dangerouslySetInnerHTML={{ __html: sanitizedHtml }}
    />
  )
}
