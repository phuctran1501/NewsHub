import { useContext } from 'react'
import { ThemeContext, type ThemeContextValue } from '../store/themeContextDef'

export function useTheme(): ThemeContextValue {
  const context = useContext(ThemeContext)
  if (!context) {
    throw new Error('useTheme phải được sử dụng bên trong ThemeProvider.')
  }
  return context
}
