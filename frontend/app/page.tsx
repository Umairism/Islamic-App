import React from 'react'
import { ResearchWorkspace } from '@/components/research/ResearchWorkspace'
import { LayoutWrapper } from '@/components/layout/LayoutWrapper'

export const metadata = {
  title: 'Research Workspace | Islamic AI Platform',
  description: 'Autonomous Jurisprudential Synthesis & Research Execution Platform',
}

export default function SearchPage() {
  return (
    <LayoutWrapper>
      <ResearchWorkspace />
    </LayoutWrapper>
  )
}
