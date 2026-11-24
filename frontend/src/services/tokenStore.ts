type AuthFailureListener = () => void

let inMemoryAccessToken: string | null = null
const failureListeners = new Set<AuthFailureListener>()

export function getAccessToken(): string | null {
  return inMemoryAccessToken
}

export function setAccessToken(token: string | null): void {
  inMemoryAccessToken = token
}

export function clearAccessToken(): void {
  inMemoryAccessToken = null
}

export function onAuthFailure(listener: AuthFailureListener): () => void {
  failureListeners.add(listener)
  return () => {
    failureListeners.delete(listener)
  }
}

export function notifyAuthFailure(): void {
  inMemoryAccessToken = null
  failureListeners.forEach((listener) => listener())
}
