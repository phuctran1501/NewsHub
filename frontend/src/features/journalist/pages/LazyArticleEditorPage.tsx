import { lazy, Suspense } from 'react'

const LazyEditorInner = lazy(async () => {
  const mod = await import('./ArticleEditorPage')
  return { default: mod.ArticleEditorPage }
})

function EditorRouteFallback() {
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

export function LazyArticleEditorPage() {
  return (
    <Suspense fallback={<EditorRouteFallback />}>
      <LazyEditorInner />
    </Suspense>
  )
}
