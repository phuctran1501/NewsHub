import { useEffect, useState } from 'react'
import { Outlet, ScrollRestoration } from 'react-router-dom'
import { CommandSearchModal } from '@/components/common/CommandSearchModal'
import { PageTransition } from '@/components/common/PageTransition'
import { ScrollToTopButton } from '@/components/common/ScrollToTopButton'
import { Footer } from './Footer'
import { Header } from './Header'
import { MobileBottomNav } from './MobileBottomNav'

export function RootLayout() {
  const [isSearchOpen, setIsSearchOpen] = useState(false)

  useEffect(() => {
    const handleGlobalShortcut = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault()
        setIsSearchOpen((prev) => !prev)
      }
    }

    window.addEventListener('keydown', handleGlobalShortcut)
    return () => window.removeEventListener('keydown', handleGlobalShortcut)
  }, [])

  return (
    <div className="min-h-screen flex flex-col bg-neutral-bg dark:bg-dark-bg text-neutral-text-main dark:text-dark-text-main">
      <ScrollRestoration />
      <Header onOpenSearch={() => setIsSearchOpen(true)} />
      <main className="flex-1">
        <PageTransition>
          <Outlet />
        </PageTransition>
      </main>
      <Footer />
      <ScrollToTopButton />
      <MobileBottomNav
        isSearchOpen={isSearchOpen}
        onOpenSearch={() => setIsSearchOpen(true)}
      />
      <CommandSearchModal isOpen={isSearchOpen} onClose={() => setIsSearchOpen(false)} />
    </div>
  )
}
