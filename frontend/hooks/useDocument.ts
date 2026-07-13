/**
 * useDocument - Custom hook for managing document state
 * Handles fetching, caching, and managing individual documents
 */

import { useState, useCallback, useEffect } from 'react'
import { Document } from '@/lib/types'
import { mockGetDocument } from '@/lib/mock-data'

export function useDocument(documentId: string | null) {
  const [document, setDocument] = useState<Document | null>(null)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  /**
   * Fetch document by ID
   * In production, this would call an actual API endpoint
   */
  const fetchDocument = useCallback(async (id: string) => {
    setIsLoading(true)
    setError(null)

    try {
      // Simulate API delay
      await new Promise((resolve) => setTimeout(resolve, 200))

      // Mock API call - replace with actual API integration
      const doc = mockGetDocument(id)

      if (!doc) {
        throw new Error('Document not found')
      }

      setDocument(doc)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch document'
      setError(message)
      setDocument(null)
    } finally {
      setIsLoading(false)
    }
  }, [])

  /**
   * Auto-fetch document when documentId changes
   */
  useEffect(() => {
    if (documentId) {
      fetchDocument(documentId)
    } else {
      setDocument(null)
    }
  }, [documentId, fetchDocument])

  /**
   * Clear document
   */
  const clearDocument = useCallback(() => {
    setDocument(null)
    setError(null)
  }, [])

  return {
    document,
    isLoading,
    error,
    fetchDocument,
    clearDocument,
  }
}
