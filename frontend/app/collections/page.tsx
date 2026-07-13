import React from 'react'
import { CollectionsPageClient } from '@/components/pages/CollectionsPageClient'
import { LayoutWrapper } from '@/components/layout/LayoutWrapper'

export const metadata = {
  title: 'Collections | Islamic Research Platform',
  description: 'Manage your saved research collections',
}

/**
 * Collections page - view and manage saved collections
 */
export default function CollectionsPage() {
  return (
    <LayoutWrapper>
      <CollectionsPageClient />
    </LayoutWrapper>
  )
}
