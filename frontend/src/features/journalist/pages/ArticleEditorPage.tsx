import { useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { Button } from '@/components/common/Button'
import { EmptyState } from '@/components/common/EmptyState'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { toast } from '@/components/ui/toastManager'
import { useCategories } from '@/features/categories/hooks/useCategories'
import { useAuth } from '@/hooks/useAuth'
import { buildCmsEditorPath, buildDraftPreviewPath, ROUTES } from '@/routes/paths'
import type { ArticleDetailDto, CategorySuggestionDto } from '@/types/article'
import { isCompanyOwnerRole, type MyCompanyDto } from '@/types/company'
import { AiCategorySuggester } from '../components/AiCategorySuggester'
import { ArticleStatusBadge } from '../components/ArticleStatusBadge'
import { CoverImageUploader, type CoverImageValue } from '../components/CoverImageUploader'
import { PublishingContextSelector } from '../components/PublishingContextSelector'
import { TipTapRichEditor } from '../components/TipTapRichEditor'
import { normalizeArticleStatus } from '../utils/articleStatus'
import { useMyCompanies } from '../hooks/useCompanyHub'
import {
  useArticleForEdit,
  useCreateArticle,
  useGenerateDraftPreviewToken,
  usePublishArticle,
  useSubmitArticleToCompany,
  useSuggestCategories,
  useUpdateArticle,
} from '../hooks/useMyArticles'

interface LocalDraftSnapshot {
  title: string
  contentHtml: string
  coverImage: CoverImageValue | null
  categoryIds: string[]
  companyId: string | null
  sourceName: string
  sourceUrl: string
  savedAt: string
}

function getDraftStorageKey(articleId: string | undefined): string {
  return `nh_editor_draft_${articleId ?? 'new'}`
}

function readLocalDraft(storageKey: string): LocalDraftSnapshot | null {
  try {
    const raw = window.localStorage.getItem(storageKey)
    if (!raw) return null
    const parsed = JSON.parse(raw) as Partial<LocalDraftSnapshot>
    if (typeof parsed.title !== 'string' || typeof parsed.contentHtml !== 'string') {
      return null
    }
    return {
      title: parsed.title,
      contentHtml: parsed.contentHtml,
      coverImage: parsed.coverImage ?? null,
      categoryIds: Array.isArray(parsed.categoryIds) ? parsed.categoryIds : [],
      companyId: typeof parsed.companyId === 'string' ? parsed.companyId : null,
      sourceName: typeof parsed.sourceName === 'string' ? parsed.sourceName : '',
      sourceUrl: typeof parsed.sourceUrl === 'string' ? parsed.sourceUrl : '',
      savedAt: typeof parsed.savedAt === 'string' ? parsed.savedAt : new Date().toISOString(),
    }
  } catch {
    return null
  }
}

function clearLocalDraft(storageKey: string): void {
  try {
    window.localStorage.removeItem(storageKey)
  } catch {
    // ignore storage errors
  }
}

function stripHtmlToText(html: string): string {
  return html
    .replace(/<[^>]+>/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
}

function formatClockTime(iso: string): string {
  const date = new Date(iso)
  if (Number.isNaN(date.getTime())) return ''
  return date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
}

interface ArticleEditorFormProps {
  articleId?: string
  initialArticle?: ArticleDetailDto
  companies: MyCompanyDto[]
  isLoadingCompanies: boolean
  onDiscardLocalDraft: () => void
}

function ArticleEditorForm({
  articleId,
  initialArticle,
  companies,
  isLoadingCompanies,
  onDiscardLocalDraft,
}: ArticleEditorFormProps) {
  const navigate = useNavigate()
  const { roleName } = useAuth()
  const { data: allCategories = [] } = useCategories()

  const storageKey = getDraftStorageKey(articleId)
  const restoredSnapshot = useMemo(() => readLocalDraft(storageKey), [storageKey])

  const initialCover: CoverImageValue | null = initialArticle?.coverImage
    ? {
        id: initialArticle.coverImage.id,
        url: initialArticle.coverImage.url,
        width: initialArticle.coverImage.width,
        height: initialArticle.coverImage.height,
        format: initialArticle.coverImage.format,
      }
    : null

  const [title, setTitle] = useState(
    () => restoredSnapshot?.title ?? initialArticle?.title ?? '',
  )
  const [contentHtml, setContentHtml] = useState(
    () => restoredSnapshot?.contentHtml ?? initialArticle?.contentHtml ?? '',
  )
  const [coverImage, setCoverImage] = useState<CoverImageValue | null>(
    () => restoredSnapshot?.coverImage ?? initialCover,
  )
  const [categoryIds, setCategoryIds] = useState<string[]>(
    () =>
      restoredSnapshot?.categoryIds ??
      initialArticle?.categories.map((category) => category.id) ??
      [],
  )
  const [companyId, setCompanyId] = useState<string | null>(() =>
    initialArticle ? initialArticle.companyId : (restoredSnapshot?.companyId ?? null),
  )
  const [sourceName, setSourceName] = useState(
    () => restoredSnapshot?.sourceName ?? initialArticle?.sourceName ?? '',
  )
  const [sourceUrl, setSourceUrl] = useState(
    () => restoredSnapshot?.sourceUrl ?? initialArticle?.sourceUrl ?? '',
  )
  const [suggestions, setSuggestions] = useState<CategorySuggestionDto[]>([])
  const [lastAutosavedAt, setLastAutosavedAt] = useState<string | null>(
    () => restoredSnapshot?.savedAt ?? null,
  )
  const [isDirty, setIsDirty] = useState(() => Boolean(restoredSnapshot))
  const [showReReviewConfirm, setShowReReviewConfirm] = useState(false)

  const createMutation = useCreateArticle()
  const updateMutation = useUpdateArticle()
  const publishMutation = usePublishArticle()
  const submitMutation = useSubmitArticleToCompany()
  const previewMutation = useGenerateDraftPreviewToken()
  const suggestMutation = useSuggestCategories()

  const isBusy =
    createMutation.isPending ||
    updateMutation.isPending ||
    publishMutation.isPending ||
    submitMutation.isPending ||
    previewMutation.isPending

  const currentStatus = initialArticle ? normalizeArticleStatus(initialArticle.status) : 'Draft'
  const selectedCompany = useMemo(
    () => companies.find((company) => company.id === companyId) ?? null,
    [companies, companyId],
  )

  const willRevertPublishedCompanyArticle = useMemo(() => {
    if (!initialArticle || !initialArticle.companyId) return false
    if (currentStatus !== 'Published') return false
    if (roleName === 'Admin') return false
    const membership = companies.find((c) => c.id === initialArticle.companyId)
    return !membership || !isCompanyOwnerRole(membership.role)
  }, [initialArticle, currentStatus, roleName, companies])

  const hasMountedRef = useRef(false)
  useEffect(() => {
    if (!hasMountedRef.current) {
      hasMountedRef.current = true
      return
    }

    const timer = window.setTimeout(() => {
      const hasAnyContent =
        title.trim().length > 0 ||
        stripHtmlToText(contentHtml).length > 0 ||
        Boolean(coverImage) ||
        categoryIds.length > 0

      if (!hasAnyContent) {
        clearLocalDraft(storageKey)
        return
      }

      const savedAt = new Date().toISOString()
      const snapshot: LocalDraftSnapshot = {
        title,
        contentHtml,
        coverImage,
        categoryIds,
        companyId,
        sourceName,
        sourceUrl,
        savedAt,
      }
      try {
        window.localStorage.setItem(storageKey, JSON.stringify(snapshot))
        setLastAutosavedAt(savedAt)
      } catch {
        // ignore quota errors
      }
    }, 1500)

    return () => window.clearTimeout(timer)
  }, [storageKey, title, contentHtml, coverImage, categoryIds, companyId, sourceName, sourceUrl])

  useEffect(() => {
    if (!isDirty) return
    const handleBeforeUnload = (event: BeforeUnloadEvent) => {
      event.preventDefault()
      event.returnValue = ''
    }
    window.addEventListener('beforeunload', handleBeforeUnload)
    return () => window.removeEventListener('beforeunload', handleBeforeUnload)
  }, [isDirty])

  const markDirty = () => {
    if (!isDirty) setIsDirty(true)
  }

  const validateBeforeSave = (): boolean => {
    if (!title.trim()) {
      toast.error('Thiếu tiêu đề bài viết', 'Vui lòng nhập tiêu đề bài viết trước khi lưu.')
      return false
    }
    if (title.trim().length > 300) {
      toast.error('Tiêu đề quá dài', 'Tiêu đề bài viết tối đa 300 ký tự.')
      return false
    }
    if (!stripHtmlToText(contentHtml)) {
      toast.error('Thiếu nội dung bài viết', 'Vui lòng nhập nội dung bài báo trước khi lưu.')
      return false
    }
    if (sourceUrl.trim() && !/^https?:\/\//i.test(sourceUrl.trim())) {
      toast.error('Đường dẫn nguồn không hợp lệ', 'URL nguồn phải bắt đầu bằng http:// hoặc https://')
      return false
    }
    return true
  }

  const saveArticleToServer = async (): Promise<ArticleDetailDto | null> => {
    if (!validateBeforeSave()) return null

    if (articleId) {
      const updated = await updateMutation.mutateAsync({
        articleId,
        payload: {
          title: title.trim(),
          contentHtml,
          coverImageId: coverImage?.id ?? null,
          categoryIds,
          sourceName: sourceName.trim(),
          sourceUrl: sourceUrl.trim(),
        },
      })
      clearLocalDraft(storageKey)
      setIsDirty(false)
      setLastAutosavedAt(null)
      return updated
    }

    const created = await createMutation.mutateAsync({
      title: title.trim(),
      contentHtml,
      coverImageId: coverImage?.id ?? null,
      categoryIds,
      companyId,
      sourceName: sourceName.trim() || null,
      sourceUrl: sourceUrl.trim() || null,
    })
    clearLocalDraft(storageKey)
    setIsDirty(false)
    setLastAutosavedAt(null)
    navigate(buildCmsEditorPath(created.id), { replace: true })
    return created
  }

  const executeSaveDraft = async () => {
    const saved = await saveArticleToServer()
    if (!saved) return
    if (normalizeArticleStatus(saved.status) === 'PendingCompanyReview') {
      toast.success(
        'Đã lưu thay đổi & chuyển về Chờ duyệt',
        'Bài viết thuộc Tòa soạn đã được chuyển sang hàng đợi chờ Tổng biên tập duyệt lại.',
      )
    } else {
      toast.success('Đã lưu bài viết thành công')
    }
  }

  const handleSaveDraftClick = () => {
    if (willRevertPublishedCompanyArticle) {
      setShowReReviewConfirm(true)
      return
    }
    void executeSaveDraft()
  }

  const handlePreviewClick = async () => {
    const saved = isDirty || !articleId ? await saveArticleToServer() : initialArticle
    const targetId = saved?.id ?? articleId
    if (!targetId) return

    const tokenResult = await previewMutation.mutateAsync(targetId)
    const previewPath = buildDraftPreviewPath(tokenResult.token)
    window.open(previewPath, '_blank', 'noopener,noreferrer')
  }

  const handlePrimaryPublishAction = async () => {
    const saved = await saveArticleToServer()
    if (!saved) return

    if (companyId) {
      await submitMutation.mutateAsync(saved.id)
      navigate(ROUTES.CMS.ARTICLES)
      return
    }

    await publishMutation.mutateAsync(saved.id)
    navigate(ROUTES.CMS.ARTICLES)
  }

  const handleToggleCategory = (catId: string) => {
    markDirty()
    setCategoryIds((prev) =>
      prev.includes(catId) ? prev.filter((id) => id !== catId) : [...prev, catId],
    )
  }

  const handleRequestAiCategories = () => {
    const snippet = stripHtmlToText(contentHtml).slice(0, 4500)
    suggestMutation.mutate(
      { title: title.trim(), contentSnippet: snippet },
      {
        onSuccess: (res) => {
          setSuggestions(res.suggestions)
          if (res.suggestions.length === 0) {
            toast.info('Chưa tìm thấy chuyên mục phù hợp', 'Bạn có thể chọn chuyên mục thủ công bên dưới.')
          } else {
            toast.success(
              `AI gợi ý ${res.suggestions.length} chuyên mục`,
              'Hãy tích chọn hoặc bỏ chọn các chuyên mục phù hợp trước khi lưu.',
            )
          }
        },
      },
    )
  }

  const plainTextLength = stripHtmlToText(contentHtml).length
  const canSuggestAi = title.trim().length >= 5 && plainTextLength >= 20

  return (
    <div className="space-y-5 pb-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="space-y-1">
          <div className="flex items-center gap-2.5">
            <h1 className="text-xl font-bold tracking-tight text-neutral-text-main dark:text-dark-text-main">
              {articleId ? 'Chỉnh sửa bài viết' : 'Soạn thảo bài viết mới'}
            </h1>
            {initialArticle && <ArticleStatusBadge status={initialArticle.status} />}
          </div>
          <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
            {lastAutosavedAt
              ? `Bản nháp tự động đã lưu cục bộ lúc ${formatClockTime(lastAutosavedAt)} - an toàn khi tải lại trang.`
              : 'Nội dung sẽ tự động sao lưu cục bộ trong lúc bạn soạn thảo.'}
          </p>
        </div>

        <div className="flex items-center gap-2">
          {restoredSnapshot && (
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => {
                clearLocalDraft(storageKey)
                onDiscardLocalDraft()
                toast.info('Đã hủy bản nháp cục bộ')
              }}
            >
              Hủy bản nháp cục bộ
            </Button>
          )}
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => navigate(ROUTES.CMS.ARTICLES)}
          >
            Danh sách bài viết
          </Button>
        </div>
      </div>

      {willRevertPublishedCompanyArticle && (
        <div
          role="status"
          className="rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface px-4 py-3 text-xs text-neutral-text-main dark:text-dark-text-main"
        >
          <span className="font-semibold">Lưu ý kiểm duyệt Tòa soạn:</span> Bài viết này đã được Tòa
          soạn duyệt xuất bản. Khi bạn lưu chỉnh sửa, hệ thống sẽ tự động chuyển bài về trạng thái{' '}
          <span className="font-semibold">Chờ duyệt</span> để Chủ tòa soạn kiểm duyệt lại trước khi
          cập nhật công khai.
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-5 items-start">
        <div className="lg:col-span-8 space-y-5">
          <div className="rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-5 space-y-4">
            <CoverImageUploader
              value={coverImage}
              disabled={isBusy}
              onChange={(next) => {
                markDirty()
                setCoverImage(next)
              }}
            />

            <div className="space-y-1.5">
              <div className="flex items-center justify-between">
                <label
                  htmlFor="article-title-input"
                  className="text-xs font-semibold text-neutral-text-muted dark:text-dark-text-muted"
                >
                  Tiêu đề bài viết
                </label>
                <span className="text-[11px] font-mono text-neutral-text-muted dark:text-dark-text-muted">
                  {title.length}/300
                </span>
              </div>
              <input
                id="article-title-input"
                type="text"
                maxLength={300}
                value={title}
                disabled={isBusy}
                onChange={(event) => {
                  markDirty()
                  setTitle(event.target.value)
                }}
                placeholder="Nhập tiêu đề bài báo rõ ràng, chuẩn mực..."
                className="w-full rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-bg px-3.5 py-2.5 text-lg font-bold tracking-tight text-neutral-text-main dark:text-dark-text-main placeholder:text-neutral-text-muted/70 focus:outline-none focus:border-primary dark:focus:border-secondary transition-colors"
              />
            </div>

            <div className="space-y-1.5">
              <label className="text-xs font-semibold text-neutral-text-muted dark:text-dark-text-muted block">
                Nội dung bài viết
              </label>
              <TipTapRichEditor
                value={contentHtml}
                disabled={isBusy}
                onChange={(html) => {
                  markDirty()
                  setContentHtml(html)
                }}
              />
            </div>
          </div>
        </div>

        <aside className="lg:col-span-4 space-y-5">
          <PublishingContextSelector
            companies={companies}
            selectedCompanyId={companyId}
            locked={Boolean(articleId)}
            lockedCompanyName={initialArticle?.companyName}
            isLoading={isLoadingCompanies}
            onChange={(nextId) => {
              markDirty()
              setCompanyId(nextId)
            }}
          />

          <AiCategorySuggester
            allCategories={allCategories}
            selectedIds={categoryIds}
            onToggleCategory={handleToggleCategory}
            suggestions={suggestions}
            isSuggesting={suggestMutation.isPending}
            onRequestSuggest={handleRequestAiCategories}
            canSuggest={canSuggestAi}
          />

          <section className="rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface p-4 space-y-3">
            <div className="space-y-0.5">
              <h2 className="text-sm font-semibold text-neutral-text-main dark:text-dark-text-main">
                Nguồn trích dẫn (Tùy chọn)
              </h2>
              <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
                Ghi rõ nguồn tư liệu hoặc cơ quan phát ngôn nếu bài viết tổng hợp.
              </p>
            </div>

            <div className="space-y-2.5">
              <input
                type="text"
                value={sourceName}
                disabled={isBusy}
                onChange={(event) => {
                  markDirty()
                  setSourceName(event.target.value)
                }}
                placeholder="Tên nguồn (VD: Bộ TT&TT, Reuters...)"
                className="w-full rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-bg px-3 py-2 text-xs text-neutral-text-main dark:text-dark-text-main focus:outline-none focus:border-primary dark:focus:border-secondary"
              />
              <input
                type="url"
                value={sourceUrl}
                disabled={isBusy}
                onChange={(event) => {
                  markDirty()
                  setSourceUrl(event.target.value)
                }}
                placeholder="Đường dẫn nguồn (https://...)"
                className="w-full rounded-[10px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-bg px-3 py-2 text-xs text-neutral-text-main dark:text-dark-text-main focus:outline-none focus:border-primary dark:focus:border-secondary"
              />
            </div>
          </section>
        </aside>
      </div>

      {/* Sticky Bottom Dynamic Action Footer — adapts automatically to Sidebar collapse/expand */}
      <div className="sticky bottom-3 z-30 rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-surface/95 dark:bg-dark-surface/95 backdrop-blur-md px-4 py-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <p className="text-xs text-neutral-text-muted dark:text-dark-text-muted">
            Ngữ cảnh:{' '}
            <span className="font-semibold text-neutral-text-main dark:text-dark-text-main">
              {companyId
                ? selectedCompany?.name || initialArticle?.companyName || 'Tòa soạn trực thuộc'
                : 'Tự do cá nhân (Freelance)'}
            </span>
          </p>

          <div className="flex flex-wrap items-center gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              isLoading={createMutation.isPending || updateMutation.isPending}
              disabled={isBusy}
              onClick={handleSaveDraftClick}
            >
              {articleId ? 'Lưu thay đổi' : 'Lưu nháp'}
            </Button>

            <Button
              type="button"
              variant="secondary"
              size="sm"
              isLoading={previewMutation.isPending}
              disabled={isBusy}
              onClick={() => void handlePreviewClick()}
            >
              Xem trước
            </Button>

            {currentStatus !== 'Published' && (
              <Button
                type="button"
                variant="primary"
                size="sm"
                isLoading={publishMutation.isPending || submitMutation.isPending}
                disabled={isBusy}
                onClick={() => void handlePrimaryPublishAction()}
              >
                {companyId ? 'Nộp bài lên Tòa soạn duyệt' : 'Xuất bản ngay'}
              </Button>
            )}
          </div>
        </div>
      </div>

      <Dialog open={showReReviewConfirm} onOpenChange={setShowReReviewConfirm}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Xác nhận chỉnh sửa bài Tòa soạn đã xuất bản</DialogTitle>
            <DialogDescription>
              Bài viết này đang hiển thị dưới danh nghĩa Tòa soạn. Sau khi lưu thay đổi, bài sẽ tạm
              chuyển về trạng thái Chờ duyệt để Chủ tòa soạn thẩm định lại. Bạn muốn tiếp tục?
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={() => setShowReReviewConfirm(false)}
            >
              Hủy bỏ
            </Button>
            <Button
              type="button"
              variant="primary"
              size="sm"
              isLoading={updateMutation.isPending}
              onClick={() => {
                setShowReReviewConfirm(false)
                void executeSaveDraft()
              }}
            >
              Lưu và gửi duyệt lại
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}

export function ArticleEditorPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const [draftResetNonce, setDraftResetNonce] = useState(0)

  const { data: companies = [], isLoading: isLoadingCompanies } = useMyCompanies()
  const {
    data: loadedArticle,
    isLoading: isLoadingArticle,
    isError: isArticleError,
  } = useArticleForEdit(id)

  if (id && isLoadingArticle) {
    return (
      <div className="space-y-4">
        <div className="h-8 w-56 rounded-[8px] bg-neutral-surface dark:bg-dark-surface animate-pulse" />
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-5">
          <div className="lg:col-span-8 h-[520px] rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface animate-pulse" />
          <div className="lg:col-span-4 h-[400px] rounded-[12px] border border-neutral-border dark:border-dark-border bg-neutral-surface dark:bg-dark-surface animate-pulse" />
        </div>
      </div>
    )
  }

  if (id && (isArticleError || !loadedArticle)) {
    return (
      <EmptyState
        title="Không tìm thấy bài viết cần chỉnh sửa"
        description="Bài viết có thể đã bị xóa hoặc bạn không có quyền chỉnh sửa nội dung này."
        actionLabel="Về danh sách bài viết"
        onAction={() => navigate(ROUTES.CMS.ARTICLES)}
      />
    )
  }

  const formKey = `${id ?? 'new'}:${loadedArticle?.updatedAt ?? loadedArticle?.createdAt ?? 'fresh'}:${draftResetNonce}`

  return (
    <ArticleEditorForm
      key={formKey}
      articleId={id}
      initialArticle={loadedArticle}
      companies={companies}
      isLoadingCompanies={isLoadingCompanies}
      onDiscardLocalDraft={() => setDraftResetNonce((prev) => prev + 1)}
    />
  )
}
