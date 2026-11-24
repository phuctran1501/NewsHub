import type { AppClientError } from '@/types/api'

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const VN_MOBILE_PATTERN = /^(?:\+84|84|0)([35789][0-9]{8})$/
const ALLOWED_AVATAR_MIME_TYPES = ['image/jpeg', 'image/png', 'image/webp']
const ALLOWED_AVATAR_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp']

export const MAX_AVATAR_FILE_BYTES = 5 * 1024 * 1024
export const BIO_MAX_LENGTH = 500
export const PRESS_CARD_MAX_LENGTH = 50

function getPasswordCriteria(password: string) {
  return {
    minLength: password.length >= 8,
    maxLength: password.length <= 100,
    hasUppercase: /[A-Z]/.test(password),
    hasLowercase: /[a-z]/.test(password),
    hasDigit: /[0-9]/.test(password),
  }
}

export function validateEmail(raw: string): string | null {
  const trimmed = raw.trim()
  if (!trimmed) {
    return 'Vui lòng nhập địa chỉ email.'
  }
  if (trimmed.length > 256) {
    return 'Địa chỉ email không được vượt quá 256 ký tự.'
  }
  if (!EMAIL_PATTERN.test(trimmed)) {
    return 'Địa chỉ email không đúng định dạng.'
  }
  return null
}

export function validateFullName(raw: string): string | null {
  const trimmed = raw.trim()
  if (!trimmed) {
    return 'Vui lòng nhập họ và tên.'
  }
  if (trimmed.length < 2) {
    return 'Họ và tên phải có ít nhất 2 ký tự.'
  }
  if (trimmed.length > 100) {
    return 'Họ và tên không được vượt quá 100 ký tự.'
  }
  return null
}

export function validatePassword(password: string): string | null {
  if (!password) {
    return 'Vui lòng nhập mật khẩu.'
  }
  const criteria = getPasswordCriteria(password)
  if (!criteria.minLength) {
    return 'Mật khẩu phải có ít nhất 8 ký tự.'
  }
  if (!criteria.maxLength) {
    return 'Mật khẩu không được vượt quá 100 ký tự.'
  }
  if (!criteria.hasUppercase || !criteria.hasLowercase || !criteria.hasDigit) {
    return 'Mật khẩu phải bao gồm chữ hoa, chữ thường và chữ số.'
  }
  return null
}

export function normalizeVietnamPhone(raw: string): string | null {
  const stripped = raw.trim().replace(/[\s.\-()]/g, '')
  if (!stripped) return null
  const match = VN_MOBILE_PATTERN.exec(stripped)
  return match ? `+84${match[1]}` : null
}

export function validateVietnamPhone(raw: string): string | null {
  const trimmed = raw.trim()
  if (!trimmed) {
    return null
  }
  if (!normalizeVietnamPhone(trimmed)) {
    return 'Số điện thoại không hợp lệ (hỗ trợ đầu số di động Việt Nam 03, 05, 07, 08, 09 hoặc +84).'
  }
  return null
}

export function validatePressCardNumber(raw: string, required = true): string | null {
  const trimmed = raw.trim()
  if (required && !trimmed) {
    return 'Vui lòng nhập số thẻ nhà báo.'
  }
  if (trimmed.length > PRESS_CARD_MAX_LENGTH) {
    return `Số thẻ nhà báo không được vượt quá ${PRESS_CARD_MAX_LENGTH} ký tự.`
  }
  return null
}

export function validateBio(raw: string): string | null {
  if (raw.length > BIO_MAX_LENGTH) {
    return `Tiểu sử không được vượt quá ${BIO_MAX_LENGTH} ký tự.`
  }
  return null
}

export function validateAvatarFile(file: File): string | null {
  if (file.size <= 0) {
    return 'Tệp ảnh rỗng hoặc không hợp lệ.'
  }
  if (file.size > MAX_AVATAR_FILE_BYTES) {
    return 'Dung lượng ảnh đại diện không được vượt quá 5MB.'
  }

  const lowerName = file.name.toLowerCase()
  const hasValidExt = ALLOWED_AVATAR_EXTENSIONS.some((ext) => lowerName.endsWith(ext))
  const hasValidMime = !file.type || ALLOWED_AVATAR_MIME_TYPES.includes(file.type)

  if (!hasValidExt || !hasValidMime) {
    return 'Chỉ chấp nhận định dạng ảnh .jpg, .jpeg, .png hoặc .webp.'
  }
  return null
}

export function extractFieldErrors(error: AppClientError): Record<string, string> {
  const mapped: Record<string, string> = {}
  for (const item of error.details) {
    if (!item.field || !item.message) continue
    const rawKey = item.field.split('.').pop() ?? item.field
    const camelKey = rawKey.charAt(0).toLowerCase() + rawKey.slice(1)
    if (!mapped[camelKey]) {
      mapped[camelKey] = item.message
    }
  }
  return mapped
}

function hasAsciiControlChars(input: string): boolean {
  for (let i = 0; i < input.length; i++) {
    if (input.charCodeAt(i) <= 0x1f) return true
  }
  return false
}

/**
 * Ngăn chặn lỗ hổng Open Redirect: chỉ cho phép đường dẫn nội bộ bắt đầu bằng đúng 1 dấu `/`,
 * không chứa ký tự điều khiển ASCII, ký tự thứ hai khác `/` và `\`, và không tự trỏ ngược lại `/auth`.
 */
export function getSafeRedirectPath(raw: string | null | undefined, fallback = '/'): string {
  if (!raw) return fallback
  const trimmed = raw.trim()
  if (!trimmed.startsWith('/') || hasAsciiControlChars(trimmed)) {
    return fallback
  }
  if (trimmed.length > 1 && (trimmed[1] === '/' || trimmed[1] === '\\')) {
    return fallback
  }
  if (trimmed === '/auth' || trimmed.startsWith('/auth/') || trimmed.startsWith('/auth?')) {
    return fallback
  }
  return trimmed
}
