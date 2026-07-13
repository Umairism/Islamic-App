'use client'

import React, { useState } from 'react'
import Link from 'next/link'
import { Heart, Share2, Copy, Quote } from 'lucide-react'
import { Document, SourceType, Hadith, Tafsir, Fiqh, QuranVerse } from '@/lib/types'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'

interface ResultCardProps {
  document: Document
  onSelect?: (document: Document) => void
  isSaved?: boolean
  onToggleSave?: () => void
}

/**
 * Result card for search results
 * Displays document metadata, preview, and action buttons
 * Supports all document types with type-specific rendering
 */
export function ResultCard({
  document,
  onSelect,
  isSaved = false,
  onToggleSave,
}: ResultCardProps) {
  const [showFullText, setShowFullText] = useState(false)

  // Determine source type styling
  const sourceTypeStyles = {
    [SourceType.QURAN]: 'bg-blue-100 text-blue-900 dark:bg-blue-900/30 dark:text-blue-200',
    [SourceType.HADITH]: 'bg-amber-100 text-amber-900 dark:bg-amber-900/30 dark:text-amber-200',
    [SourceType.TAFSIR]: 'bg-purple-100 text-purple-900 dark:bg-purple-900/30 dark:text-purple-200',
    [SourceType.FIQH]: 'bg-green-100 text-green-900 dark:bg-green-900/30 dark:text-green-200',
    [SourceType.BIOGRAPHY]: 'bg-slate-100 text-slate-900 dark:bg-slate-900/30 dark:text-slate-200',
    [SourceType.HISTORY]: 'bg-slate-100 text-slate-900 dark:bg-slate-900/30 dark:text-slate-200',
  }

  // Get type label
  const getSourceTypeLabel = (type: SourceType): string => {
    const labels: Record<SourceType, string> = {
      [SourceType.QURAN]: 'Quran',
      [SourceType.HADITH]: 'Hadith',
      [SourceType.TAFSIR]: 'Tafsir',
      [SourceType.FIQH]: 'Fiqh',
      [SourceType.BIOGRAPHY]: 'Biography',
      [SourceType.HISTORY]: 'History',
    }
    return labels[type]
  }

  // Type-specific metadata
  const getMetadata = (doc: Document): React.ReactNode => {
    if (doc.sourceType === SourceType.QURAN) {
      const quran = doc as QuranVerse
      return (
        <div className="text-sm text-muted-foreground">
          <p>
            <span className="font-medium">{quran.surahName}</span>
            {' '}: Verse {quran.ayahNumber}
          </p>
        </div>
      )
    }

    if (doc.sourceType === SourceType.HADITH) {
      const hadith = doc as Hadith
      return (
        <div className="text-sm text-muted-foreground space-y-1">
          <p>
            <span className="font-medium">{hadith.collection}</span>
            {hadith.hadithNumber && ` • #${hadith.hadithNumber}`}
          </p>
          <p>Narrator: {hadith.narrator}</p>
        </div>
      )
    }

    if (doc.sourceType === SourceType.TAFSIR) {
      const tafsir = doc as Tafsir
      return (
        <div className="text-sm text-muted-foreground">
          <p>
            By <span className="font-medium">{tafsir.scholar}</span>
          </p>
        </div>
      )
    }

    if (doc.sourceType === SourceType.FIQH) {
      const fiqh = doc as Fiqh
      return (
        <div className="text-sm text-muted-foreground space-y-1">
          <p>
            <span className="font-medium">{fiqh.school}</span> School
          </p>
          <p className="capitalize">{fiqh.topic}</p>
        </div>
      )
    }

    return null
  }

  // Get preview text
  const getPreviewText = (doc: Document): string => {
    if (doc.sourceType === SourceType.QURAN) {
      return (doc as QuranVerse).text || ''
    }
    if (doc.sourceType === SourceType.HADITH) {
      return (doc as Hadith).text || ''
    }
    if (doc.sourceType === SourceType.TAFSIR) {
      return (doc as Tafsir).text || ''
    }
    if (doc.sourceType === SourceType.FIQH) {
      return (doc as Fiqh).text || ''
    }
    return ''
  }

  const previewText = getPreviewText(document)
  const maxPreviewLength = 280
  const shouldTruncate = previewText.length > maxPreviewLength

  return (
    <article className="rounded-lg border border-border bg-card p-4 sm:p-5 hover:shadow-md transition-shadow">
      {/* Header with Source Type and Actions */}
      <div className="flex items-start justify-between gap-4 mb-3">
        <div className="flex items-center gap-2 flex-wrap">
          <Badge
            variant="secondary"
            className={cn('text-xs font-medium', sourceTypeStyles[document.sourceType])}
          >
            {getSourceTypeLabel(document.sourceType)}
          </Badge>

          {document.tags && document.tags.length > 0 && (
            <div className="flex flex-wrap gap-1">
              {document.tags.slice(0, 2).map((tag) => (
                <Badge key={tag} variant="outline" className="text-xs">
                  {tag}
                </Badge>
              ))}
            </div>
          )}
        </div>

        {/* Quick Actions */}
        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="icon"
            onClick={onToggleSave}
            className="h-8 w-8"
            aria-label={isSaved ? 'Remove from collections' : 'Save to collections'}
          >
            <Heart
              className={cn('h-4 w-4', isSaved && 'fill-current text-red-500')}
            />
          </Button>

          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8"
            aria-label="Share"
          >
            <Share2 className="h-4 w-4" />
          </Button>
        </div>
      </div>

      {/* Title */}
      <Link
        href={`/document/${document.id}`}
        onClick={() => onSelect?.(document)}
        className="block group mb-2"
      >
        <h3 className="text-lg font-semibold text-foreground group-hover:text-primary transition-colors line-clamp-2">
          {document.title}
        </h3>
      </Link>

      {/* Metadata */}
      {getMetadata(document)}

      {/* Preview Text */}
      {previewText && (
        <div className="mt-3 mb-3">
          <p className="text-sm text-foreground leading-relaxed">
            {showFullText ? previewText : `${previewText.substring(0, maxPreviewLength)}${shouldTruncate ? '...' : ''}`}
          </p>

          {shouldTruncate && (
            <button
              onClick={() => setShowFullText(!showFullText)}
              className="mt-2 text-sm text-primary hover:underline font-medium"
            >
              {showFullText ? 'Show less' : 'Show more'}
            </button>
          )}
        </div>
      )}

      {/* Footer with Stats and CTA */}
      <div className="flex items-center justify-between pt-3 border-t border-border/50">
        <div className="flex items-center gap-4 text-xs text-muted-foreground">
          {document.citations !== undefined && (
            <span>{document.citations.toLocaleString()} citations</span>
          )}

          {document.relevanceScore !== undefined && (
            <span>{Math.round(document.relevanceScore * 100)}% relevant</span>
          )}
        </div>

        <Button
          variant="outline"
          size="sm"
          asChild
          onClick={() => onSelect?.(document)}
          className="gap-1"
        >
          <Link href={`/document/${document.id}`}>
            <Quote className="h-4 w-4" />
            <span className="hidden sm:inline">View</span>
          </Link>
        </Button>
      </div>
    </article>
  )
}
