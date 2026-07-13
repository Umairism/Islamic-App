import React from 'react'
import { DocumentDetailClient } from '@/components/pages/DocumentDetailClient'
import { LayoutWrapper } from '@/components/layout/LayoutWrapper'

interface DocumentPageProps {
  params: Promise<{ id: string }>
}

export const metadata = {
  title: 'Document | Islamic Research Platform',
  description: 'View detailed Islamic source document',
}

/**
 * Document detail page
 * Displays full document with comments, citations, and related sources
 */
export default async function DocumentPage({ params }: DocumentPageProps) {
  const { id } = await params

  return (
    <LayoutWrapper>
      <DocumentDetailClient documentId={id} />
    </LayoutWrapper>
  )
}
