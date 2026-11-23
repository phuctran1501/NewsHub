/**
 * Quản lý cấu hình biến môi trường tập trung và type-safe cho NewsHub Client.
 */
const rawApiBaseUrl =
  import.meta.env.VITE_API_BASE_URL ||
  (import.meta.env.DEV ? 'http://localhost:5000' : 'https://newshub-api-staging.onrender.com')

export const env = {
  // Defensive normalize: loại bỏ khoảng trắng và dấu gạch chéo cuối nếu người dùng nhập dư
  apiBaseUrl: rawApiBaseUrl.trim().replace(/\/+$/, ''),
  isProduction: import.meta.env.PROD,
  isDevelopment: import.meta.env.DEV,
} as const
