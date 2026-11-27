import { Toast } from '@base-ui/react/toast'
import type { ButtonHTMLAttributes } from 'react'

export type AppToastType = 'success' | 'error' | 'warning' | 'info' | 'loading'

export interface AppToastOptions {
  title: string
  description?: string
  type?: AppToastType
  timeout?: number
  dedupeKey?: string
  actionProps?: ButtonHTMLAttributes<HTMLButtonElement>
}

export const toastManager = Toast.createToastManager()

const DEDUPE_WINDOW_MS = 1800
const activeDedupeMap = new Map<string, { id: string; timestamp: number }>()

function buildDedupeKey(options: AppToastOptions): string {
  return (
    options.dedupeKey ??
    `${options.type ?? 'info'}::${options.title.trim()}::${(options.description ?? '').trim()}`
  )
}

function addToast(options: AppToastOptions): string {
  const key = buildDedupeKey(options)
  const now = Date.now()
  const existing = activeDedupeMap.get(key)

  if (existing && now - existing.timestamp < DEDUPE_WINDOW_MS) {
    activeDedupeMap.set(key, { id: existing.id, timestamp: now })
    toastManager.update(existing.id, {
      title: options.title,
      description: options.description,
      type: options.type ?? 'info',
      timeout: options.timeout,
      actionProps: options.actionProps,
    })
    return existing.id
  }

  const id = toastManager.add({
    title: options.title,
    description: options.description,
    type: options.type ?? 'info',
    timeout: options.timeout,
    actionProps: options.actionProps,
    onClose: () => {
      const tracked = activeDedupeMap.get(key)
      if (tracked?.id === id) {
        activeDedupeMap.delete(key)
      }
    },
  })

  activeDedupeMap.set(key, { id, timestamp: now })
  return id
}

export const toast = {
  add: addToast,
  success(title: string, description?: string, extra?: Partial<AppToastOptions>): string {
    return addToast({ ...extra, title, description, type: 'success' })
  },
  error(title: string, description?: string, extra?: Partial<AppToastOptions>): string {
    return addToast({ ...extra, title, description, type: 'error' })
  },
  warning(title: string, description?: string, extra?: Partial<AppToastOptions>): string {
    return addToast({ ...extra, title, description, type: 'warning' })
  },
  info(title: string, description?: string, extra?: Partial<AppToastOptions>): string {
    return addToast({ ...extra, title, description, type: 'info' })
  },
  close(id: string): void {
    toastManager.close(id)
  },
  promise<T>(
    promiseValue: Promise<T>,
    messages: {
      loading: { title: string; description?: string }
      success: { title: string; description?: string } | ((data: T) => { title: string; description?: string })
      error: { title: string; description?: string } | ((err: unknown) => { title: string; description?: string })
    },
  ): Promise<T> {
    return toastManager.promise(promiseValue, {
      loading: { ...messages.loading, type: 'loading' },
      success: (data) => {
        const resolved = typeof messages.success === 'function' ? messages.success(data) : messages.success
        return { ...resolved, type: 'success' }
      },
      error: (err) => {
        const resolved = typeof messages.error === 'function' ? messages.error(err) : messages.error
        return { ...resolved, type: 'error' }
      },
    })
  },
}
