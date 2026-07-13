'use client'

import React from 'react'
import Link from 'next/link'
import { usePathname } from 'next/navigation'
import {
  Search,
  BookMarked,
  Heart,
  Settings,
  HelpCircle,
  ChevronRight,
} from 'lucide-react'
import { cn } from '@/lib/utils'

interface SidebarProps {
  open: boolean
  onClose?: () => void
}

export function Sidebar({ open, onClose }: SidebarProps) {
  const pathname = usePathname()

  const navigationItems = [
    {
      label: 'Search & Browse',
      href: '/',
      icon: Search,
      description: 'Find sources',
    },
    {
      label: 'Collections',
      href: '/collections',
      icon: BookMarked,
      description: 'Your saved items',
    },
    {
      label: 'Advanced Search',
      href: '/advanced-search',
      icon: Search,
      description: 'Complex queries',
    },
  ]

  const settingsItems = [
    {
      label: 'Settings',
      href: '/settings',
      icon: Settings,
      description: 'Preferences',
    },
    {
      label: 'Help & Documentation',
      href: '/help',
      icon: HelpCircle,
      description: 'Learn more',
    },
  ]

  const NavLink = ({
    href,
    label,
    description,
    Icon,
  }: {
    href: string
    label: string
    description: string
    Icon: React.ComponentType<{ className?: string }>
  }) => {
    const isActive = pathname === href
    return (
      <Link
        href={href}
        onClick={onClose}
        className={cn(
          'flex items-center justify-between rounded-md px-3 py-2.5 text-sm font-medium transition-colors',
          isActive
            ? 'bg-primary text-primary-foreground'
            : 'text-muted-foreground hover:bg-muted hover:text-foreground'
        )}
      >
        <div className="flex items-center gap-3">
          <Icon className="h-4 w-4 flex-shrink-0" />
          <div className="hidden sm:block">
            <div className="font-medium">{label}</div>
            <div className="text-xs opacity-70">{description}</div>
          </div>
        </div>
        <ChevronRight className="h-4 w-4 flex-shrink-0 sm:hidden" />
      </Link>
    )
  }

  return (
    <>
      {/* Mobile Overlay */}
      {open && (
        <div
          className="fixed inset-0 z-30 bg-background/80 backdrop-blur-sm lg:hidden"
          onClick={onClose}
          aria-hidden="true"
        />
      )}

      {/* Sidebar */}
      <aside
        className={cn(
          'fixed left-0 top-16 z-40 h-[calc(100vh-4rem)] w-64 border-r border-border bg-sidebar transition-transform duration-200 lg:static lg:translate-x-0 lg:top-0 lg:h-screen overflow-y-auto',
          open ? 'translate-x-0' : '-translate-x-full'
        )}
      >
        <nav className="space-y-1 p-4">
          {/* Main Navigation Section */}
          <div className="mb-6">
            <h2 className="px-3 py-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              Research
            </h2>
            <div className="space-y-1">
              {navigationItems.map((item) => (
                <NavLink key={item.href} {...item} Icon={item.icon} />
              ))}
            </div>
          </div>

          {/* Saved Collections Preview */}
          <div className="mb-6">
            <h2 className="px-3 py-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              Collections
            </h2>
            <div className="space-y-1">
              <Link
                href="/collections/daily-quran"
                onClick={onClose}
                className="flex items-center gap-3 rounded-md px-3 py-2 text-sm text-muted-foreground hover:bg-muted hover:text-foreground transition-colors"
              >
                <Heart className="h-4 w-4" />
                <span>Daily Quran</span>
              </Link>
              <Link
                href="/collections/hadith-knowledge"
                onClick={onClose}
                className="flex items-center gap-3 rounded-md px-3 py-2 text-sm text-muted-foreground hover:bg-muted hover:text-foreground transition-colors"
              >
                <Heart className="h-4 w-4" />
                <span>Hadith Knowledge</span>
              </Link>
            </div>
          </div>

          {/* Settings Section */}
          <div className="border-t border-border pt-4">
            <h2 className="px-3 py-2 text-xs font-semibold uppercase tracking-wider text-muted-foreground">
              Tools
            </h2>
            <div className="space-y-1">
              {settingsItems.map((item) => (
                <NavLink key={item.href} {...item} Icon={item.icon} />
              ))}
            </div>
          </div>
        </nav>
      </aside>
    </>
  )
}
