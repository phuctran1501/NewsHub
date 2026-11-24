import { Eye, EyeOff } from 'lucide-react'
import { useState, type ChangeEvent } from 'react'
import { Input } from '@/components/common/Input'
import { cn } from '@/utils/cn'
import { getPasswordCriteria } from '@/utils/validation'

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
  showStrengthChecklist?: boolean
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
  showStrengthChecklist = false,
}: PasswordInputProps) {
  const [isVisible, setIsVisible] = useState(false)
  const criteria = getPasswordCriteria(value)

  const checklistItems = [
    { key: 'minLength', label: 'Tối thiểu 8 ký tự', met: criteria.minLength },
    { key: 'hasUppercase', label: 'Có chữ in hoa (A-Z)', met: criteria.hasUppercase },
    { key: 'hasLowercase', label: 'Có chữ thường (a-z)', met: criteria.hasLowercase },
    { key: 'hasDigit', label: 'Có chữ số (0-9)', met: criteria.hasDigit },
  ]

  return (
    <div className="space-y-2">
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

      {showStrengthChecklist && value.length > 0 && (
        <ul
          aria-label="Tiêu chí độ mạnh mật khẩu"
          className="grid grid-cols-2 gap-1.5 pt-0.5 text-[11px]"
        >
          {checklistItems.map((item) => (
            <li
              key={item.key}
              className={cn(
                'flex items-center gap-1.5 rounded-[8px] border px-2.5 py-1 transition-colors duration-150',
                item.met
                  ? 'border-status-published/35 bg-status-published/10 text-emerald-700 dark:text-emerald-400 font-medium'
                  : 'border-neutral-border dark:border-dark-border bg-neutral-bg dark:bg-dark-bg text-neutral-text-muted dark:text-dark-text-muted',
              )}
            >
              <span
                aria-hidden="true"
                className={cn(
                  'h-1.5 w-1.5 rounded-full shrink-0',
                  item.met
                    ? 'bg-status-published'
                    : 'bg-neutral-text-muted/45 dark:bg-dark-text-muted/45',
                )}
              />
              <span>{item.label}</span>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
