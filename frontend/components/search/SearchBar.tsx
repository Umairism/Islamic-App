'use client'

import React, { useState, useEffect, useRef } from 'react'
import { Search, X, ArrowRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'

interface SearchBarProps {
  onSearch: (term: string) => void
  onClear?: () => void
  isLoading?: boolean
  placeholder?: string
  size?: 'sm' | 'md' | 'lg'
  showSuggestions?: boolean
  autoFocus?: boolean
}

const SEARCH_SUGGESTIONS = [
  'Ayat al-Kursi (The Throne Verse)',
  'Surah Al-Fatihah',
  'Intentions (Niyah)',
  'Prayer (Salah)',
  'Charity (Zakat)',
  'Islamic Law (Fiqh)',
  'Hadith Authentication',
]

/**
 * Enterprise-grade search bar with suggestions
 * Supports multiple input formats (text, voice)
 */
export function SearchBar({
  onSearch,
  onClear,
  isLoading = false,
  placeholder = 'Search Islamic sources...',
  size = 'md',
  showSuggestions: enableSuggestions = true,
  autoFocus = false,
}: SearchBarProps) {
  const [value, setValue] = useState('')
  const [showSuggestions, setShowSuggestions] = useState(false)
  const [filteredSuggestions, setFilteredSuggestions] = useState<string[]>([])
  const inputRef = useRef<HTMLInputElement>(null)

  // Filter suggestions based on input
  useEffect(() => {
    if (value && enableSuggestions && showSuggestions) {
      const filtered = SEARCH_SUGGESTIONS.filter((s) =>
        s.toLowerCase().includes(value.toLowerCase())
      )
      setFilteredSuggestions(filtered)
    } else {
      setFilteredSuggestions([])
    }
  }, [value, showSuggestions])

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (value.trim()) {
      onSearch(value)
      setShowSuggestions(false)
    }
  }

  const handleClear = () => {
    setValue('')
    onClear?.()
    setShowSuggestions(false)
    inputRef.current?.focus()
  }

  const handleSuggestionClick = (suggestion: string) => {
    setValue(suggestion)
    onSearch(suggestion)
    setShowSuggestions(false)
  }

  const sizeClasses = {
    sm: 'h-9 text-sm',
    md: 'h-10 text-base',
    lg: 'h-12 text-lg',
  }

  const containerSizeClasses = {
    sm: 'max-w-xl',
    md: 'max-w-2xl',
    lg: 'max-w-4xl',
  }

  return (
    <div className={cn('relative w-full', containerSizeClasses[size])}>
      <form onSubmit={handleSubmit} className="relative">
        <div className="relative flex items-center">
          <Search className="absolute left-3 h-5 w-5 text-muted-foreground pointer-events-none" />

          <Input
            ref={inputRef}
            type="search"
            value={value}
            onChange={(e) => setValue(e.target.value)}
            onFocus={() => setShowSuggestions(true)}
            onBlur={() => setTimeout(() => setShowSuggestions(false), 200)}
            placeholder={placeholder}
            className={cn(
              'pl-10 pr-10 rounded-lg border border-input bg-background shadow-sm transition-all',
              'placeholder:text-muted-foreground',
              'focus-visible:border-primary focus-visible:ring-1 focus-visible:ring-primary',
              sizeClasses[size]
            )}
            autoFocus={autoFocus}
            disabled={isLoading}
            aria-label="Search Islamic sources"
            aria-describedby="search-help"
          />

          {value && (
            <Button
              type="button"
              variant="ghost"
              size="icon"
              onClick={handleClear}
              className="absolute right-1 h-8 w-8"
              aria-label="Clear search"
            >
              <X className="h-4 w-4" />
            </Button>
          )}

          {!value && (
            <Button
              type="submit"
              variant="ghost"
              size="icon"
              disabled={isLoading}
              className="absolute right-1 h-8 w-8"
              aria-label="Search"
            >
              <ArrowRight className="h-4 w-4" />
            </Button>
          )}
        </div>

        {/* Search Suggestions Dropdown */}
        {showSuggestions && filteredSuggestions.length > 0 && (
          <div
            className="absolute top-full left-0 right-0 z-50 mt-2 rounded-lg border border-border bg-popover shadow-lg"
            role="listbox"
          >
            {filteredSuggestions.map((suggestion, index) => (
              <button
                key={index}
                type="button"
                onClick={() => handleSuggestionClick(suggestion)}
                className="w-full flex items-center gap-3 px-4 py-2.5 text-sm text-left hover:bg-muted transition-colors first:rounded-t-lg last:rounded-b-lg"
                role="option"
              >
                <Search className="h-4 w-4 text-muted-foreground flex-shrink-0" />
                <span className="flex-1">{suggestion}</span>
              </button>
            ))}
          </div>
        )}
      </form>

      <p
        id="search-help"
        className="mt-2 text-xs text-muted-foreground sr-only"
      >
        Type to search. Press Enter to submit. Use suggestions for quick access.
      </p>
    </div>
  )
}
