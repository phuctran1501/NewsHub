export interface ApiResponse<T> {
  data: T
  meta: Record<string, unknown>
  correlationId: string
}

export interface PaginationMeta {
  pageNumber: number
  pageSize: number
  totalCount: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface PagedApiResponse<T> {
  data: T[]
  meta: PaginationMeta
  correlationId: string
}

export interface ApiValidationErrorItem {
  field: string
  message: string
}

export interface ApiErrorDetail {
  code: string
  message: string
  details: ApiValidationErrorItem[]
}

export interface ApiErrorResponse {
  error: ApiErrorDetail
  correlationId: string
}

export class AppClientError extends Error {
  readonly status: number
  readonly code: string
  readonly details: ApiValidationErrorItem[]
  readonly correlationId?: string

  constructor(params: {
    message: string
    status: number
    code: string
    details?: ApiValidationErrorItem[]
    correlationId?: string
  }) {
    super(params.message)
    this.name = 'AppClientError'
    this.status = params.status
    this.code = params.code
    this.details = params.details ?? []
    this.correlationId = params.correlationId
  }
}
