import { useContext } from 'react'
import { AuthContext, type AuthContextValue } from '../store/authContextDef'

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth phải được sử dụng bên trong AuthProvider.')
  }
  return context
}
