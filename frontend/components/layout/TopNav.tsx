'use client'

import React, { useState } from 'react'
import Link from 'next/link'
import { Menu, Moon, Sun, Settings, LogOut } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface TopNavProps {
  onMenuToggle: () => void
  sidebarOpen: boolean
}

export function TopNav({ onMenuToggle, sidebarOpen }: TopNavProps) {
  const [isDark, setIsDark] = useState(false)

  const toggleTheme = () => {
    setIsDark(!isDark)
    document.documentElement.classList.toggle('dark')
  }

  return (
    <header className="sticky top-0 z-40 border-b border-border bg-card">
      <div className="flex h-16 items-center justify-between px-4 lg:px-6">
        {/* Left: Menu Button & Logo */}
        <div className="flex items-center gap-4">
          <Button
            variant="ghost"
            size="icon"
            onClick={onMenuToggle}
            className="lg:hidden"
            aria-label="Toggle sidebar"
          >
            <Menu className="h-5 w-5" />
          </Button>

          <Link href="/" className="flex items-center gap-2">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-primary-foreground font-bold">
              IR
            </div>
            <span className="hidden font-semibold text-foreground sm:inline">
              Islamic Research
            </span>
          </Link>
        </div>

        {/* Right: Theme Toggle & Settings */}
        <div className="flex items-center gap-2">
          <Button
            variant="ghost"
            size="icon"
            onClick={toggleTheme}
            aria-label="Toggle theme"
            title={isDark ? 'Switch to light mode' : 'Switch to dark mode'}
          >
            {isDark ? (
              <Sun className="h-5 w-5" />
            ) : (
              <Moon className="h-5 w-5" />
            )}
          </Button>

          <Button
            variant="ghost"
            size="icon"
            asChild
            aria-label="Settings"
            title="Settings"
          >
            <Link href="/settings">
              <Settings className="h-5 w-5" />
            </Link>
          </Button>

          <Button
            variant="ghost"
            size="icon"
            aria-label="Logout"
            title="Logout"
          >
            <LogOut className="h-5 w-5" />
          </Button>
        </div>
      </div>
    </header>
  )
}
