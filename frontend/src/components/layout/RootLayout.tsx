import { Outlet } from 'react-router-dom'
import { Footer } from './Footer'
import { Header } from './Header'
import { MobileBottomNav } from './MobileBottomNav'

export function RootLayout() {
  return (
    <div className="min-h-screen flex flex-col bg-neutral-bg dark:bg-dark-bg text-neutral-text-main dark:text-dark-text-main">
      <Header />
      <main className="flex-1">
        <Outlet />
      </main>
      <Footer />
      <MobileBottomNav />
    </div>
  )
}
