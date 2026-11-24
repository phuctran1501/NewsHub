export function safeGetStorageItem(key: string): string | null {
  if (typeof window === 'undefined') return null
  try {
    return window.localStorage.getItem(key)
  } catch {
    return null
  }
}

export function safeSetStorageItem(key: string, value: string): void {
  if (typeof window === 'undefined') return
  try {
    window.localStorage.setItem(key, value)
  } catch {
    // Bỏ qua an toàn khi trình duyệt chặn Web Storage (Private Mode nghiêm ngặt)
  }
}

export function safeRemoveStorageItem(key: string): void {
  if (typeof window === 'undefined') return
  try {
    window.localStorage.removeItem(key)
  } catch {
    // Bỏ qua an toàn khi trình duyệt chặn Web Storage
  }
}
