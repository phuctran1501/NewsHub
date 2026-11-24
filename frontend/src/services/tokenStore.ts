type AuthFailureListener = () => void
type TokenChangeListener = (token: string | null) => void

let inMemoryAccessToken: string | null = null
const failureListeners = new Set<AuthFailureListener>()
const tokenChangeListeners = new Set<TokenChangeListener>()

function emitTokenChange(token: string | null): void {
  tokenChangeListeners.forEach((listener) => listener(token))
}

export function getAccessToken(): string | null {
  return inMemoryAccessToken
}

export function setAccessToken(token: string | null): void {
  inMemoryAccessToken = token
  emitTokenChange(token)
}

export function clearAccessToken(): void {
  inMemoryAccessToken = null
  emitTokenChange(null)
}

export function onTokenChange(listener: TokenChangeListener): () => void {
  tokenChangeListeners.add(listener)
  return () => {
    tokenChangeListeners.delete(listener)
  }
}

export function onAuthFailure(listener: AuthFailureListener): () => void {
  failureListeners.add(listener)
  return () => {
    failureListeners.delete(listener)
  }
}

export function notifyAuthFailure(): void {
  inMemoryAccessToken = null
  emitTokenChange(null)
  failureListeners.forEach((listener) => listener())
}
