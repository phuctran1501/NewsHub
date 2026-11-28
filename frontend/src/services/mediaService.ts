import type { ApiResponse, PagedApiResponse } from '@/types/api'
import { MediaFolder, type MediaFileDto, type MediaFolderValue } from '@/types/auth'
import { apiClient, unwrapData, unwrapPaged } from './apiClient'

export const mediaService = {
  async uploadImage(file: File, folder: MediaFolderValue): Promise<MediaFileDto> {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('folder', folder)

    return unwrapData(
      apiClient.post<ApiResponse<MediaFileDto>>('/api/v1/media/upload', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      }),
    )
  },

  async uploadAvatar(file: File): Promise<MediaFileDto> {
    return this.uploadImage(file, MediaFolder.Avatar)
  },

  async getMyMedia(
    params?: {
      pageNumber?: number
      pageSize?: number
      folder?: MediaFolderValue
    },
    signal?: AbortSignal,
  ): Promise<PagedApiResponse<MediaFileDto>> {
    return unwrapPaged(
      apiClient.get<PagedApiResponse<MediaFileDto>>('/api/v1/media/my-media', {
        signal,
        params: {
          pageNumber: params?.pageNumber ?? 1,
          pageSize: params?.pageSize ?? 12,
          folder: params?.folder || undefined,
        },
      }),
    )
  },
}
