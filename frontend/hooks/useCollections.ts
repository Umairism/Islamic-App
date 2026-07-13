/**
 * useCollections - Custom hook for managing user collections
 * Handles CRUD operations for collections and saved items
 */

import { useState, useCallback, useEffect } from 'react'
import { Collection, Document } from '@/lib/types'
import { mockGetCollections } from '@/lib/mock-data'

export function useCollections() {
  const [collections, setCollections] = useState<Collection[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  /**
   * Fetch all collections
   * In production, this would call an actual API endpoint
   */
  const fetchCollections = useCallback(async () => {
    setIsLoading(true)
    setError(null)

    try {
      // Simulate API delay
      await new Promise((resolve) => setTimeout(resolve, 200))

      // Mock API call
      const cols = mockGetCollections()
      setCollections(cols)
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Failed to fetch collections'
      setError(message)
    } finally {
      setIsLoading(false)
    }
  }, [])

  /**
   * Create a new collection
   */
  const createCollection = useCallback(
    (name: string, description?: string) => {
      const newCollection: Collection = {
        id: `coll-${Date.now()}`,
        name,
        description,
        documentIds: [],
        createdAt: new Date(),
        updatedAt: new Date(),
        isPublic: false,
        tags: [],
      }

      setCollections((prev) => [...prev, newCollection])
      return newCollection
    },
    []
  )

  /**
   * Delete a collection
   */
  const deleteCollection = useCallback((collectionId: string) => {
    setCollections((prev) =>
      prev.filter((col) => col.id !== collectionId)
    )
  }, [])

  /**
   * Update collection metadata
   */
  const updateCollection = useCallback(
    (collectionId: string, updates: Partial<Collection>) => {
      setCollections((prev) =>
        prev.map((col) =>
          col.id === collectionId
            ? { ...col, ...updates, updatedAt: new Date() }
            : col
        )
      )
    },
    []
  )

  /**
   * Add document to collection
   */
  const addDocumentToCollection = useCallback(
    (collectionId: string, documentId: string) => {
      setCollections((prev) =>
        prev.map((col) =>
          col.id === collectionId && !col.documentIds.includes(documentId)
            ? {
                ...col,
                documentIds: [...col.documentIds, documentId],
                updatedAt: new Date(),
              }
            : col
        )
      )
    },
    []
  )

  /**
   * Remove document from collection
   */
  const removeDocumentFromCollection = useCallback(
    (collectionId: string, documentId: string) => {
      setCollections((prev) =>
        prev.map((col) =>
          col.id === collectionId
            ? {
                ...col,
                documentIds: col.documentIds.filter((id) => id !== documentId),
                updatedAt: new Date(),
              }
            : col
        )
      )
    },
    []
  )

  /**
   * Check if document is in collection
   */
  const isDocumentInCollection = useCallback(
    (collectionId: string, documentId: string) => {
      const collection = collections.find((col) => col.id === collectionId)
      return collection ? collection.documentIds.includes(documentId) : false
    },
    [collections]
  )

  /**
   * Get all collections containing a specific document
   */
  const getCollectionsForDocument = useCallback(
    (documentId: string) => {
      return collections.filter((col) => col.documentIds.includes(documentId))
    },
    [collections]
  )

  // Auto-fetch collections on mount
  useEffect(() => {
    fetchCollections()
  }, [fetchCollections])

  return {
    collections,
    isLoading,
    error,
    fetchCollections,
    createCollection,
    deleteCollection,
    updateCollection,
    addDocumentToCollection,
    removeDocumentFromCollection,
    isDocumentInCollection,
    getCollectionsForDocument,
  }
}
