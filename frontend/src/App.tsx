import { QueryClientProvider } from '@tanstack/react-query'
import { MotionConfig } from 'framer-motion'
import { RouterProvider } from 'react-router-dom'
import { Toaster } from '@/components/ui/toast'
import { TooltipProvider } from '@/components/ui/tooltip'
import { queryClient } from '@/lib/queryClient'
import { appRouter } from '@/routes'
import { AuthProvider } from '@/store/AuthContext'
import { ThemeProvider } from '@/store/ThemeContext'

export default function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <MotionConfig reducedMotion="user">
        <ThemeProvider>
          <AuthProvider>
            <TooltipProvider delayDuration={150}>
              <RouterProvider router={appRouter} />
              <Toaster />
            </TooltipProvider>
          </AuthProvider>
        </ThemeProvider>
      </MotionConfig>
    </QueryClientProvider>
  )
}
