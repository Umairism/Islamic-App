/**
 * useSearch - Custom hook for managing search state and operations
 * Handles search queries, filters, and result management
 * Future: Replace mockSearchDocuments with actual API call
 */

import { useState, useCallback, useMemo } from 'react'
import {
  SearchQuery,
  SearchResults,
  SourceType,
  SortOption,
  LanguageCode,
} from '@/lib/types'
import { mockSearchDocuments } from '@/lib/mock-data'

const DEFAULT_QUERY: SearchQuery = {
  term: '',
  sourceTypes: Object.values(SourceType),
  language: LanguageCode.AR,
  sort: SortOption.RELEVANCE,
  page: 1,
  pageSize: 20,
}

export function useSearch(initialQuery: Partial<SearchQuery> = {}) {
  const [query, setQuery] = useState<SearchQuery>({
    ...DEFAULT_QUERY,
    ...initialQuery,
  })

  const [results, setResults] = useState<SearchResults | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  /**
   * Execute search with current query
   * In production, this would call an actual API endpoint
   */
  const executeSearch = useCallback(async () => {
    setIsLoading(true)
    setError(null)

    try {
      // Simulate API delay
      await new Promise((resolve) => setTimeout(resolve, 300))

      // Mock API call - replace with actual API integration
      const searchResults = mockSearchDocuments(query)
      setResults(searchResults as SearchResults)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Search failed'
      setError(message)
      setResults(null)
    } finally {
      setIsLoading(false)
    }
  }, [query])

  /**
   * Update search term and reset to first page
   */
  const updateSearchTerm = useCallback((term: string) => {
    setQuery((prev) => ({
      ...prev,
      term,
      page: 1,
    }))
  }, [])

  /**
   * Update source type filters
   */
  const updateSourceTypes = useCallback((sourceTypes: SourceType[]) => {
    setQuery((prev) => ({
      ...prev,
      sourceTypes,
      page: 1,
    }))
  }, [])

  /**
   * Update sort option
   */
  const updateSort = useCallback((sort: SortOption) => {
    setQuery((prev) => ({
      ...prev,
      sort,
      page: 1,
    }))
  }, [])

  /**
   * Update page number
   */
  const goToPage = useCallback((page: number) => {
    setQuery((prev) => ({
      ...prev,
      page,
    }))
  }, [])

  /**
   * Clear all filters and search term
   */
  const clearSearch = useCallback(() => {
    setQuery(DEFAULT_QUERY)
    setResults(null)
    setError(null)
  }, [])

  /**
   * Pagination info
   */
  const pagination = useMemo(() => {
    if (!results) {
      return {
        currentPage: query.page,
        pageSize: query.pageSize,
        totalItems: 0,
        totalPages: 0,
        hasNextPage: false,
        hasPrevPage: false,
      }
    }

    const totalPages = Math.ceil(results.total / query.pageSize)
    return {
      currentPage: query.page,
      pageSize: query.pageSize,
      totalItems: results.total,
      totalPages,
      hasNextPage: query.page < totalPages,
      hasPrevPage: query.page > 1,
    }
  }, [query, results])

  return {
    query,
    results,
    isLoading,
    error,
    pagination,
    executeSearch,
    updateSearchTerm,
    updateSourceTypes,
    updateSort,
    goToPage,
    clearSearch,
  }
}
