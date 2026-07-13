import React from 'react'
import { SearchPageClient } from '@/components/pages/SearchPageClient'
import { LayoutWrapper } from '@/components/layout/LayoutWrapper'

export const metadata = {
  title: 'Search | Islamic Research Platform',
  description: 'Search and browse Quran, Hadith, Tafsir, and Fiqh sources',
}

/**
 * Main search page - server component wrapper
 * Handles layout and delegates interactive functionality to client component
 */
export default function SearchPage() {
  return (
    <LayoutWrapper>
      <SearchPageClient />
    </LayoutWrapper>
  )
}
