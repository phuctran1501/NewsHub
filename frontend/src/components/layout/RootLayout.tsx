import { Outlet, ScrollRestoration } from 'react-router-dom'
import { PageTransition } from '@/components/common/PageTransition'
import { Footer } from './Footer'
import { Header } from './Header'
import { MobileBottomNav } from './MobileBottomNav'

export function RootLayout() {
  return (
    <div className="min-h-screen flex flex-col bg-neutral-bg dark:bg-dark-bg text-neutral-text-main dark:text-dark-text-main">
      <ScrollRestoration />
      <Header />
      <main className="flex-1">
        <PageTransition>
          <Outlet />
        </PageTransition>
      </main>
      <Footer />
      <MobileBottomNav />
    </div>
  )
}
