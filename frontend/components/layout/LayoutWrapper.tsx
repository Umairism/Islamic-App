'use client'

import React, { useState, useCallback } from 'react'
import { TopNav } from './TopNav'
import { Sidebar } from './Sidebar'

interface LayoutWrapperProps {
  children: React.ReactNode
}

/**
 * Main layout wrapper providing persistent header and sidebar
 * Manages sidebar state for mobile/responsive behavior
 */
export function LayoutWrapper({ children }: LayoutWrapperProps) {
  const [sidebarOpen, setSidebarOpen] = useState(false)

  const handleMenuToggle = useCallback(() => {
    setSidebarOpen((prev) => !prev)
  }, [])

  const handleSidebarClose = useCallback(() => {
    setSidebarOpen(false)
  }, [])

  return (
    <div className="flex min-h-screen flex-col">
      {/* Fixed Header */}
      <TopNav onMenuToggle={handleMenuToggle} sidebarOpen={sidebarOpen} />

      <div className="flex flex-1 overflow-hidden">
        {/* Sidebar */}
        <Sidebar open={sidebarOpen} onClose={handleSidebarClose} />

        {/* Main Content Area */}
        <main className="flex-1 overflow-y-auto bg-background">
          {children}
        </main>
      </div>
    </div>
  )
}
