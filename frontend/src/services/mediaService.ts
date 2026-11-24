import type { ApiResponse } from '@/types/api'
import { MediaFolder, type MediaFileDto } from '@/types/auth'
import { apiClient, unwrapData } from './apiClient'

export const mediaService = {
  async uploadAvatar(file: File): Promise<MediaFileDto> {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('folder', MediaFolder.Avatar)

    return unwrapData(
      apiClient.post<ApiResponse<MediaFileDto>>('/api/v1/media/upload', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }),
    )
  },
}
