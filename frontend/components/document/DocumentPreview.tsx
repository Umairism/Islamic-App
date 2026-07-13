'use client'

import React from 'react'
import Link from 'next/link'
import {
  Copy,
  Share2,
  Heart,
  ExternalLink,
  Quote,
  BookMarked,
} from 'lucide-react'
import {
  Document,
  SourceType,
  QuranVerse,
  Hadith,
  Tafsir,
  Fiqh,
} from '@/lib/types'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'

interface DocumentPreviewProps {
  document: Document
}

/**
 * Document preview component for sidebar
 * Shows detailed view of selected document with metadata and actions
 */
export function DocumentPreview({ document }: DocumentPreviewProps) {
  const [copied, setCopied] = React.useState(false)

  const handleCopy = () => {
    const text = getPreviewText()
    navigator.clipboard.writeText(text)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  const getPreviewText = (): string => {
    if (document.sourceType === SourceType.QURAN) {
      const quran = document as QuranVerse
      return `${quran.surahName} ${quran.ayahNumber}: ${quran.text}`
    }
    if (document.sourceType === SourceType.HADITH) {
      const hadith = document as Hadith
      return `${hadith.collection} #${hadith.hadithNumber}: ${hadith.text}`
    }
    if (document.sourceType === SourceType.TAFSIR) {
      const tafsir = document as Tafsir
      return `Tafsir by ${tafsir.scholar}: ${tafsir.text}`
    }
    if (document.sourceType === SourceType.FIQH) {
      const fiqh = document as Fiqh
      return `${fiqh.school} - ${fiqh.topic}: ${fiqh.text}`
    }
    return ''
  }

  const renderContent = () => {
    if (document.sourceType === SourceType.QURAN) {
      const quran = document as QuranVerse
      return (
        <div className="space-y-4">
          <div>
            <p className="text-xs font-semibold uppercase text-muted-foreground mb-1">
              Reference
            </p>
            <p className="text-sm font-medium text-foreground">
              {quran.surahName}, Verse {quran.ayahNumber}
            </p>
          </div>

          <div>
            <p className="text-xs font-semibold uppercase text-muted-foreground mb-2">
              Arabic Text
            </p>
            <p className="text-lg leading-relaxed text-foreground font-serif text-right">
              {quran.textArabic}
            </p>
          </div>

          <div>
            <p className="text-xs font-semibold uppercase text-muted-foreground mb-2">
              English Translation
            </p>
            <p className="text-sm leading-relaxed text-foreground">
              {quran.text}
            </p>
          </div>

          {quran.transliteration && (
            <div>
              <p className="text-xs font-semibold uppercase text-muted-foreground mb-1">
                Transliteration
              </p>
              <p className="text-sm text-muted-foreground italic">
                {quran.transliteration}
              </p>
            </div>
          )}

          {quran.translations && quran.translations.length > 0 && (
            <div>
              <p className="text-xs font-semibold uppercase text-muted-foreground mb-2">
                Other Translations
              </p>
              <div className="space-y-2">
                {quran.translations.slice(1).map((trans, idx) => (
                  <div key={idx} className="rounded bg-muted/50 p-2">
                    <p className="text-xs font-medium text-muted-foreground">
                      {trans.translator}
                    </p>
                    <p className="text-xs text-foreground mt-1">{trans.text}</p>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )
    }

    if (document.sourceType === SourceType.HADITH) {
      const hadith = document as Hadith
      return (
        <div className="space-y-4">
          <div>
            <p className="text-xs font-semibold uppercase text-muted-foreground mb-1">
              Source
            </p>
            <p className="text-sm font-medium text-foreground">
              {hadith.collection}
              {hadith.hadithNumber && ` #${hadith.hadithNumber}`}
            </p>
          </div>

          <div>
            <p className="text-xs font-semibold uppercase text-muted-foreground mb-1">
              Narrator
            </p>
            <p className="text-sm text-foreground">{hadith.narrator}</p>
          </div>

          <div>
            <p className="text-xs font-semibold uppercase text-muted-foreground mb-2">
              Hadith Text
            </p>
            <p className="text-lg leading-relaxed text-foreground font-serif text-right mb-2">
              {hadith.textArabic}
            </p>
            <p className="text-sm leading-relaxed text-foreground">
              {hadith.text}
            </p>
          </div>

          <div>
            <p className="text-xs font-semibold uppercase text-muted-foreground mb-2">
              Authenticity Status
            </p>
            <Badge className="capitalize">{hadith.authenticity}</Badge>
          </div>

          {hadith.grades && hadith.grades.length > 0 && (
            <div>
              <p className="text-xs font-semibold uppercase text-muted-foreground mb-2">
                Scholarly Grades
              </p>
              <div className="space-y-1">
                {hadith.grades.map((grade, idx) => (
                  <div key={idx} className="text-xs">
                    <p className="font-medium text-foreground">
                      {grade.scholar}
                    </p>
                    <p className="text-muted-foreground capitalize">
                      {grade.authenticity}
                    </p>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )
    }

    if (document.sourceType === SourceType.TAFSIR) {
      const tafsir = document as Tafsir
      return (
        <div className="space-y-4">
          <div>
            <p className="text-xs font-semibold uppercase text-muted-foreground mb-1">
              Tafsir By
            </p>
            <p className="text-sm font-medium text-foreground">
              {tafsir.scholar}
            </p>
          </div>

          <div>
            <p className="text-xs font-semibold uppercase text-muted-foreground mb-1">
              Reference
            </p>
            <p className="text-sm text-foreground">
              {tafsir.quranReference.surahName}:{tafsir.quranReference.ayahStart}
              {tafsir.quranReference.ayahEnd !== tafsir.quranReference.ayahStart &&
                `-${tafsir.quranReference.ayahEnd}`}
            </p>
          </div>

          <div>
            <p className="text-xs font-semibold uppercase text-muted-foreground mb-2">
              Commentary
            </p>
            <p className="text-lg leading-relaxed text-foreground font-serif text-right mb-2">
              {tafsir.textArabic}
            </p>
            <p className="text-sm leading-relaxed text-foreground">
              {tafsir.text}
            </p>
          </div>
        </div>
      )
    }

    if (document.sourceType === SourceType.FIQH) {
      const fiqh = document as Fiqh
      return (
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <p className="text-xs font-semibold uppercase text-muted-foreground mb-1">
                School
              </p>
              <p className="text-sm font-medium text-foreground">
                {fiqh.school}
              </p>
            </div>
            <div>
              <p className="text-xs font-semibold uppercase text-muted-foreground mb-1">
                Topic
              </p>
              <p className="text-sm font-medium text-foreground capitalize">
                {fiqh.topic}
              </p>
            </div>
          </div>

          <div>
            <p className="text-xs font-semibold uppercase text-muted-foreground mb-2">
              Ruling
            </p>
            <p className="text-lg leading-relaxed text-foreground font-serif text-right mb-2">
              {fiqh.textArabic}
            </p>
            <p className="text-sm leading-relaxed text-foreground">
              {fiqh.text}
            </p>
          </div>

          {fiqh.rulings && fiqh.rulings.length > 0 && (
            <div>
              <p className="text-xs font-semibold uppercase text-muted-foreground mb-2">
                Key Points
              </p>
              <ul className="space-y-1">
                {fiqh.rulings.map((ruling, idx) => (
                  <li key={idx} className="text-xs text-foreground flex gap-2">
                    <span className="text-primary font-bold">•</span>
                    <span>{ruling}</span>
                  </li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )
    }

    return null
  }

  return (
    <div className="flex flex-col gap-4">
      {/* Header */}
      <div>
        <h3 className="text-lg font-semibold text-foreground line-clamp-2">
          {document.title}
        </h3>
        <p className="text-xs text-muted-foreground mt-1">
          {document.author && <>By {document.author}</>}
        </p>
      </div>

      {/* Content */}
      <div className="text-sm">
        {renderContent()}
      </div>

      {/* Metadata */}
      <div className="border-t border-border pt-3 space-y-2 text-xs">
        <div>
          <p className="font-semibold text-muted-foreground mb-1">Tags</p>
          <div className="flex flex-wrap gap-1">
            {document.tags.slice(0, 3).map((tag) => (
              <Badge key={tag} variant="secondary" className="text-xs">
                {tag}
              </Badge>
            ))}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-2">
          <div>
            <p className="font-semibold text-muted-foreground">Citations</p>
            <p className="text-foreground">
              {document.citations?.toLocaleString() || '0'}
            </p>
          </div>
          <div>
            <p className="font-semibold text-muted-foreground">Relevance</p>
            <p className="text-foreground">
              {document.relevanceScore ? `${Math.round(document.relevanceScore * 100)}%` : 'N/A'}
            </p>
          </div>
        </div>
      </div>

      {/* Actions */}
      <div className="flex flex-col gap-2 border-t border-border pt-3">
        <Button
          variant="outline"
          size="sm"
          onClick={handleCopy}
          className="w-full justify-start gap-2 text-xs"
        >
          <Copy className="h-3.5 w-3.5" />
          {copied ? 'Copied!' : 'Copy Text'}
        </Button>

        <Button
          variant="outline"
          size="sm"
          className="w-full justify-start gap-2 text-xs"
        >
          <Share2 className="h-3.5 w-3.5" />
          Share
        </Button>

        <Button
          variant="outline"
          size="sm"
          asChild
          className="w-full justify-start gap-2 text-xs"
        >
          <Link href={`/document/${document.id}`}>
            <ExternalLink className="h-3.5 w-3.5" />
            Full View
          </Link>
        </Button>
      </div>
    </div>
  )
}
