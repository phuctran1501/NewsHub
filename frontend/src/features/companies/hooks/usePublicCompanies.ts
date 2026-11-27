import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from '@/components/ui/toastManager'
import { useAuth } from '@/hooks/useAuth'
import { queryKeys } from '@/lib/queryKeys'
import { toAppClientError } from '@/services/apiClient'
import { companyService } from '@/services/companyService'

export function usePublicCompanies(params?: {
  pageNumber?: number
  pageSize?: number
  searchTerm?: string
}) {
  const normalized = {
    pageNumber: params?.pageNumber ?? 1,
    pageSize: params?.pageSize ?? 6,
    searchTerm: params?.searchTerm || undefined,
  }

  return useQuery({
    queryKey: queryKeys.companies.publicList(normalized),
    queryFn: ({ signal }) => companyService.getPublicCompanies(normalized, signal),
    staleTime: 5 * 60 * 1000,
  })
}

export function useFollowedCompanyIds() {
  const { isAuthenticated } = useAuth()

  const query = useQuery({
    queryKey: queryKeys.companies.following(1, 50),
    queryFn: ({ signal }) => companyService.getFollowedCompanies({ pageNumber: 1, pageSize: 50 }, signal),
    enabled: isAuthenticated,
    staleTime: 2 * 60 * 1000,
  })

  const followedIds = new Set((query.data?.data ?? []).map((item) => item.companyId))

  return {
    ...query,
    followedIds,
  }
}

export function useToggleFollowCompany() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: async ({
      companyId,
      isCurrentlyFollowing,
    }: {
      companyId: string
      companyName: string
      isCurrentlyFollowing: boolean
    }) => {
      if (isCurrentlyFollowing) {
        await companyService.unfollowCompany(companyId)
      } else {
        await companyService.followCompany(companyId)
      }
      return { companyId, nextFollowing: !isCurrentlyFollowing }
    },
    onSuccess: (_data, variables) => {
      if (variables.isCurrentlyFollowing) {
        toast.info('Đã bỏ theo dõi', `Bạn đã bỏ theo dõi tòa soạn ${variables.companyName}.`)
      } else {
        toast.success('Đã theo dõi tòa soạn', `Bạn sẽ nhận thông báo khi ${variables.companyName} xuất bản bài mới.`)
      }
      void queryClient.invalidateQueries({ queryKey: ['companies', 'following'] })
      void queryClient.invalidateQueries({ queryKey: ['companies', 'public'] })
    },
    onError: (err: unknown) => {
      const clientErr = toAppClientError(err)
      toast.error('Không thể thực hiện', clientErr.message)
    },
  })
}
