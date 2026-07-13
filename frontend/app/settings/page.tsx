import React from 'react'
import { SettingsPageClient } from '@/components/pages/SettingsPageClient'
import { LayoutWrapper } from '@/components/layout/LayoutWrapper'

export const metadata = {
  title: 'Settings | Islamic Research Platform',
  description: 'Manage your preferences and account settings',
}

export default function SettingsPage() {
  return (
    <LayoutWrapper>
      <SettingsPageClient />
    </LayoutWrapper>
  )
}
