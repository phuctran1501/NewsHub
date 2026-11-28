import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { toast } from '@/components/ui/toastManager'
import { useAuth } from '@/hooks/useAuth'
import { queryKeys } from '@/lib/queryKeys'
import { toAppClientError } from '@/services/apiClient'
import { companyService } from '@/services/companyService'

export function useMyCompanies() {
  const { isAuthenticated, roleName } = useAuth()
  const isJournalist = isAuthenticated && roleName === 'Journalist'

  return useQuery({
    queryKey: queryKeys.companies.myCompanies(),
    queryFn: ({ signal }) => companyService.getMyCompanies(signal),
    enabled: isJournalist,
    staleTime: 1000 * 60 * 2,
  })
}

export function useSearchCompanies(params: {
  searchTerm?: string
  pageNumber?: number
  pageSize?: number
}) {
  const { isAuthenticated, roleName } = useAuth()
  const isJournalist = isAuthenticated && roleName === 'Journalist'

  return useQuery({
    queryKey: queryKeys.companies.search(params),
    queryFn: ({ signal }) => companyService.searchCompanies(params, signal),
    enabled: isJournalist,
    staleTime: 1000 * 30,
  })
}

export function useMyInvitations() {
  const { isAuthenticated, roleName } = useAuth()
  const isJournalist = isAuthenticated && roleName === 'Journalist'

  return useQuery({
    queryKey: queryKeys.companies.myInvitations(),
    queryFn: ({ signal }) => companyService.getMyInvitations(signal),
    enabled: isJournalist,
    staleTime: 1000 * 30,
  })
}

export function useCompanyInternalArticles(
  companyId: string | undefined,
  params: { pageNumber?: number; pageSize?: number },
) {
  const { isAuthenticated, roleName } = useAuth()
  const isJournalist = isAuthenticated && roleName === 'Journalist'

  return useQuery({
    queryKey: queryKeys.companies.internalArticles(companyId ?? '', params),
    queryFn: ({ signal }) => companyService.getCompanyInternalArticles(companyId ?? '', params, signal),
    enabled: isJournalist && Boolean(companyId),
    staleTime: 1000 * 30,
  })
}

export function useRequestJoinCompany() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (companyId: string) => companyService.requestJoinCompany(companyId),
    onSuccess: (req) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.companies.all() })
      toast.success('Đã gửi đơn xin gia nhập', `Tòa soạn ${req.companyName} sẽ xem xét yêu cầu của bạn.`)
    },
    onError: (error) => {
      toast.error('Không thể gửi đơn xin gia nhập', toAppClientError(error).message)
    },
  })
}

export function useRespondToInvitation() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ requestId, accept }: { requestId: string; accept: boolean }) =>
      companyService.respondToInvitation(requestId, accept),
    onSuccess: (message) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.companies.all() })
      toast.success(message || 'Đã phản hồi lời mời thành công')
    },
    onError: (error) => {
      toast.error('Không thể phản hồi lời mời', toAppClientError(error).message)
    },
  })
}

export function useLeaveCompany() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (companyId: string) => companyService.leaveCompany(companyId),
    onSuccess: (message) => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.companies.all() })
      void queryClient.invalidateQueries({ queryKey: queryKeys.articles.all() })
      toast.success(
        'Đã rời Tòa soạn',
        message || 'Các bài viết nháp và chờ duyệt đã được chuyển về cá nhân.',
      )
    },
    onError: (error) => {
      toast.error('Không thể rời Tòa soạn', toAppClientError(error).message)
    },
  })
}
