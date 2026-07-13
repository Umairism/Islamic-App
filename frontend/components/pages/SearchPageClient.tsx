'use client'

import React, { useEffect } from 'react'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { useSearch } from '@/hooks/useSearch'
import { useCollections } from '@/hooks/useCollections'
import { SourceType, SortOption } from '@/lib/types'
import { Button } from '@/components/ui/button'
import { SearchBar } from '@/components/search/SearchBar'
import { FilterPanel } from '@/components/search/FilterPanel'
import { ResultCard } from '@/components/search/ResultCard'
import { DocumentPreview } from '@/components/document/DocumentPreview'
import { cn } from '@/lib/utils'

/**
 * Main search page client component
 * Three-column layout:
 * 1. Left: Filters & Collections
 * 2. Center: Search bar & Results
 * 3. Right: Document Preview (hidden on mobile)
 */
export function SearchPageClient() {
  const search = useSearch()
  const collections = useCollections()
  const [selectedDocument, setSelectedDocument] = React.useState(search.results?.documents?.[0] || null)
  const [showPreview, setShowPreview] = React.useState(false)

  // Auto-execute search on component mount
  useEffect(() => {
    search.executeSearch()
  }, [search])

  // Update selected document when results change
  useEffect(() => {
    if (search.results?.documents && search.results.documents.length > 0) {
      setSelectedDocument(search.results.documents[0])
    }
  }, [search.results?.documents])

  const handleSearch = (term: string) => {
    search.updateSearchTerm(term)
  }

  const handleClear = () => {
    search.clearSearch()
  }

  const handleSourceTypeChange = (types: SourceType[]) => {
    search.updateSourceTypes(types)
  }

  const handleSortChange = (sort: SortOption) => {
    search.updateSort(sort)
  }

  const handleNextPage = () => {
    if (search.pagination.hasNextPage) {
      search.goToPage(search.pagination.currentPage + 1)
      search.executeSearch()
    }
  }

  const handlePrevPage = () => {
    if (search.pagination.hasPrevPage) {
      search.goToPage(search.pagination.currentPage - 1)
      search.executeSearch()
    }
  }

  return (
    <div className="grid h-screen grid-cols-1 gap-4 overflow-hidden lg:grid-cols-[280px_1fr_350px] xl:grid-cols-[320px_1fr_400px] p-4 bg-background">
      {/* ============================================================
          LEFT SIDEBAR: Filters & Collections
          ============================================================ */}
      <aside className="hidden flex-col gap-4 overflow-y-auto rounded-lg border border-border bg-card p-4 lg:flex">
        {/* Search Filters */}
        <div>
          <h2 className="mb-4 text-sm font-semibold text-foreground">
            Refine Search
          </h2>
          <FilterPanel
            sourceTypes={search.query.sourceTypes || []}
            onSourceTypesChange={handleSourceTypeChange}
            onReset={() => search.clearSearch()}
          />
        </div>

        {/* Collections Preview */}
        <div className="border-t border-border pt-4">
          <h2 className="mb-3 text-sm font-semibold text-foreground">
            My Collections
          </h2>
          <div className="space-y-2">
            {collections.collections.length === 0 ? (
              <p className="text-xs text-muted-foreground">No collections yet</p>
            ) : (
              collections.collections.map((collection) => (
                <button
                  key={collection.id}
                  className="w-full rounded-md border border-border px-3 py-2 text-left text-sm hover:bg-muted transition-colors"
                >
                  <div className="font-medium text-foreground">
                    {collection.name}
                  </div>
                  <div className="text-xs text-muted-foreground">
                    {collection.documentIds.length} items
                  </div>
                </button>
              ))
            )}
          </div>
        </div>
      </aside>

      {/* ============================================================
          CENTER: Search Bar & Results
          ============================================================ */}
      <main className="flex flex-col gap-4 overflow-hidden">
        {/* Search Bar */}
        <div className="rounded-lg border border-border bg-card p-4 shadow-sm">
          <SearchBar
            onSearch={handleSearch}
            onClear={handleClear}
            isLoading={search.isLoading}
            placeholder="Search Islamic sources..."
            size="lg"
            autoFocus={true}
          />

          {/* Search Controls */}
          <div className="mt-4 flex flex-wrap items-center justify-between gap-2">
            <div className="text-sm text-muted-foreground">
              {search.pagination.totalItems > 0 && (
                <>
                  <span className="font-medium text-foreground">
                    {search.pagination.totalItems.toLocaleString()}
                  </span>
                  {' results found'}
                </>
              )}
            </div>

            <div className="flex items-center gap-2">
              <label htmlFor="sort" className="text-sm text-muted-foreground">
                Sort by:
              </label>
              <select
                id="sort"
                value={search.query.sort}
                onChange={(e) => handleSortChange(e.target.value as SortOption)}
                className="rounded-md border border-input bg-background px-2 py-1 text-sm text-foreground"
              >
                <option value={SortOption.RELEVANCE}>Relevance</option>
                <option value={SortOption.DATE_DESC}>Newest</option>
                <option value={SortOption.DATE_ASC}>Oldest</option>
              </select>
            </div>
          </div>
        </div>

        {/* Results Grid */}
        <div className="flex-1 overflow-y-auto space-y-3">
          {search.isLoading && (
            <div className="flex items-center justify-center py-12">
              <div className="text-center">
                <div className="mb-2 h-8 w-8 animate-spin rounded-full border-4 border-muted border-t-primary mx-auto" />
                <p className="text-sm text-muted-foreground">Searching sources...</p>
              </div>
            </div>
          )}

          {search.error && (
            <div className="rounded-lg border border-red-200 bg-red-50 p-4 dark:border-red-900 dark:bg-red-950">
              <p className="text-sm text-red-700 dark:text-red-200">
                {search.error}
              </p>
            </div>
          )}

          {!search.isLoading && !search.error && search.results?.documents.length === 0 && (
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <p className="text-lg font-medium text-foreground mb-2">
                No results found
              </p>
              <p className="text-sm text-muted-foreground">
                Try adjusting your search or filters
              </p>
            </div>
          )}

          {search.results?.documents.map((document) => (
            <ResultCard
              key={document.id}
              document={document}
              onSelect={(doc) => {
                setSelectedDocument(doc)
                setShowPreview(true)
              }}
              isSaved={collections.getCollectionsForDocument(document.id).length > 0}
              onToggleSave={() => {
                const existingCollections = collections.getCollectionsForDocument(document.id)
                if (existingCollections.length > 0) {
                  existingCollections.forEach((col) => {
                    collections.removeDocumentFromCollection(col.id, document.id)
                  })
                } else if (collections.collections.length > 0) {
                  collections.addDocumentToCollection(collections.collections[0].id, document.id)
                }
              }}
            />
          ))}
        </div>

        {/* Pagination */}
        {search.pagination.totalPages > 1 && (
          <div className="flex items-center justify-between rounded-lg border border-border bg-card p-4">
            <Button
              onClick={handlePrevPage}
              disabled={!search.pagination.hasPrevPage || search.isLoading}
              variant="outline"
              size="sm"
              className="gap-1"
            >
              <ChevronLeft className="h-4 w-4" />
              <span className="hidden sm:inline">Previous</span>
            </Button>

            <div className="text-sm text-muted-foreground">
              Page {search.pagination.currentPage} of {search.pagination.totalPages}
            </div>

            <Button
              onClick={handleNextPage}
              disabled={!search.pagination.hasNextPage || search.isLoading}
              variant="outline"
              size="sm"
              className="gap-1"
            >
              <span className="hidden sm:inline">Next</span>
              <ChevronRight className="h-4 w-4" />
            </Button>
          </div>
        )}
      </main>

      {/* ============================================================
          RIGHT SIDEBAR: Document Preview (Desktop only)
          ============================================================ */}
      {selectedDocument && (
        <aside
          className={cn(
            'hidden flex-col gap-4 overflow-hidden rounded-lg border border-border bg-card p-4 lg:flex',
            showPreview && 'lg:hidden xl:flex'
          )}
        >
          <div className="flex flex-col gap-3 overflow-y-auto">
            <h2 className="text-sm font-semibold text-foreground">
              Document Preview
            </h2>
            <DocumentPreview document={selectedDocument} />
          </div>
        </aside>
      )}

      {/* Mobile Preview Modal */}
      {selectedDocument && showPreview && (
        <div className="fixed inset-0 z-50 flex items-end lg:hidden">
          <div
            className="absolute inset-0 bg-background/80 backdrop-blur-sm"
            onClick={() => setShowPreview(false)}
          />
          <div className="relative z-10 h-[70vh] w-full rounded-t-lg border-t border-border bg-card overflow-y-auto p-4">
            <button
              onClick={() => setShowPreview(false)}
              className="mb-4 text-sm font-medium text-primary hover:underline"
            >
              Close
            </button>
            <DocumentPreview document={selectedDocument} />
          </div>
        </div>
      )}
    </div>
  )
}
