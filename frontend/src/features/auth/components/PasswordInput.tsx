import { Eye, EyeOff } from 'lucide-react'
import { useState, type ChangeEvent } from 'react'
import { Input } from '@/components/common/Input'

interface PasswordInputProps {
  id?: string
  name?: string
  label: string
  value: string
  onChange: (event: ChangeEvent<HTMLInputElement>) => void
  placeholder?: string
  error?: string
  hint?: string
  autoComplete?: string
  disabled?: boolean
}

export function PasswordInput({
  id,
  name,
  label,
  value,
  onChange,
  placeholder = 'Nhập mật khẩu',
  error,
  hint,
  autoComplete,
  disabled,
}: PasswordInputProps) {
  const [isVisible, setIsVisible] = useState(false)

  return (
    <Input
      id={id}
      name={name}
      label={label}
      type={isVisible ? 'text' : 'password'}
      value={value}
      onChange={onChange}
      placeholder={placeholder}
      error={error}
      hint={hint}
      autoComplete={autoComplete}
      disabled={disabled}
      trailingSlot={
        <button
          type="button"
          onClick={() => setIsVisible((prev) => !prev)}
          disabled={disabled}
          aria-label={isVisible ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
          className="inline-flex items-center justify-center p-1 rounded text-neutral-text-muted dark:text-dark-text-muted hover:text-neutral-text-main dark:hover:text-dark-text-main focus-visible:outline-2 focus-visible:outline-primary cursor-pointer disabled:opacity-50"
        >
          {isVisible ? (
            <EyeOff size={16} strokeWidth={1.75} />
          ) : (
            <Eye size={16} strokeWidth={1.75} />
          )}
        </button>
      }
    />
  )
}
