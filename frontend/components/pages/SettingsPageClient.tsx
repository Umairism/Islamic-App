'use client'

import React, { useState } from 'react'
import { Save, RotateCcw } from 'lucide-react'
import { LanguageCode, SortOption } from '@/lib/types'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'

/**
 * Settings page client component
 * Manages user preferences and application settings
 */
export function SettingsPageClient() {
  const [preferences, setPreferences] = useState({
    language: LanguageCode.EN,
    theme: 'system' as const,
    itemsPerPage: 20,
    defaultSort: SortOption.RELEVANCE,
    textSize: 'normal' as const,
    rtlEnabled: false,
  })

  const [saved, setSaved] = useState(false)

  const handleSave = () => {
    setSaved(true)
    setTimeout(() => setSaved(false), 2000)
  }

  const handleReset = () => {
    setPreferences({
      language: LanguageCode.EN,
      theme: 'system',
      itemsPerPage: 20,
      defaultSort: SortOption.RELEVANCE,
      textSize: 'normal',
      rtlEnabled: false,
    })
  }

  return (
    <div className="max-w-2xl mx-auto px-4 py-8">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold text-foreground mb-2">
          Settings
        </h1>
        <p className="text-muted-foreground">
          Manage your preferences and account settings
        </p>
      </div>

      {/* Settings Form */}
      <div className="space-y-6">
        {/* Display Settings */}
        <section className="rounded-lg border border-border bg-card p-6">
          <h2 className="text-lg font-semibold text-foreground mb-4">
            Display & Language
          </h2>

          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-foreground mb-2">
                Language
              </label>
              <select
                value={preferences.language}
                onChange={(e) =>
                  setPreferences({
                    ...preferences,
                    language: e.target.value as LanguageCode,
                  })
                }
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              >
                <option value={LanguageCode.EN}>English</option>
                <option value={LanguageCode.AR}>العربية (Arabic)</option>
                <option value={LanguageCode.UR}>اردو (Urdu)</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-foreground mb-2">
                Theme
              </label>
              <select
                value={preferences.theme}
                onChange={(e) =>
                  setPreferences({
                    ...preferences,
                    theme: e.target.value as 'light' | 'dark' | 'system',
                  })
                }
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              >
                <option value="light">Light</option>
                <option value="dark">Dark</option>
                <option value="system">System</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-foreground mb-2">
                Text Size
              </label>
              <select
                value={preferences.textSize}
                onChange={(e) =>
                  setPreferences({
                    ...preferences,
                    textSize: e.target.value as
                      | 'small'
                      | 'normal'
                      | 'large'
                      | 'xlarge',
                  })
                }
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              >
                <option value="small">Small</option>
                <option value="normal">Normal</option>
                <option value="large">Large</option>
                <option value="xlarge">Extra Large</option>
              </select>
            </div>

            <div className="flex items-center gap-2">
              <input
                type="checkbox"
                id="rtl"
                checked={preferences.rtlEnabled}
                onChange={(e) =>
                  setPreferences({
                    ...preferences,
                    rtlEnabled: e.target.checked,
                  })
                }
                className="h-4 w-4 rounded border-border"
              />
              <label
                htmlFor="rtl"
                className="text-sm font-medium text-foreground cursor-pointer"
              >
                Enable RTL (Right-to-Left) Layout
              </label>
            </div>
          </div>
        </section>

        {/* Search Settings */}
        <section className="rounded-lg border border-border bg-card p-6">
          <h2 className="text-lg font-semibold text-foreground mb-4">
            Search & Results
          </h2>

          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-foreground mb-2">
                Items Per Page
              </label>
              <select
                value={preferences.itemsPerPage}
                onChange={(e) =>
                  setPreferences({
                    ...preferences,
                    itemsPerPage: parseInt(e.target.value),
                  })
                }
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              >
                <option value={10}>10</option>
                <option value={20}>20</option>
                <option value={50}>50</option>
                <option value={100}>100</option>
              </select>
            </div>

            <div>
              <label className="block text-sm font-medium text-foreground mb-2">
                Default Sort Option
              </label>
              <select
                value={preferences.defaultSort}
                onChange={(e) =>
                  setPreferences({
                    ...preferences,
                    defaultSort: e.target.value as SortOption,
                  })
                }
                className="w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              >
                <option value={SortOption.RELEVANCE}>Relevance</option>
                <option value={SortOption.DATE_DESC}>Newest First</option>
                <option value={SortOption.DATE_ASC}>Oldest First</option>
                <option value={SortOption.TITLE_ASC}>Title (A-Z)</option>
              </select>
            </div>
          </div>
        </section>

        {/* Privacy & Data */}
        <section className="rounded-lg border border-border bg-card p-6">
          <h2 className="text-lg font-semibold text-foreground mb-4">
            Privacy & Data
          </h2>

          <div className="space-y-3">
            <div className="flex items-center justify-between p-3 rounded-md bg-muted/30">
              <span className="text-sm text-foreground">
                Auto-save searches and collections
              </span>
              <input type="checkbox" defaultChecked className="h-4 w-4" />
            </div>

            <div className="flex items-center justify-between p-3 rounded-md bg-muted/30">
              <span className="text-sm text-foreground">
                Allow analytics to improve experience
              </span>
              <input type="checkbox" defaultChecked className="h-4 w-4" />
            </div>

            <Button variant="outline" className="w-full mt-4">
              Download My Data
            </Button>

            <Button variant="outline" className="w-full">
              Delete Account
            </Button>
          </div>
        </section>

        {/* Action Buttons */}
        <div className="flex gap-2 pt-4">
          <Button onClick={handleSave} className="gap-2 flex-1">
            <Save className="h-4 w-4" />
            Save Preferences
          </Button>

          <Button onClick={handleReset} variant="outline" className="gap-2">
            <RotateCcw className="h-4 w-4" />
            Reset
          </Button>
        </div>

        {saved && (
          <div className="rounded-lg border border-green-200 bg-green-50 p-4 dark:border-green-900 dark:bg-green-950">
            <p className="text-sm text-green-700 dark:text-green-200">
              Preferences saved successfully!
            </p>
          </div>
        )}
      </div>

      {/* Account Info */}
      <section className="mt-8 rounded-lg border border-border bg-card p-6">
        <h2 className="text-lg font-semibold text-foreground mb-4">
          Account Information
        </h2>

        <div className="space-y-3">
          <div className="grid grid-cols-2">
            <p className="text-sm text-muted-foreground">Email:</p>
            <p className="text-sm font-medium text-foreground">
              user@example.com
            </p>
          </div>

          <div className="grid grid-cols-2">
            <p className="text-sm text-muted-foreground">Account Type:</p>
            <div>
              <Badge>Free Plan</Badge>
            </div>
          </div>

          <div className="grid grid-cols-2">
            <p className="text-sm text-muted-foreground">Member Since:</p>
            <p className="text-sm font-medium text-foreground">
              January 15, 2024
            </p>
          </div>
        </div>
      </section>
    </div>
  )
}
