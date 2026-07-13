'use client'

import React from 'react'
import Link from 'next/link'
import { ArrowLeft, Search, Download, Save } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Badge } from '@/components/ui/badge'

/**
 * Advanced search page client component
 * Provides complex multi-field query building interface
 */
export function AdvancedSearchClient() {
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

        <h1 className="text-3xl font-bold text-foreground mb-2">
          Advanced Search
        </h1>
        <p className="text-muted-foreground">
          Build complex queries with multiple filters and conditions
        </p>
      </div>

      {/* Search Form */}
      <div className="rounded-lg border border-border bg-card p-6 mb-8">
        <form className="space-y-6">
          {/* Query Builder */}
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-foreground mb-2">
                Search Text
              </label>
              <Input
                type="text"
                placeholder="Enter search terms..."
                className="w-full"
              />
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-foreground mb-2">
                  Source Type
                </label>
                <select className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm">
                  <option>All Sources</option>
                  <option>Quran</option>
                  <option>Hadith</option>
                  <option>Tafsir</option>
                  <option>Fiqh</option>
                </select>
              </div>

              <div>
                <label className="block text-sm font-medium text-foreground mb-2">
                  Sort By
                </label>
                <select className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm">
                  <option>Relevance</option>
                  <option>Newest</option>
                  <option>Oldest</option>
                  <option>Most Cited</option>
                </select>
              </div>
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-foreground mb-2">
                  Author
                </label>
                <Input type="text" placeholder="Optional filter..." />
              </div>

              <div>
                <label className="block text-sm font-medium text-foreground mb-2">
                  Date Range
                </label>
                <div className="flex gap-2">
                  <Input type="date" className="flex-1" />
                  <Input type="date" className="flex-1" />
                </div>
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-foreground mb-2">
                Tags (comma-separated)
              </label>
              <Input
                type="text"
                placeholder="e.g., prayer, fiqh, hadith..."
              />
            </div>
          </div>

          {/* Actions */}
          <div className="flex gap-2 pt-4 border-t border-border">
            <Button className="gap-2">
              <Search className="h-4 w-4" />
              Search
            </Button>

            <Button variant="outline" className="gap-2">
              <Save className="h-4 w-4" />
              Save Search
            </Button>

            <Button variant="outline" className="gap-2">
              <Download className="h-4 w-4" />
              Export Query
            </Button>
          </div>
        </form>
      </div>

      {/* Saved Searches */}
      <div>
        <h2 className="text-lg font-semibold text-foreground mb-4">
          Saved Searches
        </h2>
        <div className="space-y-2">
          <div className="rounded-lg border border-border p-4 hover:bg-muted/50 transition-colors cursor-pointer">
            <h3 className="font-medium text-foreground">Recent Hadith</h3>
            <p className="text-sm text-muted-foreground mt-1">
              Hadith sources | Sort by: Newest
            </p>
            <div className="mt-2 flex gap-1">
              <Badge variant="secondary" className="text-xs">hadith</Badge>
            </div>
          </div>

          <div className="rounded-lg border border-border p-4 hover:bg-muted/50 transition-colors cursor-pointer">
            <h3 className="font-medium text-foreground">Fiqh Resources</h3>
            <p className="text-sm text-muted-foreground mt-1">
              Islamic Jurisprudence | Hanafi & Shafi&apos;i Schools
            </p>
            <div className="mt-2 flex gap-1">
              <Badge variant="secondary" className="text-xs">fiqh</Badge>
              <Badge variant="secondary" className="text-xs">hanafi</Badge>
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
