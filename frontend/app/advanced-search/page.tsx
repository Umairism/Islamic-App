import React from 'react'
import { AdvancedSearchClient } from '@/components/pages/AdvancedSearchClient'
import { LayoutWrapper } from '@/components/layout/LayoutWrapper'

export const metadata = {
  title: 'Advanced Search | Islamic Research Platform',
  description: 'Advanced search with multiple filters and criteria',
}

export default function AdvancedSearchPage() {
  return (
    <LayoutWrapper>
      <AdvancedSearchClient />
    </LayoutWrapper>
  )
}
