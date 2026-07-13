'use client'

import React, { useEffect } from 'react'
import Link from 'next/link'
import { ArrowLeft, Copy, Share2, Heart, BookMarked } from 'lucide-react'
import { useDocument } from '@/hooks/useDocument'
import { useCollections } from '@/hooks/useCollections'
import { Button } from '@/components/ui/button'
import { DocumentPreview } from '@/components/document/DocumentPreview'
import { Badge } from '@/components/ui/badge'

interface DocumentDetailClientProps {
  documentId: string
}

/**
 * Document detail page client component
 * Shows full document view with metadata, citations, and annotations
 */
export function DocumentDetailClient({
  documentId,
}: DocumentDetailClientProps) {
  const { document, isLoading, error } = useDocument(documentId)
  const collections = useCollections()
  const [copied, setCopied] = React.useState(false)

  const isSaved =
    document &&
    collections.getCollectionsForDocument(document.id).length > 0

  const handleCopy = () => {
    if (document) {
      const text =
        'text' in document
          ? document.text
          : 'textArabic' in document
            ? document.textArabic
            : ''
      navigator.clipboard.writeText(text)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    }
  }

  const handleToggleSave = () => {
    if (!document) return

    const existingCollections = collections.getCollectionsForDocument(
      document.id
    )
    if (existingCollections.length > 0) {
      existingCollections.forEach((col) => {
        collections.removeDocumentFromCollection(col.id, document.id)
      })
    } else if (collections.collections.length > 0) {
      collections.addDocumentToCollection(
        collections.collections[0].id,
        document.id
      )
    }
  }

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-center">
          <div className="mb-4 h-8 w-8 animate-spin rounded-full border-4 border-muted border-t-primary mx-auto" />
          <p className="text-muted-foreground">Loading document...</p>
        </div>
      </div>
    )
  }

  if (error || !document) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-center">
          <p className="text-lg font-medium text-destructive mb-4">
            {error || 'Document not found'}
          </p>
          <Button asChild variant="outline">
            <Link href="/">Back to Search</Link>
          </Button>
        </div>
      </div>
    )
  }

  return (
    <div className="max-w-4xl mx-auto px-4 py-8">
      {/* Header */}
      <div className="mb-8">
        <Button
          variant="ghost"
          asChild
          className="mb-4 gap-2"
        >
          <Link href="/">
            <ArrowLeft className="h-4 w-4" />
            Back to Search
          </Link>
        </Button>

        <div className="space-y-4">
          <div>
            <h1 className="text-3xl font-bold text-foreground mb-2">
              {document.title}
            </h1>
            {document.description && (
              <p className="text-muted-foreground">{document.description}</p>
            )}
          </div>

          {document.author && (
            <div className="text-sm text-muted-foreground">
              By <span className="font-medium">{document.author}</span>
            </div>
          )}

          {/* Action Buttons */}
          <div className="flex flex-wrap gap-2 pt-4">
            <Button
              onClick={handleCopy}
              variant="outline"
              className="gap-2"
            >
              <Copy className="h-4 w-4" />
              {copied ? 'Copied!' : 'Copy'}
            </Button>

            <Button variant="outline" className="gap-2">
              <Share2 className="h-4 w-4" />
              Share
            </Button>

            <Button
              onClick={handleToggleSave}
              variant={isSaved ? 'default' : 'outline'}
              className="gap-2"
            >
              <Heart
                className={`h-4 w-4 ${isSaved ? 'fill-current' : ''}`}
              />
              {isSaved ? 'Saved' : 'Save'}
            </Button>

            <Button variant="outline" className="gap-2">
              <BookMarked className="h-4 w-4" />
              Cite
            </Button>
          </div>
        </div>
      </div>

      {/* Tags */}
      {document.tags && document.tags.length > 0 && (
        <div className="mb-8 flex flex-wrap gap-2">
          {document.tags.map((tag) => (
            <Badge key={tag} variant="secondary">
              {tag}
            </Badge>
          ))}
        </div>
      )}

      {/* Main Content */}
      <div className="rounded-lg border border-border bg-card p-8 mb-8">
        <DocumentPreview document={document} />
      </div>

      {/* Related Information */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="rounded-lg border border-border bg-card p-4">
          <p className="text-xs font-semibold uppercase text-muted-foreground mb-2">
            Citations
          </p>
          <p className="text-2xl font-bold text-foreground">
            {document.citations?.toLocaleString() || '0'}
          </p>
        </div>

        <div className="rounded-lg border border-border bg-card p-4">
          <p className="text-xs font-semibold uppercase text-muted-foreground mb-2">
            Relevance
          </p>
          <p className="text-2xl font-bold text-foreground">
            {document.relevanceScore
              ? `${Math.round(document.relevanceScore * 100)}%`
              : 'N/A'}
          </p>
        </div>

        <div className="rounded-lg border border-border bg-card p-4">
          <p className="text-xs font-semibold uppercase text-muted-foreground mb-2">
            Saved in Collections
          </p>
          <p className="text-2xl font-bold text-foreground">
            {collections.getCollectionsForDocument(document.id).length}
          </p>
        </div>
      </div>

      {/* Related Documents Section - Placeholder */}
      <div className="mt-8 rounded-lg border border-border bg-card p-6">
        <h2 className="text-lg font-semibold text-foreground mb-4">
          Related Documents
        </h2>
        <p className="text-muted-foreground text-sm">
          Related documents will appear here once the backend is integrated.
        </p>
      </div>
    </div>
  )
}
